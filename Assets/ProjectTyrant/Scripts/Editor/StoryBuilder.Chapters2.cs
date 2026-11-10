using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 6 / M31 + M33: Kapitel 5–10 (Bündnis-Wahl, Bündnis-Instanzen in K06, Leitstern in K07, Herold 2 in K08, Vision und Griffon in K09, Finale und Epilog in K10).</summary>
    public static partial class StoryBuilder
    {
        const string Smuggler = "Enemy_Kueste_Schmuggler", SmugQueen = "Enemy_Kueste_Schmugglerkoenigin", Construct = "Enemy_Kueste_Archivwaechter",
            Thorn = "Enemy_Wald_Dornenschreck", Priest = "Enemy_Wald_Schattenpriester", DuneRaider = "Enemy_Duene_Raeuber", Glass = "Enemy_Duene_Glaselementar",
            Crawler = "Enemy_Tiefe_Kristallkrabbler", BlindHunter = "Enemy_Tiefe_BlinderJaeger", SkyPirate = "Enemy_Himmel_Pirat",
            Guard = "Enemy_Feste_Garde", Knight = "Enemy_Feste_Ritter", Breaker = "Enemy_Feste_Siegelbrecher", Vareth = "Enemy_Story_Vareth";

        static partial void ChaptersLater() { Chapter5(); Chapter6(); Chapter7(); Chapter8(); Chapter9(); Chapter10(); ChaptersExpansions(); }   // Lauf 11: Kapitel 11–26

        static void Chapter5()
        {
            Ch(5, "Asche und Eid", "gluetoede", 30, "Drei Bündnisse werben um den Helden. Der Sturm über der Glutöde wird zur Bewährung, am Ende steht die Bündnis-Wahl.",
                "Level-Cap", "Beherrschungen", "Meta-Phasen", "Elite-Spezialisierung", "Herabstufung");
            St("K05_S01", StepType.Talk, "Drei Werber", "gluetoede", V(0, 10, 60), "In der Glutschmiede warten Haldis, Tovin und eine Fremde namens Veyra.").Npc("haldis")
                .Obj(O(ObjectiveKind.Talk, "haldis", 1, "Sprich mit den Werbern in der Glutschmiede"), O(ObjectiveKind.Mastery, "*", 1, "Setze einen Beherrschungspunkt ein (Heldenfenster)", "mastery").Opt())
                .Scenes("K05_S01", null).Reward(2000, 150, 1000, 0, 1);
            St("K05_S02", StepType.OpenWorld, "Sturm über der Glutöde", "gluetoede", V(10, 10, 40), "Halte mit den Bündnissen die Glutschmiede während des Meta-Ereignisses.").Npc("haldis")
                .Obj(O(ObjectiveKind.Meta, "meta_", 1, "Bestehe eine Phase des Meta-Ereignisses", "meta"), O(ObjectiveKind.Elite, "*", 1, "Schalte eine Elite-Spezialisierung frei (Heldenfenster)", "elite").Opt())
                .Scenes("K05_S02", "K05_S02E").Reward(2500, 200, 1200);
            St("K05_S03", StepType.Instance, "Die Aschwacht", "gluetoede", V(250, 7, -150), "Am Kraterrand lagern die Aschkrallen Siegelsplitter. Hol sie dir.").Mate("veyra")
                .Obj(O(ObjectiveKind.Kill, Raider, 6, "Besiege die Aschwacht").At(0, 8),
                     O(ObjectiveKind.Interact, "splitter", 3, "Birg die Siegelsplitter").At(-6, -4).L("Siegelsplitter"),
                     O(ObjectiveKind.Boss, Brute, 1, "Besiege den Splitterhüter").At(6, 12).L("Splitterhüter").X(3f))
                .Scenes("K05_S03", "K05_S03E").Cp(0, 1).R(45).Reward(3000, 250, 1500, 1)
                .Props("zelt@-8,10", "barrikade@0,4", "feuer@4,-6", "kisten@-6,-4", "rauch@8,8");
            St("K05_S04", StepType.Talk, "Der Eid", "gluetoede", V(0, 10, 60), "Entscheide, welchem Bündnis du dich anschließt.").Npc("mira")
                .Obj(O(ObjectiveKind.Talk, "mira", 1, "Triff deine Wahl in der Glutschmiede"))
                .Scenes("K05_S04", null).Reward(2000, 300, 2000).Letter("L_K05");
        }

        static void Chapter6()
        {
            Ch(6, "Der Hafen der hundert Segel", "silberkueste", 40, "Ankunft in Silbermund, Portalplatz und die Politik der Bündnisse. Jedes Bündnis hat einen eigenen Auftrag.",
                "Kontinentreise", "Hub-Dienste (Bank, Bündnis-Händler)", "Herzen in der Stadt", "Sprungrätsel");
            St("K06_S01", StepType.Travel, "Silbermund", "silberkueste", V(0, 2, 40), "Reise nach Silbermund und finde den Portalplatz.").Npc("gorran")
                .Obj(O(ObjectiveKind.Reach, "K06_S01", 1, "Erreiche den Portalplatz von Silbermund"), O(ObjectiveKind.Waypoint, "wp_kueste", 1, "Entdecke die Wegmarke Silbermund", "waypoint"))
                .Scenes(null, "K06_S01E").Reward(2000, 150, 1000);
            St("K06_S02W", StepType.Instance, "Sturm auf die Grotte", "silberkueste", V(-250, 2, -200), "Die Wacht räumt die Schmugglergrotte an der Steilküste.").When(new StoryCondition { Pact = Pact.Wacht }).Mate("haldis")
                .Obj(O(ObjectiveKind.Kill, Smuggler, 6, "Brich durch die Schmuggler").At(0, 8).Sc("K06_S02W"),
                     O(ObjectiveKind.Defiance, SmugQueen, 1, "Brich die Trotzleiste der Schmugglerkönigin", "defiance").At(4, 14).L("Die Schmugglerkönigin").X(2f))
                .Scenes("K06_S02", "K06_S02WE").Cp(0).R(45).Reward(3500, 300, 2000, 1).Flags("k06_grotte")
                .Props("kisten@-6,6", "kisten@6,4", "feuer@0,10", "barrikade@0,2");
            St("K06_S02A", StepType.Instance, "Einbruch im Archiv", "silberkueste", V(-80, 3, 60), "Hol mit Tovin das gestohlene Krönungsbuch zurück.").When(new StoryCondition { Pact = Pact.Archiv }).Mate("tovin")
                .Obj(O(ObjectiveKind.Interact, "regal", 3, "Durchsuche die Regale").At(-6, 4).L("Regal durchsuchen").Sc("K06_S02A"),
                     O(ObjectiveKind.Kill, Construct, 3, "Halte die Archivwächter auf").At(0, 8).L("Archivwächter"),
                     O(ObjectiveKind.Interrupt, "*", 2, "Unterbrich zwei Zauber", "interrupt"))
                .Scenes("K06_S02", "K06_S02AE").Cp(0, 1).R(40).Reward(3500, 300, 2000, 1).Flags("k06_buch")
                .Props("kisten@-8,0", "kisten@8,2");
            St("K06_S02S", StepType.Instance, "Der Doppelagent", "silberkueste", V(60, 2, 10), "Veyra stellt einem Doppelagenten im Hafen eine Falle.").When(new StoryCondition { Pact = Pact.Schleier }).Mate("veyra")
                .Obj(O(ObjectiveKind.Talk, "veyra", 1, "Lenke den Agenten ab (sprich mit Veyra)").Sc("K06_S02S"),
                     O(ObjectiveKind.Interact, "beweis", 2, "Stiehl die Beweise").At(-4, 6).L("Beweis an dich nehmen"),
                     O(ObjectiveKind.Kill, Smuggler, 4, "Schalte die Leibwächter aus").At(4, -4).L("Leibwächter"))
                .Scenes("K06_S02", "K06_S02SE").Cp(1).R(40).Reward(3500, 300, 2000, 1).Flags("k06_agent")
                .Props("kisten@-6,-4", "zelt@6,6");
            St("K06_S03", StepType.Talk, "Der Rat der Bündnisse", "silberkueste", V(-60, 3, 80), "Der Rat tagt im Bündnis-Viertel.").Npc("gorran")
                .Obj(O(ObjectiveKind.Talk, "gorran", 1, "Sprich mit dem Rat (Hafenmeister Gorran)"))
                .Scenes("K06_S03", null).Reward(2000, 300, 2000).Letter("L_K06");
        }

        static void Chapter7()
        {
            Ch(7, "Wurzeln der Finsternis", "daemmerwald", 50, "Die Verderbnis des Tyrannen frisst den Dämmerwald. Ein Dorf wird evakuiert, und der Leitstern des Helden kehrt zurück.",
                "Verderbnis-Zonen", "Eskorte", "Auto-Angriffs-Ketten", "Unterbrechen");
            St("K07_S01", StepType.OpenWorld, "Violettes Licht", "daemmerwald", V(-150, 3, -200), "Hilf Ältestem Fenn, die Verderbnis an den Wächtersteinen zurückzudrängen.").Npc("fenn")
                .Obj(O(ObjectiveKind.Event, "wald_", 1, "Dränge die Verderbnis zurück (Ereignis)", "event"), O(ObjectiveKind.Interrupt, "*", 2, "Unterbrich zwei Schattenpriester", "interrupt"))
                .Scenes("K07_S01", "K07_S01E").Reward(3000, 250, 1500);
            St("K07_S02", StepType.Instance, "Die Lichtung", "daemmerwald", V(-120, 3, -140), "Bring die Dorfbewohner der Lichtung in Sicherheit.").Mate("pact")
                .Obj(O(ObjectiveKind.Escort, "dorfbewohner", 1, "Geleite die Dorfbewohner zum Waldrand", "escort").At(0, 4).L("Dorfbewohner"),
                     O(ObjectiveKind.Kill, Thorn, 5, "Halte die Dornenschrecken fern").At(6, 10),
                     O(ObjectiveKind.Kill, Priest, 2, "Besiege die Schattenpriester").At(-6, 14).L("Schattenpriester"))
                .Scenes("K07_S02", "K07_S02E").Cp(0, 1).R(50).Reward(4000, 300, 2000, 1)
                .Props("feuer@6,-6", "rauch@6,-6", "kisten@-4,2", "zelt@-8,-4");
            St("K07_S03", StepType.OpenWorld, "Der Wächterstein", "daemmerwald", V(0, 5, 300), "Am alten Wächterstein wartet etwas aus deiner Vergangenheit.").Npc("fenn")
                .Obj(O(ObjectiveKind.Reach, "K07_S03", 1, "Erreiche den Wächterstein"), O(ObjectiveKind.Bundle, "laterne", 1, "Trage eine Laterne zum Stein", "bundle"))
                .Scenes("K07_S03", "K07_S03E").Reward(3000, 300, 2000).Letter("L_K07").Flags("k07_leitstern");
        }

        static void Chapter8()
        {
            Ch(8, "Das Lied der Dünen", "duenenmeer", 60, "Die Jagd nach dem Siegelschlüssel führt durch Sandstürme. Der Herold stellt eine Falle und entkommt.",
                "Raptor-Weitsprung", "Rennen (optional)", "Mehrphasen-Boss", "Gefährten");
            St("K08_S01", StepType.Travel, "Drei Schluchten", "duenenmeer", V(-200, 4, -100), "Reite mit Karawanenführerin Nadira zu den Ruinen von Qasir.").Npc("nadira")
                .Obj(O(ObjectiveKind.Talk, "nadira", 1, "Sprich mit Nadira an der Oase"), O(ObjectiveKind.Mount, "Raptor", 1, "Steig auf einen Raptor", "mount"), O(ObjectiveKind.Reach, "K08_S01", 1, "Erreiche die Ruinen von Qasir"))
                .Scenes("K08_S01", "K08_S01E").Reward(3000, 250, 1500);
            St("K08_S02", StepType.Instance, "Die Ruinen von Qasir", "duenenmeer", V(250, 6, 200), "Im Siegelgewölbe wartet der Herold.").Mate("pact")
                .Obj(O(ObjectiveKind.Kill, DuneRaider, 5, "Durchbrich die Wachen").At(0, 6),
                     O(ObjectiveKind.Defiance, Herold, 1, "Brich die Trotzleiste des Herolds", "defiance").At(0, 12).L("Der Herold").X(3f),
                     O(ObjectiveKind.Kill, Glass, 3, "Zerschlage die Glas-Elementare").At(6, 12),
                     O(ObjectiveKind.Survive, "*", 20, "Halte durch, bis der Sturm abflaut"))
                .Scenes("K08_S02", "K08_S02E").Cp(0, 1, 2).R(50).Reward(5000, 400, 3000, 2).Flags("herold_2")
                .Props("barrikade@-6,4", "kisten@6,-2", "feuer@0,-6");
            St("K08_S03", StepType.Talk, "Am Feuer", "duenenmeer", V(-200, 4, -100), "Berichte Nadira an der Oase.").Npc("nadira")
                .Obj(O(ObjectiveKind.Talk, "nadira", 1, "Sprich mit Nadira"))
                .Scenes("K08_S03", null).Reward(2500, 300, 2000).Letter("L_K08");
        }

        static void Chapter9()
        {
            Ch(9, "Unter Kristall, über Wolken", "kristalltiefe", 70, "Zwei Hälften des Schlüssels: in der Tiefe eine Vision des Tyrannen, in den Spitzen der Greif als Vertrauensbeweis.",
                "Dunkelheit und Licht", "Aufwinde", "Greif", "Einblicke");
            St("K09_S01", StepType.Instance, "Die Stimme in der Tiefe", "kristalltiefe", V(150, 2, 150), "Trag Licht in die Kristalltiefe und hol die erste Hälfte des Schlüssels.").Mate("pact")
                .Obj(O(ObjectiveKind.Bundle, "laterne", 1, "Nimm eine Laterne auf", "bundle").At(-4, 0),
                     O(ObjectiveKind.Kill, Crawler, 4, "Vertreibe die Kristallkrabbler").At(0, 8),
                     O(ObjectiveKind.Kill, BlindHunter, 2, "Besiege die blinden Jäger").At(6, 14).L("Blinder Jäger"),
                     O(ObjectiveKind.Interact, "schluessel", 1, "Nimm die Schlüsselhälfte").At(0, 18).L("Schlüsselhälfte"))
                .Scenes("K09_S01", "K09_S01E").Cp(1, 2).R(45).Reward(4000, 300, 2000, 1).Flags("schluessel_tiefe")
                .Props("kisten@-6,4", "zelt@8,-4");
            St("K09_S02", StepType.Travel, "Über den Wolken", "himmelsspitzen", V(0, 40, -300), "Reise in die Himmelsspitzen. Aufwinde tragen dich nach oben.").Npc("sela")
                .Obj(O(ObjectiveKind.Glide, "*", 1, "Gleite mit einem Aufwind", "glide"), O(ObjectiveKind.Reach, "K09_S02", 1, "Erreiche den Himmelswächter-Tempel"))
                .Scenes(null, "K09_S02E").Reward(3000, 250, 1500);
            St("K09_S03", StepType.Instance, "Die Greifeneier", "himmelsspitzen", V(-200, 30, 100), "Rette die Greifeneier vor den Himmelspiraten.").Mate("pact")
                .Obj(O(ObjectiveKind.Kill, SkyPirate, 6, "Vertreibe die Himmelspiraten").At(0, 8),
                     O(ObjectiveKind.Interact, "ei", 3, "Bring die Greifeneier ins Nest").At(-6, 4).L("Greifenei tragen"))
                .Scenes("K09_S03", "K09_S03E").Cp(0).R(45).Reward(4000, 300, 2000, 1).Flags("greif_vertrauen")
                .Props("kisten@4,6", "zelt@-8,8");
            St("K09_S04", StepType.Talk, "Der Schlüssel", "himmelsspitzen", V(0, 50, 250), "Sela setzt den Schlüssel zusammen.").Npc("sela")
                .Obj(O(ObjectiveKind.Talk, "sela", 1, "Sprich mit Sela im Tempel"), O(ObjectiveKind.Mount, "Griffon", 1, "Steig auf deinen Greif", "mount"))
                .Scenes("K09_S04", null).Reward(3000, 400, 3000, 0, 1).Letter("L_K09").Flags("schluessel_ganz", "tyrannenfeste_offen");
        }

        static void Chapter10()
        {
            Ch(10, "Der Tyrann erwacht", "tyrannenfeste", 80, "Finale in drei Instanzen: Sturm auf die Feste, der Herold, Vareths Versiegelung. Epilog in Silbermund an Brennars Grab.",
                "Reittier-Wechsel", "Gleiten", "Trotzleiste", "Meta-Finale", "Gefährte");
            St("K10_S01", StepType.Instance, "Drei Tore", "tyrannenfeste", V(0, 6, -300), "Stürme mit den Bündnissen die drei Tore der Feste.").Mate("pact")
                .Obj(O(ObjectiveKind.Kill, Guard, 6, "Erstes Tor: Herolds-Garde").At(-8, 6).L("Herolds-Garde"),
                     O(ObjectiveKind.Kill, Breaker, 3, "Zweites Tor: Siegelbrecher").At(8, 12).L("Siegelbrecher"),
                     O(ObjectiveKind.Boss, Knight, 1, "Drittes Tor: Verderbter Ritter").At(0, 18).L("Verderbter Torritter").X(3f))
                .Scenes("K10_S01", "K10_S01E").Cp(0, 1, 2).R(55).Reward(5000, 400, 3000, 1)
                .Props("barrikade@-8,2", "barrikade@8,8", "feuer@0,4", "feuer@-6,14", "rauch@0,4");
            St("K10_S02", StepType.Instance, "Der Herold", "tyrannenfeste", V(0, 8, 0), "Im Hof wartet der Herold ein letztes Mal.").Mate("pact")
                .Obj(O(ObjectiveKind.Defiance, Herold, 1, "Brich die Trotzleiste des Herolds", "defiance").At(0, 8).L("Der Herold").X(4f),
                     O(ObjectiveKind.Kill, Guard, 4, "Halte seine Garde auf").At(6, 4),
                     O(ObjectiveKind.Boss, Herold, 1, "Besiege den Herold").At(0, 8).L("Der Herold").X(4f))
                .Scenes("K10_S02", "K10_S02E").Cp(0, 1).R(45).Reward(6000, 500, 4000, 2).Flags("herold_gefallen")
                .Props("feuer@-8,0", "feuer@8,0", "rauch@-8,0");
            St("K10_S03", StepType.Instance, "Die Versiegelung", "tyrannenfeste", V(0, 12, 250), "Versiegle Vareth im Thronsaal.").Mate("pact")
                .Obj(O(ObjectiveKind.Interact, "siegel", 3, "Entzünde die drei Siegel").At(0, 6).L("Siegel entzünden"),
                     O(ObjectiveKind.Defiance, Vareth, 1, "Brich Vareths Trotzleiste", "defiance").At(0, 12).L("Vareth, der Tyrann").X(5f),
                     O(ObjectiveKind.Survive, "*", 25, "Halte die Siegel, bis sie sich schließen"))
                .Scenes("K10_S03", "K10_S03E").Cp(0, 1).R(45).Reward(8000, 800, 6000, 3, 2).Flags("vareth_versiegelt")
                .Props("feuer@-10,8", "feuer@10,8");
            St("K10_S04", StepType.Talk, "Erste Zeile", "silberkueste", V(40, 3, 120), "In Silbermund wartet Mira an Brennars Grab.").Npc("mira")
                .Obj(O(ObjectiveKind.Talk, "mira", 1, "Sprich mit Mira an Brennars Grab"))
                .Scenes("K10_S04", null).Reward(3000, 1000, 10000, 0, 3).Letter("L_K10").Flags("story_abgeschlossen");
        }
    }
}
