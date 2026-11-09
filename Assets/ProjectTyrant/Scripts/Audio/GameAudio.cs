using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M24: schmale Fassade für Spielsysteme (kein Singleton – leitet an den <see cref="AudioPool"/> am GameManager weiter).</summary>
    public static class GameAudio
    {
        public static bool Play(string id, Vector3 pos, float volumeDb = 0f, float pitch = 1f, int priority = -1)
        {
            var p = AudioPool.Current; return p != null && p.Play(id, pos, volumeDb, pitch, priority);
        }

        public static void Ui(string id) { var p = AudioPool.Current; if (p != null) p.PlayUi(id); }

        static readonly RaycastHit[] _hits = new RaycastHit[6];

        /// <summary>Schritt: Untergrund per Strahl nach unten (Kollider-Name) und Zone, Wasser unter dem Wasserspiegel.</summary>
        public static void Step(Transform who, float volumeDb, float pitch, int priority = VoicePriority.Step)
        {
            var p = AudioPool.Current; if (p == null || who == null) return;
            var pos = who.position;
            var cam = Camera.main; if (cam != null && (cam.transform.position - pos).sqrMagnitude > 28f * 28f) return;
            string col = null; float best = float.MaxValue;
            int n = Physics.RaycastNonAlloc(pos + Vector3.up * 0.6f, Vector3.down, _hits, 1.6f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i]; if (h.collider == null || h.collider.transform.IsChildOf(who.root)) continue;
                if (h.distance < best) { best = h.distance; col = h.collider.name; }
            }
            var zone = GameManager.Instance != null ? GameManager.Instance.CurrentZone : null;
            bool legacy = zone == null || zone.LegacyLayout;
            float water = legacy ? Procedural.ZoneLayout.WaterLevel : zone.Biome != null ? zone.Biome.WaterLevel : -100f;
            if (legacy && Vector2.Distance(new Vector2(pos.x, pos.z), Procedural.ZoneLayout.Hub.Center) < Procedural.ZoneLayout.Hub.Radius) col = "plaza";   // Weiler: gepflasterter Platz
            string s = GameSoundMap.Surface(col, zone != null ? zone.Id : "sonnfurt", pos.y < water + 0.1f);
            p.Play("Step/" + s, pos, volumeDb, pitch, priority);
        }
    }
}
