using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Reine Welt-Regeln: Portal-Zuordnung, Herabstufung, Reisekosten über Zonen, Chunk-Zerlegung.</summary>
    public static class WorldModel
    {
        /// <summary>Löst ein Portal auf: Zielzone und Zielportal (oder Spawn), null wenn ungültig.</summary>
        public static bool ResolvePortal(IList<ZoneDefinition> zones, string zoneId, string portalId, out ZoneDefinition target, out PortalDef targetPortal)
        {
            target = null; targetPortal = null;
            var z = Find(zones, zoneId); if (z == null) return false;
            var p = z.Portal(portalId); if (p == null) return false;
            target = Find(zones, p.TargetZone); if (target == null) return false;
            targetPortal = target.Portal(p.TargetPortal);
            return true;
        }

        public static ZoneDefinition Find(IList<ZoneDefinition> zones, string id)
        {
            if (zones == null || string.IsNullOrEmpty(id)) return null;
            foreach (var z in zones) if (z != null && z.Id == id) return z;
            return null;
        }

        /// <summary>Prüft, dass jedes Portal ein Gegenstück hat, das zurückführt. Gibt Fehlertexte zurück.</summary>
        public static List<string> ValidatePortals(IList<ZoneDefinition> zones)
        {
            var errors = new List<string>();
            foreach (var z in zones)
            {
                if (z == null) continue;
                foreach (var p in z.Portals)
                {
                    if (!ResolvePortal(zones, z.Id, p.Id, out var t, out var tp)) { errors.Add(z.Id + "/" + p.Id + ": Ziel fehlt"); continue; }
                    if (tp == null) { errors.Add(z.Id + "/" + p.Id + ": Zielportal fehlt"); continue; }
                    if (tp.TargetZone != z.Id || tp.TargetPortal != p.Id) errors.Add(z.Id + "/" + p.Id + ": Rückweg zeigt woanders hin");
                }
            }
            return errors;
        }

        // ---------------- Herabstufung (Dynamic Level Adjustment) ----------------
        /// <summary>Effektives Level: in Gebieten unter dem eigenen Level → Gebiets-Maximum + 1.</summary>
        public static int EffectiveLevel(int realLevel, int zoneMaxLevel)
        {
            if (zoneMaxLevel <= 0) return realLevel;
            return Mathf.Clamp(Mathf.Min(realLevel, zoneMaxLevel + 1), 1, AttributeSet.MaxLevel);
        }

        public const int StrongDownlevelGap = 8;
        /// <summary>EP-Faktor: stark herabgestuft (≥ 8 Stufen) → 0,5.</summary>
        public static float XpFactor(int realLevel, int effectiveLevel) => realLevel - effectiveLevel >= StrongDownlevelGap ? 0.5f : 1f;

        /// <summary>Ausrüstungswerte skalieren proportional, wenn das effektive Level unter dem Item-Level liegt.</summary>
        public static float ItemScale(int itemLevel, int effectiveLevel)
        {
            if (effectiveLevel >= itemLevel) return 1f;
            return AttributeSet.PrimaryBase(effectiveLevel) / AttributeSet.PrimaryBase(Mathf.Max(1, itemLevel));
        }

        // ---------------- Reisekosten ----------------
        public const int ZoneSurcharge = 50;
        /// <summary>Reise innerhalb der Zone wie Lauf 3; in eine andere Zone mit Aufschlag (Kupfer).</summary>
        public static int TravelCost(Vector3 from, Vector3 to, int level, bool otherZone, float discount = 0f)
        {
            int c = otherZone ? TravelModel.BaseCost + ZoneSurcharge + Mathf.RoundToInt(level * 6f) : TravelModel.Cost(from, to, level);
            return Mathf.Max(1, Mathf.RoundToInt(c * (1f - Mathf.Clamp01(discount))));
        }

        // ---------------- Chunks ----------------
        /// <summary>Zerlegt eine zentrierte Zone (Kantenlänge size) in Chunks der Größe chunk (Randchunks ggf. kleiner).</summary>
        public static List<Rect> SplitChunks(float size, float chunk)
        {
            var list = new List<Rect>();
            if (size <= 0f || chunk <= 0f) return list;
            float half = size * 0.5f;
            int n = Mathf.CeilToInt(size / chunk - 1e-4f);
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    float x0 = -half + x * chunk, z0 = -half + z * chunk;
                    float w = Mathf.Min(chunk, half - x0), h = Mathf.Min(chunk, half - z0);
                    list.Add(new Rect(x0, z0, w, h));
                }
            return list;
        }
    }
}
