using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G2: setzt aus Zonenprofil, Tageszeit und Wetter Sonne/Mond, Trilight-Umgebung, Höhennebel, Himmel, Kontur, Volume und
    /// Zonen-Cubemap (Echtzeit-Reflexionssonde nur mit Himmel, keine gebackenen Sonden). Funktioniert im Spiel (GraphicsDirector) und im
    /// Editor (Captures). Rein visuell.
    /// </summary>
    public static class SceneLighting
    {
        public const float MaxFog = 0.92f, OutlineStrength = 0.55f;
        /// <summary>Faktor von Spielmechaniken (Sandsturm, Dunkelheit der Kristalltiefe) – Sichtweite bleibt Gameplay.</summary>
        public static float ExternalFogScale = 1f;
        public static Color ExternalFogTint = Color.clear;
        /// <summary>Blitz-Aufhellung 0..1 (G4, bereits lichtempfindlich gedeckelt).</summary>
        public static float Lightning;
        static float LightScaleNow = 1f;
        public static bool LeafCookieEnabled = false;

        static readonly int SunDir = Shader.PropertyToID("_PT_SunDir"), SunColor = Shader.PropertyToID("_PT_SunColor"),
            AmbSky = Shader.PropertyToID("_PT_AmbSky"), AmbEq = Shader.PropertyToID("_PT_AmbEquator"), AmbGround = Shader.PropertyToID("_PT_AmbGround"),
            FogColor = Shader.PropertyToID("_PT_FogColor"), FogSun = Shader.PropertyToID("_PT_FogSunColor"), FogParams = Shader.PropertyToID("_PT_FogParams"),
            Night = Shader.PropertyToID("_PT_Night"), SkyTint = Shader.PropertyToID("_PT_SkyTint"), SkyWeather = Shader.PropertyToID("_PT_SkyWeather"),
            Weather = Shader.PropertyToID("_PT_Weather"), Outline = Shader.PropertyToID("_PT_OutlineColor"), OutlineParams = Shader.PropertyToID("_PT_OutlineParams"),
            Noise = Shader.PropertyToID("_PT_Noise"), DistortScale = Shader.PropertyToID("_PT_DistortScale"), WaterShallow = Shader.PropertyToID("_PT_WaterShallow"),
            WaterDeep = Shader.PropertyToID("_PT_WaterDeep"), Heat = Shader.PropertyToID("_PT_Heat"), HasOpaque = Shader.PropertyToID("_PT_HasOpaque"), UIPulse = Shader.PropertyToID("_PT_UIPulse"), ExposureMul = Shader.PropertyToID("_PT_ExposureMul");

        /// <summary>Ergebnis der Berechnung (rein, testbar über <see cref="Compute"/>).</summary>
        public struct Frame
        {
            public Vector3 LightDir; public Color LightColor; public float LightIntensity; public bool Moon;
            public Color AmbSky, AmbEquator, AmbGround; public float UnderLight;
            public Color Fog, FogSun; public float FogDensity, FogBase, FogFalloff, Scatter;
            public Color SkyTint; public float SkyBrightness, StarAlpha, NightAmount, Emissive, Exposure, CloudExtra;
        }

        /// <summary>Reine Berechnung aus Profil, Uhrzeit und Wetter.</summary>
        public static Frame Compute(ZoneArtProfile p, float hour, WeatherLook w)
        {
            var key = TimeOfDayModel.Sample(hour);
            var f = new Frame();
            float elev = TimeOfDayModel.SunElevation(hour, p.SunNoonElevation);
            var sun = TimeOfDayModel.SunDirection(hour, p.SunAzimuth, p.SunNoonElevation);
            f.Moon = elev < -4f;
            if (p.Underground) { sun = TimeOfDayModel.SunDirection(12f, p.SunAzimuth, p.SunNoonElevation); f.Moon = false; key = TimeOfDayModel.Sample(12f); }
            f.LightDir = f.Moon ? new Vector3(-sun.x, Mathf.Max(0.35f, -sun.y), -sun.z).normalized : (sun.y < 0.05f ? new Vector3(sun.x, 0.05f, sun.z).normalized : sun);
            float light = Mathf.Max(0f, w.LightScale);
            f.LightColor = f.Moon ? new Color(0.62f, 0.7f, 1f) : p.LightColor * key.LightTint;
            f.LightIntensity = (f.Moon ? Mathf.Max(0.3f, key.LightIntensity) * 0.9f : p.SunIntensity * key.LightIntensity) * Mathf.Lerp(0.55f, 1f, light);
            float amb = key.AmbientScale * Mathf.Lerp(0.8f, 1f, light);
            f.AmbSky = p.AmbientSky * amb; f.AmbEquator = p.AmbientEquator * amb; f.AmbGround = p.AmbientGround * amb; f.UnderLight = p.UnderLight;
            f.Fog = p.FogColor * Mathf.Lerp(0.35f, 1f, key.SkyBrightness) * key.SkyTint;
            if (ExternalFogTint.a > 0f) f.Fog = Color.Lerp(f.Fog, ExternalFogTint, ExternalFogTint.a);
            f.FogSun = Color.Lerp(f.Fog, f.LightColor * 1.2f, 0.6f);
            f.FogDensity = p.FogDensity * key.FogScale * Mathf.Max(0.2f, w.FogScale) * Mathf.Max(0.05f, ExternalFogScale);
            f.FogBase = p.FogHeight; f.FogFalloff = p.FogHeightFalloff; f.Scatter = f.Moon ? 0.1f : p.SunScatter;
            f.SkyTint = key.SkyTint; f.SkyBrightness = key.SkyBrightness; f.StarAlpha = p.Stars ? key.StarAlpha : 0f;
            f.NightAmount = Mathf.Clamp01((-elev + 6f) / 12f);
            f.Emissive = EmissiveSchedule.Level(hour);
            f.Exposure = key.Exposure;
            f.CloudExtra = Mathf.Max(0f, w.CloudCover - p.CloudCover);
            return f;
        }

        /// <summary>C#-Spiegel von PT_FogAmount (PT_Common.hlsl) ohne Sonnenstreuung: Deckung 0..MaxFog für eine Sichtlinie.</summary>
        public static float FogAmount(float distance, float cameraY, float deltaY, float density, float baseY, float falloff)
        {
            float fall = Mathf.Max(falloff, 1e-4f);
            float k = fall * deltaY;
            float height = Mathf.Exp(-fall * (cameraY - baseY)) * (Mathf.Abs(k) > 1e-3f ? (1f - Mathf.Exp(-k)) / k : 1f);
            float f = 1f - Mathf.Exp(-density * Mathf.Min(height, 8f) * Mathf.Max(distance, 1e-3f));
            return Mathf.Clamp01(f) * MaxFog;
        }

        /// <summary>Wendet Profil, Uhrzeit und Wetter auf die geladene Szene an (Spiel und Editor).</summary>
        public static void Apply(ZoneArtProfile p, float hour, WeatherLook w, GraphicsLibrary lib)
        {
            if (p == null) return;
            var f = Compute(p, hour, w);
            LightScaleNow = w.LightScale;
            var sun = FindSun();
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.LookRotation(-f.LightDir);
                sun.color = f.LightColor; sun.intensity = f.LightIntensity + Lightning * 1.2f;
                LeafCookie(sun, p, lib);
                sun.shadows = LightShadows.Soft;
                RenderSettings.sun = sun;
            }
            Shader.SetGlobalVector(SunDir, new Vector4(f.LightDir.x, f.LightDir.y, f.LightDir.z, f.Moon ? 0f : Mathf.Clamp01(f.LightDir.y)));
            var lc = f.LightColor.linear; Shader.SetGlobalVector(SunColor, new Vector4(lc.r, lc.g, lc.b, f.LightIntensity));
            Shader.SetGlobalColor(AmbSky, f.AmbSky); Shader.SetGlobalColor(AmbEq, f.AmbEquator);
            var g = f.AmbGround; g.a = f.UnderLight; Shader.SetGlobalColor(AmbGround, g);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = f.AmbSky; RenderSettings.ambientEquatorColor = f.AmbEquator; RenderSettings.ambientGroundColor = f.AmbGround;

            bool fogOn = GraphicsSettingsRuntime.Feature("Nebel");
            RenderState.FogOn = fogOn;
            var fc = f.Fog * Mathf.Lerp(1f, skyCompFog(p), 0.35f); fc.a = fogOn ? 1f : 0f; Shader.SetGlobalColor(FogColor, fc);
            var fs = f.FogSun * Mathf.Lerp(1f, skyCompFog(p), 0.35f); fs.a = MaxFog; Shader.SetGlobalColor(FogSun, fs);
            Shader.SetGlobalVector(FogParams, new Vector4(f.FogDensity, f.FogBase, f.FogFalloff, f.Scatter));
            RenderSettings.fog = false;   // Unity-Nebel aus: der Nebel-Pass übernimmt (Shader ohne multi_compile_fog)

            Shader.SetGlobalVector(Night, new Vector4(f.Emissive, f.NightAmount, f.StarAlpha, 0f));
            // Himmel nicht mit der Zonen-Belichtungskorrektur aufhellen (dunkle Zonen: sonst gleißender Himmel)
            float skyComp = Mathf.Lerp(1f, Mathf.Pow(2f, -Mathf.Max(0f, p.ExposureCalibration)), 0.5f);
            var st = f.SkyTint.linear; Shader.SetGlobalVector(SkyTint, new Vector4(st.r, st.g, st.b, f.SkyBrightness * skyComp));
            Shader.SetGlobalVector(SkyWeather, new Vector4(f.CloudExtra, Lightning * 0.6f, 0f, 0f));
            Shader.SetGlobalVector(Weather, new Vector4(w.Wetness, w.Snow, w.Dust, 0f));
            var oc = p.OutlineColor; oc.a = GraphicsSettingsRuntime.Feature("Outline") ? OutlineStrength : 0f;
            RenderState.OutlineOn = oc.a > 0f;
            Shader.SetGlobalColor(Outline, oc);
            Shader.SetGlobalVector(OutlineParams, new Vector4(1.25f, 55f, 140f, 0f));
            Shader.SetGlobalColor(WaterShallow, p.WaterShallow); Shader.SetGlobalColor(WaterDeep, p.WaterDeep);
            Shader.SetGlobalFloat(DistortScale, Photosensitive.Enabled ? 0.5f : 1f);
            bool hot = p.Biome == "glut" || p.Biome == "wueste";
            Shader.SetGlobalVector(Heat, new Vector4(hot ? (p.Biome == "glut" ? 3.5f : 2.2f) : 0f, 2.5f, 0.35f, 0f));
            RenderState.DistortionOn = GraphicsSettingsRuntime.Feature("Verzerrung") && (hot || Shockwaves.Count > 0);
            if (lib != null && lib.Noise != null) Shader.SetGlobalTexture(Noise, lib.Noise);
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Shader.SetGlobalFloat(HasOpaque, urp != null && urp.supportsCameraOpaqueTexture ? 1f : 0f);
            Shader.SetGlobalFloat(UIPulse, Photosensitive.Enabled ? 0.3f : 1f);

            if (p.SkyMaterial != null && RenderSettings.skybox != p.SkyMaterial) RenderSettings.skybox = p.SkyMaterial;
            ApplyVolumes(p, f);
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.TryGetComponent(out UniversalAdditionalCameraData cd) && !cd.stopNaN) cd.stopNaN = true;   // ein NaN-Pixel darf über Bloom nicht das Bild schwärzen
            Godrays(f);
        }

        /// <summary>Blätter-Cookie der Sonne in Wald-Biomen (Licht fällt gesprenkelt), sonst ohne.</summary>
        static void LeafCookie(Light sun, ZoneArtProfile p, GraphicsLibrary lib)
        {
            // nur in Wald-Zonen und bei direkter Sonne (bei Bedeckung gibt es keine scharfen Blattschatten)
            bool forestZone = p.ZoneId == "daemmerwald" || p.ZoneId == "fluesterhain" || p.ZoneId == "gruener_schlund";
            // G7: global über die Sonne projiziert wirkt das Blattmuster auf offenem Gelände wie Flecken – bis zu Licht-Inseln unter Baumkronen aus
            bool forest = LeafCookieEnabled && forestZone && LightScaleNow > 0.85f && GraphicsSettingsRuntime.Feature("Umgebung") && lib != null && lib.LeafCookie != null;
            var tex = forest ? lib.LeafCookie : null;
            if (sun.cookie == tex) return;
            sun.cookie = tex;
            var data = sun.GetComponent<UniversalAdditionalLightData>();
            if (data != null && tex != null) data.lightCookieSize = new Vector2(60f, 60f);
        }

        static float skyCompFog(ZoneArtProfile p) => Mathf.Pow(2f, -Mathf.Max(0f, p.ExposureCalibration));

        static Light _sun;
        public static Light FindSun()
        {
            if (_sun != null && _sun.isActiveAndEnabled) return _sun;
            _sun = RenderSettings.sun;
            if (_sun == null || !_sun.isActiveAndEnabled)
                foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional && l.isActiveAndEnabled) { _sun = l; break; }
            return _sun;
        }

        // ---------------- Volume ----------------
        static Volume _zoneVol, _timeVol;
        static VolumeProfile _timeProfile;
        static ColorAdjustments _timeAdjust;

        static void ApplyVolumes(ZoneArtProfile p, Frame f)
        {
            if (_zoneVol == null) _zoneVol = MakeVolume("PT_GfxVolume_Zone", 50);
            if (_timeVol == null) _timeVol = MakeVolume("PT_GfxVolume_Zeit", 60);
            if (p.Volume != null) _zoneVol.sharedProfile = p.Volume;
            if (_timeProfile == null)
            {
                _timeProfile = ScriptableObject.CreateInstance<VolumeProfile>(); _timeProfile.hideFlags = HideFlags.DontSave;
                _timeAdjust = _timeProfile.Add<ColorAdjustments>(true);
            }
            float zoneExposure = p.Volume != null && p.Volume.TryGet<ColorAdjustments>(out var za) && za.postExposure.overrideState ? za.postExposure.value : 0f;
            _timeAdjust.postExposure.Override(zoneExposure + f.Exposure);   // höhere Volume-Priorität ersetzt, daher Zone + Tageszeit
            Shader.SetGlobalFloat(ExposureMul, Mathf.Pow(2f, zoneExposure + f.Exposure));   // Telegrafen schätzen damit die Bildschirmhelligkeit des Bodens
            _timeAdjust.colorFilter.Override(Color.Lerp(Color.white, new Color(0.82f, 0.88f, 1f), f.NightAmount * 0.6f));
            _timeVol.sharedProfile = _timeProfile;
        }

        static Volume MakeVolume(string name, float priority)
        {
            var go = GameObject.Find(name);
            if (go == null) { go = new GameObject(name); go.hideFlags = HideFlags.DontSave; if (Application.isPlaying) Object.DontDestroyOnLoad(go); }
            var v = go.GetComponent<Volume>(); if (v == null) v = go.AddComponent<Volume>();
            v.isGlobal = true; v.priority = priority; v.weight = 1f;
            return v;
        }

        // ---------------- Lichtschaft ----------------
        static readonly int SunScreen = Shader.PropertyToID("_PT_SunScreen");
        static void Godrays(Frame f)
        {
            var cam = Camera.main;
            bool on = GraphicsSettingsRuntime.Feature("Lichtschaft") && cam != null && !f.Moon && f.LightDir.y > 0.02f;
            float vis = 0f; Vector3 vp = Vector3.zero;
            if (on)
            {
                vp = cam.WorldToViewportPoint(cam.transform.position + f.LightDir * 1000f);
                float edge = Mathf.Max(Mathf.Abs(vp.x - 0.5f), Mathf.Abs(vp.y - 0.5f));
                vis = vp.z > 0f ? Mathf.Clamp01((0.85f - edge) / 0.35f) : 0f;
            }
            RenderState.GodraysOn = vis > 0.01f;
            Shader.SetGlobalVector(SunScreen, new Vector4(vp.x, vp.y, vis, Photosensitive.ClampBloom(0.9f)));
        }

        // ---------------- Zonen-Cubemap ----------------
        static ReflectionProbe _probe;
        /// <summary>Echtzeit-Reflexionssonde nur mit Himmel (Culling-Maske leer): Umgebung für Wasser, Glas, Kristall. Neu bei Zonenwechsel und alle 2 Spielstunden.</summary>
        public static void RefreshCubemap()
        {
            if (_probe == null)
            {
                var go = GameObject.Find("PT_ZonenCubemap");
                if (go == null) { go = new GameObject("PT_ZonenCubemap"); go.hideFlags = HideFlags.DontSave; if (Application.isPlaying) Object.DontDestroyOnLoad(go); }
                _probe = go.GetComponent<ReflectionProbe>(); if (_probe == null) _probe = go.AddComponent<ReflectionProbe>();
                _probe.mode = ReflectionProbeMode.Realtime; _probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                _probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                _probe.cullingMask = 0; _probe.clearFlags = ReflectionProbeClearFlags.Skybox; _probe.resolution = 128;
                _probe.size = new Vector3(20000f, 4000f, 20000f); _probe.importance = 0; _probe.boxProjection = false;
            }
            _probe.RenderProbe();
        }

        // ---------------- Schockwellen (Verzerrung) ----------------
        public static class Shockwaves
        {
            struct Wave { public Vector3 Pos; public float Start, Duration, Radius, Strength; }
            static readonly System.Collections.Generic.List<Wave> _waves = new System.Collections.Generic.List<Wave>();
            static readonly Vector4[] _buf = new Vector4[8];
            static readonly int Shock = Shader.PropertyToID("_PT_Shock"), ShockCount = Shader.PropertyToID("_PT_ShockCount");
            public static int Count => _waves.Count;
            /// <summary>Ring-Verzerrung an einem Weltpunkt (Explosion, Landung). Radius in Metern, Dauer in Sekunden.</summary>
            public static void Spawn(Vector3 pos, float radius, float duration = 0.35f, float strength = 1f)
            {
                if (_waves.Count >= 8) _waves.RemoveAt(0);
                _waves.Add(new Wave { Pos = pos, Start = Time.time, Duration = duration, Radius = radius, Strength = strength });
            }
            public static void Update(Camera cam)
            {
                int n = 0;
                for (int i = _waves.Count - 1; i >= 0; i--) if (Time.time - _waves[i].Start > _waves[i].Duration) _waves.RemoveAt(i);
                if (cam != null)
                    foreach (var w in _waves)
                    {
                        float t = (Time.time - w.Start) / w.Duration;
                        var c = cam.WorldToViewportPoint(w.Pos); if (c.z <= 0f) continue;
                        var e = cam.WorldToViewportPoint(w.Pos + cam.transform.right * w.Radius * t);
                        float r = Mathf.Abs(e.x - c.x) * cam.aspect;
                        _buf[n++] = new Vector4(c.x, c.y, Mathf.Max(0.005f, r), w.Strength * (1f - t));
                    }
                for (int i = n; i < 8; i++) _buf[i] = Vector4.zero;
                Shader.SetGlobalVectorArray(Shock, _buf); Shader.SetGlobalFloat(ShockCount, n);
            }
        }
    }
}
