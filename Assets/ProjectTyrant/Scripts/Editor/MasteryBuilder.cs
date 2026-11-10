using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 4 / M18: vier Beherrschungslinien (Daten) + Umwelt-Objekte (Giftnebel, Sprungpilze, Aufwinde, verborgene Truhen).</summary>
    public static class MasteryBuilder
    {
        public const string Dir = DataDir + "/Mastery";

        public static MasteryLineDefinition Load(MasteryLine l) => AssetDatabase.LoadAssetAtPath<MasteryLineDefinition>(Dir + "/Mastery_" + l + ".asset");
        public static MasteryLineDefinition[] LoadAll() => new[] { Load(MasteryLine.Reittiere), Load(MasteryLine.Gleiten), Load(MasteryLine.Umwelt), Load(MasteryLine.Kommandant), Load(MasteryLine.Greif), Load(MasteryLine.Himmelsschuppe), Load(MasteryLine.Duenenlaeufer), Load(MasteryLine.Jadegeist), Load(MasteryLine.Boot) };

        static MasteryRankCost R(string n, string e, long xp, int pts) => new MasteryRankCost { Name = n, Effect = e, Xp = xp, Points = pts };

        public static void BuildAll()
        {
            EnsureFolder(Dir);
            Line(MasteryLine.Reittiere, "Reittier-Training", "Bessere Reittiere: mehr Ausdauer, weitere Sprünge, schnelleres Laden.", "mount_raptor", new Color(1f, 0.7f, 0.35f), new[] {
                R("Zähe Reittiere", "+25 % Reittier-HP", 6000, 1), R("Weiter Satz", "Raptor-Sprung +20 % Weite", 12000, 2),
                R("Schnelle Ladung", "Springer lädt 30 % schneller", 18000, 2), R("Galopp", "Reittiere +10 % Tempo", 24000, 3) });
            Line(MasteryLine.Gleiten, "Gleiten", "Der Gleiter: Leertaste in der Luft. Später Aufwinde und schnelleres Gleiten.", "mastery_glide", new Color(0.95f, 0.85f, 0.55f), new[] {
                R("Gleitschirm", "Leertaste in der Luft öffnet den Gleiter", 4000, 1), R("Aufwinde", "Aufwind-Säulen heben dich beim Gleiten an", 10000, 2),
                R("Segelflug", "Gleiten schneller (11 m/s) und flacher (Sinken 2,2 m/s)", 18000, 3) });
            Line(MasteryLine.Umwelt, "Umweltkunde", "Die Gefahren der neuen Zonen meistern.", "mastery_env", new Color(0.45f, 1f, 0.6f), new[] {
                R("Giftresistenz", "Immun gegen Giftnebel im Nebelmoor", 5000, 1), R("Pilzspringen", "Sprungpilze schleudern dich hoch", 10000, 1),
                R("Glutläufer", "Lavarisse verbrennen dich nicht mehr", 16000, 2), R("Schatzsucher", "Verborgene Truhen werden sichtbar", 22000, 2) });
            Line(MasteryLine.Kommandant, "Kommandant", "Komfort im Feld: Beute und Reisen.", "mastery_cmd", new Color(0.6f, 0.8f, 1f), new[] {
                R("Auto-Beute", "Beute in 6 m wird automatisch eingesammelt", 4000, 1), R("Reisekasse", "Wegmarken-Reisen −20 %", 9000, 1),
                R("Reisemeister", "Wegmarken-Reisen −40 %", 15000, 2) });
            Line(MasteryLine.Greif, "Greif", "Der Greif der Himmelswächter (Persönliche Geschichte, Kapitel 9).", "mount_griffon", new Color(0.98f, 0.9f, 0.6f), new[] {
                R("Flügelkraft", "+2 Auftrieb-Ladungen (5 statt 3)", 6000, 1), R("Aufwindflug", "Aufwinde heben den Greif und füllen den Auftrieb", 12000, 1),
                R("Sturzflug-Meister", "Abfangen wandelt 85 % statt 60 % des Sturz-Tempos in Höhe; Sturz bis 4,6× Lauftempo", 18000, 2), R("Luftakrobatik", "Ausweichrolle in der Luft (V) ohne Tempoverlust", 24000, 2) });
            Line(MasteryLine.Himmelsschuppe, "Himmelsschuppe", "Der junge Himmelsdrache: senkrechter Flug, Schweben, Feuerstoß (Sammlung in den Himmelsspitzen).", "mount_skyscale", new Color(0.55f, 0.75f, 1f), new[] {
                R("Wandhaften", "An steilen Wänden (≥ 60°) festhalten und Flugausdauer regenerieren (max. 20 s)", 8000, 1), R("Flugkondition", "Flugausdauer 150 statt 100, Schweben kostet 3/s statt 5/s", 14000, 2),
                R("Aufwind-Segeln", "Aufwinde füllen die Flugausdauer sofort", 18000, 2), R("Vertrauensband", "In der Luft zwischen Greif und Himmelsschuppe wechseln; Flugausdauer regeneriert 5/s im Flug", 26000, 3) });
            // Lauf 11: Regionen-Linien
            Line(MasteryLine.Duenenlaeufer, "Dünenläufer", "Der Sprinter der Glutwüste: Blinkschritt und Sandportale.", "mount_dunerunner", new Color(0.95f, 0.75f, 0.4f), new[] {
                R("Dritte Ladung", "Blinkschritt mit 3 statt 2 Ladungen", 20000, 2), R("Portalmeister", "Sandportale lassen sich aus 4 m statt 2,5 m nutzen", 26000, 2),
                R("Sandgleiten", "Blinkschritt auch in der Luft", 32000, 3) });
            Line(MasteryLine.Jadegeist, "Jadegeist", "Dein schwebender Begleiter aus Jade (Werkstatt in Jadehafen).", "mastery_jade", new Color(0.4f, 0.9f, 0.7f), new[] {
                R("Rettungsprotokoll", "Belebt dich einmal je 10 Minuten aus dem Kampfunfähig-Zustand (50 % HP)", 18000, 1), R("Gleitverstärkung", "Gleiter-Ausdauer +50 %, Jade-Aufwinde", 24000, 2),
                R("Jadebatterien", "Treibt Jade-Geräte an: Aufzüge, Brücken, Kanonen, Truhen", 30000, 2), R("Jadeband", "Sammelt Beute im Umkreis von 20 m, markiert Angelstellen und Einblicke", 36000, 3) });
            Line(MasteryLine.Boot, "Boot", "Das Boot der Bootswerft in Jadehafen.", "mastery_boat", new Color(0.5f, 0.8f, 0.95f), new[] {
                R("Rudermeister", "Boot +20 % Tempo", 14000, 1), R("Jade-Schub", "5 s +60 % Tempo, 20 s Abklingzeit", 22000, 2) });
            AssetDatabase.SaveAssets();
        }

        static void Line(MasteryLine l, string name, string desc, string icon, Color c, MasteryRankCost[] ranks)
        {
            string path = Dir + "/Mastery_" + l + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<MasteryLineDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<MasteryLineDefinition>(); AssetDatabase.CreateAsset(d, path); }
            d.Line = l; d.DisplayName = name; d.Description = desc; d.Icon = icon; d.Color = c; d.Ranks = ranks;
            EditorUtility.SetDirty(d);
        }

        // ---------------- Umwelt-Objekte ----------------
        public static void PlaceEnvironment(Procedural.BzLayout l, Transform parent, System.Func<Vector2, float, Vector3> g)
        {
            var root = Child(parent, "Mastery", Vector3.zero).transform;
            var mistMat = Mat("PoisonMist", new Color(0.5f, 0.85f, 0.3f), 0.1f, new Color(0.3f, 0.6f, 0.1f) * 0.6f, 0.2f);
            foreach (var m in l.Mists)
            {
                var go = Child(root, "PoisonMist", g(m.pos, 0f));
                go.AddComponent<PoisonMist>().Radius = m.radius;
                Fog(go.transform, m.radius, new Color(0.55f, 0.9f, 0.35f, 0.35f));
                AddPlaceholder(go, "PoisonMist (Tier 3)", "– prozedurale Partikel");
            }
            var cap = Mat("MushroomCap", new Color(0.75f, 0.35f, 0.85f), 0.2f, new Color(0.6f, 0.2f, 0.8f) * 0.8f);
            var stem = Mat("MushroomStem", new Color(0.92f, 0.88f, 0.78f));
            foreach (var p in l.Mushrooms)
            {
                var go = Child(root, "BounceMushroom", g(p, 0f));
                Part(go.transform, PrimitiveType.Cylinder, new Vector3(0, 0.4f, 0), new Vector3(0.6f, 0.4f, 0.6f), stem, "Stem", null, true);
                Part(go.transform, PrimitiveType.Sphere, new Vector3(0, 0.9f, 0), new Vector3(2.4f, 0.9f, 2.4f), cap, "Cap", null, true);
                go.AddComponent<BounceMushroom>();
                AddPlaceholder(go, "BounceMushroom (Tier 2)", "stylized painterly fantasy giant glowing purple bounce mushroom, game-ready low poly, hand-painted texture");
            }
            foreach (var p in l.Updrafts)
            {
                var go = Child(root, "Updraft", g(p, 0f));
                go.AddComponent<Updraft>();
                Rising(go.transform, new Color(0.85f, 0.95f, 1f, 0.6f));
                var ring = Mat("UpdraftRing", new Color(0.8f, 0.95f, 1f), 0.1f, new Color(0.6f, 0.85f, 1f));
                Part(go.transform, PrimitiveType.Cylinder, new Vector3(0, 0.05f, 0), new Vector3(9f, 0.02f, 9f), ring, "Ring");
            }
            var wood = Mat("ChestWood", new Color(0.5f, 0.32f, 0.18f)); var gold = Mat("ChestGold", new Color(1f, 0.8f, 0.3f), 0.1f, new Color(1f, 0.7f, 0.2f) * 0.6f);
            foreach (var c in l.Chests)
            {
                var go = Child(root, "HiddenChest_" + c.id, g(c.pos, 0f));
                Part(go.transform, PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(1.2f, 0.7f, 0.8f), wood, "Box");
                Part(go.transform, PrimitiveType.Cube, new Vector3(0, 0.75f, 0), new Vector3(1.25f, 0.2f, 0.85f), gold, "Lid");
                var hc = go.AddComponent<HiddenChest>();
                hc.ChestId = c.id; hc.Loot = ItemBuilder.Table("ChampionBag"); hc.PromptText = "Öffnen"; hc.Range = 2.8f;
                AddPlaceholder(go, "HiddenChest (Tier 2)", "stylized painterly fantasy small treasure chest with gold trim, game-ready low poly, hand-painted texture");
            }
        }

        static void Fog(Transform t, float radius, Color c)
        {
            var ps = new GameObject("Mist").AddComponent<ParticleSystem>();
            ps.transform.SetParent(t, false);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = 6f; main.startSpeed = 0.3f; main.startSize = new ParticleSystem.MinMaxCurve(4f, 8f); main.maxParticles = 60; main.startColor = c; main.loop = true;
            var em = ps.emission; em.rateOverTime = 8f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius; sh.rotation = new Vector3(90, 0, 0);
            ConfigureRenderer(ps);
        }

        static void Rising(Transform t, Color c)
        {
            var ps = new GameObject("Rising").AddComponent<ParticleSystem>();
            ps.transform.SetParent(t, false);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = 4f; main.startSpeed = 10f; main.startSize = 0.25f; main.maxParticles = 120; main.startColor = c; main.loop = true;
            var em = ps.emission; em.rateOverTime = 25f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 4f; sh.rotation = new Vector3(-90, 0, 0);
            ConfigureRenderer(ps);
        }

        static void ConfigureRenderer(ParticleSystem ps)
        {
            var lib = AssetDatabase.LoadAssetAtPath<VfxLibrary>(DataDir + "/VfxLibrary.asset");
            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (lib != null && lib.FlashSolid != null) r.sharedMaterial = lib.FlashSolid;
            ps.Play();
        }
    }
}
