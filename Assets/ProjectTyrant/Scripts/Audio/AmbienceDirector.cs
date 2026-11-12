using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M24: Umgebungsklang am GameManager-Objekt: Zonen-Bett (Wind/Insekten/Vögel/Glut/Schnee), Wasser nach Nähe, Wind nach Höhe,
    /// Hub-Kulisse bzw. Fackel als 3D-Quelle, Zufallsgeräusche alle 10–30 s (Vogelruf, Donner, Wolfsheulen, Glocke, Hammer).
    /// </summary>
    public class AmbienceDirector : MonoBehaviour
    {
        /// <summary>Kanäle: Bett, Wasser, Wind, 3D-Spot (Hub/Fackel).</summary>
        public const int Channels = 4;
        AudioPool _pool;
        AudioPool.Channel _bed, _water, _wind, _spot;
        string _zone; float _nextOneShot = 8f, _scan;
        readonly List<Bounds> _waterBounds = new List<Bounds>();
        readonly List<Transform> _torches = new List<Transform>();
        public int OneShots { get; private set; }
        public string BedKey { get; private set; }

        void Start()
        {
            _pool = GetComponent<AudioPool>(); if (_pool == null) { enabled = false; return; }
            _bed = Loop("AmbBed", MixGroup.Ambience, false); _water = Loop("AmbWater", MixGroup.Ambience, false);
            _wind = Loop("AmbWind", MixGroup.Ambience, false); _spot = Loop("AmbSpot", MixGroup.Ambience, true);
        }

        AudioPool.Channel Loop(string n, MixGroup g, bool spatial) { var c = _pool.CreateChannel(n, g, spatial); c.Source.loop = true; return c; }

        void SetClip(AudioPool.Channel ch, string id)
        {
            var e = _pool.Entry(id); var clip = e != null && e.Clips.Length > 0 ? e.Clips[0] : null;
            if (ch.Source.clip == clip) { if (clip != null && !ch.Source.isPlaying && ch.Volume > 0.001f) ch.Source.Play(); return; }
            ch.Source.Stop(); ch.Source.clip = clip; ch.ClipRmsLin = e != null && e.Stats.Length > 0 ? Synth.DbToLin(e.Stats[0].RmsDb) : 0.15f;
            if (clip != null) { ch.Source.time = Random.value * clip.length * 0.9f; ch.Source.Play(); }
        }

        void Update()
        {
            if (_pool == null) return;
            var gm = GameManager.Instance; var cam = Camera.main; if (gm == null || cam == null) return;
            var zone = gm.CurrentZone; string zid = zone != null ? zone.Id : "sonnfurt";
            var lp = gm.Player != null ? gm.Player.transform.position : cam.transform.position;
            float dt = Time.unscaledDeltaTime;
            if (zid != _zone || gm.Transitioning) { _zone = zid; _scan = 0f; }
            _scan -= dt;
            if (_scan <= 0f) { _scan = 3f; Rescan(zone); }

            BedKey = "Amb/" + zid; SetClip(_bed, BedKey); _bed.Volume = Mathf.MoveTowards(_bed.Volume, gm.Transitioning ? 0f : 0.8f, dt);

            // Wasser nach Nähe
            float wd = WaterDistance(lp, zone);
            float wv = Mathf.Clamp01(1f - (wd - 4f) / 26f) * 0.9f;
            if (wv > 0.01f) SetClip(_water, "Amb/Water"); _water.Volume = Mathf.MoveTowards(_water.Volume, wv, dt * 0.5f);
            if (_water.Volume <= 0.001f && _water.Source.isPlaying) _water.Source.Stop();

            // Wind nach Höhe (über dem Zonen-Grund)
            float baseH = zone != null && zone.Biome != null ? zone.Biome.BaseHeight : 0f;
            float hv = Mathf.Clamp01((lp.y - baseH - 12f) / 30f) * 0.9f;
            if (hv > 0.01f) SetClip(_wind, "Amb/WindHigh"); _wind.Volume = Mathf.MoveTowards(_wind.Volume, hv, dt * 0.4f);
            if (_wind.Volume <= 0.001f && _wind.Source.isPlaying) _wind.Source.Stop();

            // Hub-Kulisse oder nächste Fackel als 3D-Quelle
            bool hub = (zone == null || zone.LegacyLayout) && Vector2.Distance(new Vector2(lp.x, lp.z), Procedural.ZoneLayout.Hub.Center) < Procedural.ZoneLayout.Hub.Radius + 25f;
            Transform torch = null; float td = 12f * 12f;
            foreach (var t in _torches) if (t != null) { float d = (t.position - lp).sqrMagnitude; if (d < td) { td = d; torch = t; } }
            if (hub) { SetClip(_spot, "Amb/HubMurmur"); var c = Procedural.ZoneLayout.Hub.Center; _spot.Source.transform.position = new Vector3(c.x, lp.y, c.y); _spot.Source.minDistance = 10f; _spot.Volume = 0.7f; }
            else if (torch != null) { SetClip(_spot, "Amb/Torch"); _spot.Source.transform.position = torch.position + Vector3.up * 3f; _spot.Source.minDistance = 2f; _spot.Volume = 0.8f; }
            else { _spot.Volume = 0f; if (_spot.Source.isPlaying) _spot.Source.Stop(); }

            // Zufallsgeräusche
            _nextOneShot -= dt;
            if (_nextOneShot <= 0f && !gm.Transitioning)
            {
                _nextOneShot = Random.Range(10f, 30f);
                string id = PickOneShot(zid, hub);
                var dir = Random.insideUnitCircle.normalized * Random.Range(15f, 35f);
                if (id != null && GameAudio.Play(id, lp + new Vector3(dir.x, 6f, dir.y), -3f, 1f, VoicePriority.AmbientDetail)) OneShots++;
            }
        }

        /// <summary>Zufallsgeräusch je Zone (Wolfsheulen nur nachts).</summary>
        public static string PickOneShot(string zone, bool hub)
        {
            float r = Random.value;
            switch (zone)
            {
                case "nebelmoor": return r < 0.6f ? "Amb/BirdCall" : "Amb/Thunder";
                case "frostkamm": return IsNight() || r < 0.3f ? "Amb/WolfHowl" : "Amb/Thunder";
                case "gluetoede": return "Amb/Thunder";
                default: return hub ? (r < 0.4f ? "Amb/Bell" : r < 0.8f ? "Amb/Hammer" : "Amb/BirdCall") : "Amb/BirdCall";
            }
        }

        static bool IsNight()
        {
            var sun = RenderSettings.sun; return sun != null && Vector3.Dot(sun.transform.forward, Vector3.down) < 0.05f;
        }

        void Rescan(ZoneDefinition zone)
        {
            _waterBounds.Clear(); _torches.Clear();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                if (r == null || r.sharedMaterial == null) continue;
                if (r.sharedMaterial.shader != null && (r.sharedMaterial.shader.name == "ProjectTyrant/Water" || r.sharedMaterial.shader.name == Visuals.PTShaders.Water2)) _waterBounds.Add(r.bounds);
                if (r.name == "Lamp") _torches.Add(r.transform);
            }
        }

        float WaterDistance(Vector3 p, ZoneDefinition zone)
        {
            float best = 999f;
            foreach (var b in _waterBounds) { var c = b.ClosestPoint(p); best = Mathf.Min(best, Vector2.Distance(new Vector2(c.x, c.z), new Vector2(p.x, p.z))); }
            return best;
        }
    }
}
