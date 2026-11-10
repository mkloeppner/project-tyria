using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 11: Zwischenspiel „Asche des Siegels“ (Kapitel 11–12), Erweiterung „Thron der Glut“ (13–19, Glutwüste) und „Jadeherz“ (20–26, Dracheninseln),
    /// alle auf Stufe 80. Verzweigungen: Kapitel 17 (Sturm/List → eigene Instanz, Ilyas’ Schicksal in 18), Kapitel 23 (Kaiserin/Daro), Kapitel 25 (Drache frei/Stadt gesichert → Epilog-Varianten).
    /// Jede Region führt ihre Mechaniken ein (Kopfgeld, Dünenläufer, Rennen, Luftspiegelungen, Jadegeist, Boot, Angeln, Geisterwege, Jade-Kanonen).
    /// </summary>
    public static partial class StoryBuilder
    {
        static string En(string id) => "Enemy_" + id;

        static void ChaptersExpansions()
        {
            // ---------------- Zwischenspiel ----------------
            Ch(11, "Risse im Siegel", "schaedelkueste", 80, "Verderbnis sickert in die Schädelküste. Ein versunkener Altar füttert das Siegel von außen.", "Regionen-Reise", "Verderbnis-Widerstand");
            St("K11_S01", StepType.Talk, "Schwarze Netze", "silberkueste", V(40, 3, 100), "Mira hat Neuigkeiten aus der Schädelküste.").Npc("mira")
                .Obj(O(ObjectiveKind.Talk, "mira", 1, "Sprich mit Mira", "region_travel")).Scenes("K11_S01", null).Reward(4000, 200, 2000);
            St("K11_S02", StepType.OpenWorld, "Die Toten am Strand", "schaedelkueste", V(0, 2, -200), "Untersuche die Schädelküste.")
                .Obj(O(ObjectiveKind.Kill, En("Kern_Untoter"), 6, "Vertreibe die Ertrunkenen", "corruption_res"), O(ObjectiveKind.Reach, "K11_S02", 1, "Erreiche den Schädelhafen"))
                .Scenes(null, "K11_S02E").Reward(5000, 300, 3000);
            St("K11_S03", StepType.Instance, "Der versunkene Altar", "schaedelkueste", V(-120, 2, 60), "Brich den Altar unter Wasser.").Mate("pact")
                .Obj(O(ObjectiveKind.Kill, En("Kern_Untoter"), 5, "Halte die Ertrunkenen auf").At(0, 8),
                     O(ObjectiveKind.Interact, "altar", 3, "Zerschlage die Altarsteine").At(0, 16).L("Altarstein"))
                .Scenes("K11_S03", "K11_S03E").Cp(0).R(45).Reward(6000, 400, 4000, 1).Letter("L_K11").Flags("siegel_risse");
            Ch(12, "Der Orden der Glut", "silberkueste", 80, "Tovin entdeckt, dass der Herold dem Orden der Glut diente. Mira organisiert eine Expedition nach Sonnentor.", "Regionen-Reise", "Schiffe");
            St("K12_S01", StepType.Talk, "Rollen aus dem Süden", "silberkueste", V(-60, 3, 80), "Tovin wartet im Archiv von Silbermund.").Npc("tovin")
                .Obj(O(ObjectiveKind.Talk, "tovin", 1, "Sprich mit Tovin")).Scenes("K12_S01", null).Reward(4000, 200, 2000);
            St("K12_S02", StepType.Travel, "Das Portal im Vorland", "tyrannenfeste", V(0, 6, -380), "Sieh dir das Portal im Vorland der Tyrannenfeste an.")
                .Obj(O(ObjectiveKind.Reach, "K12_S02", 1, "Erreiche das Wüstenportal"), O(ObjectiveKind.Waypoint, "*", 1, "Nutze eine Wegmarke", "region_travel"))
                .Scenes(null, "K12_S02E").Reward(4000, 200, 2000);
            St("K12_S03", StepType.Talk, "Abreise", "silberkueste", V(80, 2, -40), "Mira wartet am Hafen.").Npc("mira")
                .Obj(O(ObjectiveKind.Talk, "mira", 1, "Verabschiede dich von Mira")).Scenes("K12_S03", null).Reward(6000, 500, 5000, 1, 1).Letter("L_K12").Flags("expedition");

            // ---------------- Thron der Glut ----------------
            Ch(13, "Sand im Getriebe", "sonnentor", 80, "Ankunft in Sonnentor. Nadira nimmt den Helden mit; der Orden der Glut brandschatzt Karawanen.", "Kopfgeldtafel", "Hub");
            St("K13_S01", StepType.Travel, "Sonnentor", "sonnentor", V(0, 3, -250), "Reise nach Sonnentor.")
                .Obj(O(ObjectiveKind.Reach, "K13_S01", 1, "Erreiche den Großen Basar")).Scenes(null, "K13_S01E").Reward(5000, 300, 3000);
            St("K13_S02", StepType.Talk, "Nadira", "sonnentor", V(20, 3, -200), "Nadira erklärt die Kopfgeldtafel.").Npc("nadira")
                .Obj(O(ObjectiveKind.Talk, "nadira", 1, "Sprich mit Nadira", "bounty")).Scenes("K13_S02", null).Reward(4000, 200, 2000);
            St("K13_S03", StepType.OpenWorld, "Die erste Jagd", "sonnentor", V(60, 3, -120), "Nimm einen Kopfgeldauftrag an und jage die Kultisten.")
                .Obj(O(ObjectiveKind.Interact, "kopfgeldtafel", 1, "Nimm einen Auftrag an der Kopfgeldtafel an"), O(ObjectiveKind.Kill, En("Wueste_Flammenkultist"), 6, "Besiege Flammenkultisten"))
                .Scenes(null, "K13_S03E").Reward(7000, 500, 5000, 1).Letter("L_K13");
            Ch(14, "Der Sandläufer", "grosse_leere", 80, "Nadiras Stamm zähmt Dünenläufer. Flucht vor einem Sandsturm.", "Dünenläufer", "Sandportale", "Sandsturm");
            St("K14_S01", StepType.Talk, "Der Stamm", "grosse_leere", V(-100, 4, -300), "Nadiras Stamm lagert an der Riesenrippe.").Npc("nadira")
                .Obj(O(ObjectiveKind.Talk, "nadira", 1, "Sprich mit Nadira")).Scenes("K14_S01", null).Reward(4000, 200, 2000);
            St("K14_S02", StepType.Instance, "Mit den Läufern", "grosse_leere", V(-60, 4, -200), "Lauf mit den Dünenläufern, bis einer dich wählt.").Mate("nadira")
                .Obj(O(ObjectiveKind.Interact, "laeufer", 3, "Fange die Läufer ein").At(0, 10).L("Dünenläufer"),
                     O(ObjectiveKind.Mount, "DuneRunner", 1, "Sitz auf deinem Dünenläufer auf", "dunerunner"))
                .Scenes(null, "K14_S02E").Cp(0).R(55).Reward(7000, 400, 4000, 1).Flags("duenenlaeufer");
            St("K14_S03", StepType.OpenWorld, "Vor dem Sturm", "grosse_leere", V(80, 4, 0), "Reite vor dem Sandsturm davon.")
                .Obj(O(ObjectiveKind.Reach, "K14_S03", 1, "Erreiche den Nomadenbrunnen"), O(ObjectiveKind.Survive, "*", 30, "Überstehe den Sturm"))
                .Scenes(null, "K14_S03E").Reward(6000, 400, 4000).Letter("L_K14");
            Ch(15, "Spiegel der Sonne", "spiegel_der_sonne", 80, "Luftspiegelungen führen zum Archiv des Ordens. Der Sonnenpriester Ilyas wird zum zweifelnden Gefährten.", "Luftspiegelungen", "Rennen");
            St("K15_S01", StepType.Talk, "Ilyas", "spiegel_der_sonne", V(-80, 2, -250), "Ein Priester am Salzspiegel.").Npc("ilyas")
                .Obj(O(ObjectiveKind.Talk, "ilyas", 1, "Sprich mit Ilyas", "mirage")).Scenes("K15_S01", null).Reward(4000, 200, 2000);
            St("K15_S02", StepType.OpenWorld, "Lichtsteine", "spiegel_der_sonne", V(40, 2, -60), "Finde die Spiegelung an den Lichtsteinen.")
                .Obj(O(ObjectiveKind.Interact, "lichtstein", 3, "Schau von den Lichtsteinen zum Horizont"), O(ObjectiveKind.Event, "spiegel_der_sonne_angriff", 1, "Wehre den Angriff der Sandgeister ab", "race").Opt())
                .Scenes(null, "K15_S02E").Reward(6000, 300, 3000);
            St("K15_S03", StepType.Instance, "Das Archiv", "spiegel_der_sonne", V(120, 2, 160), "Dring in das Archiv des Ordens ein.").Mate("ilyas")
                .Obj(O(ObjectiveKind.Kill, En("Wueste_Knochenkonstrukt"), 3, "Besiege die Archivwächter").At(0, 10).L("Knochenkonstrukt"),
                     O(ObjectiveKind.Interact, "archiv", 1, "Lies die Rolle des Ordens").At(0, 18).L("Ordensrolle"))
                .Scenes("K15_S03", "K15_S03E").Cp(0).R(45).Reward(8000, 500, 5000, 1).Letter("L_K15").Flags("archiv_ilyas");
            Ch(16, "Der Flammenkönig", "glutkessel", 80, "Zarath, der sich zum Flammenkönig krönte, will Vareths Macht tragen. Erste Konfrontation im Flammentempel.", "Meta-Teilnahme");
            St("K16_S01", StepType.Talk, "Die Belagerung", "glutkessel", V(-60, 4, -300), "Nadira wartet vor dem Flammentempel.").Npc("nadira")
                .Obj(O(ObjectiveKind.Talk, "nadira", 1, "Sprich mit Nadira")).Scenes("K16_S01", null).Reward(4000, 200, 2000);
            St("K16_S02", StepType.OpenWorld, "Die Tore halten", "glutkessel", V(0, 4, -100), "Nimm an der Belagerung des Flammentempels teil.")
                .Obj(O(ObjectiveKind.Event, "glutkessel_flammentempel_p1", 1, "Schließe die erste Phase der Belagerung ab"))
                .Scenes(null, "K16_S02E").Reward(8000, 600, 6000, 1);
            St("K16_S03", StepType.Instance, "Im Haus der Flamme", "glutkessel", V(40, 4, 120), "Stell dich Zarath im Tempel.").Mate("ilyas")
                .Obj(O(ObjectiveKind.Kill, En("Wueste_Flammenkultist"), 5, "Besiege die Tempelwache").At(0, 8),
                     O(ObjectiveKind.Survive, "*", 25, "Halte durch, bis der Tempel einstürzt"))
                .Scenes("K16_S03", "K16_S03E").Cp(0).R(45).Reward(8000, 500, 5000, 1).Letter("L_K16").Flags("zarath_geflohen");
            Ch(17, "Knochenmauer", "klingenschlucht", 80, "Der Weg nach Zarathis: Nadiras Sturm oder Ilyas’ List – jede Wahl mit eigener Instanz.", "Greif- und Himmelsschuppen-Route");
            St("K17_S01", StepType.Talk, "Sturm oder List", "klingenschlucht", V(-40, 6, -280), "Entscheide, wie ihr die Knochenmauer überwindet.").Npc("nadira")
                .Obj(O(ObjectiveKind.Talk, "nadira", 1, "Berate dich mit Nadira und Ilyas")).Scenes("K17_S01", null).Reward(4000, 200, 2000);
            St("K17_S02A", StepType.Instance, "Sturm auf die Mauer", "klingenschlucht", V(20, 6, 0), "Reite mit Nadiras Stamm gegen die Knochenmauer.").Mate("nadira").When(new StoryCondition { Flag = "weg_sturm" })
                .Obj(O(ObjectiveKind.Kill, En("Wueste_Knochenkonstrukt"), 3, "Brich die Mauerwächter").At(0, 10), O(ObjectiveKind.Kill, En("Wueste_Flammenkultist"), 6, "Treib den Kult von der Mauer").At(6, 16))
                .Scenes("K17_S02A", null).Cp(0).R(55).Reward(8000, 500, 5000, 1);
            St("K17_S02B", StepType.Instance, "Das Tor der Priester", "klingenschlucht", V(-20, 6, 20), "Schleich mit Ilyas durch das Tor der Priester.").Mate("ilyas").When(new StoryCondition { Flag = "weg_list" })
                .Obj(O(ObjectiveKind.Interact, "losung", 2, "Gib die Losung an den Toren").At(0, 8).L("Tor"), O(ObjectiveKind.Kill, En("Wueste_Flammenpriester"), 1, "Schalte den Torpriester aus").At(0, 16))
                .Scenes("K17_S02B", null).Cp(0).R(55).Reward(8000, 500, 5000, 1);
            St("K17_S03", StepType.Travel, "Über die Schlucht", "klingenschlucht", V(60, 30, 300), "Flieg über die Schlucht zur Stadt.")
                .Obj(O(ObjectiveKind.Reach, "K17_S03", 1, "Erreiche den Blick auf Zarathis"), O(ObjectiveKind.Glide, "*", 1, "Gleite oder fliege über die Schlucht").Opt())
                .Scenes(null, "K17_S03E").Reward(6000, 400, 4000).Letter("L_K17");
            Ch(18, "Die Verlorene Stadt", "zarathis", 80, "Die Stadt einnehmen. Im Ordens-Archiv: Vareth war Zaraths Vorgänger. Ilyas opfert sich oder wird gerettet.", "Finale-Meta");
            St("K18_S01", StepType.OpenWorld, "Vier Viertel", "zarathis", V(-80, 6, -300), "Nimm am Fall von Zarathis teil.").Npc("nadira")
                .Obj(O(ObjectiveKind.Event, "zarathis_zarathis_p1", 1, "Durchbrich die Knochenmauer (Meta, Phase 1)"))
                .Scenes("K18_S01", null).Reward(9000, 600, 6000, 1);
            St("K18_S02", StepType.Instance, "Das Archiv der Gefäße", "zarathis", V(40, 6, 60), "Durchsuche das Archiv des Ordens.").Mate("ilyas")
                .Obj(O(ObjectiveKind.Interact, "gefaesse", 1, "Lies die Liste der Gefäße").At(0, 10).L("Liste"), O(ObjectiveKind.Survive, "*", 20, "Entkomme dem einstürzenden Archiv"))
                .Scenes("K18_S02", "K18_S02E").Cp(0).R(45).Reward(9000, 600, 6000, 1);
            St("K18_S03", StepType.Talk, "Der Gedenkstein", "zarathis", V(60, 6, 80), "Nadira wartet am eingestürzten Archiv.").Npc("nadira")
                .Obj(O(ObjectiveKind.Talk, "nadira", 1, "Sprich mit Nadira am Gedenkstein")).Scenes("K18_S03", null).Reward(5000, 400, 4000).Letter("L_K18");
            Ch(19, "Thron der Glut", "zarathis", 80, "Zarath auf dem Thron: drei Fenster-Leisten, Muster des Flammenkönigs. Epilog in Sonnentor mit Nadira.");
            St("K19_S01", StepType.Instance, "Der Thron", "zarathis", V(0, 8, 300), "Stell dich Zarath auf dem Thron.").Mate("nadira")
                .Obj(O(ObjectiveKind.Defiance, En("Champion_Flammenkoenig"), 1, "Brich Zaraths Trotzleiste", "defiance").At(0, 10).L("Zarath, der Flammenkönig").X(4f),
                     O(ObjectiveKind.Kill, En("Wueste_Flammenpriester"), 2, "Besiege seine Priester").At(6, 6),
                     O(ObjectiveKind.Boss, En("Champion_Flammenkoenig"), 1, "Besiege Zarath").At(0, 10).L("Zarath, der Flammenkönig").X(4f))
                .Scenes("K19_S01", "K19_S01E").Cp(0, 1).R(50).Reward(12000, 800, 8000, 2, 2).Flags("zarath_besiegt");
            St("K19_S02", StepType.Talk, "Datteln und Briefe", "sonnentor", V(20, 3, -200), "Sonnentor feiert.").Npc("nadira")
                .Obj(O(ObjectiveKind.Talk, "nadira", 1, "Sprich mit Nadira")).Scenes("K19_S02", null).Reward(6000, 600, 6000);
            St("K19_S03", StepType.Talk, "Das Schiff nach Osten", "sonnentor", V(-40, 2, -260), "Nadira begleitet dich zum Kai.").Npc("nadira")
                .Obj(O(ObjectiveKind.Talk, "nadira", 1, "Verabschiede dich am Kai")).Scenes("K19_S03", null).Reward(4000, 400, 4000, 0, 2).Letter("L_K19").Flags("glut_abgeschlossen");

            // ---------------- Jadeherz ----------------
            Ch(20, "Jadehafen", "jadehafen", 80, "Ankunft per Schiff. Lin Sao baut den Jadegeist; Kaiserin Yuen empfängt den Helden misstrauisch.", "Jadegeist", "Vertikalstadt");
            St("K20_S01", StepType.Travel, "Ankunft", "jadehafen", V(0, 3, -250), "Reise mit dem Schiff nach Jadehafen.")
                .Obj(O(ObjectiveKind.Reach, "K20_S01", 1, "Erreiche die Hafenebene", "region_travel")).Scenes(null, "K20_S01E").Reward(5000, 300, 3000);
            St("K20_S02", StepType.Talk, "Die Werkstatt", "jadehafen", V(40, 10, -120), "Lin Sao baut dir einen Jadegeist.").Npc("linsao")
                .Obj(O(ObjectiveKind.Talk, "linsao", 1, "Sprich mit Lin Sao", "jadebot")).Scenes("K20_S02", null).Reward(6000, 300, 3000, 1).Flags("jadegeist");
            St("K20_S03", StepType.Talk, "Die Kaiserin", "jadehafen", V(0, 30, 160), "Kaiserin Yuen empfängt dich in der Oberstadt.").Npc("yuen")
                .Obj(O(ObjectiveKind.Talk, "yuen", 1, "Sprich mit der Kaiserin")).Scenes("K20_S03", null).Reward(6000, 400, 4000).Letter("L_K20");
            Ch(21, "Grüne Flut", "gruene_flut", 80, "Die Jade-Technik bezieht Kraft aus einem gefangenen Meeresdrachen; Fischer berichten von Beben.", "Boot", "Angeln");
            St("K21_S01", StepType.Talk, "Am Ufer", "gruene_flut", V(-120, 2, -300), "Lin Sao erklärt Boot und Angel.").Npc("linsao")
                .Obj(O(ObjectiveKind.Talk, "linsao", 1, "Sprich mit Lin Sao", "boat")).Scenes("K21_S01", null).Reward(4000, 200, 2000);
            St("K21_S02", StepType.OpenWorld, "Fang und Flucht", "gruene_flut", V(-40, 2, -150), "Ruf dein Boot und wirf die Angel aus.")
                .Obj(O(ObjectiveKind.Interact, "boot", 1, "Ruf dein Boot", "boat"), O(ObjectiveKind.Interact, "angelstelle", 3, "Fange Fische an den Schwärmen", "fishing"))
                .Scenes(null, "K21_S02E").Reward(6000, 400, 4000);
            St("K21_S03", StepType.OpenWorld, "Gestohlene Kerne", "gruene_flut", V(80, 2, 40), "Hol die gestohlenen Jade-Kerne von den Piraten zurück.")
                .Obj(O(ObjectiveKind.Kill, En("Jade_Pirat"), 6, "Besiege die Jade-Piraten"), O(ObjectiveKind.Event, "gruene_flut_angriff", 1, "Schlage den Piratenangriff zurück").Opt())
                .Scenes(null, "K21_S03E").Reward(7000, 500, 5000, 1).Letter("L_K21");
            Ch(22, "Flüsterhain", "fluesterhain", 80, "Das Kloster der Schwester Mei kennt den alten Vertrag mit dem Drachen.", "Geisterwege");
            St("K22_S01", StepType.Talk, "Schwester Mei", "fluesterhain", V(-60, 4, -260), "Im Kloster der Stille wartet Schwester Mei.").Npc("mei")
                .Obj(O(ObjectiveKind.Talk, "mei", 1, "Sprich mit Schwester Mei", "spiritpath")).Scenes("K22_S01", null).Reward(4000, 200, 2000);
            St("K22_S02", StepType.OpenWorld, "Geisterwege", "fluesterhain", V(20, 4, -60), "Folge den Geisterwegen.")
                .Obj(O(ObjectiveKind.Interact, "geisterweg", 3, "Folge den Lichtern der Geisterwege")).Scenes(null, "K22_S02E").Reward(6000, 400, 4000);
            St("K22_S03", StepType.Instance, "Das besetzte Kloster", "fluesterhain", V(80, 4, 160), "Befreie das Kloster.").Mate("mei")
                .Obj(O(ObjectiveKind.Kill, En("Jade_Geisterwesen"), 6, "Beruhige die Geisterwesen").At(0, 10), O(ObjectiveKind.Interact, "vertrag", 1, "Öffne den Vertrag").At(0, 18).L("Vertrag"))
                .Scenes("K22_S03", "K22_S03E").Cp(0).R(45).Reward(8000, 500, 5000, 1).Letter("L_K22").Flags("vertrag_gelesen");
            Ch(23, "Sonnenstufen", "sonnenstufen", 80, "Kaiserliche Festung: mit der Kaiserin oder mit den Abtrünnigen des Hauptmanns Daro.", "Kaiserliche Patrouillen");
            St("K23_S01", StepType.Talk, "Zwei Stimmen", "sonnenstufen", V(-40, 8, -280), "Kaiserin und Hauptmann fordern deine Hilfe.").Npc("yuen")
                .Obj(O(ObjectiveKind.Talk, "yuen", 1, "Entscheide dich")).Scenes("K23_S01", null).Reward(4000, 200, 2000);
            St("K23_S02A", StepType.Instance, "Der Reisspeicher", "sonnenstufen", V(20, 8, 0), "Hol den Reisspeicher für die Kaiserin zurück.").Mate("pact").When(new StoryCondition { Flag = "pakt_kaiserin" })
                .Obj(O(ObjectiveKind.Kill, En("Jade_Abtruenniger"), 6, "Vertreibe Daros Leute").At(0, 10)).Scenes("K23_S02A", null).Cp(0).R(45).Reward(8000, 500, 5000, 1);
            St("K23_S02B", StepType.Instance, "Die Festung bei Nacht", "sonnenstufen", V(-20, 8, 30), "Hol mit Daro die gestohlenen Kerne aus der Festung.").Mate("pact").When(new StoryCondition { Flag = "pakt_daro" })
                .Obj(O(ObjectiveKind.Kill, En("Jade_Konstrukt"), 2, "Schalte die Festungskonstrukte aus").At(0, 10), O(ObjectiveKind.Interact, "kerne", 3, "Nimm die Kerne").At(4, 16).L("Jade-Kern"))
                .Scenes("K23_S02B", null).Cp(0).R(45).Reward(8000, 500, 5000, 1);
            St("K23_S03", StepType.Talk, "Die Spur der Kerne", "sonnenstufen", V(60, 8, 120), "Lin Sao wertet die Kerne aus.").Npc("linsao")
                .Obj(O(ObjectiveKind.Talk, "linsao", 1, "Sprich mit Lin Sao")).Scenes("K23_S03", null).Reward(5000, 300, 3000).Letter("L_K23");
            Ch(24, "Nebelküste", "nebelkueste", 80, "Kael, Schüler des Herolds, sabotiert die Jade-Kerne; der Drache regt sich.", "Weltboss als Story-Schritt");
            St("K24_S01", StepType.Talk, "Kael", "nebelkueste", V(-80, 4, -260), "Eine Gestalt im Nebel erwartet dich.").Npc("kael")
                .Obj(O(ObjectiveKind.Talk, "kael", 1, "Stell Kael zur Rede")).Scenes("K24_S01", null).Reward(4000, 200, 2000);
            St("K24_S02", StepType.OpenWorld, "Die Schlange im Nebel", "nebelkueste", V(0, 4, 0), "Stell dich der Nebelschlange.")
                .Obj(O(ObjectiveKind.Event, "nebelkueste_nebelschlange", 1, "Besiege die Nebelschlange (Weltboss)")).Scenes(null, "K24_S02E").Reward(9000, 700, 7000, 1);
            St("K24_S03", StepType.Instance, "Sabotierte Kerne", "nebelkueste", V(80, 4, 160), "Sichere Kaels zurückgelassene Kerne.").Mate("pact")
                .Obj(O(ObjectiveKind.Kill, En("Jade_Pirat"), 6, "Besiege Kaels Söldner").At(0, 8), O(ObjectiveKind.Interact, "kern", 3, "Sichere die Kerne").At(0, 16).L("Sabotierter Kern"))
                .Scenes(null, "K24_S03E").Cp(0).R(45).Reward(8000, 500, 5000, 1).Letter("L_K24");
            Ch(25, "Das Jadeherz", "jadehafen", 80, "Kernkammer unter Jadehafen: den Drachen befreien oder die Stadt sichern.", "Jade-Kanonen");
            St("K25_S01", StepType.Instance, "Die Kernkammer", "jadehafen", V(0, 3, 0), "Halte Kael in der Kernkammer auf.").Mate("linsao")
                .Obj(O(ObjectiveKind.Interact, "kanone", 3, "Lade die Jade-Kanonen").At(0, 8).L("Jade-Kanone"), O(ObjectiveKind.Defiance, En("Jade_Konstrukt"), 1, "Brich das Wächterkonstrukt", "defiance").At(0, 14).L("Wächterkonstrukt").X(3f))
                .Scenes("K25_S01", null).Cp(0).R(45).Reward(9000, 600, 6000, 1);
            St("K25_S02", StepType.Talk, "Die Entscheidung", "jadehafen", V(0, 30, 160), "Entscheide über den Drachen.").Npc("yuen")
                .Obj(O(ObjectiveKind.Talk, "yuen", 1, "Triff die Entscheidung")).Scenes("K25_S02", null).Reward(6000, 500, 5000);
            St("K25_S03", StepType.Talk, "Vor dem Erwachen", "jadehafen", V(40, 10, -100), "Lin Sao prüft die flackernden Leitungen.").Npc("linsao")
                .Obj(O(ObjectiveKind.Talk, "linsao", 1, "Sprich mit Lin Sao")).Scenes("K25_S03", null).Reward(4000, 400, 4000).Letter("L_K25");
            Ch(26, "Herz der Flut", "herz_der_flut", 80, "Finale: der Meeresdrache erwacht; je nach Wahl Bindung oder Befreiung. Epilog mit Lin Sao, Mira per Brief.", "Finale-Meta", "Boot-Anfahrt");
            St("K26_S01", StepType.OpenWorld, "Die Plattformen", "herz_der_flut", V(-60, 2, -280), "Nimm am Erwachen des Meeresdrachen teil.").Npc("linsao")
                .Obj(O(ObjectiveKind.Event, "herz_der_flut_drachenende_p1", 1, "Sichere die vier Plattformen (Meta, Phase 1)")).Scenes("K26_S01", null).Reward(9000, 600, 6000, 1);
            St("K26_S02", StepType.Instance, "Der Meeresdrache", "herz_der_flut", V(0, 2, 200), "Stell dich Kael und dem Drachen.").Mate("linsao")
                .Obj(O(ObjectiveKind.Defiance, En("Champion_Meeresdrache"), 1, "Brich die Trotzleiste des Drachen", "defiance").At(0, 12).L("Der Meeresdrache").X(5f),
                     O(ObjectiveKind.Interact, "kanone", 2, "Feuere die Jade-Kanonen").At(6, 6).L("Jade-Kanone"),
                     O(ObjectiveKind.Survive, "*", 25, "Halte der Flut stand"))
                .Scenes("K26_S02", "K26_S02E").Cp(0, 1).R(55).Reward(14000, 1000, 10000, 3, 3).Flags("drache_finale");
            St("K26_S03", StepType.Talk, "Lichter", "jadehafen", V(40, 10, -120), "Lin Sao wartet in der Werkstatt.").Npc("linsao")
                .Obj(O(ObjectiveKind.Talk, "linsao", 1, "Sprich mit Lin Sao")).Scenes("K26_S03", null).Reward(8000, 1500, 15000, 0, 3).Letter("L_K26").Flags("jade_abgeschlossen");
        }
    }
}
