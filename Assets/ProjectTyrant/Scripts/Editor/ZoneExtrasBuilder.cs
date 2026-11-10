using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectTyrant.Anim;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 6 / M30 + M32: Zonen-Besonderheiten der neuen Zonen, eingesetzt vom Zonen-Build: Silbermund (Häuser, ≥ 40 Bewohner, Markt, Bündnis-Viertel,
    /// Bündnis-Händler, Stallmeister, Brennars Grabmal, Figurenplatz), Verderbnis-Flecken mit Laternen (Dämmerwald), Sandsturm mit Späher-Zelten (Dünenmeer),
    /// Leuchtkristalle und Dunkelheit (Kristalltiefe), schwebende Inseln (Himmelsspitzen), Mauern, Tore und Thron (Tyrannenfeste) sowie drei Sprungrätsel.
    /// </summary>
    public static class ZoneExtrasBuilder
    {
        static Func<float, float, float> _h;
        static Vector3 G(float x, float z, float y = 0f) => new Vector3(x, _h(x, z) + y, z);
        static System.Random _rnd;
        static float R(float a, float b) => a + (float)_rnd.NextDouble() * (b - a);

        public static void Populate(ZoneDefinition z, Transform zoneRoot, Func<float, float, float> height)
        {
            if (z == null) return;
            _h = height; _rnd = new System.Random(z.Seed + 606);
            var root = Child(zoneRoot, "PT_ZoneExtras", Vector3.zero).transform;
            switch (z.Id)
            {
                case "silberkueste": City(root); JumpPuzzle(root, "leuchtturm", "Leuchtturm-Treppe", new Vector3(285f, 0f, 205f), 14, 1.6f, 0.9f, true); break;
                case "daemmerwald": Corruption(root); break;
                case "duenenmeer": root.gameObject.AddComponent<SandstormDirector>(); Shelters(root); break;
                case "kristalltiefe": root.gameObject.AddComponent<DarknessDirector>(); Crystals(root); JumpPuzzle(root, "kristallsaeulen", "Kristallsäulen", new Vector3(-60f, 0f, 120f), 12, 2.2f, 1.1f, false); break;
                case "himmelsspitzen": Islands(root); JumpPuzzle(root, "wolkensteine", "Wolkensteine", new Vector3(-120f, 0f, -200f), 16, 2.4f, 1.3f, false); break;
                case "tyrannenfeste": Fortress(root); break;
                default: RegionExtras(z, root); break;   // Lauf 11
            }
        }

        /// <summary>Lauf 11: Regions-Zonen – Sprungrätsel je Zone, Signatur-Umwelt (Sandsturm, Verderbnis, Dunkelheit) nach Biom.</summary>
        static void RegionExtras(ZoneDefinition z, Transform root)
        {
            var s = RegionZones.Get(z.Id); if (s == null) return;
            var l = RegionZones.Layout(z.Id);
            var anchor = l.Pois[l.Pois.Length - 1].Pos + new Vector2(25f, 20f);
            JumpPuzzle(root, z.Id + "_raetsel", "Sprungrätsel: " + s.Places[6], new Vector3(anchor.x, 0f, anchor.y), 12 + (z.Seed % 5), 1.8f, 1.1f, z.Seed % 2 == 0);
            switch (s.Biome)
            {
                case "duenen": case "salz": case "zarathis": root.gameObject.AddComponent<SandstormDirector>(); Shelters(root); break;
                case "ruinen": Corruption(root); break;
                case "eis": root.gameObject.AddComponent<SandstormDirector>(); Shelters(root); break;   // Schneesturm-Zyklus (gleiche Mechanik)
            }
        }

        // ------------------------------------------------------------ Silbermund
        static void City(Transform root)
        {
            var city = Child(root, "Silbermund", Vector3.zero).transform;
            var paving = Mat("CityPaving", new Color(0.72f, 0.7f, 0.66f));
            var wall = Mat("CityWall", new Color(0.86f, 0.82f, 0.74f));
            var roof = Mat("CityRoof", new Color(0.62f, 0.28f, 0.2f));
            var roofBlue = Mat("CityRoofBlue", new Color(0.24f, 0.36f, 0.56f));
            var wood = Mat("CityWood", new Color(0.45f, 0.32f, 0.2f));
            var gold = Mat("CityGold", new Color(1f, 0.82f, 0.4f), 0.1f, new Color(1f, 0.7f, 0.25f) * 0.8f);
            // Plätze (gepflastert, folgen dem Gelände in Ringen)
            foreach (var (c, r) in new[] { (new Vector2(0, 40), 24f), (new Vector2(-40, 10), 16f), (new Vector2(-60, 80), 18f), (new Vector2(90, -40), 18f), (new Vector2(40, 120), 10f) })
                Part(city, PrimitiveType.Cylinder, G(c.x, c.y, 0.06f), new Vector3(r * 2f, 0.08f, r * 2f), paving, "Plaza", null, true);
            // Portalplatz: Brunnen + Säulen
            Part(city, PrimitiveType.Cylinder, G(0, 40, 0.5f), new Vector3(5f, 0.5f, 5f), wall, "Fountain", null, true);
            Part(city, PrimitiveType.Cylinder, G(0, 40, 2f), new Vector3(0.8f, 2f, 0.8f), wall, "FountainPillar", null, true);
            Part(city, PrimitiveType.Sphere, G(0, 40, 4.4f), Vector3.one * 1.2f, gold, "Orb").AddComponent<Bobber>();
            // Häuser in zwei Ringen um das Zentrum (frei von Plätzen und Hafen)
            var avoid = new[] { (new Vector2(0, 40), 30f), (new Vector2(-40, 10), 20f), (new Vector2(-60, 80), 24f), (new Vector2(90, -40), 26f), (new Vector2(40, 120), 14f) };
            int houses = 0;
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < 18; i++)
                {
                    float a = (i + ring * 0.5f) / 18f * Mathf.PI * 2f, rad = 62f + ring * 34f + R(-4f, 4f);
                    var p = new Vector2(Mathf.Sin(a) * rad, 40f + Mathf.Cos(a) * rad * 0.9f);
                    bool blocked = false; foreach (var (c, r) in avoid) if (Vector2.Distance(p, c) < r) blocked = true;
                    if (blocked) continue;
                    float w = R(6f, 9f), d = R(6f, 8f), hgt = R(4.5f, 7f);
                    float ground = Mathf.Min(_h(p.x - w * 0.5f, p.y - d * 0.5f), _h(p.x + w * 0.5f, p.y + d * 0.5f), _h(p.x, p.y)) - 1f;
                    float top = Mathf.Max(_h(p.x, p.y), _h(p.x + w * 0.5f, p.y), _h(p.x, p.y + d * 0.5f)) + hgt;
                    var house = Child(city, "House_" + houses, new Vector3(p.x, 0, p.y)).transform;
                    house.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                    Part(house, PrimitiveType.Cube, new Vector3(0, (ground + top) * 0.5f, 0), new Vector3(w, top - ground, d), wall, "Body", null, true);
                    var rf = Part(house, PrimitiveType.Cube, new Vector3(0, top + 1.2f, 0), new Vector3(w * 0.74f, w * 0.74f, d + 0.6f), (houses % 3 == 0) ? roofBlue : roof, "Roof", new Vector3(0, 0, 45f));
                    rf.transform.localScale = new Vector3(w * 0.72f, w * 0.72f, d + 0.6f);
                    Part(house, PrimitiveType.Cube, new Vector3(0, _h(p.x, p.y) + 1.1f, d * 0.5f + 0.05f), new Vector3(1.2f, 2.2f, 0.1f), wood, "Door");
                    AddPlaceholder(house.gameObject, "Silbermund_House (Tier 2)", "stylized painterly fantasy harbor town house, white walls, terracotta roof, game-ready low poly, hand-painted texture");
                    houses++;
                }
            // Hafen: Mole + Kräne + Schiffe
            for (int i = 0; i < 6; i++) Part(city, PrimitiveType.Cube, G(110f + i * 9f, -60f, 0.4f), new Vector3(8.5f, 1f, 5f), wood, "Pier", null, true);
            for (int i = 0; i < 3; i++)
            {
                var ship = Child(city, "Ship_" + i, new Vector3(130f + i * 22f, 0.8f, -82f)).transform;
                Part(ship, PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(4f, 1.6f, 12f), wood, "Hull", null, true);
                Part(ship, PrimitiveType.Cylinder, new Vector3(0, 6f, 0), new Vector3(0.3f, 5f, 0.3f), wood, "Mast");
                Part(ship, PrimitiveType.Cube, new Vector3(0, 6.5f, 0.2f), new Vector3(5.5f, 5f, 0.06f), Mat("Sail", new Color(0.95f, 0.93f, 0.86f)), "Sail");
                AddPlaceholder(ship.gameObject, "Silbermund_Ship (Tier 2)", "stylized painterly fantasy sailing ship, game-ready low poly");
            }
            // Bewohner: 44 Bürger mit Tagesroutine (Markt, Hafen, Plätze)
            var spots = new[] { new Vector2(0, 40), new Vector2(-40, 10), new Vector2(-60, 80), new Vector2(90, -40), new Vector2(40, 120), new Vector2(20, 70), new Vector2(-20, -10) };
            string[] acts = { "Talk", "Work", "Idle2", "Sit", "Cheer", "Interact" };
            var npcRoot = Child(city, "Citizens", Vector3.zero).transform;
            for (int i = 0; i < 44; i++)
            {
                var a = spots[i % spots.Length]; var b = spots[(i * 3 + 1) % spots.Length];
                var pa = a + new Vector2(R(-10f, 10f), R(-10f, 10f)); var pb = b + new Vector2(R(-10f, 10f), R(-10f, 10f));
                var npc = Child(npcRoot, "Citizen_" + i, G(pa.x, pa.y)).transform;
                var robe = Color.HSVToRGB((i * 0.137f) % 1f, R(0.3f, 0.6f), R(0.45f, 0.8f));
                CharacterRigs.Npc(npc, robe, "citizen_silbermund_" + i, i % 4 == 0);
                var rt = npc.gameObject.AddComponent<VillagerRoutine>();
                rt.Stops = new[]
                {
                    new VillagerRoutine.Stop { Position = G(pa.x, pa.y), Yaw = R(0f, 360f), Activity = acts[i % acts.Length], Duration = R(8f, 16f) },
                    new VillagerRoutine.Stop { Position = G(pb.x, pb.y), Yaw = R(0f, 360f), Activity = acts[(i + 2) % acts.Length], Duration = R(8f, 16f) },
                };
                rt.WalkSpeed = R(1.2f, 1.7f);
            }
            // Dienste: Bündnis-Händler (Viertel), Karma-Händler (Markt), Stallmeister (Hafen)
            var db = ItemBuilder.Database;
            string[] pacts = { "Wacht", "Archiv", "Schleier" }; Color[] pc = { new Color(0.6f, 0.2f, 0.15f), new Color(0.25f, 0.45f, 0.35f), new Color(0.25f, 0.2f, 0.35f) };
            for (int i = 0; i < 3; i++)
            {
                var v = Vendor("buendnis_" + pacts[i].ToLowerInvariant(), "Bündnis-Händler (" + pacts[i] + ")", VendorKind.Karma, db, new[] { ("ar_glut_chest", 2000), ("ar_glut_legs", 1800), ("tr_acc_glut", 2200), ("potion_big", 40) });
                var npc = ItemBuilder.Place(city, v, G(-70f + i * 10f, 92f), 180f, pc[i]);
                if (npc != null) { var gate = npc.gameObject.AddComponent<PactVendorGate>(); gate.Pact = (Pact)(i + 1); }
            }
            var market = Vendor("markt_silbermund", "Markthändler Oswin", VendorKind.General, db, new[] { ("food_soup", 50), ("food_pie", 40), ("potion_big", 45), ("kit_fine", 100) });
            ItemBuilder.Place(city, market, G(-46f, 18f), 90f, new Color(0.7f, 0.55f, 0.25f));
            MountBuilder.PlaceStallmeister(city, MountKind.Raptor, G(70f, -20f), 220f, "Stallmeisterin Hedda von Silbermund");
            // Brennars Grabmal (sichtbar nach Kapitel 4) + Figurenplatz
            var tomb = Child(city, "BrennarTomb", G(40f, 120f)).transform;
            Part(tomb, PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(3f, 1.2f, 1.6f), wall, "Base", null, true);
            Part(tomb, PrimitiveType.Cube, new Vector3(0, 2.1f, 0), new Vector3(1.2f, 2f, 0.4f), wall, "Stele", null, true);
            Part(tomb, PrimitiveType.Cube, new Vector3(0, 3.2f, 0), new Vector3(1.6f, 0.2f, 0.6f), gold, "Cap");
            AddPlaceholder(tomb.gameObject, "Brennar_Tomb (Tier 1)", "stylized painterly fantasy memorial tomb of a fallen captain, white stone stele with golden cap, game-ready low poly");
            var tg = tomb.gameObject.AddComponent<StoryFlagVisibility>(); tg.Flag = "brennar_gefallen";
            ZoneContentBuilder.Nameplate(tomb, "Brennar Feld", "Hat die Linie gehalten", new Color(0.9f, 0.9f, 1f));
            string[] figs = { "mira", "haldis", "tovin", "veyra", "fenn", "nadira", "sela", "gorran" }; int[] after = { 1, 5, 3, 5, 7, 8, 9, 6 };
            for (int i = 0; i < figs.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoryFigureBuilder.Dir + "/PT_Story_" + figs[i] + ".prefab");
                if (prefab == null) continue;
                float a = i / (float)figs.Length * Mathf.PI * 2f;
                var pos = G(20f + Mathf.Sin(a) * 9f, 70f + Mathf.Cos(a) * 9f);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, city);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, a * Mathf.Rad2Deg + 180f, 0f));
                var spot = go.AddComponent<FigurePlaza>(); spot.Speaker = figs[i]; spot.AfterChapter = after[i];
            }
        }

        static VendorDefinition Vendor(string id, string name, VendorKind kind, ItemDatabase db, (string item, int price)[] stock)
        {
            string path = ItemBuilder.VendorDir + "/Vendor_" + id + ".asset";
            var v = AssetDatabase.LoadAssetAtPath<VendorDefinition>(path);
            if (v == null) { v = ScriptableObject.CreateInstance<VendorDefinition>(); AssetDatabase.CreateAsset(v, path); }
            v.Id = id; v.DisplayName = name; v.Kind = kind; v.Stock = new List<VendorEntry>();
            foreach (var (item, price) in stock)
            {
                var it = db != null ? db.Get(item) : null; if (it == null) continue;
                v.Stock.Add(kind == VendorKind.Karma ? new VendorEntry { Item = it, PriceKarma = price, Count = 1 } : new VendorEntry { Item = it, PriceCopper = price, Count = 1 });
            }
            EditorUtility.SetDirty(v); return v;
        }

        // ------------------------------------------------------------ Dämmerwald
        static void Corruption(Transform root)
        {
            var violet = Mat("CorruptionGround", new Color(0.35f, 0.15f, 0.5f), 0.1f, new Color(0.5f, 0.2f, 0.9f) * 0.9f);
            var stone = Mat("WardStone", new Color(0.55f, 0.55f, 0.5f));
            var flame = Mat("WardFlame", new Color(1f, 0.9f, 0.6f), 0.1f, new Color(1f, 0.85f, 0.5f) * 2.5f);
            var spots = new[] { (new Vector2(-260, 120), 16f), (new Vector2(220, 240), 14f), (new Vector2(-40, 60), 12f), (new Vector2(300, -120), 12f), (new Vector2(-300, -80), 12f), (new Vector2(100, 220), 12f) };
            int i = 0;
            foreach (var (p, r) in spots)
            {
                var go = Child(root, "Corruption_" + i++, G(p.x, p.y)).transform;
                var vis = Part(go, PrimitiveType.Cylinder, new Vector3(0, 0.08f, 0), new Vector3(r * 2f, 0.04f, r * 2f), violet, "Ground");
                var f = go.gameObject.AddComponent<CorruptionField>(); f.Visual = vis.transform; f.BaseRadius = r;
                var l = new GameObject("Glow").AddComponent<Light>(); l.transform.SetParent(go, false); l.transform.localPosition = Vector3.up * 2f; l.type = LightType.Point; l.color = new Color(0.6f, 0.3f, 1f); l.range = r * 1.2f; l.intensity = 1.2f; l.shadows = LightShadows.None;
                AddPlaceholder(go.gameObject, "Corruption_Patch (Tier 3)", "– prozeduraler Verderbnis-Fleck");
            }
            var wards = new[] { new Vector2(-160, -180), new Vector2(-130, -210), new Vector2(230, 120), new Vector2(20, 280), new Vector2(-240, 100), new Vector2(200, 230), new Vector2(-40, 200), new Vector2(160, -80) };
            i = 0;
            foreach (var p in wards)
            {
                var go = Child(root, "Ward_" + i++, G(p.x, p.y)).transform;
                Part(go, PrimitiveType.Cylinder, new Vector3(0, 1.2f, 0), new Vector3(0.35f, 1.2f, 0.35f), stone, "Post", null, true);
                Part(go, PrimitiveType.Sphere, new Vector3(0, 2.6f, 0), Vector3.one * 0.5f, flame, "Lantern");
                var l = new GameObject("Light").AddComponent<Light>(); l.transform.SetParent(go, false); l.transform.localPosition = Vector3.up * 2.6f; l.type = LightType.Point; l.color = new Color(1f, 0.85f, 0.55f); l.range = 9f; l.intensity = 1.8f; l.shadows = LightShadows.None;
                l.gameObject.AddComponent<FlickerLight>();
                go.gameObject.AddComponent<CorruptionWard>().Radius = 7f;
                AddPlaceholder(go.gameObject, "Ward_Lantern (Tier 2)", "stylized painterly fantasy stone lantern post with warm light, game-ready low poly");
            }
        }

        // ------------------------------------------------------------ Dünenmeer
        static void Shelters(Transform root)
        {
            var cloth = Mat("ShelterCloth", new Color(0.85f, 0.75f, 0.55f));
            var pole = Mat("ShelterPole", new Color(0.45f, 0.32f, 0.2f));
            var spots = new[] { new Vector2(-430, 0), new Vector2(-180, -60), new Vector2(30, -40), new Vector2(240, 190), new Vector2(310, -270), new Vector2(-60, 280), new Vector2(120, 120) };
            int i = 0;
            foreach (var p in spots)
            {
                var go = Child(root, "Shelter_" + i++, G(p.x, p.y)).transform;
                for (int k = 0; k < 4; k++) Part(go, PrimitiveType.Cylinder, new Vector3((k % 2 == 0 ? -2.4f : 2.4f), 1.4f, (k < 2 ? -2.4f : 2.4f)), new Vector3(0.15f, 1.4f, 0.15f), pole, "Pole", null, true);
                Part(go, PrimitiveType.Cube, new Vector3(0, 2.9f, 0), new Vector3(5.6f, 0.1f, 5.6f), cloth, "Roof", new Vector3(0, 0, 6f));
                go.gameObject.AddComponent<SandShelter>().Radius = 5f;
                ZoneContentBuilder.Nameplate(go, "Späher-Zelt", "Schutz vor dem Sandsturm", new Color(1f, 0.9f, 0.6f));
                AddPlaceholder(go.gameObject, "Scout_Tent (Tier 2)", "stylized painterly fantasy desert scout tent, sand colored cloth on wooden poles, game-ready low poly");
            }
        }

        // ------------------------------------------------------------ Kristalltiefe
        static void Crystals(Transform root)
        {
            var mats = new[] { Mat("CaveCrystalBlue", new Color(0.4f, 0.7f, 1f), 0.1f, new Color(0.3f, 0.6f, 1f) * 2.2f), Mat("CaveCrystalViolet", new Color(0.7f, 0.45f, 1f), 0.1f, new Color(0.6f, 0.35f, 1f) * 2.2f) };
            var path = new List<Vector2>();
            Vector2[] route = { new Vector2(10, -360), new Vector2(-150, -40), new Vector2(0, 80), new Vector2(150, 150), new Vector2(200, -120), new Vector2(-240, 220), new Vector2(120, 200) };
            for (int i = 0; i + 1 < route.Length; i++) for (int k = 0; k < 6; k++) path.Add(Vector2.Lerp(route[i], route[i + 1], k / 6f) + new Vector2(R(-8f, 8f), R(-8f, 8f)));
            int n = 0;
            foreach (var p in path)
            {
                var go = Child(root, "Crystal_" + n, G(p.x, p.y)).transform;
                int count = 2 + n % 3;
                for (int k = 0; k < count; k++) Part(go, PrimitiveType.Cube, new Vector3(R(-0.8f, 0.8f), R(0.6f, 1.4f), R(-0.8f, 0.8f)), new Vector3(R(0.3f, 0.6f), R(1.2f, 2.8f), R(0.3f, 0.6f)), mats[n % 2], "Shard", new Vector3(R(-20f, 20f), R(0f, 90f), R(-20f, 20f)));
                var l = new GameObject("Light").AddComponent<Light>(); l.transform.SetParent(go, false); l.transform.localPosition = Vector3.up * 1.8f; l.type = LightType.Point; l.color = n % 2 == 0 ? new Color(0.45f, 0.7f, 1f) : new Color(0.75f, 0.5f, 1f); l.range = DarknessModel.CrystalRadius + 3f; l.intensity = 2.2f; l.shadows = LightShadows.None;
                go.gameObject.AddComponent<CrystalLight>();
                AddPlaceholder(go.gameObject, "Cave_Crystal (Tier 2)", "stylized painterly fantasy glowing cave crystal cluster, game-ready low poly");
                n++;
            }
        }

        // ------------------------------------------------------------ Himmelsspitzen
        static void Islands(Transform root)
        {
            var grass = Mat("IslandGrass", new Color(0.55f, 0.75f, 0.45f));
            var rock = Mat("IslandRock", new Color(0.62f, 0.6f, 0.65f));
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f + R(-0.2f, 0.2f), r = R(150f, 380f);
                var p = new Vector2(Mathf.Sin(a) * r, Mathf.Cos(a) * r);
                float y = _h(p.x, p.y) + R(25f, 55f), s = R(8f, 16f);
                var go = Child(root, "Island_" + i, new Vector3(p.x, y, p.y)).transform;
                Part(go, PrimitiveType.Cylinder, new Vector3(0, -0.3f, 0), new Vector3(s * 2f, 0.6f, s * 2f), grass, "Top", null, true);
                Part(go, PrimitiveType.Sphere, new Vector3(0, -s * 0.45f, 0), new Vector3(s * 1.9f, s, s * 1.9f), rock, "Rock", null, true);
                if (i % 3 == 0)
                {
                    var up = Child(root, "IslandUpdraft_" + i, G(p.x + s + 4f, p.y)).transform;
                    var u = up.gameObject.AddComponent<Updraft>(); u.Height = y - up.position.y + 15f; u.Radius = 5f; u.Strength = 12f;
                }
                AddPlaceholder(go.gameObject, "Floating_Island (Tier 2)", "stylized painterly fantasy floating sky island with grass top and rocky underside, game-ready low poly");
            }
        }

        // ------------------------------------------------------------ Tyrannenfeste
        static void Fortress(Transform root)
        {
            var black = Mat("FortressStone", new Color(0.16f, 0.14f, 0.18f));
            var rune = Mat("FortressRune", new Color(0.8f, 0.3f, 0.2f), 0.1f, new Color(1f, 0.35f, 0.15f) * 1.8f);
            // Außenmauer mit drei Toren (Lücken bei x = −60, 0, 60)
            float wallZ = -270f;
            for (float x = -200f; x <= 200f; x += 20f)
            {
                if (Mathf.Abs(x + 60f) < 9f || Mathf.Abs(x) < 9f || Mathf.Abs(x - 60f) < 9f) continue;
                float g = Mathf.Min(_h(x - 10f, wallZ), _h(x + 10f, wallZ)) - 1f;
                Part(root, PrimitiveType.Cube, new Vector3(x, g + 7f, wallZ), new Vector3(20.5f, 16f, 4f), black, "Wall", null, true);
            }
            var siege = root.gameObject.AddComponent<SiegeDirector>();
            var gates = new List<GameObject>();
            string[] ids = { "belagerung_tor1", "belagerung_tor2", "belagerung_tor3" };
            float[] gx = { -60f, 0f, 60f };
            for (int i = 0; i < 3; i++)
            {
                var gate = Child(root, "Gate_" + (i + 1), G(gx[i], wallZ)).transform;
                Part(gate, PrimitiveType.Cube, new Vector3(0, 5f, 0), new Vector3(16f, 10f, 1.5f), black, "Door", null, true);
                Part(gate, PrimitiveType.Cube, new Vector3(0, 5f, -0.8f), new Vector3(3f, 3f, 0.2f), rune, "Seal");
                gates.Add(gate.gameObject);
                AddPlaceholder(gate.gameObject, "Fortress_Gate (Tier 2)", "stylized painterly fantasy massive black iron fortress gate with glowing red seal, game-ready low poly");
            }
            siege.Gates = gates.ToArray(); siege.GateEvents = ids;
            // Hof-Säulen und Thron
            for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2f; var p = new Vector2(Mathf.Sin(a) * 26f, Mathf.Cos(a) * 26f); Part(root, PrimitiveType.Cylinder, G(p.x, p.y, 6f), new Vector3(2f, 6f, 2f), black, "Pillar", null, true); }
            var throne = Child(root, "Throne", G(0f, 262f)).transform;
            Part(throne, PrimitiveType.Cube, new Vector3(0, 1f, 0), new Vector3(14f, 2f, 10f), black, "Dais", null, true);
            Part(throne, PrimitiveType.Cube, new Vector3(0, 4f, 2f), new Vector3(3f, 4f, 2f), black, "Seat", null, true);
            Part(throne, PrimitiveType.Cube, new Vector3(0, 7.5f, 3f), new Vector3(3.6f, 3f, 0.6f), rune, "Back");
            AddPlaceholder(throne.gameObject, "Tyrant_Throne (Tier 1)", "stylized painterly fantasy empty obsidian throne with glowing runes, game-ready low poly");
        }

        // ------------------------------------------------------------ Sprungrätsel
        static void JumpPuzzle(Transform root, string id, string title, Vector3 basePos, int steps, float rise, float size, bool spiral)
        {
            var mat = Mat("JumpStone_" + id, new Color(0.72f, 0.7f, 0.68f));
            var pz = Child(root, "JumpPuzzle_" + id, Vector3.zero).transform;
            float y = _h(basePos.x, basePos.z) + 1f; Vector3 p = new Vector3(basePos.x, y, basePos.z);
            for (int i = 0; i < steps; i++)
            {
                float a = i * (spiral ? 0.55f : 1.1f);
                p += new Vector3(Mathf.Sin(a) * 3.6f, rise, Mathf.Cos(a) * 3.6f) * (spiral ? 1f : 1f);
                if (spiral) p = new Vector3(basePos.x + Mathf.Sin(a) * 6f, p.y, basePos.z + Mathf.Cos(a) * 6f);
                Part(pz, PrimitiveType.Cube, p, new Vector3(size * 2.2f, 0.5f, size * 2.2f), mat, "Step_" + i, new Vector3(0, a * Mathf.Rad2Deg, 0), true);
            }
            var top = Child(pz, "Chest", p + Vector3.up * 0.25f).transform;
            Part(top, PrimitiveType.Cube, new Vector3(0, 0.4f, 0), new Vector3(1.2f, 0.8f, 0.8f), Mat("JumpChest", new Color(0.55f, 0.35f, 0.15f)), "Box", null, true);
            Part(top, PrimitiveType.Cube, new Vector3(0, 0.85f, 0), new Vector3(1.25f, 0.15f, 0.85f), Mat("CityGold", new Color(1f, 0.82f, 0.4f), 0.1f, new Color(1f, 0.7f, 0.25f) * 0.8f), "Lid");
            var c = top.gameObject.AddComponent<JumpPuzzleChest>(); c.Id = id; c.Title = title; c.PromptText = "Truhe öffnen"; c.Range = 2.5f;
            AddPlaceholder(top.gameObject, "JumpPuzzle_Chest (Tier 1)", "stylized painterly fantasy treasure chest with golden lid, game-ready low poly");
        }
    }
}
