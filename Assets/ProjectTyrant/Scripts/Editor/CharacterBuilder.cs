using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Primitive-Komposition für Charaktere (Fallback, bis Meshy-Modelle vorliegen).</summary>
    public static class CharacterBuilder
    {
        public const string PlayerPrompt = "stylized painterly fantasy hero, armored human warden with blue and gold tabard, short cape, game-ready low poly, hand-painted texture, A-pose, full body";

        /// <summary>Erzeugt das Spieler-Visual (Wurzel auf Fußhöhe, ~1,8 m).</summary>
        /// <summary>Nur der Spieler nutzt das Meshy-Modell; Wachen bleiben prozedural (eigene Silhouette).</summary>
        public static bool UseMeshyPlayer = true;

        public static GameObject BuildPlayerVisual(Transform parent, out GameObject greatsword, out GameObject staff) => BuildPlayerVisual(parent, out greatsword, out staff, null, Anim.AnimDriver.Role.Player);

        /// <summary>Lauf 5: geriggter Held (Meshy-Skinning, sonst MeshKit-Rig) mit Waffen am Hand-/Rücken-Socket.</summary>
        public static GameObject BuildPlayerVisual(Transform parent, out GameObject greatsword, out GameObject staff, RigBuilder.HumanLook look, Anim.AnimDriver.Role role)
        {
            bool player = look == null;
            look = look ?? CharacterRigs.PlayerLook();
            var r = CharacterRigs.Human(parent, player ? "PT_Player" : "PT_Guard", look, player && UseMeshyPlayer ? "PT_Meshy_PlayerHero" : null);
            var root = r.Visual;
            CharacterRigs.Finish(r, "Greatsword", true);
            var trim = Mat("Player_Trim", Palette.PlayerTrim, 0.1f);
            var leather = Mat("Leather", new Color(0.42f, 0.27f, 0.18f));
            greatsword = Child(r.HandSocket, "Greatsword", Vector3.zero);
            Part(greatsword.transform, PrimitiveType.Cube, new Vector3(0, 0.75f, 0), new Vector3(0.11f, 1.3f, 0.03f), Mat("Blade_Ember", new Color(0.95f, 0.55f, 0.25f), 0.1f, new Color(0.35f, 0.08f, 0f), 1f), "Blade");
            Part(greatsword.transform, PrimitiveType.Cube, new Vector3(0, 0.08f, 0), new Vector3(0.42f, 0.06f, 0.08f), trim, "Guard");
            Part(greatsword.transform, PrimitiveType.Cube, new Vector3(0, -0.1f, 0), new Vector3(0.05f, 0.3f, 0.05f), leather, "Hilt");
            staff = Child(r.HandSocket, "Staff", Vector3.zero);
            Part(staff.transform, PrimitiveType.Cylinder, new Vector3(0, 0.35f, 0), new Vector3(0.06f, 0.9f, 0.06f), Mat("Wood", Palette.Wood), "Shaft");
            Part(staff.transform, PrimitiveType.Sphere, new Vector3(0, 1.3f, 0), Vector3.one * 0.2f, Mat("Orb_Storm", Palette.Lightning, 0f, new Color(0.35f, 0.25f, 0.8f), 1f), "Orb");
            staff.SetActive(false);
            AddPlaceholder(root, player ? "player_hero" : "ally_guard", PlayerPrompt);
            if (r.Smr != null && r.Smr.sharedMesh != null && r.Smr.sharedMesh.name.StartsWith("PT_Meshy")) root.GetComponent<PlaceholderMarker>().ReplacedByMeshy = true;
            if (player)
            {
                ReplaceWithMeshy(greatsword, "PT_Meshy_Greatsword", 1.5f, true, 0.18f, -1);
                ReplaceWithMeshy(staff, "PT_Meshy_StormStaff", 1.6f, true, 0.35f, 1);
            }
            var trails = new[] { CharacterRigs.AddTrail(greatsword, 1.35f, new Color(1f, 0.55f, 0.2f)), CharacterRigs.AddTrail(staff, 1.3f, new Color(0.65f, 0.55f, 1f)) };
            // Lauf 5 / M25: Waffen der neuen Klassen (Haupthand rechts, Zweithand links), Index = WeaponVisual
            var weaponList = new List<Transform> { greatsword.transform, staff.transform };
            if (player)
            {
                var handL = r.Visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "HandSocket_L");
                var models = new GameObject[System.Enum.GetValues(typeof(WeaponVisual)).Length]; var off = new GameObject[models.Length];
                models[(int)WeaponVisual.Greatsword] = greatsword; models[(int)WeaponVisual.Staff] = staff;
                var wood = Mat("Wood", Palette.Wood); var steel = Mat("Blade_Steel", new Color(0.8f, 0.82f, 0.88f), 0.6f);
                GameObject W(WeaponVisual v, bool offhand) { var g = Child(offhand && handL != null ? handL : r.HandSocket, (offhand ? "Off_" : "W_") + v, Vector3.zero); g.SetActive(false); weaponList.Add(g.transform); if (offhand) off[(int)v] = g; else models[(int)v] = g; return g; }
                void Bow(GameObject g, float len) { for (int i = -2; i <= 2; i++) { var pp = Part(g.transform, PrimitiveType.Cube, new Vector3(0, i * len * 0.2f, -Mathf.Abs(i) * 0.06f), new Vector3(0.05f, len * 0.22f, 0.05f), wood, "Limb"); pp.transform.localRotation = Quaternion.Euler(i * 12f, 0, 0); } Part(g.transform, PrimitiveType.Cylinder, new Vector3(0, 0, -0.17f), new Vector3(0.008f, len * 0.5f, 0.008f), Mat("String", new Color(0.9f, 0.9f, 0.85f)), "String"); }
                void Blade(GameObject g, float len, Material m) { Part(g.transform, PrimitiveType.Cube, new Vector3(0, len * 0.5f + 0.08f, 0), new Vector3(0.06f, len, 0.02f), m, "Blade"); Part(g.transform, PrimitiveType.Cube, new Vector3(0, 0.06f, 0), new Vector3(0.18f, 0.03f, 0.05f), trim, "Guard"); Part(g.transform, PrimitiveType.Cube, new Vector3(0, -0.04f, 0), new Vector3(0.035f, 0.14f, 0.035f), leather, "Hilt"); }
                var longbow = W(WeaponVisual.Longbow, false); Bow(longbow, 1.5f);
                ReplaceWithMeshy(longbow, "PT_Meshy_Longbow", 1.5f, true, 0.5f, 0);   // M27 Meshy-Signaturwaffe Wildhüter (Griff in der Mitte)
                Bow(W(WeaponVisual.Shortbow, false), 1.0f);
                var axe = W(WeaponVisual.AxeTorch, false); Part(axe.transform, PrimitiveType.Cylinder, new Vector3(0, 0.25f, 0), new Vector3(0.04f, 0.32f, 0.04f), wood, "Haft"); Part(axe.transform, PrimitiveType.Cube, new Vector3(0.09f, 0.52f, 0), new Vector3(0.18f, 0.14f, 0.02f), steel, "Head");
                var torch = W(WeaponVisual.AxeTorch, true); Part(torch.transform, PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0), new Vector3(0.045f, 0.25f, 0.045f), wood, "Stick"); Part(torch.transform, PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), Vector3.one * 0.12f, Mat("TorchFlame", new Color(1f, 0.6f, 0.2f), 0f, new Color(2f, 0.9f, 0.3f), 0f), "Flame");
                Blade(W(WeaponVisual.SwordWarhorn, false), 0.85f, steel);
                var horn = W(WeaponVisual.SwordWarhorn, true); var hp = Part(horn.transform, PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(0.07f, 0.15f, 0.07f), Mat("Horn", new Color(0.85f, 0.78f, 0.6f)), "Horn"); hp.transform.localRotation = Quaternion.Euler(0, 0, 30f);
                var sc = W(WeaponVisual.ScepterFocus, false); Part(sc.transform, PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0), new Vector3(0.035f, 0.28f, 0.035f), trim, "Rod"); Part(sc.transform, PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), Vector3.one * 0.12f, Mat("Orb_Arcane", new Color(0.9f, 0.5f, 1f), 0f, new Color(0.8f, 0.3f, 1.2f), 1f), "Orb");
                var fo = W(WeaponVisual.ScepterFocus, true); Part(fo.transform, PrimitiveType.Sphere, new Vector3(0, 0.12f, 0), Vector3.one * 0.16f, Mat("Orb_Focus", new Color(0.5f, 0.75f, 1f), 0f, new Color(0.4f, 0.8f, 1.4f), 1f), "Focus");
                var dagR = W(WeaponVisual.DaggerDagger, false); Blade(dagR, 0.32f, steel); var dagL = W(WeaponVisual.DaggerDagger, true); Blade(dagL, 0.32f, steel);
                ReplaceWithMeshy(dagR, "PT_Meshy_ShadowDagger", 0.45f, true, 0.22f, -1); ReplaceWithMeshy(dagL, "PT_Meshy_ShadowDagger", 0.45f, true, 0.22f, -1);   // M27 Meshy-Signaturwaffe Schattenklinge
                var es = W(WeaponVisual.ElementStaff, false);
                Part(es.transform, PrimitiveType.Cylinder, new Vector3(0, 0.35f, 0), new Vector3(0.06f, 0.9f, 0.06f), Mat("Wood_Dark", new Color(0.3f, 0.2f, 0.12f)), "Shaft");
                Part(es.transform, PrimitiveType.Sphere, new Vector3(0, 1.3f, 0), Vector3.one * 0.2f, Mat("Orb_Element", new Color(1f, 0.7f, 0.3f), 0f, new Color(1.2f, 0.6f, 0.2f), 1f), "Orb");
                ReplaceWithMeshy(es, "PT_Meshy_ElementStaff", 1.6f, true, 0.35f, 1);   // M27 Meshy-Signaturwaffe Elementarweber
                Blade(W(WeaponVisual.SwordPistol, false), 0.8f, steel);
                var pis = W(WeaponVisual.SwordPistol, true); Part(pis.transform, PrimitiveType.Cube, new Vector3(0, 0.02f, 0.08f), new Vector3(0.05f, 0.05f, 0.28f), Mat("Pistol", new Color(0.25f, 0.22f, 0.2f), 0.5f), "Barrel"); Part(pis.transform, PrimitiveType.Cube, new Vector3(0, -0.06f, -0.02f), new Vector3(0.05f, 0.14f, 0.06f), wood, "Grip");
                var cj = W(WeaponVisual.Conjured, false); Part(cj.transform, PrimitiveType.Cube, new Vector3(0, 0.45f, 0), new Vector3(0.12f, 0.8f, 0.04f), Mat("Conjured", new Color(0.8f, 0.7f, 1f), 0f, new Color(0.9f, 0.6f, 1.6f), 0.5f), "Blade");
                // Lauf 5 / M26: Waffen der Elite-Spezialisierungen
                var iron = Mat("Hammer_Iron", new Color(0.45f, 0.46f, 0.5f), 0.55f); var dark = Mat("Rifle_Metal", new Color(0.22f, 0.21f, 0.2f), 0.5f);
                GameObject Hammer(GameObject g) { Part(g.transform, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(0.045f, 0.6f, 0.045f), wood, "Haft"); Part(g.transform, PrimitiveType.Cube, new Vector3(0, 1.08f, 0), new Vector3(0.36f, 0.2f, 0.2f), iron, "Head"); Part(g.transform, PrimitiveType.Cube, new Vector3(0, 1.08f, 0), new Vector3(0.38f, 0.06f, 0.22f), trim, "Band"); return g; }
                GameObject Rifle(GameObject g) { Part(g.transform, PrimitiveType.Cube, new Vector3(0, 0.45f, 0), new Vector3(0.05f, 0.95f, 0.05f), dark, "Barrel"); Part(g.transform, PrimitiveType.Cube, new Vector3(0, 0.02f, -0.03f), new Vector3(0.07f, 0.35f, 0.1f), wood, "Stock"); Part(g.transform, PrimitiveType.Cube, new Vector3(0, 0.3f, 0.05f), new Vector3(0.03f, 0.08f, 0.06f), trim, "Scope"); return g; }
                GameObject Scepter(GameObject g, Color orb, string mat) { Part(g.transform, PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0), new Vector3(0.035f, 0.28f, 0.035f), trim, "Rod"); Part(g.transform, PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), Vector3.one * 0.12f, Mat(mat, orb, 0f, orb * 1.4f, 1f), "Orb"); return g; }
                GameObject HornOff(GameObject g) { var hh = Part(g.transform, PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(0.07f, 0.15f, 0.07f), Mat("Horn", new Color(0.85f, 0.78f, 0.6f)), "Horn"); hh.transform.localRotation = Quaternion.Euler(0, 0, 30f); return g; }
                Hammer(W(WeaponVisual.Hammer, false));
                Rifle(W(WeaponVisual.Rifle, false));
                Blade(W(WeaponVisual.DaggerWarhorn, false), 0.34f, steel); HornOff(W(WeaponVisual.DaggerWarhorn, true));
                Scepter(W(WeaponVisual.ScepterWarhorn, false), new Color(0.85f, 0.9f, 1f), "Orb_Storm"); HornOff(W(WeaponVisual.ScepterWarhorn, true));
                Blade(W(WeaponVisual.SwordFocus, false), 0.8f, Mat("Blade_Weave", new Color(0.75f, 0.6f, 1f), 0.6f)); var fo2 = W(WeaponVisual.SwordFocus, true); Part(fo2.transform, PrimitiveType.Sphere, new Vector3(0, 0.12f, 0), Vector3.one * 0.16f, Mat("Orb_Focus2", new Color(0.7f, 0.55f, 1f), 0f, new Color(0.9f, 0.6f, 1.5f), 1f), "Focus");
                Scepter(W(WeaponVisual.ScepterDagger, false), new Color(1f, 0.45f, 0.15f), "Orb_Lava"); Blade(W(WeaponVisual.ScepterDagger, true), 0.3f, Mat("Blade_Obsidian", new Color(0.15f, 0.12f, 0.14f), 0.8f));
                Scepter(W(WeaponVisual.ScepterPistol, false), new Color(0.55f, 0.25f, 0.85f), "Orb_Void"); var pis2 = W(WeaponVisual.ScepterPistol, true); Part(pis2.transform, PrimitiveType.Cube, new Vector3(0, 0.02f, 0.08f), new Vector3(0.05f, 0.05f, 0.28f), dark, "Barrel"); Part(pis2.transform, PrimitiveType.Cube, new Vector3(0, -0.06f, -0.02f), new Vector3(0.05f, 0.14f, 0.06f), wood, "Grip");
                var ax2 = W(WeaponVisual.AxeDagger, false); Part(ax2.transform, PrimitiveType.Cylinder, new Vector3(0, 0.22f, 0), new Vector3(0.035f, 0.28f, 0.035f), wood, "Haft"); Part(ax2.transform, PrimitiveType.Cube, new Vector3(0.08f, 0.46f, 0), new Vector3(0.16f, 0.12f, 0.02f), Mat("Axe_Cyan", new Color(0.7f, 0.85f, 0.9f), 0.7f), "Head"); Blade(W(WeaponVisual.AxeDagger, true), 0.3f, steel);
                Blade(W(WeaponVisual.SwordShield, false), 0.8f, steel); var sh = W(WeaponVisual.SwordShield, true); Part(sh.transform, PrimitiveType.Cube, new Vector3(0, 0.12f, 0.05f), new Vector3(0.42f, 0.55f, 0.05f), Mat("Shield_Blue", new Color(0.55f, 0.65f, 0.95f), 0.4f), "Shield"); Part(sh.transform, PrimitiveType.Cube, new Vector3(0, 0.12f, 0.08f), new Vector3(0.1f, 0.4f, 0.02f), trim, "Boss");
                Scepter(W(WeaponVisual.ScepterTorch, false), new Color(0.35f, 0.9f, 0.5f), "Orb_Emerald"); var tor2 = W(WeaponVisual.ScepterTorch, true); Part(tor2.transform, PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0), new Vector3(0.045f, 0.25f, 0.045f), wood, "Stick"); Part(tor2.transform, PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), Vector3.one * 0.12f, Mat("TorchFlame", new Color(1f, 0.6f, 0.2f), 0f, new Color(2f, 0.9f, 0.3f), 0f), "Flame");
                var pv = parent.GetComponent<PlayerVisuals>(); if (pv == null) pv = parent.gameObject.AddComponent<PlayerVisuals>();
                pv.Models = models; pv.OffhandModels = off; pv.OrigCloth = look.Cloth; pv.OrigTrim = look.Trim;
            }
            if (role == Anim.AnimDriver.Role.Player)
            {
                var fx = GetOrAdd<SkillFxDirector>(parent.gameObject);
                fx.Hand = r.HandSocket; fx.Trails = trails;
            }
            var d = CharacterRigs.Drive(parent.gameObject, root, role);
            d.HandSocket = r.HandSocket; d.BackSocket = r.BackSocket; d.Weapons = weaponList.ToArray();
            if (role != Anim.AnimDriver.Role.Player) d.SheatheDelay = 1e6f;
            return root;
        }

        /// <summary>Ersetzt die Primitive-Kinder eines Objekts durch ein normalisiertes Meshy-Modell (falls vorhanden).</summary>
        public static bool ReplaceWithMeshy(GameObject target, string modelName, float height, bool alignLongest = false, float pivotFraction = 0f, int widestEnd = 0)
        {
            var meshy = MeshyIntegrator.GetOrBuild(modelName, height, alignLongest, pivotFraction, false, widestEnd);
            if (meshy == null || target == null) return false;
            for (int i = target.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(target.transform.GetChild(i).gameObject);
            var m = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(meshy);
            m.transform.SetParent(target.transform, false);
            m.name = "MeshyModel";
            var marker = target.GetComponent<PlaceholderMarker>();
            if (marker != null) marker.ReplacedByMeshy = true;
            return true;
        }

        /// <summary>Lauf 5: geriggter Plünderer (Hörner, glühende Augen, Keule in der Hand) je Rang; Meshy-Skinning wird zuerst versucht.</summary>
        public static GameObject BuildEnemyVisual(Transform parent, EnemyTier tier, out Transform head)
        {
            string meshyName = tier == EnemyTier.Champion ? "PT_Meshy_ChampionWarlord" : tier == EnemyTier.Veteran ? "PT_Meshy_VeteranBrute" : "PT_Meshy_TrashRaider";
            var r = CharacterRigs.Human(parent, "PT_Enemy_" + tier, CharacterRigs.EnemyLook(tier), meshyName);
            CharacterRigs.Finish(r, "Hammer", true);
            var bone = Mat("Bone", new Color(0.92f, 0.87f, 0.75f), 0.1f);
            var club = Child(r.HandSocket, "Club", Vector3.zero);
            Part(club.transform, PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(0.08f, 0.8f, 0.08f), Mat("Wood", Palette.Wood), "Shaft");
            Part(club.transform, PrimitiveType.Sphere, new Vector3(0, 0.8f, 0), new Vector3(0.24f, 0.32f, 0.24f), bone, "Head");
            head = r.Bone(Anim.SkeletonDef.Head);
            return r.Visual;
        }
    }
}
