using System.Collections.Generic;
using System.Linq;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Grafik-Lauf G4: Umgebungs-Volumen je Zone aus den Kamera-Lesezeichen (Blickpunkt 22 m vor der Kamera) und den Biom-Effekten.</summary>
    public static partial class GraphicsBuilder
    {
        public const int MinAmbientVolumes = 4;

        public static void BuildAmbientVolumes()
        {
            int total = 0;
            foreach (var (zone, _, _) in ZoneBiomes)
            {
                var p = Profile(zone); if (p == null) continue;
                var marks = Bookmarks(zone).Where(b => b.Kind != BookmarkKind.Innenraum && b.Kind != BookmarkKind.SkillShowcase).OrderBy(b => b.Name).ToList();
                var kinds = p.AmbientFx.Count > 0 ? p.AmbientFx : new List<AmbientFxKind> { AmbientFxKind.Lichtstaub };
                p.AmbientVolumes = Volumes(marks.Select(b => (b.Position, b.Euler)).ToList(), kinds);
                EditorUtility.SetDirty(p); total += p.AmbientVolumes.Count;
            }
            RenderReport.Add(total + " Umgebungs-Volumen");
        }

        /// <summary>Rein: je Lesezeichen ein Volumen am Blickpunkt, Arten reihum; mindestens 4 (fehlende um den ersten Punkt verteilt).</summary>
        public static List<ZoneArtProfile.AmbientVolume> Volumes(List<(Vector3 pos, Vector3 euler)> views, IList<AmbientFxKind> kinds)
        {
            var list = new List<ZoneArtProfile.AmbientVolume>();
            for (int i = 0; i < views.Count; i++)
            {
                var fwd = Quaternion.Euler(views[i].euler) * Vector3.forward; fwd.y = 0; if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward; fwd.Normalize();
                list.Add(new ZoneArtProfile.AmbientVolume { Center = views[i].pos + fwd * 22f + Vector3.down * 3f, Radius = 24f, Kind = kinds[i % kinds.Count] });
            }
            var origin = views.Count > 0 ? views[0].pos : Vector3.zero;
            for (int i = list.Count; i < MinAmbientVolumes; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                list.Add(new ZoneArtProfile.AmbientVolume { Center = origin + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 40f, Radius = 24f, Kind = kinds[i % kinds.Count] });
            }
            return list;
        }
    }
}
