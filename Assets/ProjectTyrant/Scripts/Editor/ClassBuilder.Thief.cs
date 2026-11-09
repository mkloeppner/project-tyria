using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    public static partial class ClassBuilder
    {
        /// <summary>Schattenklinge: mittlere Rüstung, Initiative (12), Stehlen (F1), Tarnung; Dolch+Dolch · Schwert+Pistole · Kurzbogen; Tricks, Gifte, Täuschungen.</summary>
        static ClassDefinition BuildThief()
        {
            var violet = new Color(0.55f, 0.35f, 0.75f);
            Begin("schattenklinge", violet, VfxPalette.Schatten);
            var d = Class("schattenklinge", "Schattenklinge", ArmorWeight.Medium, violet, new Color(0.15f, 0.12f, 0.18f), ClassMechanicKind.Initiative, "Initiative",
                new[] { "Stehlen", "", "", "" },
                "Initiative: 12 Punkte, Waffenskills 2–5 kosten Initiative statt Abklingzeit (1 Punkt/s). F1 Stehlen: Schattenschritt zum Ziel und ein gestohlener Skill nach Gegnerart. Tarnung macht Skill 1 zum Tarnungsangriff.",
                "Mittlere Rüstung · Initiative und Stehlen · Dolch + Dolch, Schwert + Pistole, Kurzbogen · Tricks, Gifte, Täuschungen.");

            // ---- Dolch + Dolch
            _style = "Dagger";
            var dd1 = S("DD1_Klingenhieb", "Klingenhieb", "Klinge", SkillDelivery.Melee, 0.45f, 0f, 3f, 0.55f, "Schneller Dolchhieb.");
            Chain(dd1, "Doppelstich", "Giftstoß", 0.55f, 0.62f, 0.73f, B(EffectType.Poison, 1, 4f));
            var dd2 = S("DD2_Herzstich", "Herzstich", "Herz", SkillDelivery.Melee, 0.5f, 0f, 3f, 1.6f, "Tiefer Stich; mehr Schaden gegen geschwächte Ziele.");
            dd2.InitiativeCost = 4; dd2.TargetBuffs.Add(B(EffectType.Vulnerability, 3, 6f));
            var dd3 = S("DD3_Todeswirbel", "Todeswirbel", "Wirbel", SkillDelivery.Self, 0.25f, 0f, 0f, 0.35f, "Doppelwaffen-Skill: wirbelnder Sprung (5 Treffer), weicht aus, Wirbel-Finisher.");
            dd3.InitiativeCost = 4; dd3.Radius = 3.2f; dd3.Hits = 5; dd3.HitInterval = 0.18f; dd3.EvadesDuringCast = true; dd3.Finisher = ComboFinisherType.Whirl; dd3.TargetBuffs.Add(B(EffectType.Bleeding, 1, 6f));
            var dd4 = S("DD4_Giftstreuer", "Giftstreuer", "Gift", SkillDelivery.Projectile, 0.25f, 0f, 18f, 0.4f, "Wirft einen Dolch: Gift und Schwäche.");
            dd4.InitiativeCost = 3; dd4.TargetBuffs.Add(B(EffectType.Poison, 2, 5f)); dd4.TargetBuffs.Add(B(EffectType.Weakness, 1, 4f));
            var dd5 = S("DD5_Rauchschleier", "Rauchschleier", "Rauch", SkillDelivery.GroundTarget, 0.25f, 0f, 12f, 0f, "Rauchfeld (4 s) am Zielort, blendet.");
            dd5.InitiativeCost = 6; dd5.Radius = 4f; dd5.Field = ComboFieldType.Smoke; dd5.FieldDuration = 4f; dd5.TargetBuffs.Add(B(EffectType.Blind, 1, 2f));
            var ddS = S("DDS_Hinterhalt", "Hinterhalt", "Hinterh.", SkillDelivery.Melee, 0.4f, 0f, 3f, 2.2f, "Tarnungsangriff: massiver Stich in den Rücken, 5 Verwundbarkeit.");
            ddS.StealthAttack = true; ddS.TargetBuffs.Add(B(EffectType.Vulnerability, 5, 8f));
            var setDd = Set("DolchDolch", "Dolch & Dolch", WeaponKind.DaggerDagger, WeaponVisual.DaggerDagger, violet, dd1, dd2, dd3, dd4, dd5);
            setDd.StealthSkill = ddS; ddS.Weapon = WeaponKind.DaggerDagger;

            // ---- Schwert + Pistole
            _style = "Sword";
            var sp1 = S("SP1_Saebelhieb", "Säbelhieb", "Säbel", SkillDelivery.Melee, 0.5f, 0f, 3f, 0.6f, "Säbelhieb.");
            Chain(sp1, "Rückhandhieb", "Fesselhieb", 0.6f, 0.68f, 0.8f, B(EffectType.Weakness, 1, 3f));
            var sp2 = S("SP2_Schattensprung", "Schattensprung", "Schatten", SkillDelivery.Melee, 0f, 0f, 18f, 0.7f, "Schattenschritt zum Ziel und Hieb.");
            sp2.InitiativeCost = 3; sp2.Special = SkillSpecial.ShadowStep; sp2.Palette = VfxPalette.Schatten;
            var sp3 = S("SP3_Pistolenpeitsche", "Pistolenpeitsche", "Peitsche", SkillDelivery.Melee, 0.5f, 0f, 3f, 0.3f, "Doppelwaffen-Skill: betäubt, dann 6 schnelle Hiebe.");
            sp3.InitiativeCost = 5; sp3.Hits = 6; sp3.HitInterval = 0.2f; sp3.TargetBuffs.Add(B(EffectType.Stun, 1, 0.5f)); sp3.Special = SkillSpecial.Interrupt;
            _style = "Scepter";
            var sp4 = S("SP4_Kopfschuss", "Kopfschuss", "Kopf", SkillDelivery.Projectile, 0f, 0f, 24f, 0.5f, "Pistolenschuss, unterbricht das Ziel.");
            sp4.InitiativeCost = 4; sp4.Special = SkillSpecial.Interrupt; sp4.TargetBuffs.Add(B(EffectType.Stun, 1, 0.5f)); sp4.ProjectileSpeed = 60f;
            var sp5 = S("SP5_Schwarzpulver", "Schwarzpulver", "Pulver", SkillDelivery.GroundTarget, 0.25f, 0f, 18f, 0.2f, "Rauchfeld, blendet Feinde im Bereich.");
            sp5.InitiativeCost = 6; sp5.Radius = 4f; sp5.Field = ComboFieldType.Smoke; sp5.FieldDuration = 4f; sp5.TargetBuffs.Add(B(EffectType.Blind, 1, 3f));
            _style = "Sword";
            var spS = S("SPS_Schattenhieb", "Schattenhieb", "S-Hieb", SkillDelivery.Melee, 0.4f, 0f, 3f, 1.8f, "Tarnungsangriff: Hieb aus dem Schatten, Schwäche.");
            spS.StealthAttack = true; spS.TargetBuffs.Add(B(EffectType.Weakness, 1, 4f));
            var setSp = Set("SchwertPistole", "Schwert & Pistole", WeaponKind.SwordPistol, WeaponVisual.SwordPistol, new Color(0.7f, 0.7f, 0.75f), sp1, sp2, sp3, sp4, sp5);
            setSp.StealthSkill = spS; spS.Weapon = WeaponKind.SwordPistol;

            // ---- Kurzbogen
            _style = "Bow";
            var sb1 = S("SB1_Spaltpfeil", "Spaltpfeil", "Spalt", SkillDelivery.Projectile, 0.55f, 0f, 24f, 0.6f, "Pfeil, der auf ein zweites Ziel überspringt.");
            sb1.TargetCount = 2; sb1.Finisher = ComboFinisherType.Projectile;
            var sb2 = S("SB2_Streupfeil", "Streupfeil", "Streu", SkillDelivery.Projectile, 0.5f, 0f, 24f, 0.9f, "Explodierender Pfeil, Explosions-Finisher.");
            sb2.InitiativeCost = 3; sb2.Radius = 3f; sb2.Finisher = ComboFinisherType.Blast;
            var sb3 = S("SB3_Ausweichsalto", "Ausweichsalto", "Salto", SkillDelivery.Self, 0.3f, 0f, 0f, 0f, "Doppelwaffen-Skill: Salto, weicht aus, Eile.");
            sb3.InitiativeCost = 4; sb3.EvadesDuringCast = true; sb3.SelfBuffs.Add(B(EffectType.Swiftness, 1, 6f));
            var sb4 = S("SB4_Giftpfeil", "Giftpfeil", "Gift", SkillDelivery.Projectile, 0.4f, 0f, 24f, 0.3f, "Giftwolke am Ziel (Gift).");
            sb4.InitiativeCost = 4; sb4.Radius = 3f; sb4.TargetBuffs.Add(B(EffectType.Poison, 3, 6f));
            var sb5 = S("SB5_Infiltrationspfeil", "Infiltrationspfeil", "Infil.", SkillDelivery.GroundTarget, 0.25f, 0f, 24f, 0f, "Schattenschritt zum Zielort.");
            sb5.InitiativeCost = 4; sb5.Special = SkillSpecial.ShadowStep; sb5.Radius = 1.5f;
            var sbS = S("SBS_Ueberraschungsschuss", "Überraschungsschuss", "Überr.", SkillDelivery.Projectile, 0.4f, 0f, 24f, 1.5f, "Tarnungsangriff: betäubender Schuss.");
            sbS.StealthAttack = true; sbS.TargetBuffs.Add(B(EffectType.Stun, 1, 1f));
            var setSb = Set("Kurzbogen", "Kurzbogen", WeaponKind.Shortbow, WeaponVisual.Shortbow, new Color(0.5f, 0.4f, 0.3f), sb1, sb2, sb3, sb4, sb5);
            setSb.StealthSkill = sbS; sbS.Weapon = WeaponKind.Shortbow;
            _style = null;

            // ---- Heilung, Hilfe, Elite
            var h1 = Heal(S("H1_Schattenzuflucht", "Schattenzuflucht", "Zuflucht", SkillDelivery.Self, 0.5f, 30f, 0f, 0f, "Heilt dich und gewährt 3 s Tarnung."), 4200f, 0.8f, true);
            h1.StealthSeconds = 3f;
            var h2 = Heal(S("H2_TiefDurchatmen", "Tief durchatmen", "Atmen", SkillDelivery.Self, 0.5f, 25f, 0f, 0f, "Heilt dich, entfernt 2 Zustände, 2 Initiative."), 4800f, 1f, false);
            h2.CleanseCount = 2;
            var h3 = Heal(S("H3_Kraeutertrunk", "Kräutertrunk", "Trunk", SkillDelivery.Self, 1f, 30f, 0f, 0f, "Starke Heilung und Regeneration."), 6000f, 1.1f, false);
            h3.SelfBuffs.Add(B(EffectType.Regeneration, 1, 8f));
            var k1 = Util(S("K1_Blendstaub", "Blendstaub", "Blend", SkillDelivery.Self, 0f, 25f, 0f, 0f, "Trick: blendet Feinde in der Nähe, 3 s Tarnung."), UtilityCategory.Trick, 1, true);
            k1.Radius = 4f; k1.TargetBuffs.Add(B(EffectType.Blind, 1, 3f)); k1.StealthSeconds = 3f;
            var k2 = Util(S("K2_Hechtrolle", "Hechtrolle", "Rolle", SkillDelivery.Self, 0.25f, 20f, 0f, 0f, "Trick: Rolle mit Ausweichen, entfernt Bewegungs-Zustände, Eile."), UtilityCategory.Trick, 2, false);
            k2.EvadesDuringCast = true; k2.CleanseCount = 1; k2.SelfBuffs.Add(B(EffectType.Swiftness, 1, 8f));
            var k3 = Util(S("K3_Fallstrick", "Fallstrick", "Strick", SkillDelivery.GroundTarget, 0.25f, 30f, 15f, 0.3f, "Trick: Fallstrick am Zielort, Verkrüppelung und Immobilisierung."), UtilityCategory.Trick, 3, false);
            k3.Radius = 3.5f; k3.TargetBuffs.Add(B(EffectType.Cripple, 1, 4f)); k3.TargetBuffs.Add(B(EffectType.Immobilize, 1, 2f));
            var v1 = Util(S("V1_Laehmgift", "Lähmgift", "Lähm", SkillDelivery.Melee, 0f, 25f, 6f, 0.2f, "Gift: vergiftete Klinge – Immobilisierung und Gift beim nächsten Treffer."), UtilityCategory.Venom, 1, false);
            v1.TargetBuffs.Add(B(EffectType.Immobilize, 1, 1.5f)); v1.TargetBuffs.Add(B(EffectType.Poison, 2, 5f));
            var v2 = Util(S("V2_Schwaechegift", "Schwächegift", "Schwäche", SkillDelivery.Melee, 0f, 25f, 6f, 0.2f, "Gift: Schwäche und Verwundbarkeit."), UtilityCategory.Venom, 2, false);
            v2.TargetBuffs.Add(B(EffectType.Weakness, 1, 5f)); v2.TargetBuffs.Add(B(EffectType.Vulnerability, 4, 8f));
            var v3 = Util(S("V3_Spinnengift", "Spinnengift", "Spinne", SkillDelivery.Melee, 0f, 30f, 6f, 0.2f, "Gift: 4 Gift, 1 Qual."), UtilityCategory.Venom, 3, false);
            v3.TargetBuffs.Add(B(EffectType.Poison, 4, 6f)); v3.TargetBuffs.Add(B(EffectType.Torment, 2, 5f));
            var t1 = Util(S("T1_Schattenschritt", "Schattenschritt", "Schritt", SkillDelivery.Self, 0f, 30f, 0f, 0f, "Täuschung: Schattenschritt 15 m nach vorn, entfernt 2 Zustände."), UtilityCategory.Deception, 1, false);
            t1.Special = SkillSpecial.Blink; t1.LeapDistance = 15f; t1.CleanseCount = 2;
            var t2 = Util(S("T2_Rauchwolke", "Rauchwolke", "Wolke", SkillDelivery.Self, 0f, 35f, 0f, 0f, "Täuschung: 4 s Tarnung."), UtilityCategory.Deception, 2, false);
            t2.StealthSeconds = 4f;
            var t3 = Util(S("T3_Ablenkung", "Ablenkung", "Ablenk.", SkillDelivery.Projectile, 0f, 30f, 18f, 0.2f, "Täuschung: blendet und betäubt das Ziel kurz."), UtilityCategory.Deception, 3, false);
            t3.TargetBuffs.Add(B(EffectType.Blind, 1, 3f)); t3.TargetBuffs.Add(B(EffectType.Stun, 1, 1f)); t3.Special = SkillSpecial.Interrupt;
            var e1 = Elite(S("E1_Klingenregen", "Klingenregen", "ELITE", SkillDelivery.GroundTarget, 0.75f, 90f, 20f, 0.6f, "Elite: Klingenregen am Zielort (6 Wellen), Blutung."), true);
            e1.Radius = 5f; e1.Hits = 6; e1.HitInterval = 0.5f; e1.TargetBuffs.Add(B(EffectType.Bleeding, 2, 6f));
            var e2 = Elite(S("E2_Schattensturm", "Schattensturm", "Sturm", SkillDelivery.Self, 0.5f, 120f, 0f, 0f, "Elite: Tarnung (4 s) für dich und Verbündete, Schnelligkeit."), false);
            e2.StealthSeconds = 4f; e2.ShareWithAllies = true; e2.Radius = 8f; e2.SelfBuffs.Add(B(EffectType.Quickness, 1, 5f));

            // ---- Gestohlene Skills (F1 nach dem Stehlen)
            var st1 = S("Stolen_Moor", "Gestohlene Giftwolke", "Wolke", SkillDelivery.GroundTarget, 0f, 0f, 15f, 0.4f, "Gestohlen (Moor): Giftwolke, Rauchfeld.");
            st1.Id = "stolen_moor"; st1.Radius = 4f; st1.Field = ComboFieldType.Smoke; st1.FieldDuration = 4f; st1.TargetBuffs.Add(B(EffectType.Poison, 3, 6f)); st1.Palette = VfxPalette.Natur;
            var st2 = S("Stolen_Frost", "Eissplitter", "Splitter", SkillDelivery.Projectile, 0f, 0f, 24f, 1.2f, "Gestohlen (Frost): Eissplitter, Kälte.");
            st2.Id = "stolen_frost"; st2.TargetBuffs.Add(B(EffectType.Chill, 1, 3f)); st2.Palette = VfxPalette.Frost;
            var st3 = S("Stolen_Glut", "Glutstoß", "Glut", SkillDelivery.Self, 0f, 0f, 0f, 1.4f, "Gestohlen (Glut): Explosion, Brennen.");
            st3.Id = "stolen_glut"; st3.Radius = 4f; st3.Finisher = ComboFinisherType.Blast; st3.TargetBuffs.Add(B(EffectType.Burning, 3, 4f)); st3.Palette = VfxPalette.Glut;
            var st4 = S("Stolen_Beast", "Fell der Bestie", "Fell", SkillDelivery.Self, 0f, 0f, 0f, 0f, "Gestohlen (Tier): 5 Macht und Schutz.");
            st4.Id = "stolen_beast"; st4.SelfBuffs.Add(B(EffectType.Might, 5, 10f)); st4.SelfBuffs.Add(B(EffectType.Protection, 1, 4f)); st4.Palette = VfxPalette.Natur;
            var st5 = S("Stolen_Bandit", "Felsbrocken", "Fels", SkillDelivery.Projectile, 0f, 0f, 18f, 1.5f, "Gestohlen (Plünderer): Felsbrocken, betäubt.");
            st5.Id = "stolen_bandit"; st5.TargetBuffs.Add(B(EffectType.Stun, 1, 1f)); st5.Palette = VfxPalette.Erde;
            foreach (var s in new[] { st1, st2, st3, st4, st5 }) { s.SlotType = SkillSlotType.Utility; s.WeaponStyleOverride = ""; }

            d.WeaponSets.AddRange(new[] { setDd, setSp, setSb });
            d.StartWeapons = new[] { WeaponKind.DaggerDagger, WeaponKind.Shortbow };
            d.SkillPool.AddRange(new[] { h1, h2, h3, k1, k2, k3, v1, v2, v3, t1, t2, t3, e1, e2 });
            d.ExtraSkills.AddRange(new[] { st1, st2, st3, st4, st5 });
            d.Palette = VfxPalette.Schatten; d.SignatureSkill = "DD3_Todeswirbel";
            d.Specializations = ThiefTraits();
            return d;
        }
    }
}
