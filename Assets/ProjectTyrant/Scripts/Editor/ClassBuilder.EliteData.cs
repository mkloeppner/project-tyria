using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 5 / M26: die 16 Elite-Spezialisierungen als Daten (Namen sind eigene Erfindungen).</summary>
    public static partial class ClassBuilder
    {
        static void BuildElites(ClassLibrary lib)
        {
            var r = lib.Get("wildhueter"); var w = lib.Get("elementarweber"); var t = lib.Get("schattenklinge"); var g = lib.Get("waechter");
            // Reihenfolge wie im Auftrag: die 12 der neuen Klassen zuerst, je Klasse eine, dann die zweite usw.; danach der Wächter.
            if (r != null) { Begin("wildhueter", r.Color, VfxPalette.Natur); r.EliteSpecs = new List<EliteSpecDefinition> { Hainwaechter(), Bestienbund(), Dornenwaechter(), Sturmfalke() }; }
            if (w != null) { Begin("elementarweber", w.Color, VfxPalette.Arkan); var sf = w.SetFor(WeaponKind.ScepterFocus); w.EliteSpecs = new List<EliteSpecDefinition> { Sturmrufer(sf), Weber(sf), Katalysator(), Glutbinder(sf) }; }
            if (t != null) { Begin("schattenklinge", t.Color, VfxPalette.Schatten); t.EliteSpecs = new List<EliteSpecDefinition> { Wegelagerer(), Phantomtaenzer(), Schattenhueter(), Klingenwirbel() }; }
            if (g != null) { Begin("waechter", g.Color, VfxPalette.Licht); g.EliteSpecs = new List<EliteSpecDefinition> { Glutrichter(), Bollwerkmeister(), Sturmherold(), Dornenschild() }; }
            _style = null; _spec = null;
        }

        // ================================================================== Wildhüter
        static EliteSpecDefinition Hainwaechter()
        {
            var e = BeginSpec("hainwaechter", "Hainwächter", new Color(0.78f, 0.88f, 1f), VfxPalette.Mondlicht, new Theme(EffectType.Cripple, EffectType.Regeneration, Attr.HealingPower, Attr.Concentration, Attr.Vitality, ComboFieldType.Water),
                EliteMechanic.Hainform, "Hainform", "F5 Hainform: die Leiste füllt sich durch Heilung (und langsam im Kampf); voll → 15 s Avatar mit 5 Heil- und Unterstützungs-Skills (F5 beendet früher).",
                UtilityCategory.Glyph, "Heilung, Mondlicht und Wurzeln – Stab, Hainform, Glyphen.", "CastChannel");
            e.Weapon = EWeapon("Hainstab", WeaponKind.Staff, WeaponVisual.Staff, "Staff", true, 24f, true, new[] { "Mondstrahl", "Blütenschauer", "Wurzelsprung", "Silberwelle", "Lichtung", "", "", "" });
            ETypeSkills(UtilityCategory.Glyph, new[] { "Glyphe des Mondtaus", "Glyphe der Wurzeln", "Glyphe des Hains", "Glyphe der Lichtung", "Glyphe des Erwachens" });
            _style = "Staff";
            var hf = new[] { ("Mondsegen", "Heilt Verbündete im Umkreis, Regeneration."), ("Wurzelnetz", "Ranken immobilisieren Gegner um dich."), ("Blütenwirbel", "Reinigt 2 Zustände bei Verbündeten, Schutz."), ("Silberschleier", "Stabilität und Aegis für Verbündete."), ("Sternenlichtung", "Starker Heilkreis, belebt Verbündete auf (Wasserfeld).") };
            for (int i = 0; i < 5; i++)
            {
                var s = EM("HF" + (i + 1), hf[i].Item1, SkillDelivery.Self, 0.5f, 3f + 3f * i, 0f, 0.2f, "Hainform: " + hf[i].Item2);
                s.Radius = 7f; s.ShareWithAllies = true; s.HealBase = 1400f + 300f * i; s.HealCoefficient = 0.35f;
                if (i == 0) s.SelfBuffs.Add(B(EffectType.Regeneration, 1, 6f));
                if (i == 1) { s.TargetBuffs.Add(B(EffectType.Immobilize, 1, 2f)); s.Coefficient = 0.5f; }
                if (i == 2) { s.CleanseCount = 2; s.SelfBuffs.Add(B(EffectType.Protection, 1, 4f)); }
                if (i == 3) { s.SelfBuffs.Add(B(EffectType.Stability, 2, 4f)); s.SelfBuffs.Add(B(EffectType.Aegis, 1, 4f)); }
                if (i == 4) { s.HealBase = 4200f; s.Field = ComboFieldType.Water; s.FieldDuration = 4f; }
            }
            ELine(new[] { "Mondtau", "Wurzelkraft", "Hainruhe", "Stab der Lichtung", "Glyphenkunde", "Erwachter Hain", "Fesselnde Ranken", "Silbersplitter", "Ewiger Frühling", "Letztes Licht", "Tanzende Blüten", "Wildwuchs des Hains" });
            EndSpec("hainwaechter_HF5"); return e;
        }

        static EliteSpecDefinition Bestienbund()
        {
            var e = BeginSpec("bestienbund", "Bestienbund", new Color(0.78f, 0.38f, 0.2f), VfxPalette.Wildblut, new Theme(EffectType.Bleeding, EffectType.Might, Attr.Power, Attr.Ferocity, Attr.Precision),
                EliteMechanic.Verschmelzen, "Verschmelzen", "F5 Verschmelzen: der Gefährte wird Teil von dir – kein aktiver Gefährte, dafür +150 Kraft/Präzision, +100 Wildheit; F1/F2 setzen die Familien-Skills beider Gefährten ein, F3 Raserei. F5 trennt wieder.",
                UtilityCategory.Stance, "Wildheit und Blut – Dolch & Kriegshorn, Verschmelzen, Haltungen.", "Cast1H");
            e.Weapon = EWeapon("Dolch & Kriegshorn", WeaponKind.DaggerWarhorn, WeaponVisual.DaggerWarhorn, "Dagger", false, 3f, false, new[] { "Fangzahn", "Rudelwirbel", "Raubsprung", "Kehlbiss", "Jagdgeheul", "Krallenriss", "Blutrausch", "" });
            ETypeSkills(UtilityCategory.Stance, new[] { "Haltung des Lagers", "Haltung der Pirsch", "Haltung des Rudels", "Haltung des Felsens", "Haltung der Urbestie" });
            _style = "Dagger";
            var ra = EM("BB_Raserei", "Raserei", SkillDelivery.Self, 0.25f, 20f, 0f, 0f, "Verschmolzen (F3): Schnelligkeit, Wut und 5 Macht für 6 s.");
            ra.SelfBuffs.Add(B(EffectType.Quickness, 1, 4f)); ra.SelfBuffs.Add(B(EffectType.Fury, 1, 6f)); ra.SelfBuffs.Add(B(EffectType.Might, 5, 6f));
            ELine(new[] { "Geteiltes Blut", "Rudelinstinkt", "Wilde Klauen", "Dolchmeister", "Haltungswechsel", "Ruf des Bundes", "Blutspur", "Klauenhieb", "Zorn der Bestie", "Witterung", "Pirschschritt", "Urinstinkt" });
            EndSpec("bestienbund_BB_Raserei"); return e;
        }

        static EliteSpecDefinition Dornenwaechter()
        {
            var e = BeginSpec("dornenwaechter", "Dornenwächter", new Color(0.5f, 0.9f, 0.2f), VfxPalette.Dorngift, new Theme(EffectType.Poison, EffectType.Protection, Attr.Toughness, Attr.ConditionDamage, Attr.Power, ComboFieldType.None),
                EliteMechanic.Entfesseln, "Entfesseln", "F5 Entfesseln: die Verstärkung wechselt zwischen dir (+10 % Schaden, Skills 1–2 werden zu Dornenhieb und Rankensprung) und dem Gefährten (10 Macht, Wut). Entfesselungs-Rufe lassen den Gefährten Kontrolle einsetzen.",
                UtilityCategory.Unleash, "Gift und Ranken – Hammer, Entfesseln, Entfesselungs-Rufe.", "Cast2H");
            e.Weapon = EWeapon("Rankenhammer", WeaponKind.Hammer, WeaponVisual.Hammer, "Hammer", false, 3.5f, false, new[] { "Dornenschlag", "Rankenwirbel", "Wurzelsturz", "Giftwurz", "Dornenfeld", "Stachelhieb", "Giftschlag", "" });
            ETypeSkills(UtilityCategory.Unleash, new[] { "Ruf der Wurzeln", "Ruf der Dornen", "Ruf des Dickichts", "Ruf der Ranken", "Ruf des Urwalds" });
            _style = "Hammer";
            var h = EM("DW_Hieb", "Dornenhieb", SkillDelivery.Melee, 0.5f, 0f, 3.5f, 1.1f, "Entfesselt: schwerer Hieb, 2 Gift."); h.TargetBuffs.Add(B(EffectType.Poison, 2, 5f));
            var j = EM("DW_Sprung", "Rankensprung", SkillDelivery.Leap, 0.3f, 6f, 10f, 1.2f, "Entfesselt: Sprung zum Ziel, Ranken immobilisieren (1,5 s)."); j.LeapDistance = 10f; j.Finisher = ComboFinisherType.Leap; j.TargetBuffs.Add(B(EffectType.Immobilize, 1, 1.5f));
            ELine(new[] { "Dornenhaut", "Giftsaft", "Wurzelstand", "Hammerschmied", "Lauter Ruf", "Entfesselte Wut", "Giftdorn", "Stachelkrone", "Schutzborke", "Würgeranke", "Zähe Wurzel", "Dickicht" });
            EndSpec("dornenwaechter_W5"); return e;
        }

        static EliteSpecDefinition Sturmfalke()
        {
            var e = BeginSpec("sturmfalke", "Sturmfalke", new Color(0.55f, 0.82f, 1f), VfxPalette.Himmel, new Theme(EffectType.Vulnerability, EffectType.Fury, Attr.Precision, Attr.Power, Attr.Ferocity),
                EliteMechanic.Markieren, "Markieren", "F5 Markieren: das Ziel erhält 5 Verwundbarkeit, der Gefährte greift es an; dein nächster Skill 1 wird zum aufgeladenen Präzisionsschuss (15 s).",
                UtilityCategory.Mark, "Federn und Zielstrahl – Gewehr, Markieren, Markierungen.", "Bow_Shoot");
            e.Weapon = EWeapon("Falkengewehr", WeaponKind.Rifle, WeaponVisual.Rifle, "Bow", true, 30f, false, new[] { "Federschuss", "Salve", "Sprungschuss", "Fesselschuss", "Sturmhagel", "", "", "" });
            ETypeSkills(UtilityCategory.Mark, new[] { "Zeichen der Rast", "Zeichen der Beute", "Zeichen der Schwäche", "Zeichen des Blendens", "Zeichen des Sturms" });
            _style = "Bow";
            var p = EM("SF_Praezision", "Präzisionsschuss", SkillDelivery.Projectile, 1.0f, 0f, 36f, 2.6f, "Aufgeladener Schuss auf das markierte Ziel; durchschlägt Verteidigung (Explosions-Finisher)."); p.Finisher = ComboFinisherType.Blast; p.TargetBuffs.Add(B(EffectType.Vulnerability, 3, 8f)); p.ProjectileSpeed = 60f;
            var m = EM("SF_Markierung", "Falkenmarke", SkillDelivery.Projectile, 0f, 0f, 36f, 0.2f, "Markiert das Ziel (Zielstrahl)."); m.ProjectileSpeed = 80f;
            ELine(new[] { "Scharfer Blick", "Höhenwind", "Ruhige Hand", "Gewehrmeister", "Spurenleser", "Falkenauge", "Offene Flanke", "Federkiel", "Jagdwut", "Fangschuss", "Ausweichflug", "Sturmfeder" });
            EndSpec("sturmfalke_SF_Praezision"); return e;
        }

        // ================================================================== Elementarweber
        static EliteSpecDefinition Sturmrufer(WeaponSet sf)
        {
            var e = BeginSpec("sturmrufer", "Sturmrufer", new Color(0.9f, 0.92f, 1f), VfxPalette.Gewitter, new Theme(EffectType.Vulnerability, EffectType.Swiftness, Attr.Power, Attr.Concentration, Attr.Toughness),
                EliteMechanic.Ueberladen, "Überladen", "Überladen: nach 5 s in einer Einstimmung deren Taste erneut drücken (oder F5) – Überlade-Kanal mit Flächenwirkung des Elements; danach lädt diese Einstimmung 5 s länger.",
                UtilityCategory.Shout, "Gewitter und Donner – Zepter & Kriegshorn, Überladen, Schreie.", "CastChannel");
            e.Weapon = EWeaverWeapon("Zepter & Kriegshorn", WeaponKind.ScepterWarhorn, WeaponVisual.ScepterWarhorn, "Scepter", sf, new[] { false, false, false, true, true }, new[] { "fanfare", "signal" }, false, 22f);
            _pal = e.Palette; ETypeSkills(UtilityCategory.Shout, new[] { "Schrei der Stille", "Schrei des Windes", "Schrei der Glut", "Schrei der Brandung", "Schrei des Gewitters" });
            _style = "Scepter";
            string[] ul = { "Überladung: Feuer", "Überladung: Wasser", "Überladung: Luft", "Überladung: Erde" };
            EffectType[] ue = { EffectType.Burning, EffectType.Chill, EffectType.Vulnerability, EffectType.Bleeding };
            for (int i = 0; i < 4; i++)
            {
                _pal = AttunementModel.PaletteOf((Element)i);
                var s = EM("UL_" + i, ul[i], SkillDelivery.Self, 3f, 0f, 0f, 0.5f, "Überlade-Kanal (3 s, 6 Treffer um dich): " + EffName(ue[i]) + (i == 1 ? "; heilt Verbündete." : i == 3 ? "; Schutz für dich." : "."));
                s.Radius = 6f; s.Hits = 6; s.HitInterval = 0.5f; s.TargetBuffs.Add(B(ue[i], 2, 4f)); s.Element = (Element)i;
                if (i == 1) { s.HealBase = 1200f; s.HealCoefficient = 0.2f; s.ShareWithAllies = true; }
                if (i == 3) s.SelfBuffs.Add(B(EffectType.Protection, 1, 4f));
                if (i == 2) s.Field = ComboFieldType.Lightning;
            }
            _pal = e.Palette;
            ELine(new[] { "Donnerstimme", "Sturmlauf", "Gewitterhaut", "Hornbläser", "Laute Schreie", "Nachhall", "Blitzrisse", "Funkenschlag", "Rückenwind", "Letzter Donner", "Windschritt", "Hagelschauer" });
            EndSpec("sturmrufer_UL_2"); return e;
        }

        static EliteSpecDefinition Weber(WeaponSet sf)
        {
            var e = BeginSpec("weber", "Weber", new Color(0.85f, 0.55f, 0.85f), VfxPalette.Zwielicht, new Theme(EffectType.Burning, EffectType.Fury, Attr.Power, Attr.Precision, Attr.Ferocity),
                EliteMechanic.DoppelEinstimmung, "Doppel-Einstimmung", "Doppel-Einstimmung: Hauptelement = gewählte, Nebenelement = vorherige Einstimmung; Skill 3 wird zum Kombi-Skill beider Elemente (6 Kombinationen).",
                UtilityCategory.Stance, "Verwobene Elemente – Schwert & Fokus, Doppel-Einstimmung, Haltungen.", "Cast1H");
            e.Weapon = EWeaverWeapon("Degen & Fokus", WeaponKind.SwordFocus, WeaponVisual.SwordFocus, "Sword", sf, new[] { true, true, true, false, false }, new[] { "degen", "stich", "tanz" }, true, 3f);
            _pal = e.Palette; ETypeSkills(UtilityCategory.Stance, new[] { "Haltung der Fäden", "Haltung der Glut", "Haltung der Woge", "Haltung des Sturms", "Haltung der Vier" });
            _style = "Sword";
            string[] ko = { "Dampfstoß", "Funkenfeuer", "Lavasprung", "Hagelwind", "Schlammflut", "Sandsturm" };
            (EffectType, EffectType)[] ke = { (EffectType.Burning, EffectType.Chill), (EffectType.Burning, EffectType.Vulnerability), (EffectType.Burning, EffectType.Bleeding), (EffectType.Chill, EffectType.Vulnerability), (EffectType.Chill, EffectType.Bleeding), (EffectType.Vulnerability, EffectType.Blind) };
            for (int i = 0; i < 6; i++)
            {
                var s = EM("KO_" + i, ko[i], i == 2 ? SkillDelivery.Leap : SkillDelivery.Self, 0.4f, 10f, i == 2 ? 9f : 0f, 1.1f, "Kombi-Skill (" + EffName(ke[i].Item1) + " + " + EffName(ke[i].Item2) + "): wirkt beide Elemente zugleich um dich.");
                s.Radius = 4f; s.TargetBuffs.Add(B(ke[i].Item1, 2, 4f)); s.TargetBuffs.Add(B(ke[i].Item2, 2, 4f)); s.Finisher = i == 2 ? ComboFinisherType.Leap : ComboFinisherType.Blast; if (i == 2) s.LeapDistance = 9f;
            }
            ELine(new[] { "Fadenkunde", "Doppelklinge", "Zwiefache Flamme", "Degenkunst", "Haltungswechsel", "Verwobener Strom", "Zwei Herzen", "Bänderschnitt", "Glimmender Faden", "Letzte Masche", "Gewandter Schritt", "Elementares Muster" });
            EndSpec("weber_KO_2"); return e;
        }

        static EliteSpecDefinition Katalysator()
        {
            var e = BeginSpec("katalysator", "Katalysator", new Color(0.35f, 0.9f, 0.55f), VfxPalette.Jade, new Theme(EffectType.Bleeding, EffectType.Might, Attr.Power, Attr.Concentration, Attr.Ferocity),
                EliteMechanic.Elementarkern, "Elementarkern", "F5 Elementarkern: Treffer laden Energie (max. 30); eine Sphäre kostet 10 und gibt dir und Verbündeten Segen nach Einstimmung (Feuer Macht, Wasser Regeneration, Luft Wut, Erde Schutz), 10 s Abklingzeit.",
                UtilityCategory.Augment, "Jade und rotierende Ringe – Hammer, Elementarkern, Verstärkungen.", "Cast2H");
            e.Weapon = EWeaverWeapon("Jadehammer", WeaponKind.Hammer, WeaponVisual.Hammer, "Hammer", null, new[] { true, true, true, true, true }, new[] { "hammer", "schmiede", "ring", "welle", "beben" }, true, 3.5f);
            _pal = e.Palette; ETypeSkills(UtilityCategory.Augment, new[] { "Verstärkung der Quelle", "Verstärkung der Kraft", "Verstärkung des Schilds", "Verstärkung der Eile", "Verstärkung der Vier" });
            _style = "Hammer";
            EffectType[] kb = { EffectType.Might, EffectType.Regeneration, EffectType.Fury, EffectType.Protection };
            string[] kn = { "Glutkern", "Gischtkern", "Donnerkern", "Felskern" };
            for (int i = 0; i < 4; i++)
            {
                _pal = AttunementModel.PaletteOf((Element)i);
                var s = EM("KE_" + i, kn[i], SkillDelivery.Self, 0.25f, 0f, 0f, 0f, "Elementarkern-Sphäre: " + EffName(kb[i]) + " für dich und Verbündete im Umkreis (8 m).");
                s.Radius = 8f; s.ShareWithAllies = true; s.SelfBuffs.Add(B(kb[i], kb[i] == EffectType.Might ? 5 : 1, 8f)); s.Element = (Element)i; s.Field = AttunementModel.FieldOf((Element)i); s.FieldDuration = 3f;
            }
            _pal = e.Palette;
            ELine(new[] { "Jadeherz", "Ringkraft", "Kernhitze", "Hammerform", "Verstärkte Segen", "Pulsierender Kern", "Splitterring", "Zermalmen", "Kraftfeld", "Brechender Schlag", "Leichter Fuß", "Elementarer Kreislauf" });
            EndSpec("katalysator_KE_0"); return e;
        }

        static EliteSpecDefinition Glutbinder(WeaponSet sf)
        {
            var e = BeginSpec("glutbinder", "Glutbinder", new Color(1f, 0.45f, 0.1f), VfxPalette.Lava, new Theme(EffectType.Burning, EffectType.Might, Attr.ConditionDamage, Attr.Expertise, Attr.Power, ComboFieldType.Fire),
                EliteMechanic.Erdbruch, "Erdbruch", "F5 Erdbruch: Feuer und Erde verschmelzen – ein Lava-Feld am Ziel (Brennen, Blutung, Feuerfeld, 20 s Abklingzeit). Beschwörungen bleiben 50 % länger.",
                UtilityCategory.Conjure, "Lava auf Obsidian – Zepter & Dolch, Erdbruch, Beschwörungen.", "Cast1H");
            e.Weapon = EWeaverWeapon("Zepter & Glutdolch", WeaponKind.ScepterDagger, WeaponVisual.ScepterDagger, "Scepter", sf, new[] { false, false, false, true, true }, new[] { "kralle", "schlund" }, false, 22f);
            _pal = e.Palette; ETypeSkills(UtilityCategory.Conjure, new[] { "Glutgeist rufen", "Aschegeist rufen", "Lavageist rufen", "Obsidiangeist rufen", "Vulkangeist rufen" });
            _style = "Scepter";
            var l = EM("EB_Lava", "Lavafeld", SkillDelivery.GroundTarget, 0.5f, 0f, 22f, 0.35f, "Erdbruch: Lava-Feld am Zielort (5 Treffer) – Brennen und Blutung, Feuerfeld.");
            l.Radius = 4.5f; l.Hits = 5; l.HitInterval = 0.8f; l.Field = ComboFieldType.Fire; l.FieldDuration = 5f; l.FieldTickCoefficient = 0.1f; l.TargetBuffs.Add(B(EffectType.Burning, 2, 4f)); l.TargetBuffs.Add(B(EffectType.Bleeding, 2, 6f));
            ELine(new[] { "Glutblut", "Obsidianhaut", "Lavaader", "Dolchglut", "Bindekunst", "Brodelnder Grund", "Glutrisse", "Aschenregen", "Schmelzhitze", "Vulkanzorn", "Funkenflug", "Glühender Bund" });
            EndSpec("glutbinder_EB_Lava"); return e;
        }

        // ================================================================== Schattenklinge
        static EliteSpecDefinition Wegelagerer()
        {
            var e = BeginSpec("wegelagerer", "Wegelagerer", new Color(0.85f, 0.15f, 0.15f), VfxPalette.Pulver, new Theme(EffectType.Vulnerability, EffectType.Fury, Attr.Precision, Attr.Power, Attr.Ferocity),
                EliteMechanic.Bosheit, "Knien", "F1 Stehlen markiert das Ziel; Treffer darauf bauen Bosheit auf (max. 5). Bei voller Bosheit wird Skill 5 zum Todesschuss (+15 % Schaden je Bosheit). F5 Knien: unbeweglich, +10 % Schaden, +150 Präzision.",
                UtilityCategory.Cantrip, "Pulverrauch und Laserlinie – Gewehr, Bosheit, Kantrips.", "Bow_Shoot");
            e.Weapon = EWeapon("Wegelagerer-Gewehr", WeaponKind.Rifle, WeaponVisual.Rifle, "Bow", true, 30f, false, new[] { "Pulverschuss", "Streuschuss", "Hechtschuss", "Kniekehlenschuss", "Kugelhagel", "", "", "Schuss aus dem Dunkel" });
            ETypeSkills(UtilityCategory.Cantrip, new[] { "Kantrip: Atemholen", "Kantrip: Schnellladen", "Kantrip: Abschütteln", "Kantrip: Kaltblut", "Kantrip: Letzte Kugel" });
            _style = "Bow";
            var f = EM("WL_Todesschuss", "Todesschuss", SkillDelivery.Projectile, 1.2f, 0f, 36f, 2.8f, "Finisher bei voller Bosheit: verbraucht Bosheit und Markierung, +15 % Schaden je Bosheit.");
            f.InitiativeCost = 0; f.ProjectileSpeed = 70f; f.TargetBuffs.Add(B(EffectType.Vulnerability, 5, 8f));
            ELine(new[] { "Kaltes Blut", "Pulverdampf", "Schmale Kimme", "Gewehrlauf", "Kleine Tricks", "Markiertes Wild", "Offene Wunde", "Streifschuss", "Jagdfieber", "Gnadenschuss", "Abrollen", "Letzte Patrone" });
            EndSpec("wegelagerer_WL_Todesschuss"); return e;
        }

        static EliteSpecDefinition Phantomtaenzer()
        {
            var e = BeginSpec("phantomtaenzer", "Phantomtänzer", new Color(0.92f, 0.75f, 0.4f), VfxPalette.Staubgold, new Theme(EffectType.Weakness, EffectType.Vigor, Attr.Power, Attr.Ferocity, Attr.Vitality),
                EliteMechanic.Phantomtanz, "Ausweich-Skill", "Drei Ausdauer-Balken (150); jedes Ausweichen wird zum Angriff. F5 wählt die Ausweich-Fertigkeit: Staubwirbel (Fläche), Phantomstoß (Einzelziel, betäubt), Sandsprung (Sprung).",
                UtilityCategory.Physical, "Staubgold und Wirbelspuren – Stab, Phantomtanz, Körperfertigkeiten.", "Staff_Spin");
            e.Weapon = EWeapon("Kampfstab", WeaponKind.Staff, WeaponVisual.Staff, "Staff", false, 3.5f, false, new[] { "Stabschlag", "Wirbelstab", "Stabsprung", "Kniefeger", "Staubsturm", "Doppelschlag", "Rundumhieb", "Hinterhaltstoß" });
            ETypeSkills(UtilityCategory.Physical, new[] { "Zweiter Atem", "Fußfeger", "Sprungtritt", "Ringergriff", "Phantomsturm" });
            _style = "Staff";
            var a = EM("PT_0", "Staubwirbel", SkillDelivery.Self, 0f, 0f, 0f, 0.8f, "Ausweich-Fertigkeit: Wirbel um dich (Schwäche)."); a.Radius = 3.5f; a.TargetBuffs.Add(B(EffectType.Weakness, 1, 3f)); a.Finisher = ComboFinisherType.Whirl;
            var b = EM("PT_1", "Phantomstoß", SkillDelivery.Melee, 0f, 0f, 4f, 1.2f, "Ausweich-Fertigkeit: Stoß aufs Ziel, betäubt (0,5 s)."); b.TargetBuffs.Add(B(EffectType.Stun, 1, 0.5f)); b.Special = SkillSpecial.Interrupt;
            var c = EM("PT_2", "Sandsprung", SkillDelivery.Self, 0f, 0f, 0f, 0.6f, "Ausweich-Fertigkeit: Landung wirft Sand auf (Blindheit, Explosions-Finisher)."); c.Radius = 3f; c.TargetBuffs.Add(B(EffectType.Blind, 1, 2f)); c.Finisher = ComboFinisherType.Blast;
            ELine(new[] { "Leichte Füße", "Ausdauernder Atem", "Staubkleid", "Stabkunst", "Körperbeherrschung", "Tänzerischer Fluss", "Gelenkschlag", "Wirbelnde Staubwolke", "Hochmut", "Letzter Tanz", "Unermüdlich", "Phantomschritt" });
            EndSpec("phantomtaenzer_PT_0"); return e;
        }

        static EliteSpecDefinition Schattenhueter()
        {
            var e = BeginSpec("schattenhueter", "Schattenhüter", new Color(0.5f, 0.2f, 0.8f), VfxPalette.Tiefviolett, new Theme(EffectType.Torment, EffectType.Protection, Attr.ConditionDamage, Attr.HealingPower, Attr.Vitality),
                EliteMechanic.Schattenschleier, "Schattenschleier", "F2 oder F5 Schattenschleier: zweite Lebensleiste aus Schattenkraft (ab 50 %), Schaden trifft zuerst den Schleier, die Kraft sinkt mit der Zeit; Skills 1–5 werden zu Schleier-Skills mit Unterstützung für Verbündete. Treffer und Initiative laden Schattenkraft.",
                UtilityCategory.Well, "Tentakel-Schatten und Risse – Zepter & Pistole, Schattenschleier, Brunnen.", "Cast1H");
            e.Weapon = EWeapon("Zepter & Pistole", WeaponKind.ScepterPistol, WeaponVisual.ScepterPistol, "Scepter", true, 22f, false, new[] { "Schattenbolzen", "Rissfeuer", "Schattengang", "Fesselriss", "Schattenkreis", "", "", "Schuss aus dem Riss" });
            ETypeSkills(UtilityCategory.Well, new[] { "Brunnen der Zuflucht", "Brunnen der Pein", "Brunnen der Stille", "Brunnen des Portals", "Brunnen der Nacht" });
            _style = "Scepter";
            var ss = new[] { ("Schleierpfeil", "Schaden und Pein am Ziel; heilt den nächsten Verbündeten."), ("Tentakelgriff", "Tentakel ziehen Gegner zusammen (Immobilisierung)."), ("Schattenhülle", "Schutz und Regeneration für dich und Verbündete."), ("Risswelle", "Welle aus Schatten, 3 Pein."), ("Nachtgang", "Schattenschritt nach vorn, Tarnung 2 s.") };
            for (int i = 0; i < 5; i++)
            {
                var s = EM("SS" + (i + 1), ss[i].Item1, i == 0 || i == 3 ? SkillDelivery.Projectile : SkillDelivery.Self, 0.4f, i == 0 ? 0f : 4f + 2f * i, i == 0 || i == 3 ? 22f : 0f, i == 2 ? 0f : 0.7f, "Schattenschleier: " + ss[i].Item2);
                s.InitiativeCost = 0;
                if (i == 0) { s.TargetBuffs.Add(B(EffectType.Torment, 1, 4f)); s.HealBase = 500f; s.HealCoefficient = 0.1f; s.ShareWithAllies = true; s.Radius = 6f; }
                if (i == 1) { s.Radius = 5f; s.TargetBuffs.Add(B(EffectType.Immobilize, 1, 1.5f)); }
                if (i == 2) { s.Radius = 7f; s.ShareWithAllies = true; s.SelfBuffs.Add(B(EffectType.Protection, 1, 4f)); s.SelfBuffs.Add(B(EffectType.Regeneration, 1, 6f)); }
                if (i == 3) { s.Hits = 2; s.HitInterval = 0.3f; s.TargetBuffs.Add(B(EffectType.Torment, 3, 5f)); }
                if (i == 4) { s.Special = SkillSpecial.Blink; s.LeapDistance = 10f; s.StealthSeconds = 2f; }
            }
            ELine(new[] { "Schattenkraft", "Nachtsicht", "Dunkle Zuflucht", "Zepterkunde", "Brunnenmeister", "Schleierweber", "Peinigende Schatten", "Rissbolzen", "Tiefe Nacht", "Letzter Schatten", "Schattensprung", "Hüter der Risse" });
            EndSpec("schattenhueter_SS2"); return e;
        }

        static EliteSpecDefinition Klingenwirbel()
        {
            var e = BeginSpec("klingenwirbel", "Klingenwirbel", new Color(0.5f, 0.88f, 0.95f), VfxPalette.Stahl, new Theme(EffectType.Bleeding, EffectType.Swiftness, Attr.Power, Attr.Precision, Attr.ConditionDamage),
                EliteMechanic.Rueckruf, "Rückruf", "Geworfene Äxte kehren zurück: nach einem Axtwurf wird F1 (oder F5) zum Rückruf – Treffer auf dem Rückflug, +3 Initiative. Nach 6 s fällt die Axt von selbst zurück.",
                UtilityCategory.Preparation, "Stahlsilber und Rotations-Trails – Axt & Dolch, Rückruf, Vorbereitungen.", "Axe_Throw");
            e.Weapon = EWeapon("Wurfaxt & Dolch", WeaponKind.AxeDagger, WeaponVisual.AxeDagger, "Axe", true, 18f, false, new[] { "Kreiselaxt", "Doppelwurf", "Wirbelsprung", "Kniebrecher", "Axtsturm", "", "", "Wurf aus dem Schatten" });
            ETypeSkills(UtilityCategory.Preparation, new[] { "Vorbereitung: Verband", "Vorbereitung: Stolperdraht", "Vorbereitung: Klingenfalle", "Vorbereitung: Fangnetz", "Vorbereitung: Klingenkreis" });
            _style = "Axe";
            var rr = EM("KW_Rueckruf", "Rückruf", SkillDelivery.Projectile, 0f, 0f, 20f, 0.9f, "Die Axt kehrt zurück und trifft auf dem Rückflug (2 Treffer, Blutung); +3 Initiative.");
            rr.Hits = 2; rr.HitInterval = 0.25f; rr.TargetBuffs.Add(B(EffectType.Bleeding, 2, 6f)); rr.ProjectileSpeed = 45f;
            ELine(new[] { "Stahlnerv", "Rotation", "Klingenauge", "Axtkunde", "Gute Vorbereitung", "Bumerang", "Tiefe Kerbe", "Pfeifende Klinge", "Schwung", "Letzter Wurf", "Ausweichrolle", "Wirbelsturm aus Stahl" });
            EndSpec("klingenwirbel_KW_Rueckruf"); return e;
        }

        // ================================================================== Wächter
        static EliteSpecDefinition Glutrichter()
        {
            var e = BeginSpec("glutrichter", "Glutrichter", new Color(1f, 0.68f, 0.25f), VfxPalette.Richtglut, new Theme(EffectType.Burning, EffectType.Fury, Attr.Power, Attr.Ferocity, Attr.Precision),
                EliteMechanic.Urteil, "Urteil", "F5 Urteil: markiert das Ziel (2 Brennen); nach 2,5 s explodiert die Marke mit Brennen im Umkreis (20 s Abklingzeit). Ergänzt die Linien aus Lauf 3.",
                UtilityCategory.Trap, "Gold-Orange und Lichtpfeile – Langbogen, Urteil, Licht-Fallen.", "Bow_Shoot");
            e.Weapon = EWeapon("Richterbogen", WeaponKind.Longbow, WeaponVisual.Longbow, "Bow", true, 30f, false, new[] { "Lichtpfeil", "Strahlensalve", "Rückzugspfeil", "Bannpfeil", "Lichtregen", "", "", "" });
            ETypeSkills(UtilityCategory.Trap, new[] { "Lichtfalle der Gnade", "Lichtfalle des Banns", "Lichtfalle der Glut", "Lichtfalle des Kreises", "Falle des Gerichts" });
            _style = "Bow";
            var u = EM("GR_Urteil", "Urteilsspruch", SkillDelivery.GroundTarget, 0f, 0f, 40f, 1.4f, "Explosion der Urteilsmarke: Brennen im Umkreis (Explosions-Finisher)."); u.Radius = 4f; u.Finisher = ComboFinisherType.Blast; u.TargetBuffs.Add(B(EffectType.Burning, 3, 5f));
            ELine(new[] { "Richterblick", "Glutpfeil", "Heiliger Eifer", "Bogenkunde", "Fallensteller des Lichts", "Schnelles Urteil", "Brandmal", "Lichtsplitter", "Gerechter Zorn", "Vollstreckung", "Lichtschritt", "Ewiges Feuer" });
            EndSpec("glutrichter_GR_Urteil"); return e;
        }

        static EliteSpecDefinition Bollwerkmeister()
        {
            var e = BeginSpec("bollwerkmeister", "Bollwerkmeister", new Color(0.65f, 0.78f, 1f), VfxPalette.Bollwerk, new Theme(EffectType.Weakness, EffectType.Protection, Attr.Toughness, Attr.Vitality, Attr.Concentration),
                EliteMechanic.Festung, "Festung", "F5 Festung: 10 s Kuppel – Projektile werden abgewehrt, ein Drittel des Schadens trägt die Kuppel; Schutz für dich und Verbündete (8 m), 30 s Abklingzeit.",
                UtilityCategory.Mantra, "Blauweiße Schildsegmente – Schwert & Schild, Festung, Mantras.", "Cast1H");
            e.Weapon = EWeapon("Schwert & Turmschild", WeaponKind.SwordShield, WeaponVisual.SwordShield, "Sword", false, 3f, false, new[] { "Schildhieb", "Mauerwirbel", "Schildsprung", "Schildstoß", "Bollwerk", "Gardehieb", "Wallstoß", "" });
            ETypeSkills(UtilityCategory.Mantra, new[] { "Mantra der Linderung", "Mantra des Schilds", "Mantra der Wacht", "Mantra der Mauer", "Mantra der Festung" });
            _style = "Sword";
            var f = EM("BM_Festung", "Festungskuppel", SkillDelivery.Self, 0f, 0f, 0f, 0f, "Die Kuppel der Festung (Projektilschutz)."); f.Radius = 8f;
            ELine(new[] { "Fester Stand", "Wallkraft", "Schildwacht", "Schildkunde", "Mantrenkraft", "Kuppelbauer", "Entkräften", "Prallschild", "Unerschütterlich", "Letzte Bastion", "Rückhalt", "Ewige Mauer" });
            EndSpec("bollwerkmeister_W5"); return e;
        }

        static EliteSpecDefinition Sturmherold()
        {
            var e = BeginSpec("sturmherold", "Sturmherold", new Color(0.3f, 0.62f, 1f), VfxPalette.Sturmblau, new Theme(EffectType.Vulnerability, EffectType.Quickness, Attr.Power, Attr.Concentration, Attr.Precision, ComboFieldType.Lightning),
                EliteMechanic.Sturmzorn, "Sturmzorn", "F5 Sturmzorn: 10 s lang schlägt bei jedem gewirkten Skill ein Blitz am Ziel ein (max. 1/s); Schnelligkeit für dich und Verbündete (10 m), 25 s Abklingzeit.",
                UtilityCategory.Symbol, "Elektroblaue Blitzsäulen – Hammer, Sturmzorn, Symbole.", "Cast2H");
            e.Weapon = EWeapon("Donnerhammer", WeaponKind.Hammer, WeaponVisual.Hammer, "Hammer", false, 3.5f, false, new[] { "Donnerschlag", "Blitzwirbel", "Sturmsprung", "Bannschlag", "Gewitterkreis", "Funkenhieb", "Wetterschlag", "" });
            ETypeSkills(UtilityCategory.Symbol, new[] { "Symbol der Ruhe", "Symbol des Funkens", "Symbol des Donners", "Symbol der Eile", "Symbol des Sturmherolds" });
            _style = "Hammer";
            var b = EM("SH_Blitz", "Sturmzorn-Blitz", SkillDelivery.GroundTarget, 0f, 0f, 40f, 0.5f, "Blitzeinschlag am Ziel (Sturmzorn), 1 Verwundbarkeit."); b.Radius = 2.5f; b.TargetBuffs.Add(B(EffectType.Vulnerability, 1, 5f));
            ELine(new[] { "Donnerherz", "Herold der Eile", "Funkenhaut", "Hammerkunde", "Wanderndes Symbol", "Sturmruf", "Elektrisiert", "Kettenblitz", "Rasende Eile", "Donnergericht", "Blitzschritt", "Auge des Sturms" });
            EndSpec("sturmherold_SH_Blitz"); return e;
        }

        static EliteSpecDefinition Dornenschild()
        {
            var e = BeginSpec("dornenschild", "Dornenschild", new Color(0.3f, 0.85f, 0.45f), VfxPalette.Smaragd, new Theme(EffectType.Bleeding, EffectType.Regeneration, Attr.Toughness, Attr.ConditionDamage, Attr.HealingPower),
                EliteMechanic.Dornenhaut, "Dornenhaut", "F5 Dornenhaut: 8 s Vergeltung – direkte Treffer verletzen den Angreifer, jeden Zeitpunkt (1 s) wird ein Zustand von dir als Brennen auf den nächsten Gegner zurückgeworfen; Entschlossenheit, 30 s Abklingzeit.",
                UtilityCategory.Consecration, "Smaragd-Gold und Dornenkreise – Zepter & Fackel, Dornenhaut, Weihen.", "Cast1H");
            e.Weapon = EWeapon("Dornenzepter & Fackel", WeaponKind.ScepterTorch, WeaponVisual.ScepterTorch, "Scepter", true, 20f, false, new[] { "Dornenfunke", "Rankenfeuer", "Lichtsprung", "Dornenbann", "Fackelkreis", "", "", "" });
            ETypeSkills(UtilityCategory.Consecration, new[] { "Weihe der Heilung", "Weihe des Schutzes", "Weihe der Dornen", "Weihe des Willens", "Weihe des Smaragdhains" });
            _style = "Scepter";
            var d = EM("DS_Dornenhaut", "Dornenhaut", SkillDelivery.Self, 0f, 0f, 0f, 0f, "Die Dornenhaut (Vergeltung, Zustands-Rückwurf)."); d.Radius = 4f;
            ELine(new[] { "Smaragdhaut", "Wurzelglaube", "Dornengebet", "Zepterkunde", "Geweihter Boden", "Vergeltung", "Spitze Dornen", "Rückwurf", "Grüne Gnade", "Letzte Weihe", "Schutzranke", "Dornenkrone" });
            EndSpec("dornenschild_DS_Dornenhaut"); return e;
        }
    }
}
