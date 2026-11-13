using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G5: spielt die Lagen einer VFX-2.0-Phase ab – Kompositionen über den bestehenden VfxPool (SkillFx.Vfx), gepoolte Punktlichter
    /// mit Abklingkurve, Schockwellen im Verzerrungs-Pass, Kamera-Impuls (lichtempfindlich: aus). Verzögerte Lagen über eine Warteschlange.
    /// </summary>
    public class VfxLayerPlayer : MonoBehaviour
    {
        struct Pending { public float At; public VfxLayer Layer; public Vector3 Pos; public Quaternion Rot; public Transform Follow; public float Intensity; }
        class PooledLight { public Light L; public float Start, Duration, Peak; }

        static VfxLayerPlayer _host;
        readonly List<Pending> _queue = new List<Pending>();
        readonly List<PooledLight> _lights = new List<PooledLight>();
        public const int MaxLights = 12;
        public static int PlayedLayers { get; private set; }
        public static System.Action<float, float> CameraKick;   // Amplitude, Dauer (Kamera-Rig abonniert)

        static VfxLayerPlayer Host
        {
            get
            {
                if (_host != null) return _host;
                var go = new GameObject("PT_VfxLayerPlayer"); go.hideFlags = HideFlags.DontSave;
                if (Application.isPlaying) DontDestroyOnLoad(go);
                _host = go.AddComponent<VfxLayerPlayer>();
                return _host;
            }
        }

        /// <summary>Spielt eine Phase; liefert die Anzahl der ausgelösten Lagen (inkl. verzögerter).</summary>
        public static int Play(VfxDefinition d, VfxPhase phase, Vector3 pos, Quaternion rot, Transform follow, float intensity, bool includeTrail = false)
        {
            if (d == null) return 0;
            var p = d.Get(phase); if (p == null) return 0;
            int n = 0;
            foreach (var l in p.Layers)
            {
                if (l.Kind == VfxLayerKind.Trail && !includeTrail) continue;
                if (l.Delay > 0f && Application.isPlaying) Host._queue.Add(new Pending { At = Time.time + l.Delay, Layer = l, Pos = pos, Rot = rot, Follow = follow, Intensity = intensity });
                else Fire(l, pos, rot, follow, intensity);
                n++;
            }
            return n;
        }

        static void Fire(VfxLayer l, Vector3 pos, Quaternion rot, Transform follow, float intensity)
        {
            PlayedLayers++;
            switch (l.Kind)
            {
                case VfxLayerKind.Composite: SkillFx.Vfx(l.Id, follow != null ? follow.position : pos, rot, follow, intensity); break;
                case VfxLayerKind.Light: if (Application.isPlaying) Host.Pulse(follow != null ? follow.position : pos, l.Color, Photosensitive.ClampFlash(l.Strength * intensity) * (Photosensitive.Enabled ? 4f : 1f), l.Radius, l.Duration); break;
                case VfxLayerKind.Shockwave: if (GraphicsSettingsRuntime.Feature("Verzerrung")) SceneLighting.Shockwaves.Spawn(pos, l.Radius, l.Duration, l.Strength * intensity); break;
                case VfxLayerKind.CameraKick: if (!Photosensitive.Enabled) CameraKick?.Invoke(Photosensitive.Shake(l.Strength), l.Duration); break;
                case VfxLayerKind.Sim: case VfxLayerKind.FxMesh: case VfxLayerKind.Refract: case VfxLayerKind.WorldMark: case VfxLayerKind.Gpu:
                    ElementFxHost.Play(l, follow != null ? follow.position : pos, rot, follow, intensity); break;
            }
        }

        void Pulse(Vector3 pos, Color c, float intensity, float range, float duration)
        {
            PooledLight pl = null;
            foreach (var x in _lights) if (Time.time - x.Start > x.Duration) { pl = x; break; }
            if (pl == null)
            {
                if (_lights.Count >= MaxLights) { pl = _lights[0]; foreach (var x in _lights) if (x.Start < pl.Start) pl = x; }
                else
                {
                    var go = new GameObject("VfxLicht"); go.transform.SetParent(transform, false);
                    var L = go.AddComponent<Light>(); L.type = LightType.Point; L.shadows = LightShadows.None;
                    pl = new PooledLight { L = L }; _lights.Add(pl);
                }
            }
            pl.L.transform.position = pos + Vector3.up * 0.6f; pl.L.color = c; pl.L.range = range; pl.Peak = intensity; pl.Start = Time.time; pl.Duration = Mathf.Max(0.05f, duration);
            pl.L.intensity = intensity; pl.L.enabled = true;
        }

        void Update()
        {
            for (int i = _queue.Count - 1; i >= 0; i--)
                if (Time.time >= _queue[i].At) { var q = _queue[i]; _queue.RemoveAt(i); Fire(q.Layer, q.Follow != null ? q.Follow.position : q.Pos, q.Rot, q.Follow, q.Intensity); }
            foreach (var pl in _lights)
            {
                if (!pl.L.enabled) continue;
                float t = (Time.time - pl.Start) / pl.Duration;
                if (t >= 1f) { pl.L.enabled = false; continue; }
                pl.L.intensity = pl.Peak * LightCurve(t);
            }
        }

        /// <summary>Licht-Hüllkurve (rein): schneller Anstieg (10 %), dann quadratisches Abklingen.</summary>
        public static float LightCurve(float t) => t < 0.1f ? t / 0.1f : (1f - (t - 0.1f) / 0.9f) * (1f - (t - 0.1f) / 0.9f);
    }
}
