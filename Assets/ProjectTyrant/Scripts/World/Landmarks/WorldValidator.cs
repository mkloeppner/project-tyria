using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Ein Marker der Zone, wie ihn der Validator sieht.</summary>
    public struct MarkerCheck
    {
        public string Id; public MapMarkerKind Kind; public Vector3 Position; public Traversal Requires;
        /// <summary>Stufen-Belohnung desselben Wahrzeichens/Aufstiegs (Gipfel-Aussicht neben der Gipfel-Truhe) – zählt nicht für den Geheimnis-Abstand.</summary>
        public bool OwnStage;
    }

    public class WorldReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public float MainVisibility = -1f;
        public float HighestPoint, LowestPoint;
        public int Gates, ReturnGates;
        public readonly Dictionary<Traversal, int> GatesByKey = new Dictionary<Traversal, int>();
        public float BestShortcutSaving;
        public bool Ok => Errors.Count == 0;
        public override string ToString() => (Ok ? "OK" : Errors.Count + " Fehler") + (Warnings.Count > 0 ? ", " + Warnings.Count + " Hinweise" : "");
    }

    /// <summary>
    /// Welt-Lauf W1: Validator (rein, getestet) – Pflicht zu Fuß, Schlüssel nach Band, Sichtbarkeit ≥ 60 %, Rettungsfenster, Kontrollpunkte,
    /// Abkürzungs-Ersparnis ≥ 90 s, Geheimnisse außerhalb der Marker, Mindestmengen je Zone. Gefahrenstufen/Story werden nicht angefasst.
    /// </summary>
    public static class WorldValidator
    {
        public const float MinVisibility = 0.6f;
        public const int MinSecrets = 6, MinLore = 3, MinLoops = 5, MinScenes = 3;

        public static (float min, float max) HeightRange(LandmarkKind k, bool main)
        {
            if (!main) return (25f, 60f);
            switch (k)
            {
                case LandmarkKind.Pyramid: return (60f, 120f);
                case LandmarkKind.Cathedral: return (80f, 140f);
                case LandmarkKind.Massif: return (150f, 300f);
                case LandmarkKind.SkyIsland: return (60f, 400f);
                case LandmarkKind.GiantTree: return (60f, 200f);
                default: return (60f, 200f);
            }
        }

        public static WorldReport Validate(string zoneId, int levelMax, ZoneWorldPlan plan, IList<MarkerCheck> markers, float mainVisibility, bool requireCounts = true)
        {
            var r = new WorldReport { MainVisibility = mainVisibility };
            plan = plan ?? new ZoneWorldPlan();
            markers = markers ?? new List<MarkerCheck>();
            // 1. Pflichtinhalt zu Fuß
            int vistas = 0, vistasGated = 0, ch = 0, chGated = 0;
            foreach (var m in markers)
            {
                if (GateRules.IsMandatory(m.Kind) && m.Requires != Traversal.None) r.Errors.Add(zoneId + ": Pflicht-Marker " + m.Id + " (" + m.Kind + ") verlangt " + ReachabilityModel.Label(m.Requires));
                if (m.Kind == MapMarkerKind.Vista) { vistas++; if (m.Requires != Traversal.None) vistasGated++; }
                if (m.Kind == MapMarkerKind.HeroChallenge) { ch++; if (m.Requires != Traversal.None) chGated++; }
            }
            if (vistasGated * 2 > vistas) r.Errors.Add(zoneId + ": mehr als die Hälfte der Aussichtspunkte hinter Toren (" + vistasGated + "/" + vistas + ")");
            if (chGated * 2 > ch) r.Errors.Add(zoneId + ": mehr als die Hälfte der Heldenherausforderungen hinter Toren (" + chGated + "/" + ch + ")");
            // 2. Tore nach Band
            void Gate(string where, GateDef g)
            {
                if (g == null || g.Keys == Traversal.None) return;
                r.Gates++; if (g.ReturnGate) r.ReturnGates++;
                foreach (var k in GateRules.Singles(g.Keys)) { r.GatesByKey.TryGetValue(k, out int n); r.GatesByKey[k] = n + 1; }
                if (!GateRules.Valid(g, levelMax)) r.Errors.Add(zoneId + ": Tor " + where + " verlangt " + ReachabilityModel.Label(g.Keys) + " – nicht im Band (≤ " + levelMax + ") und nicht als Rückkehr-Tor markiert");
            }
            foreach (var l in plan.Landmarks) foreach (var s in l.Stages) Gate(l.Id + "/" + s.Index, s.Gate);
            foreach (var c in plan.Climbs) foreach (var s in c.Stages) Gate(c.Id + "/" + s.Index, s.Gate);
            foreach (var s in plan.Secrets) Gate(s.Id, s.Gate);
            foreach (var h in plan.Hazards) if (h.Keys != Traversal.None) { r.Gates++; foreach (var k in GateRules.Singles(h.Keys)) { r.GatesByKey.TryGetValue(k, out int n); r.GatesByKey[k] = n + 1; } }
            // 3. Haupt-Wahrzeichen: vorhanden, Maßstab, Sichtbarkeit, Innenraum
            var main = plan.Main;
            if (main == null) r.Errors.Add(zoneId + ": kein Haupt-Wahrzeichen");
            else
            {
                var range = HeightRange(main.Kind, true);
                if (main.Height < range.min - 0.01f || main.Height > range.max + 0.01f) r.Errors.Add(zoneId + ": " + main.Name + " " + Mathf.RoundToInt(main.Height) + " m außerhalb " + range.min + "–" + range.max + " m");
                if (!main.Interior && main.Kind != LandmarkKind.Massif) r.Errors.Add(zoneId + ": " + main.Name + " ohne begehbaren Innenraum");
                if (mainVisibility >= 0f && mainVisibility < MinVisibility) r.Errors.Add(zoneId + ": " + main.Name + " nur von " + Mathf.RoundToInt(mainVisibility * 100f) + " % sichtbar (< 60 %)");
            }
            for (int i = 0; i < plan.Landmarks.Count; i++)
                for (int j = i + 1; j < plan.Landmarks.Count; j++)
                {
                    var a = plan.Landmarks[i]; var b = plan.Landmarks[j];
                    if (a.Kind == b.Kind && ColorDistance(a.Primary, b.Primary) < 0.25f && Mathf.Abs(a.Height - b.Height) < 15f) r.Errors.Add(zoneId + ": " + a.Name + " und " + b.Name + " nicht unterscheidbar (Form/Farbe/Höhe)");
                }
            // 4. Hürden: Rettungsfenster
            foreach (var h in plan.Hazards)
                if (HazardRules.RescueWindow(h.Kind) < HazardRules.MinRescueWindow) r.Errors.Add(zoneId + ": Hürde " + h.Id + " ohne Rettungsfenster");
            // 5. Aufstiege: drei Stufen, Kontrollpunkte, Stufe 1 zu Fuß
            foreach (var c in plan.Climbs)
            {
                if (c.Stages.Count != 3) { r.Errors.Add(zoneId + ": Aufstieg " + c.Id + " hat " + c.Stages.Count + " statt 3 Stufen"); continue; }
                if (c.Stages[0].Gate.Keys != Traversal.None) r.Errors.Add(zoneId + ": Aufstieg " + c.Id + " Stufe 1 nicht zu Fuß");
                if ((c.Stages[1].Gate.Keys & Traversal.Springer) == 0 || !c.Stages[1].Gate.FootAlternative) r.Errors.Add(zoneId + ": Aufstieg " + c.Id + " Stufe 2 nicht Springer + Grat");
                if ((c.Stages[2].Gate.Keys & (Traversal.Griffon | Traversal.Skyscale)) == 0) r.Errors.Add(zoneId + ": Aufstieg " + c.Id + " Stufe 3 nicht Greif/Himmelsschuppe");
                foreach (var s in c.Stages) if (s.Checkpoints.Count == 0) r.Errors.Add(zoneId + ": Aufstieg " + c.Id + " Stufe " + s.Index + " ohne Kontrollpunkt");
                float desc = ClimbTemplate.DescentSeconds(c.Peak, c.DescentTarget);
                if (desc > 30f) r.Warnings.Add(zoneId + ": Abfahrt " + c.Id + " " + (float.IsInfinity(desc) ? "nicht im Gleitflug" : Mathf.RoundToInt(desc) + " s") + " (> 30 s)");
            }
            // 6. Geheimnisse außerhalb der Marker
            var markerPos = new List<Vector3>(); foreach (var m in markers) if (!m.OwnStage) markerPos.Add(m.Position);
            foreach (var s in plan.Secrets) if (!SecretRules.OutsideMarkers(s.Position, markerPos)) r.Errors.Add(zoneId + ": Geheimnis " + s.Id + " innerhalb eines Marker-Radius");
            // 7. Abkürzungen ≥ 90 s
            foreach (var s in plan.Shortcuts)
            {
                float saved = ShortcutModel.SavedSeconds(s);
                r.BestShortcutSaving = Mathf.Max(r.BestShortcutSaving, saved);
                if (saved < ShortcutModel.MinSaving) (plan.CompactZone ? r.Warnings : r.Errors).Add(zoneId + ": Abkürzung " + s.Id + " spart nur " + Mathf.RoundToInt(saved) + " s (< 90 s)");
            }
            // 8. Mindestmengen
            if (requireCounts)
            {
                if (plan.Climbs.Count < 1) r.Errors.Add(zoneId + ": kein Aufstieg");
                if (plan.Shortcuts.Count < 1) (plan.CompactZone ? r.Warnings : r.Errors).Add(zoneId + ": keine Reittier-Abkürzung" + (plan.CompactZone ? " (kompakte Zone)" : ""));
                if (plan.Secrets.Count < MinSecrets) r.Errors.Add(zoneId + ": " + plan.Secrets.Count + " Geheimnisse (< " + MinSecrets + ")");
                if (plan.Lore.Count < MinLore) r.Errors.Add(zoneId + ": " + plan.Lore.Count + " Lore-Tafeln (< " + MinLore + ")");
                if (plan.Loops.Count < MinLoops) r.Errors.Add(zoneId + ": " + plan.Loops.Count + " Entdeckungsschleifen (< " + MinLoops + ")");
                if (plan.Scenes.Count < MinScenes) r.Errors.Add(zoneId + ": " + plan.Scenes.Count + " Umwelt-Szenen (< " + MinScenes + ")");
                if (r.ReturnGates < 1) r.Warnings.Add(zoneId + ": kein Rückkehr-Tor");
            }
            foreach (var l in plan.Loops) if (string.IsNullOrEmpty(l.TriggerKind) || string.IsNullOrEmpty(l.Target)) r.Errors.Add(zoneId + ": Schleife " + l.Id + " ohne Auslöser/Ziel");
            return r;
        }

        static float ColorDistance(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }
}
