using ProjectTyrant.Procedural;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 11: regionale Systeme in den Zonenszenen (aus <see cref="BalanceBuilder.ApplyZone"/>, idempotent): Kopfgeldtafeln und Sandportale (Glutwüste),
    /// Rennstrecken (Große Leere, Zarathis), Angelstellen (Angel-Zonen), Aufzüge (Jadehafen), Hub-Dienste (Sonnentor, Jadehafen).
    /// </summary>
    public static class Lauf11Builder
    {
        public const string RootName = "Lauf11_Regionen";
        public static readonly System.Collections.Generic.List<string> LastReport = new System.Collections.Generic.List<string>();

        static Vector3 G(Vector2 p) => new Vector3(p.x, ZoneRoot.GroundHeight(p.x, p.y), p.y);

        public static void ApplyZone(ZoneDefinition z, ZoneRoot root)
        {
            if (z == null || root == null) return;
            var old = root.transform.Find(RootName); if (old != null) Object.DestroyImmediate(old.gameObject);
            var s = RegionZones.Get(z.Id);
            bool fishing = System.Array.IndexOf(FishCatalog.FishingZones, z.Id) >= 0;
            if (s == null && !fishing) return;
            var parent = Child(root.transform, RootName, Vector3.zero).transform;
            var l = BiomeZoneContent.For(z.Id);
            int boards = 0, portals = 0, spots = 0, races = 0, lifts = 0;
            var wood = Mat("BountyWood", new Color(0.55f, 0.38f, 0.22f)); var paper = Mat("BountyPaper", new Color(0.95f, 0.9f, 0.75f));
            var sand = Mat("SandPortal", new Color(1f, 0.8f, 0.4f), 0.1f, new Color(1f, 0.7f, 0.25f) * 1.5f);
            var ring = Mat("RaceRing", new Color(0.4f, 0.9f, 1f), 0.1f, new Color(0.3f, 0.8f, 1f) * 1.2f);
            var jade = Mat("JadeMetal", new Color(0.35f, 0.75f, 0.6f), 0.15f, new Color(0.2f, 0.8f, 0.5f) * 0.6f);

            if (s != null && s.Region == WorldRegion.Glutwueste)
            {
                // Kopfgeldtafel an der zweiten Wegmarke
                var bp = l.Waypoints[1].Pos + new Vector2(6f, 4f);
                var bgo = Child(parent, "Kopfgeldtafel", G(bp));
                Part(bgo.transform, PrimitiveType.Cube, new Vector3(0, 1.3f, 0), new Vector3(2.4f, 1.6f, 0.15f), wood, "Tafel");
                for (int i = 0; i < 3; i++) Part(bgo.transform, PrimitiveType.Cube, new Vector3(-0.7f + i * 0.7f, 1.4f, -0.09f), new Vector3(0.5f, 0.7f, 0.02f), paper, "Steckbrief" + i);
                AddPlaceholder(bgo, "Kopfgeldtafel", "stylized painterly fantasy desert bounty board with wanted posters, game-ready low poly, hand-painted texture");
                var board = bgo.AddComponent<BountyBoard>(); board.PromptText = "Kopfgeldtafel"; board.Range = 3f;
                for (int i = 0; i < 4; i++)
                {
                    var area = l.Pois[i % l.Pois.Length].Pos;
                    board.Targets.Add(new BountyBoard.Target { Id = z.Id + "_gesucht_" + i, Name = (i == 3 ? "Legendärer " : "") + s.Enemy + "-Anführer " + "ABCD"[i], Archetype = i == 3 ? "Champion_" + z.Id : s.Archetypes[i % s.Archetypes.Length], Champion = i == 3, Area = G(area) });
                }
                boards++;
                // zwei Sandportal-Paare: Ebene ↔ Plateau (Aussichtspunkt)
                for (int k = 0; k < 2; k++)
                {
                    var a = Child(parent, "Sandportal_" + k + "_A", G(l.Waypoints[(k + 2) % l.Waypoints.Length].Pos + new Vector2(-8f, 6f)));
                    var b = Child(parent, "Sandportal_" + k + "_B", G(l.Vistas[k % l.Vistas.Length].Pos + new Vector2(5f, 5f)));
                    foreach (var go in new[] { a, b }) { Part(go.transform, PrimitiveType.Cylinder, new Vector3(0, 0.05f, 0), new Vector3(4f, 0.04f, 4f), sand, "Kreis"); AddPlaceholder(go, "Sandportal", "– Platzhalter-Glyphe"); }
                    var pa = a.AddComponent<SandPortal>(); var pb = b.AddComponent<SandPortal>(); pa.Partner = pb; pb.Partner = pa; portals += 2;
                }
            }
            // Rennstrecken
            if (z.Id == "grosse_leere" || z.Id == "zarathis")
            {
                var st = Child(parent, "Rennleiter", G(l.Waypoints[0].Pos + new Vector2(14f, 8f)));
                CharacterRigs.Npc(st.transform, new Color(0.8f, 0.6f, 0.3f), "race_" + z.Id, true);
                ZoneContentBuilder.Nameplate(st.transform, z.Id == "grosse_leere" ? "Rennleiterin Yara" : "Rennleiter Faruk", "Reittier-Rennen", new Color(1f, 0.85f, 0.45f));
                var rs = st.AddComponent<RaceStarter>(); rs.RaceId = z.Id + "_rennen"; rs.Title = z.Id == "grosse_leere" ? "Lauf durch die Leere" : "Ring von Zarathis"; rs.Range = 3.2f; rs.TimeLimit = 200f; rs.GoldTime = 110f;
                var pts = new[] { l.Waypoints[1].Pos, l.Pois[1].Pos, l.Waypoints[2].Pos, l.Pois[3].Pos, l.Waypoints[3].Pos, l.Waypoints[0].Pos + new Vector2(20f, 0f) };
                for (int i = 0; i < pts.Length; i++)
                {
                    var cp = Child(parent, "Checkpoint_" + i, G(pts[i]) + Vector3.up * 3f);
                    Part(cp.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(7f, 0.15f, 7f), ring, "Ring", new Vector3(90f, 0f, 0f));
                    rs.Checkpoints.Add(cp.transform);
                    if (i % 2 == 1) { var pad = Child(parent, "BoostPad_" + i, G(Vector2.Lerp(pts[i - 1], pts[i], 0.5f))); Part(pad.transform, PrimitiveType.Cube, new Vector3(0, 0.05f, 0), new Vector3(4f, 0.1f, 4f), sand, "Pad"); rs.BoostPads.Add(pad.transform); }
                }
                AddPlaceholder(st, "Rennleiter (Tier 2)", "stylized painterly fantasy race marshal with flags, game-ready low poly, hand-painted texture, full body");
                races++;
            }
            // Angelstellen
            if (fishing)
            {
                var ws = l != null ? l.Pois : null;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 p = ws != null && ws.Length > 0 ? ws[(i * 2) % ws.Length].Pos + new Vector2(12f, -8f) : new Vector2(i * 40f, 0f);
                    var sp = Child(parent, "Angelstelle_" + i, G(p));
                    Part(sp.transform, PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0), new Vector3(5f, 0.02f, 5f), Mat("FishSwarm", new Color(0.6f, 0.85f, 1f), 0.1f, new Color(0.4f, 0.8f, 1f) * 0.6f), "Schwarm");
                    var fs = sp.AddComponent<FishingSpot>(); fs.Zone = z.Id; fs.Range = 4f; fs.PromptText = "Angeln";
                    AddPlaceholder(sp, "Angelstelle", "– Fischschwarm-Platzhalter");
                    spots++;
                }
            }
            // Aufzüge in Jadehafen
            if (z.Id == "jadehafen")
            {
                for (int i = 0; i < 3; i++)
                {
                    var p = l.Waypoints[i + 1].Pos + new Vector2(10f, 0f);
                    var el = Child(parent, "Aufzug_" + i, G(p));
                    var plat = Part(el.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(4f, 0.3f, 4f), jade, "Plattform", null, true).transform;
                    for (int k = 0; k < 4; k++) Part(el.transform, PrimitiveType.Cube, new Vector3((k % 2 == 0 ? -2.2f : 2.2f), 19f, (k < 2 ? -2.2f : 2.2f)), new Vector3(0.3f, 38f, 0.3f), jade, "Säule");
                    var je = el.AddComponent<JadeElevator>(); je.Platform = plat; je.NeedsJade = i == 2; je.Range = 4f;
                    AddPlaceholder(el, "Jade-Aufzug", "stylized painterly fantasy jade tech elevator with golden frame, game-ready low poly, hand-painted texture");
                    lifts++;
                }
            }
            // Hub-Dienste
            if (s != null && s.Hub)
            {
                var d = Child(parent, "Quartiermeister", G(l.Waypoints[0].Pos + new Vector2(-12f, 10f)));
                CharacterRigs.Npc(d.transform, new Color(0.5f, 0.4f, 0.6f), "qm_" + z.Id, true);
                ZoneContentBuilder.Nameplate(d.transform, z.Id == "sonnentor" ? "Quartiermeisterin Halima" : "Quartiermeister Wen", "Marken-Händler", new Color(1f, 0.85f, 0.45f));
                AddPlaceholder(d, "Quartiermeister (Tier 2)", "stylized painterly fantasy quartermaster npc with ledger, game-ready low poly, hand-painted texture, full body");
            }
            LastReport.Add(z.Id + ": " + boards + " Kopfgeldtafeln, " + portals + " Sandportale, " + races + " Rennen, " + spots + " Angelstellen, " + lifts + " Aufzüge");
        }
    }
}
