using System.Collections.Generic;
using ProjectTyrant.Landmarks;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Welt-Lauf W1: Wahrzeichen-Plan je Zone (Abschnitt 7) als Daten. Farben/Höhen/Namen; Geometrie und Platz bestimmt der Planer.</summary>
    public static partial class WorldPlanner
    {
        public class Lm
        {
            public LandmarkKind Kind; public string Name; public float Height; public Color Prim, Acc; public string Lore; public Traversal Top;
        }

        public class ZSpec
        {
            public string Id; public Lm Main; public Lm[] Sec = new Lm[0];
            public TerrainOpKind Signature; public float ClimbHeight;
            public HazardKind[] Hazards = new HazardKind[0];
            public string Figure; public string[] Scenes; public bool Mountain;
        }

        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static Lm L(LandmarkKind k, string name, float h, string prim, string acc, string lore, Traversal top = Traversal.Griffon | Traversal.Skyscale)
            => new Lm { Kind = k, Name = name, Height = h, Prim = C(prim), Acc = C(acc), Lore = lore, Top = top };
        static ZSpec Z(string id, Lm main, Lm[] sec, TerrainOpKind sig, float climb, HazardKind[] hz, string fig, string[] scenes, bool mountain = false)
            => new ZSpec { Id = id, Main = main, Sec = sec, Signature = sig, ClimbHeight = climb, Hazards = hz, Figure = fig, Scenes = scenes, Mountain = mountain };

        const LandmarkKind Cat = LandmarkKind.Cathedral, Pyr = LandmarkKind.Pyramid, Cit = LandmarkKind.Citadel, Pag = LandmarkKind.Pagoda, Tree = LandmarkKind.GiantTree,
            Col = LandmarkKind.Colossus, Light = LandmarkKind.Lighthouse, Ice = LandmarkKind.GlacierPalace, Sunk = LandmarkKind.SunkenCity, Sky = LandmarkKind.SkyIsland, Mas = LandmarkKind.Massif;
        const HazardKind Sand = HazardKind.Quicksand, Tox = HazardKind.ToxicWater, Lava = HazardKind.Lava, IceW = HazardKind.IceWater, Abyss = HazardKind.Abyss,
            Steep = HazardKind.SteepWall, Wind = HazardKind.WindWall, Portal = HazardKind.SandPortal, Thorn = HazardKind.ThornHedge, Corr = HazardKind.CorruptionField;
        static readonly string[] Wild = { "Lager", "Schlachtfeld", "Grab" }, Desert = { "Karawane", "Grab", "Schlachtfeld" }, Coast = { "Wrack", "Grab", "Lager" }, Jade = { "Wrack", "Lager", "Grab" };

        public static readonly Dictionary<string, ZSpec> Specs = Build();

        static Dictionary<string, ZSpec> Build()
        {
            var list = new List<ZSpec>
            {
                Z("sonnfurt", L(Light, "Leuchtfeuerturm Sonnfurt", 60, "#b9a27a", "#ffcf6a", "Die Siedler entzündeten das Leuchtfeuer in der ersten Nacht nach dem Aschenwinter. Seitdem brennt es jede Nacht, damit Heimkehrer Sonnfurt finden."),
                    new[] { L(Cat, "Ruinenkathedrale von Elmhain", 45, "#8f8a80", "#d8c48a", "Elmhain betete zu einem Licht, das nie antwortete. Als der Turm einstürzte, blieb nur die Krypta – und die Dornen wuchsen um sie herum.") },
                    TerrainOpKind.Peak, 40, new[] { Thorn, Abyss }, "Mira", Wild),
                Z("nebelmoor", L(Cat, "Versunkene Kathedrale", 90, "#6f7a72", "#9ee07a", "Die Kathedrale sank in einer einzigen Nacht. Die Glocken läuten noch, sagen die Fischer, wenn der Nebel tief hängt."),
                    new[] { L(Tree, "Pilzhain-Dom", 45, "#a0503a", "#f0d8a0", "Riesenpilze, älter als das Moor selbst. Ihre Kronen tragen das Gewicht von Menschen – wenn man weiß, wo man springt.") },
                    TerrainOpKind.Basin, 150, new[] { Tox, Tox }, "Tovin", Wild),
                Z("frostkamm", L(Cit, "Eiszitadelle", 120, "#c8d8e8", "#7ab8ff", "Die Zitadelle wurde gegen einen Feind gebaut, der nie kam. Ihre Wächter erfroren auf den Zinnen, den Blick nach Norden."),
                    new[] { L(Ice, "Gletscherpalast", 45, "#a8d8f0", "#e8f8ff", "Unter dem Gletscher liegt ein Palast aus Eis. Wer seine Brücken überquert, hört das Eis knacken – und manchmal Stimmen.") },
                    TerrainOpKind.Massif, 220, new[] { IceW, Wind, Steep }, "Brennar", Wild, true),
                Z("gluetoede", L(Pyr, "Obsidianpyramide", 100, "#2a2228", "#ff6a2a", "Schwarzes Glas, aus Lava gegossen von einem Volk ohne Namen. In der Spitze glüht ein Kristall, der nicht erlischt."),
                    new[] { L(Ice, "Glutkristall-Nadeln", 45, "#ff7a3a", "#ffd080", "Die Nadeln wuchsen aus der Erde, als die Glutöde noch Wald war. Sie singen, wenn der heiße Wind durch sie fährt.") },
                    TerrainOpKind.Mesa, 160, new[] { Lava, Wind }, "Kael", Desert),
                Z("silberkueste", L(Light, "Leuchtturm Silbermund", 70, "#e8e0d0", "#ffd870", "Der Leuchtturm ist älter als der Hafen. Die Seefahrer sagen, er habe die Stadt gerufen, nicht umgekehrt."),
                    new[] { L(Cat, "Klippenkathedrale der Seefahrer", 55, "#d0c8b8", "#6ab0e0", "Jeder Kapitän, der nicht heimkehrte, hat hier eine Kerze. Es brennen mehr Kerzen als Schiffe im Hafen liegen.") },
                    TerrainOpKind.CliffCoast, 150, new[] { Abyss, Steep }, "Mira", Coast),
                Z("daemmerwald", L(Cat, "Dämmerkathedrale", 140, "#4a4458", "#b080ff", "Wurzeln drangen durch den Boden der Kathedrale, als die Verderbnis kam. Die Mönche blieben – in der Krypta, heißt es, wachen sie noch."),
                    new[] { L(Tree, "Hängende Gärten", 55, "#4a6a3a", "#e0a0ff", "Die Gärten hingen einst über dem Kreuzgang. Jetzt hängen sie über dem Nichts.") },
                    TerrainOpKind.Peak, 160, new[] { Thorn, Corr, Thorn }, "Tovin", Wild, true),
                Z("duenenmeer", L(Pyr, "Pyramide des Sandkönigs", 90, "#d8b878", "#4ad0e0", "Drei Könige, drei Pyramiden – und nur einer wollte gefunden werden. Seine Pyramide ist die höchste, sein Treibsand der tiefste."),
                    new[] { L(Pyr, "Pyramide der Sandkönigin", 60, "#b89058", "#e0a040", "Die Königin ließ ihre Pyramide neben die ihres Gemahls setzen – zwanzig Meter niedriger, aber mit dem schöneren Blick."), L(Sunk, "Versunkene Oasenstadt", 30, "#c8a070", "#60c0b0", "Die Oase trocknete aus, dann kam der Sand. Die Dächer ragen noch heraus wie Inseln.") },
                    TerrainOpKind.Peak, 160, new[] { Sand, Wind }, "Nadira", Desert),
                Z("kristalltiefe", L(Cat, "Kristallkathedrale", 90, "#7ac0e0", "#e0f8ff", "Niemand hat sie gebaut. Die Kristalle wuchsen so, wie Menschen beten – nach oben, dem Licht entgegen."),
                    new[] { L(Ice, "Kristallbrücken", 40, "#a0e0ff", "#ffffff", "Die Brücken tragen nur den, der nicht zögert. Wer stehen bleibt, hört das Kristall singen.") },
                    TerrainOpKind.Massif, 170, new[] { Abyss, Steep }, "Tovin", Wild, true),
                Z("himmelsspitzen", L(Sky, "Wolkenthron", 220, "#e8e8f0", "#9ad0ff", "Ein Tempel, der nie den Boden berührte. Die Greifen kennen den Weg – die Menschen nur die Legende.", Traversal.Griffon),
                    new[] { L(Light, "Windorgel-Nadel", 50, "#c0c8d8", "#ffffff", "Der Wind spielt auf der Nadel eine Melodie, die niemand aufgeschrieben hat. Die Himmelsschuppen nisten in ihren Pfeifen.") },
                    TerrainOpKind.Peak, 300, new[] { Wind, Abyss, Steep }, "Veyra", Wild, true),
                Z("tyrannenfeste", L(Cit, "Thronturm", 150, "#3a3038", "#ff4a3a", "Vom Thronturm aus sah der Tyrann bis zum Meer. Jetzt sieht man von ihm aus nur Asche – und das Siegel, das ihn hält."),
                    new[] { L(Ice, "Siegelkammern", 35, "#5a3a7a", "#c080ff", "Jede Kammer hält ein Siegel, jedes Siegel einen Namen. Einer der Namen ist ausgekratzt.") },
                    TerrainOpKind.Mesa, 160, new[] { Corr, Lava }, "Herold", Wild),
                Z("weite_von_ahrn", L(Col, "Liegender Steinriese", 100, "#9a9080", "#e0c070", "Der Riese legte sich zur Ruhe, als die Nomaden kamen. Sie bauen ihre Feuer zwischen seinen Fingern – und erzählen ihm Geschichten."),
                    new[] { L(Cit, "Nomaden-Hochlager", 30, "#a07850", "#e04a2a", "Das Hochlager liegt dort, wo kein Pferd hinkommt. Die Nomaden mögen es so.") },
                    TerrainOpKind.Mesa, 150, new[] { Abyss, Steep }, "Daro", Wild),
                Z("nebelkamm", L(Cat, "Nebelkathedrale", 85, "#7a8088", "#c0e0d0", "Die Ruine steht offen wie eine Hand. Im Nebel hört man Gesang – keiner weiß, wer singt."),
                    new[] { L(Mas, "Großer Steinkreis", 40, "#8a8a80", "#a0e0ff", "Neun Steine, neun Wächter. Der zehnte Stein fehlt – er liegt im Moor, sagt man.") },
                    TerrainOpKind.Mesa, 170, new[] { Tox, Steep }, "Mei", Wild, true),
                Z("sturmbucht", L(Cat, "Klippenkathedrale über dem Meer", 110, "#c8c0b0", "#5aa0d0", "Die Kathedrale lehnt sich über die Klippe wie ein Seemann über die Reling. Bei Sturm läuten ihre Glocken von selbst."),
                    new[] { L(Sunk, "Wrack-Riff", 30, "#6a5040", "#e0c080", "Hundert Schiffe, ein Riff. Die Wracks liegen so dicht, dass man von Deck zu Deck springen kann.") },
                    TerrainOpKind.CliffCoast, 160, new[] { Abyss, Steep }, "Mira", Coast),
                Z("aschenkessel", L(Pyr, "Feuertempel im Krater", 70, "#5a3028", "#ff7a2a", "Die Priester bauten den Tempel auf die einzige Insel im Lavasee. Sie dachten, das Feuer würde sie schützen."),
                    new[] { L(Cit, "Ascheposten am Kraterrand", 30, "#6a5a50", "#ffb040", "Vom Posten aus sieht man den ganzen Kessel. Die Wachen tauschen jede Stunde – länger hält niemand die Hitze aus.") },
                    TerrainOpKind.Crater, 160, new[] { Lava, Wind }, "Kael", Desert),
                Z("gruener_schlund", L(Tree, "Weltenbaum", 200, "#5a4030", "#6ad04a", "Der Weltenbaum war schon alt, als die Berge jung waren. In seiner Krone lebt ein Dorf, das nie den Boden betreten hat."),
                    new[] { L(Cat, "Wurzeltempel", 40, "#6a6050", "#a0e070", "Die Wurzeln haben den Tempel nicht zerstört – sie haben ihn umarmt.") },
                    TerrainOpKind.Massif, 160, new[] { Thorn, Abyss }, "Ysa", Wild, true),
                Z("schilfmuendung", L(Pag, "Delta-Tempel auf Pfählen", 70, "#8a6a4a", "#e0c060", "Der Tempel steht auf tausend Pfählen. Jedes Jahr sinkt er eine Handbreit – und jedes Jahr setzen die Mönche einen Pfahl nach."),
                    new[] { L(Tree, "Mangroven-Kathedrale", 45, "#3a5a3a", "#c0e080", "Die Mangroven wuchsen zu Bögen, die Bögen zu einem Dach. Gebetet wird hier bei Ebbe.") },
                    TerrainOpKind.Delta, 150, new[] { Tox, Tox }, "Noam", Coast),
                Z("weisse_stille", L(Pyr, "Eispyramide", 120, "#d8ecf8", "#7ac0ff", "Die Pyramide ist nicht aus Eis gebaut – sie wurde vom Eis begraben. Ihre Spitze ist das Einzige, was der Gletscher freigab."),
                    new[] { L(Ice, "Gletscherpalast der Stille", 45, "#b0d8f0", "#ffffff", "Im Palast spricht niemand. Wer spricht, hört seine Stimme zehnmal zurück."), L(Light, "Heiligtum der Urskai", 30, "#e0e8f0", "#ffd070", "Die Urskai zünden hier ein Licht für jeden, der im Schnee blieb.") },
                    TerrainOpKind.Massif, 200, new[] { IceW, Wind, Steep }, "Brennar", Wild, true),
                Z("schaedelkueste", L(Cat, "Versunkene Kathedrale der alten Hauptstadt", 100, "#5a5a6a", "#a060e0", "Hier stand die Hauptstadt, bevor das Meer sie holte. Der Turm ragt noch heraus – und die Verderbnis steigt aus der Gruft."),
                    new[] { L(Sunk, "Schädelfelsen", 30, "#d0c8b0", "#6a4a8a", "Die Felsen sehen aus wie Schädel. Die Fischer meiden sie – nicht wegen der Form, sondern wegen der Stimmen.") },
                    TerrainOpKind.CliffCoast, 150, new[] { Tox, Corr, Abyss }, "Tovin", Coast),
                Z("sonnentor", L(Cit, "Oasenpalast", 70, "#e0c890", "#3ab0c0", "Der Palast wurde um die Quelle gebaut, nicht die Quelle um den Palast. Jeder Händler trinkt hier, bevor er feilscht."),
                    new[] { L(Light, "Palastturm", 45, "#e8d8b0", "#ff9a3a", "Vom Palastturm aus sieht man die Rennbahn – und wer betrügt.") },
                    TerrainOpKind.Mesa, 150, new HazardKind[0], "Nadira", Desert),
                Z("grosse_leere", L(Pyr, "Große Pyramide von Zarathis", 120, "#e8c888", "#ff5a2a", "Zarathis ließ die Pyramide inmitten von Treibsand bauen, damit niemand sie betritt, der nicht eingeladen ist. Eingeladen wurde nie jemand."),
                    new[] { L(Col, "Riesen-Skelett", 60, "#e8e0c8", "#c08040", "Der Riese starb im Sand und wurde nie begraben. Die Läufer jagen zwischen seinen Rippen.") },
                    TerrainOpKind.Peak, 160, new[] { Sand, Wind, Portal }, "Zarath", Desert),
                Z("klingenschlucht", L(Cat, "Felsbrücken-Kathedrale", 100, "#b07050", "#ffd080", "Der Wind formte die Bögen, die Priester nur die Fenster. Es ist die einzige Kathedrale, die nie ein Dach hatte."),
                    new[] { L(Cit, "Hochplateau-Festung", 40, "#906050", "#e04a2a", "Die Festung bewacht die Schlucht von oben. Ihre Mauern sind aus der Klippe selbst gehauen.") },
                    TerrainOpKind.Canyon, 170, new[] { Abyss, Wind, Portal }, "Ilyas", Desert, true),
                Z("spiegel_der_sonne", L(Pag, "Spiegeltempel", 70, "#f0e8d8", "#ffd040", "Der Tempel spiegelt sich im Salz so klar, dass Pilger zum falschen gingen – und nie ankamen."),
                    new[] { L(Sunk, "Versunkene Salzstadt", 30, "#e0d8c8", "#a0c0d0", "Die Stadt versank im Salz, nicht im Wasser. Die Häuser stehen noch, nur tiefer.") },
                    TerrainOpKind.Mesa, 150, new[] { Sand }, "Ilyas", Desert),
                Z("glutkessel", L(Pyr, "Flammentempel", 80, "#4a2a22", "#ff4a1a", "Der Flammenkönig ließ die Pyramidenbasis unter den alten Tempel schieben. Jetzt brennt das Feuer höher – und heißer."),
                    new[] { L(Light, "Glutturm", 40, "#5a3a30", "#ffa040", "Der Turm leitet die Hitze aus dem Kessel. Wer oben steht, spürt die Erde atmen.") },
                    TerrainOpKind.Crater, 160, new[] { Lava }, "Zarath", Desert),
                Z("zarathis", L(Pyr, "Thronpyramide", 110, "#d0b080", "#ff3a2a", "Vier Tore führen zur Spitze, und jedes verlangt etwas anderes. Zarath ließ sie so bauen, damit nur er sie alle besitzt."),
                    new[] { L(Cit, "Knochenmauer-Bastion", 40, "#e0d8c0", "#8a2a2a", "Die Mauer ist aus den Knochen derer gebaut, die sie angriffen. Die Bastion aus denen, die sie verteidigten.") },
                    TerrainOpKind.Terraces, 150, new[] { Sand, Lava, Portal }, "Zarath", Desert),
                Z("jadehafen", L(Pag, "Hafenpagode", 90, "#2a8a6a", "#80ffd0", "Die Pagode ist das erste, was Seefahrer von Jadehafen sehen – und das letzte, wenn sie gehen. Ihre Laternen brennen mit Jade-Licht."),
                    new[] { L(Cit, "Jade-Thron", 45, "#1a6a5a", "#ffd870", "Der Thron steht auf der höchsten Terrasse. Die Kaiserin sitzt selten darauf – sie bevorzugt die Werkstätten.") },
                    TerrainOpKind.Terraces, 150, new HazardKind[0], "Lin Sao", Jade),
                Z("gruene_flut", L(Cat, "Jadekathedrale", 120, "#3ab08a", "#c0ffe8", "Die Kathedrale war aus Stein, bis die Jade-Flut sie berührte. Jetzt ist sie aus Licht."),
                    new[] { L(Sunk, "Wrack des Riesenschiffs", 35, "#5a4a3a", "#80e0c0", "Das Schiff war zu groß für jeden Hafen. Also strandete es hier – mit Absicht, sagen manche.") },
                    TerrainOpKind.Peak, 150, new[] { Tox }, "Lin Sao", Jade),
                Z("fluesterhain", L(Pag, "Große Pagode", 110, "#8a3a2a", "#e0d080", "Neun Ebenen, neun Geister. Die Mönche sagen, die neunte Ebene gehöre niemandem – nicht einmal ihnen."),
                    new[] { L(Tree, "Riesenbambus", 55, "#5a8a3a", "#d0f0a0", "Der Bambus wächst einen Meter am Tag. Die Mönche haben aufgehört, die Pagode höher zu bauen.") },
                    TerrainOpKind.Massif, 180, new[] { Abyss, Steep }, "Mei", Jade, true),
                Z("sonnenstufen", L(Cit, "Kaiserpalast", 100, "#c04a3a", "#ffd060", "Der Palast steht auf der höchsten Stufe, damit die Kaiserin jedes Reisfeld sehen kann. Die Bauern sagen, sie zählt die Körner."),
                    new[] { L(Light, "Wachturm der Festungsmauer", 40, "#a08060", "#ff8a3a", "Die Mauer hält Piraten fern. Der Turm hält die Wachen wach.") },
                    TerrainOpKind.Terraces, 150, new[] { Abyss }, "Yuen", Jade),
                Z("nebelkueste", L(Col, "Drachenknochen-Bogen", 160, "#e8e0d0", "#80c0d0", "Die Knochen eines Drachen, älter als das Meer. Der Bogen ist sein Rückgrat – und manchmal knarrt er."),
                    new[] { L(Light, "Nebelturm", 50, "#a0a8b0", "#c0e0ff", "Der Turm warnt Schiffe vor dem Bogen. Die Schiffe, die ihn nicht hörten, liegen darunter.") },
                    TerrainOpKind.CliffCoast, 150, new[] { Abyss, Wind }, "Kael", Coast),
                Z("herz_der_flut", L(Sky, "Drachenarena", 90, "#2a7a6a", "#80ffe0", "Die Plattformen schweben über dem Meer, gehalten von Jade und Erinnerung. Hier erwacht der Drache, wenn er gerufen wird."),
                    new[] { L(Pag, "Flutschrein", 35, "#2a6a8a", "#ffe080", "Ein Schrein für die, die auf See blieben. Die Laternen schwimmen jede Nacht hinaus.") },
                    TerrainOpKind.Peak, 150, new[] { Abyss }, "Lin Sao", Jade),
            };
            var d = new Dictionary<string, ZSpec>();
            foreach (var z in list) d[z.Id] = z;
            return d;
        }
    }
}
