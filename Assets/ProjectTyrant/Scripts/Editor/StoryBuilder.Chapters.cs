using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 6: Struktur der „Chronik des Tyrannen“ (Kapitel → Schritte → Ziele). Texte liegen in der Text-Tabelle (CSV);
    /// hier stehen nur Ids, Orte, Ziele, Pflichtmechaniken, Belohnungen und Flags.
    /// </summary>
    public static partial class StoryBuilder
    {
        static readonly List<StoryChapter> _chapters = new List<StoryChapter>();
        static StoryChapter _ch;

        static void BuildChapters(StoryDatabase db)
        {
            _chapters.Clear();
            Chapter1(); Chapter2(); Chapter3(); Chapter4();
            ChaptersLater();
            db.Chapters = new List<StoryChapter>(_chapters);
        }

        static partial void ChaptersLater();

        // ------------------------------------------------------------ Helfer
        static StoryChapter Ch(int n, string title, string zone, int minLevel, string summary, params string[] mechanics)
        {
            var c = GetOrCreate<StoryChapter>(ChapterDir + "/Chapter_" + n.ToString("00") + ".asset");
            c.Number = n; c.Title = title; c.Zone = zone; c.MinLevel = minLevel; c.Summary = summary; c.Steps = new List<StoryStep>();
            c.JournalLines = new List<string> { "K" + n.ToString("00") + "_J" }; c.Mechanics = new List<string>(mechanics);
            UnityEditor.EditorUtility.SetDirty(c); _chapters.Add(c); _ch = c; return c;
        }

        static StoryStep St(string id, StepType type, string title, string zone, Vector3 pos, string desc)
        {
            var s = new StoryStep { Id = id, Type = type, Title = title, Zone = zone, Position = pos, Description = desc };
            _ch.Steps.Add(s); return s;
        }

        static StoryObjective O(ObjectiveKind k, string target, int count, string text, string hint = null) => new StoryObjective { Kind = k, Target = target, Count = count, Text = text, Hint = hint };

        static StoryStep Obj(this StoryStep s, params StoryObjective[] o) { s.Objectives.AddRange(o); return s; }
        static StoryStep Scenes(this StoryStep s, string start, string end) { s.SceneStart = start; s.SceneEnd = end; return s; }
        static StoryStep Reward(this StoryStep s, int xp, int karma, int copper, int heroPoints = 0, int mastery = 0) { s.Reward = new StoryReward { Experience = xp, Karma = karma, Copper = copper, HeroPoints = heroPoints, MasteryPoints = mastery }; return s; }
        static StoryStep Flags(this StoryStep s, params string[] f) { s.SetFlags.AddRange(f); return s; }
        static StoryStep When(this StoryStep s, StoryCondition c) { s.Condition = c; return s; }
        static StoryStep Npc(this StoryStep s, string n) { s.Npc = n; return s; }
        static StoryStep Letter(this StoryStep s, string l) { s.Letter = l; return s; }
        static StoryStep Cp(this StoryStep s, params int[] idx) { foreach (var i in idx) s.Checkpoints.Add(i.ToString()); return s; }
        static StoryStep Mate(this StoryStep s, string c) { s.Companion = c; return s; }
        static StoryStep R(this StoryStep s, float r) { s.Radius = r; return s; }
        static StoryStep Props(this StoryStep s, params string[] p) { s.Props = new List<string>(p); return s; }
        static StoryObjective At(this StoryObjective o, float x, float z) { o.Offset = new Vector3(x, 0, z); return o; }
        static StoryObjective L(this StoryObjective o, string label) { o.Label = label; return o; }
        static StoryObjective X(this StoryObjective o, float scale) { o.Scale = scale; return o; }
        static StoryObjective Sc(this StoryObjective o, string scene) { o.Scene = scene; return o; }
        static StoryObjective Opt(this StoryObjective o) { o.Optional = true; return o; }
        static StoryObjective With(this StoryObjective o, string partner) { o.Partner = partner; return o; }

        const string Raider = "Enemy_Trash_Pluenderer", Brute = "Enemy_Veteran_Brecher", Grolmok = "Enemy_Champion_Grolmok", Herold = "Enemy_Story_Herold";
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // ------------------------------------------------------------ Kapitel 1–4
        static void Chapter1()
        {
            Ch(1, "Rauch über Sonnfurt", "sonnfurt", 1, "Die Aschkrallen überfallen die Grenzmark. Der Held rettet, wen er kann, und folgt den Plünderern bis zu ihrem Lager.",
                "Bewegung, Kamera", "Fertigkeiten 1–5", "Ausweichen (Pflicht)", "Heilen", "Waffenwechsel", "Interaktion", "erstes Herz", "erste Wegmarke");
            var o = new[] { Origin.Hof, Origin.Posten, Origin.Ruinen };
            var where = new[] { V(-8, 1, -60), V(-70, 1, -10), V(58, 2, -55) };
            var title = new[] { "Feuer auf dem Hof", "Feuer an der Straße", "Besuch in den Ruinen" };
            var desc = new[] { "Die Aschkrallen zünden den Hof an. Hol die Tiere und die Nachbarn raus.", "Plünderer überfallen den Karren aus Sonnfurt. Halte die Straße.", "Plünderer graben in Elmhains Ruinen. Schütze Magister Elmar." };
            for (int i = 0; i < 3; i++)
                St("K01_S01" + "HPR"[i], StepType.Instance, title[i], "sonnfurt", where[i], desc[i]).When(new StoryCondition { Origin = o[i] })
                    .Obj(O(ObjectiveKind.Dodge, "*", 3, "Weiche drei Hieben aus", "dodge").With(Brute).At(0, 7).L("Rußklaue, Aschkrallen-Brecher"),
                         O(ObjectiveKind.Kill, Raider, 4, "Besiege die Plünderer", "skills").At(6, 4).L("Aschkrallen-Plünderer").X(0.6f),
                         O(ObjectiveKind.Heal, "*", 1, "Heile dich (6)", "heal"),
                         O(ObjectiveKind.Interact, "dorfbewohner", 3, "Bring die Leute in Sicherheit", "interact").At(-6, 6).L("Befreien"))
                    .Scenes("K01_S01", "K01_S01E").Cp(1, 2).R(40).Reward(250, 40, 100)
                    .Props(i == 0 ? new[] { "feuer@-6,-4", "feuer@5,-8", "feuer@-2,9", "rauch@-6,-4", "rauch@5,-8", "kisten@8,3" }
                         : i == 1 ? new[] { "barrikade@0,-6", "barrikade@4,8", "feuer@-5,2", "kisten@3,-2", "rauch@-5,2" }
                         : new[] { "zelt@-7,5", "kisten@6,-3", "feuer@2,8", "kisten@-4,-6" });
            St("K01_S02", StepType.OpenWorld, "Miras Liste", "sonnfurt", V(-60, 1, -60), "Der Weiler braucht Hilfe. Mira führt Buch – hilf ihr beim Herz „Sonnfurt sichern“.").Npc("mira")
                .Obj(O(ObjectiveKind.Heart, "heart_sonnfurt_mira", 1, "Hilf Mira, Sonnfurt zu sichern (Herz)", "heart"),
                     O(ObjectiveKind.Waypoint, "wp_", 1, "Entdecke eine Wegmarke", "waypoint"),
                     O(ObjectiveKind.WeaponSwap, "*", 1, "Wechsle einmal die Waffe", "swap"))
                .Scenes(null, "K01_S02E").Reward(300, 50, 150);
            St("K01_S03", StepType.Instance, "Das Lager in der Rabenschanze", "sonnfurt", V(58, 2, 58), "Die Spuren führen zur Rabenschanze. Hol Joss und die Zwillinge zurück.").Mate("mira")
                .Obj(O(ObjectiveKind.Kill, Raider, 5, "Besiege die Lagerwachen").At(0, 6),
                     O(ObjectiveKind.Interact, "dorfbewohner", 3, "Befreie die Gefangenen").At(-8, -6).L("Fesseln lösen"),
                     O(ObjectiveKind.Boss, Brute, 1, "Besiege den Lageranführer").At(4, 10).L("Rußzahn, Lageranführer").X(2.5f))
                .Scenes("K01_S03", "K01_S03E").Cp(0, 1).R(45).Reward(500, 80, 300, 1).Letter("L_K01")
                .Props("zelt@-8,8", "zelt@6,12", "kaefig@-8,-6", "kaefig@-11,-3", "feuer@0,4", "barrikade@0,-10");
        }

        static void Chapter2()
        {
            Ch(2, "Die Wacht von Dornwacht", "sonnfurt", 5, "Brennar nimmt den Helden in die Wacht auf. Die Verteidigung der Palisade wird zur Bewährung, Grolmok nennt sterbend den Herold.",
                "Ereignisse und Medaillen", "Am Boden / Wiederbeleben", "Kombo-Felder", "Inventar und Ausrüstung", "Beute");
            St("K02_S01", StepType.Talk, "Die Einladung", "sonnfurt", V(2, 2, 2), "Hauptmann Brennar wartet am Grenzposten Dornwacht.").Npc("brennar")
                .Obj(O(ObjectiveKind.Talk, "brennar", 1, "Sprich mit Hauptmann Brennar")).Scenes("K02_S01", null).Reward(200, 30, 100);
            St("K02_S02", StepType.OpenWorld, "Die Palisade", "sonnfurt", V(5, 2, 5), "Die Aschkrallen greifen Dornwacht an. Kämpf mit den Wächtern – und nutze Feld und Finisher.")
                .Obj(O(ObjectiveKind.Event, "dornwacht", 1, "Verteidige Dornwacht (Ereignis)", "event"),
                     O(ObjectiveKind.Combo, "*", 1, "Löse eine Kombo aus", "combo").Opt())
                .Scenes(null, "K02_S02E").Reward(400, 60, 200);
            St("K02_S03", StepType.Instance, "Grolmoks Lager", "sonnfurt", V(55, 3, 55), "Mit Mira und den Wächtern zu Grolmoks Lager. Niemand bleibt liegen.").Mate("mira")
                .Obj(O(ObjectiveKind.Kill, Raider, 4, "Durchbrich die Vorposten").At(0, 8),
                     O(ObjectiveKind.Revive, "mira", 1, "Belebe Mira wieder", "downed").At(-4, 4).Sc("K02_S03M"),
                     O(ObjectiveKind.Equip, "*", 1, "Lege ein Beutestück an (I)", "equip").Opt(),
                     O(ObjectiveKind.Boss, Grolmok, 1, "Besiege Grolmok").At(6, 14).L("Grolmok, Aschenfürst").X(1.2f))
                .Scenes("K02_S03", "K02_S03E").Cp(0, 1).R(50).Reward(700, 100, 500, 1)
                .Props("zelt@-6,16", "zelt@10,16", "zelt@12,6", "barrikade@0,4", "barrikade@-6,8", "feuer@2,12", "feuer@-4,-2", "kaefig@8,-4", "kisten@-10,10");
            St("K02_S04", StepType.Talk, "Ein Name", "sonnfurt", V(2, 2, 2), "Berichte Brennar von Grolmoks letzten Worten.").Npc("brennar")
                .Obj(O(ObjectiveKind.Talk, "brennar", 1, "Berichte Hauptmann Brennar")).Scenes("K02_S04", null).Reward(300, 50, 200).Letter("L_K02").Flags("kennt_herold");
        }

        static void Chapter3()
        {
            Ch(3, "Flüstern im Moor", "nebelmoor", 12, "Spuren des Herolds führen ins Nebelmoor. Tovin deutet das Siegel, der Held rettet ein Fischerdorf aus dem Giftnebel.",
                "Skimmer", "Eigenschaften", "Zustände reinigen", "Herzen mit Bündeln", "Späher", "Händler");
            St("K03_S01", StepType.Travel, "Ins Nebelmoor", "nebelmoor", V(14, 2, -352), "Reise ins Nebelmoor und sprich mit einem Späher am Torfsteg.").Npc("merle")
                .Obj(O(ObjectiveKind.Reach, "K03_S01", 1, "Erreiche den Torfsteg"), O(ObjectiveKind.Scout, "*", 1, "Sprich mit einem Späher", "scout"))
                .Scenes(null, "K03_S01E").Reward(600, 60, 300);
            St("K03_S02", StepType.OpenWorld, "Merles Fähre", "nebelmoor", V(300, 2, -95), "Fährfrau Merle braucht Hilfe, bevor sie dir einen Skimmer leiht.").Npc("merle")
                .Obj(O(ObjectiveKind.Heart, "heart_moor_merle", 1, "Unterstütze Fährfrau Merle (Herz)", "bundle"),
                     O(ObjectiveKind.Mount, "Skimmer", 1, "Steig auf einen Skimmer", "mount"))
                .Scenes("K03_S02", "K03_S02E").Reward(800, 80, 400);
            St("K03_S03", StepType.Talk, "Der Gelehrte", "nebelmoor", V(190, 4, 160), "Auf der Insel der Irrlichter sucht ein Gelehrter nach Spuren.").Npc("tovin")
                .Obj(O(ObjectiveKind.Talk, "tovin", 1, "Sprich mit Tovin"), O(ObjectiveKind.Trait, "*", 1, "Wähle eine Eigenschaft (H)", "traits"))
                .Scenes("K03_S03", null).Reward(700, 80, 400, 1);
            St("K03_S04", StepType.Instance, "Das Fischerdorf", "nebelmoor", V(330, 3, -60), "Der Giftnebel kriecht nach Krötenfurt. Hol die Leute raus und reinige dich.").Mate("tovin")
                .Obj(O(ObjectiveKind.Cleanse, "*", 2, "Reinige zwei Zustände", "cleanse"),
                     O(ObjectiveKind.Interact, "dorfbewohner", 4, "Hol die Fischer aus dem Nebel").At(-8, 6).L("Fischer stützen"),
                     O(ObjectiveKind.Kill, "Enemy_Moor_Schleicher", 5, "Vertreibe die Moorschleicher").At(6, -6))
                .Scenes("K03_S04", "K03_S04E").Cp(0, 1).R(45).Reward(1200, 120, 800, 1).Letter("L_K03").Flags("kennt_siegel")
                .Props("rauch@-6,2", "rauch@6,-4", "kisten@4,6", "kisten@-8,-6", "feuer@0,10");
        }

        static void Chapter4()
        {
            Ch(4, "Der Pass der Stürme", "frostkamm", 20, "Die Wacht zieht gegen den Herold zum Pass. Der Frostschlund wird geweckt, Brennar hält den Pass und fällt.",
                "Springer", "Gleiten", "Weltboss-Zyklus und Vor-Events", "Trotzleiste (Pflicht)", "Heldenherausforderungen");
            St("K04_S01", StepType.Travel, "Über die Firnpforte", "frostkamm", V(24, 7, -410), "Reise in den Frostkamm. Nutze Aufwinde und Gleiter, um voranzukommen.").Npc("runa")
                .Obj(O(ObjectiveKind.Reach, "K04_S01", 1, "Erreiche die Firnpforte"), O(ObjectiveKind.Glide, "*", 1, "Gleite ein Stück", "glide"), O(ObjectiveKind.Mount, "Springer", 1, "Steig auf einen Springer", "mount").Opt())
                .Scenes(null, "K04_S01E").Reward(1500, 100, 600);
            St("K04_S02", StepType.Talk, "Lagebesprechung", "frostkamm", V(120, 21, -60), "Brennar wartet am Kammpass.").Npc("brennar")
                .Obj(O(ObjectiveKind.Talk, "brennar", 1, "Sprich mit Brennar am Kammpass"), O(ObjectiveKind.Event, "wb_", 1, "Hilf bei den Vor-Ereignissen des Frostschlunds", "worldboss"))
                .Scenes("K04_S02", null).Reward(1500, 120, 800);
            St("K04_S03", StepType.Instance, "Der Pass", "frostkamm", V(60, 14, 300), "Der Herold steht am Siegel unter dem Frostschlund.").Mate("brennar")
                .Obj(O(ObjectiveKind.Kill, "Enemy_Frost_Eiswolf", 4, "Brich durch die Eiswölfe").At(0, -8),
                     O(ObjectiveKind.Defiance, Herold, 1, "Brich die Trotzleiste des Herolds", "defiance").At(0, 8).L("Der Herold").X(2f),
                     O(ObjectiveKind.Survive, "*", 20, "Halte stand"))
                .Scenes("K04_S03", "K04_S03E").Cp(0, 1).R(45).Reward(2500, 200, 1500, 2).Flags("brennar_gefallen")
                .Props("barrikade@-4,-8", "barrikade@5,-6", "feuer@0,-12", "kisten@-8,2");
            St("K04_S04", StepType.Talk, "Die erste Zeile", "frostkamm", V(-240, 9, -270), "Mira wartet im Bergsteigerlager.").Npc("mira")
                .Obj(O(ObjectiveKind.Talk, "mira", 1, "Sprich mit Mira")).Scenes("K04_S04", null).Reward(1000, 150, 1000).Letter("L_K04");
        }
    }
}
