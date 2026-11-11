using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Welt-Lauf W1: Menü – Pläne neu berechnen (deterministisch) und Bericht ausgeben. Gebaut wird über Rebuild Zone / Rebuild Everything.</summary>
    public static class WorldW1Menu
    {
        [MenuItem("Project Tyrant/Welt W1/Alle Zonen neu planen")]
        public static void ReplanAll()
        {
            WorldW1Builder.LastReport.Clear(); WorldPlanner.LastReport.Clear();
            foreach (var id in WorldPlanner.Specs.Keys.ToList())
            {
                var z = WorldBuilder.Zone(id); if (z == null) continue;
                WorldW1Builder.PlanZone(z); AssetDatabase.SaveAssetIfDirty(z);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectTyrant] Welt W1 – Pläne:\n" + string.Join("\n", WorldW1Builder.LastReport.Concat(WorldPlanner.LastReport)));
        }

        [MenuItem("Project Tyrant/Welt W1/Bericht")]
        public static void Report() => Debug.Log("[ProjectTyrant] Welt W1 – Bericht:\n" + string.Join("\n", WorldW1Builder.LastReport.Concat(WorldPlanner.LastReport)));
    }
}
