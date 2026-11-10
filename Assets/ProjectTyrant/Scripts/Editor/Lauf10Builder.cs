using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 10: Welt-Inhalte für Flug-Reittiere, aufgerufen aus <see cref="BalanceBuilder.ApplyZone"/> (geöffnete Zonenszene, idempotent).
    /// Himmelsspitzen: Sammlung „Himmelsschuppe“ – Brutpflegerin an der ersten Wegmarke, Ei im Horst auf dem höchsten Gipfel,
    /// fünf Glutsteine auf einem Ring um die Brutpflegerin, zwei Aufwinde am Fuß des Gipfels.
    /// </summary>
    public static class Lauf10Builder
    {
        public const string Zone = "himmelsspitzen", RootName = "Lauf10_Himmelsschuppe";
        public static readonly System.Collections.Generic.List<string> LastReport = new System.Collections.Generic.List<string>();

        public static void ApplyZone(ZoneDefinition z, ZoneRoot root)
        {
            if (z == null || root == null || z.Id != Zone) return;
            var old = root.transform.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var parent = Child(root.transform, RootName, Vector3.zero).transform;

            Vector3 basePos = z.Waypoints.Count > 0 ? z.Waypoints[0].Position : z.DefaultSpawn;
            var keeperPos = Ground(basePos + new Vector3(7f, 0f, 5f));
            var keeper = Child(parent, "Brutpflegerin", keeperPos);
            keeper.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
            CharacterRigs.Npc(keeper.transform, new Color(0.35f, 0.5f, 0.75f), "sky_keeper", true);
            ZoneContentBuilder.Nameplate(keeper.transform, "Brutpflegerin Ilva", "Himmelsschuppe", new Color(0.75f, 0.88f, 1f));
            var ko = keeper.AddComponent<SkyscaleNestObject>(); ko.Kind = SkyscaleNestObject.Role.Keeper; ko.Range = 3.2f; ko.PromptText = "Sprechen";
            AddPlaceholder(keeper, "Brutpflegerin (Tier 2)", "stylized painterly fantasy dragon keeper npc in blue robes, game-ready low poly, hand-painted texture, full body");

            // Höchster Gipfel (Raster 20 m, innerhalb 45 % der Kantenlänge)
            float half = z.Size * 0.45f; Vector3 peak = keeperPos; float best = float.NegativeInfinity;
            for (float x = -half; x <= half; x += 20f)
                for (float zz = -half; zz <= half; zz += 20f)
                {
                    float h = ZoneRoot.GroundHeight(x, zz);
                    if (h > best) { best = h; peak = new Vector3(x, h, zz); }
                }
            var shell = Mat("SkyscaleEgg", new Color(0.55f, 0.7f, 0.95f), 0.15f, new Color(0.4f, 0.6f, 1f) * 0.5f);
            var nest = Mat("SkyscaleNest", new Color(0.45f, 0.35f, 0.22f));
            var egg = Child(parent, "Himmelsschuppen_Ei", peak);
            Part(egg.transform, PrimitiveType.Cylinder, new Vector3(0, 0.1f, 0), new Vector3(1.8f, 0.12f, 1.8f), nest, "Horst");
            Part(egg.transform, PrimitiveType.Sphere, new Vector3(0, 0.55f, 0), new Vector3(0.6f, 0.8f, 0.6f), shell, "Ei");
            var eo = egg.AddComponent<SkyscaleNestObject>(); eo.Kind = SkyscaleNestObject.Role.Egg; eo.Range = 2.6f;
            AddPlaceholder(egg, "Himmelsschuppen-Ei", "stylized painterly fantasy blue dragon egg in a twig nest, game-ready low poly, hand-painted texture");

            var glow = Mat("Glutstein", new Color(0.9f, 0.45f, 0.2f), 0.1f, new Color(1f, 0.45f, 0.15f) * 1.4f);
            for (int i = 0; i < SkyscaleCollection.Stones; i++)
            {
                float a = i * Mathf.PI * 2f / SkyscaleCollection.Stones + 0.4f;
                var p = Ground(keeperPos + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 120f);
                var st = Child(parent, "Glutstein_" + i, p);
                Part(st.transform, PrimitiveType.Sphere, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.45f, 0.55f), glow, "Stein", new Vector3(0, i * 40f, 0));
                var so = st.AddComponent<SkyscaleNestObject>(); so.Kind = SkyscaleNestObject.Role.Stone; so.Index = i; so.Range = 2.4f;
                AddPlaceholder(st, "Glutstein", "stylized painterly fantasy glowing ember stone, game-ready low poly, hand-painted texture");
            }

            float upTop = 0f;
            var ring = Mat("UpdraftRing", new Color(0.8f, 0.95f, 1f), 0.1f, new Color(0.6f, 0.85f, 1f));
            for (int i = 0; i < 2; i++)
            {
                var dir = (keeperPos - peak); dir.y = 0f; dir = dir.sqrMagnitude > 1f ? dir.normalized : Vector3.forward;
                var side = Quaternion.Euler(0f, i == 0 ? 35f : -35f, 0f) * dir;
                var up = Child(parent, "Gipfel_Aufwind_" + i, Ground(peak + side * 60f));
                var u = up.AddComponent<Updraft>(); u.Height = Mathf.Max(40f, best - up.transform.position.y + 30f); u.Radius = 6f; u.Strength = 12f;
                upTop = Mathf.Max(upTop, up.transform.position.y + u.Height);
                Part(up.transform, PrimitiveType.Cylinder, new Vector3(0, 0.05f, 0), new Vector3(11f, 0.02f, 11f), ring, "Ring");
                AddPlaceholder(up, "Aufwind (Lauf 10)", "– Ring-Platzhalter");
            }
            // Zwei-Wege-Prüfung: Greif über den Aufwind (60 m Gleitstrecke), Himmelsschuppe mit Rasten an Hang/Wand
            bool griffon = Flight.FlightReach.GriffonReaches(keeperPos.y, upTop, best, 60f, Flight.FlightParams.Griffon(0));
            float tank = Flight.FlightReach.SkyscaleClimbPerTank(Flight.FlightParams.Skyscale(1));
            int rests = Mathf.Max(0, Mathf.CeilToInt((best - keeperPos.y) / Mathf.Max(1f, tank)) - 1);
            if (!griffon) Debug.LogWarning("[ProjectTyrant] Lauf 10: Himmelsschuppen-Ei für den Greif nicht erreichbar (Aufwind bis " + Mathf.RoundToInt(upTop) + " m, Gipfel " + Mathf.RoundToInt(best) + " m)");
            LastReport.Add(z.Id + ": Erreichbarkeit Ei – Greif " + (griffon ? "ja" : "NEIN") + " (Aufwind bis " + Mathf.RoundToInt(upTop) + " m), Himmelsschuppe mit " + rests + " Rast(en) à " + Mathf.RoundToInt(tank) + " m");
            LastReport.Add(z.Id + ": Himmelsschuppe – Ei auf " + Mathf.RoundToInt(best) + " m (" + Mathf.RoundToInt(peak.x) + "/" + Mathf.RoundToInt(peak.z) + "), Pflegerin " + keeperPos.ToString("F0") + ", " + SkyscaleCollection.Stones + " Glutsteine, 2 Aufwinde");
        }

        static Vector3 Ground(Vector3 p) => new Vector3(p.x, ZoneRoot.GroundHeight(p.x, p.z), p.z);
    }
}
