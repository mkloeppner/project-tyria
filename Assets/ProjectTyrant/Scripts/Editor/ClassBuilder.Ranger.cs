using System.Collections.Generic;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    public static partial class ClassBuilder
    {
        /// <summary>Wildhüter: mittlere Rüstung, Tiergefährte (F1–F4), Langbogen · Axt+Fackel · Schwert+Kriegshorn; Fallen, Signale, Wildnisfertigkeiten.</summary>
        static ClassDefinition BuildRanger()
        {
            var green = new Color(0.45f, 0.75f, 0.3f); var gold = new Color(0.9f, 0.75f, 0.3f);
            Begin("wildhueter", green, VfxPalette.Natur);
            var d = Class("wildhueter", "Wildhüter", ArmorWeight.Medium, green, gold, ClassMechanicKind.Pet, "Tiergefährte",
                new[] { "Angriff", "Familie", "Zurück", "Wechsel" },
                "Tiergefährte: F1 greift dein Ziel an, F2 Familien-Skill, F3 ruft zurück (Passiv), F4 wechselt den Gefährten (2 aktiv, 20 s). Wolf, Bär oder Falke.",
                "Mittlere Rüstung · Tiergefährte · Langbogen, Axt + Fackel, Schwert + Kriegshorn · Fallen, Signale, Wildnis.");

            // ---- Langbogen (2H, 30 m)
            _style = "Bow";
            var lb1 = S("LB1_Weitschuss", "Weitschuss", "Weit", SkillDelivery.Projectile, 0.6f, 0f, 30f, 0.75f, "Pfeil, der auf große Entfernung mehr Schaden verursacht. Projektil-Finisher.");
            lb1.Finisher = ComboFinisherType.Projectile; lb1.ProjectileSpeed = 42f;
            var lb2 = S("LB2_Pfeilhagel", "Pfeilhagel", "Hagel", SkillDelivery.Projectile, 0.25f, 10f, 30f, 0.28f, "Schnelle Folge von 6 Pfeilen; 1 Verwundbarkeit je Treffer.");
            lb2.Hits = 6; lb2.HitInterval = 0.25f; lb2.TargetBuffs.Add(B(EffectType.Vulnerability, 1, 6f));
            var lb3 = S("LB3_Rueckzugsschuss", "Rückzugsschuss", "Rückzug", SkillDelivery.Projectile, 0.3f, 15f, 30f, 0.5f, "Weicht beim Schuss aus und gewährt Eile.");
            lb3.EvadesDuringCast = true; lb3.SelfBuffs.Add(B(EffectType.Swiftness, 1, 6f));
            var lb4 = S("LB4_Wuchtpfeil", "Wuchtpfeil", "Wucht", SkillDelivery.Projectile, 0.5f, 20f, 30f, 0.6f, "Schwerer Pfeil, der das Ziel betäubt (Unterbrechung).");
            lb4.Special = SkillSpecial.Interrupt; lb4.TargetBuffs.Add(B(EffectType.Stun, 1, 0.75f));
            var lb5 = S("LB5_Dornenregen", "Dornenregen", "Regen", SkillDelivery.GroundTarget, 0.5f, 24f, 30f, 0.3f, "Pfeilregen am Zielort (6 Wellen), Blutung.");
            lb5.Radius = 4.5f; lb5.Hits = 6; lb5.HitInterval = 0.8f; lb5.TargetBuffs.Add(B(EffectType.Bleeding, 1, 5f));
            var setLb = Set("Langbogen", "Langbogen", WeaponKind.Longbow, WeaponVisual.Longbow, gold, lb1, lb2, lb3, lb4, lb5);

            // ---- Axt + Fackel
            _style = "Axe";
            var ax1 = S("AX1_Wirbelaxt", "Wirbelaxt", "Axt", SkillDelivery.Projectile, 0.55f, 0f, 18f, 0.72f, "Geworfene Axt, die zurückfliegt; 1 Verwundbarkeit.");
            ax1.TargetBuffs.Add(B(EffectType.Vulnerability, 1, 6f)); ax1.ProjectileSpeed = 28f; ax1.Finisher = ComboFinisherType.Projectile;
            var ax2 = S("AX2_Doppelwurf", "Doppelwurf", "Doppel", SkillDelivery.Projectile, 0.5f, 8f, 18f, 0.6f, "Zwei Äxte, die verkrüppeln.");
            ax2.Hits = 2; ax2.HitInterval = 0.3f; ax2.TargetBuffs.Add(B(EffectType.Cripple, 1, 3f));
            var ax3 = S("AX3_Klingenwirbel", "Klingenwirbel", "Wirbel", SkillDelivery.Self, 0.25f, 12f, 0f, 0.42f, "Wirbelt mit den Äxten (5 Treffer), Wirbel-Finisher.");
            ax3.Radius = 3.2f; ax3.Hits = 5; ax3.HitInterval = 0.2f; ax3.Finisher = ComboFinisherType.Whirl; ax3.EvadesDuringCast = true;
            var ax4 = S("AX4_Fackelstoss", "Fackelstoß", "Fackel", SkillDelivery.Melee, 0.5f, 15f, 3f, 0.9f, "Stößt die Fackel ins Ziel: 3 Brennen.");
            ax4.TargetBuffs.Add(B(EffectType.Burning, 3, 4f)); ax4.Palette = VfxPalette.Feuer;
            var ax5 = S("AX5_Feuerkreis", "Feuerkreis", "Kreis", SkillDelivery.Self, 0.5f, 20f, 0f, 0.3f, "Entzündet einen Feuerkreis (Feuerfeld, 5 s).");
            ax5.Radius = 4f; ax5.Field = ComboFieldType.Fire; ax5.FieldDuration = 5f; ax5.FieldTickCoefficient = 0.15f; ax5.Palette = VfxPalette.Feuer;
            var setAx = Set("AxtFackel", "Axt & Fackel", WeaponKind.AxeTorch, WeaponVisual.AxeTorch, new Color(0.9f, 0.5f, 0.2f), ax1, ax2, ax3, ax4, ax5);

            // ---- Schwert + Kriegshorn (Kette)
            _style = "Sword";
            var sw1 = S("SW1_Schnitthieb", "Schnitthieb", "Hieb", SkillDelivery.Melee, 0.5f, 0f, 3f, 0.6f, "Schneller Schwerthieb.");
            Chain(sw1, "Kreuzhieb", "Ausfallhieb", 0.6f, 0.68f, 0.76f, B(EffectType.Cripple, 1, 2f));
            var sw2 = S("SW2_Hechtsprung", "Hechtsprung", "Hecht", SkillDelivery.Leap, 0.25f, 8f, 9f, 1.0f, "Springt zum Ziel. Sprung-Finisher.");
            sw2.LeapDistance = 9f; sw2.Finisher = ComboFinisherType.Leap;
            var sw3 = S("SW3_Ausweichstoss", "Ausweichstoß", "Ausw.", SkillDelivery.Melee, 0.4f, 12f, 3f, 0.8f, "Rollt zur Seite und kontert; weicht aus.");
            sw3.EvadesDuringCast = true; sw3.SelfBuffs.Add(B(EffectType.Vigor, 1, 4f));
            var sw4 = S("SW4_RufDerWildnis", "Ruf der Wildnis", "Horn", SkillDelivery.Self, 0.25f, 25f, 0f, 0f, "Kriegshorn: Wut und Eile für dich, Verbündete und den Gefährten.");
            sw4.ShareWithAllies = true; sw4.Radius = 6f; sw4.SelfBuffs.Add(B(EffectType.Fury, 1, 6f)); sw4.SelfBuffs.Add(B(EffectType.Swiftness, 1, 8f));
            var sw5 = S("SW5_Vogelschwarm", "Vogelschwarm", "Schwarm", SkillDelivery.Projectile, 0.5f, 30f, 18f, 0.3f, "Ein Schwarm Vögel blendet das Ziel (5 Treffer).");
            sw5.Hits = 5; sw5.HitInterval = 0.4f; sw5.TargetBuffs.Add(B(EffectType.Blind, 1, 2f));
            var setSw = Set("SchwertHorn", "Schwert & Kriegshorn", WeaponKind.SwordWarhorn, WeaponVisual.SwordWarhorn, new Color(0.75f, 0.8f, 0.85f), sw1, sw2, sw3, sw4, sw5);
            _style = null;

            // ---- Heilung, Hilfe (Fallen, Signale, Wildnis), Elite
            var h1 = Heal(S("H1_Heilquelle", "Heilquelle", "Quelle", SkillDelivery.Self, 0.75f, 25f, 0f, 0f, "Heilt dich und erzeugt ein Wasserfeld (5 s)."), 5200f, 1f, true);
            h1.Field = ComboFieldType.Water; h1.FieldDuration = 5f; h1.Palette = VfxPalette.Wasser;
            var h2 = Heal(S("H2_Wildatem", "Wildatem", "Atem", SkillDelivery.Self, 0.5f, 20f, 0f, 0f, "Heilt dich, Verbündete und den Gefährten; Regeneration."), 3800f, 0.8f, false);
            h2.ShareWithAllies = true; h2.Radius = 6f; h2.SelfBuffs.Add(B(EffectType.Regeneration, 1, 8f));
            var h3 = Heal(S("H3_Lebenskraut", "Lebenskraut", "Kraut", SkillDelivery.Self, 1f, 30f, 0f, 0f, "Starke Heilung, entfernt 2 Zustände."), 6000f, 1.1f, false);
            h3.CleanseCount = 2;
            var t1 = Util(S("T1_Dornfalle", "Dornfalle", "Dorn", SkillDelivery.Self, 0.5f, 20f, 0f, 0.5f, "Falle: löst bei Kontakt aus – 4 Blutung, Verkrüppelung."), UtilityCategory.Trap, 1, true);
            t1.Special = SkillSpecial.TrapPlace; t1.TrapLifetime = 20f; t1.Radius = 3f; t1.TargetBuffs.Add(B(EffectType.Bleeding, 4, 8f)); t1.TargetBuffs.Add(B(EffectType.Cripple, 1, 3f));
            var t2 = Util(S("T2_Flammenfalle", "Flammenfalle", "Flamme", SkillDelivery.Self, 0.5f, 24f, 0f, 0.6f, "Falle: Feuerfeld und 3 Brennen bei Kontakt."), UtilityCategory.Trap, 2, false);
            t2.Special = SkillSpecial.TrapPlace; t2.TrapLifetime = 20f; t2.Radius = 3.5f; t2.Field = ComboFieldType.Fire; t2.FieldDuration = 4f; t2.TargetBuffs.Add(B(EffectType.Burning, 3, 4f)); t2.Palette = VfxPalette.Feuer;
            var t3 = Util(S("T3_Frostfalle", "Frostfalle", "Frost", SkillDelivery.Self, 0.5f, 28f, 0f, 0.5f, "Falle: Kälte und Immobilisierung bei Kontakt."), UtilityCategory.Trap, 3, false);
            t3.Special = SkillSpecial.TrapPlace; t3.TrapLifetime = 20f; t3.Radius = 3.5f; t3.TargetBuffs.Add(B(EffectType.Chill, 1, 4f)); t3.TargetBuffs.Add(B(EffectType.Immobilize, 1, 1.5f)); t3.Palette = VfxPalette.Frost;
            var g1 = Util(S("G1_Sammelruf", "Sammelruf", "Sammeln", SkillDelivery.Self, 0f, 25f, 0f, 0f, "Signal: 5 Macht und Wut für dich, Verbündete und den Gefährten."), UtilityCategory.Signal, 1, false);
            g1.ShareWithAllies = true; g1.Radius = 8f; g1.SelfBuffs.Add(B(EffectType.Might, 5, 10f)); g1.SelfBuffs.Add(B(EffectType.Fury, 1, 6f));
            var g2 = Util(S("G2_Schutzruf", "Schutzruf", "Schutz", SkillDelivery.Self, 0f, 30f, 0f, 0f, "Signal: Schutz und Regeneration für alle."), UtilityCategory.Signal, 2, false);
            g2.ShareWithAllies = true; g2.Radius = 8f; g2.SelfBuffs.Add(B(EffectType.Protection, 1, 5f)); g2.SelfBuffs.Add(B(EffectType.Regeneration, 1, 8f));
            var g3 = Util(S("G3_Jagdruf", "Jagdruf", "Jagd", SkillDelivery.Self, 0f, 35f, 0f, 0f, "Signal: Schnelligkeit und Eile für alle."), UtilityCategory.Signal, 3, false);
            g3.ShareWithAllies = true; g3.Radius = 8f; g3.SelfBuffs.Add(B(EffectType.Quickness, 1, 4f)); g3.SelfBuffs.Add(B(EffectType.Swiftness, 1, 10f));
            var w1 = Util(S("W1_Reinigungskraeuter", "Reinigungskräuter", "Kräuter", SkillDelivery.Self, 0.25f, 25f, 0f, 0f, "Wildnis: entfernt 3 Zustände, Widerstand."), UtilityCategory.Wilderness, 1, false);
            w1.CleanseCount = 3; w1.SelfBuffs.Add(B(EffectType.Resistance, 1, 3f));
            var w2 = Util(S("W2_Tarnblatt", "Tarnblatt", "Tarnen", SkillDelivery.Self, 0f, 30f, 0f, 0f, "Wildnis: 3 s Tarnung."), UtilityCategory.Wilderness, 2, false);
            w2.StealthSeconds = 3f; w2.Palette = VfxPalette.Schatten;
            var w3 = Util(S("W3_Quellgrund", "Quellgrund", "Quelle", SkillDelivery.GroundTarget, 0.5f, 30f, 15f, 0f, "Wildnis: Heilquelle am Zielort (Wasserfeld, Regeneration)."), UtilityCategory.Wilderness, 3, false);
            w3.Radius = 4f; w3.Field = ComboFieldType.Water; w3.FieldDuration = 8f; w3.HealBase = 1800f; w3.HealCoefficient = 0.3f; w3.ShareWithAllies = true; w3.SelfBuffs.Add(B(EffectType.Regeneration, 1, 5f)); w3.Palette = VfxPalette.Wasser;
            var e1 = Elite(S("E1_Rudelruf", "Rudelruf", "ELITE", SkillDelivery.Self, 0.5f, 90f, 0f, 0f, "Elite: 10 Macht, Wut und Schnelligkeit für dich, Verbündete und den Gefährten."), true);
            e1.ShareWithAllies = true; e1.Radius = 8f; e1.SelfBuffs.Add(B(EffectType.Might, 10, 15f)); e1.SelfBuffs.Add(B(EffectType.Fury, 1, 10f)); e1.SelfBuffs.Add(B(EffectType.Quickness, 1, 5f));
            var e2 = Elite(S("E2_Urgewalt", "Urgewalt", "Urgewalt", SkillDelivery.Self, 0.5f, 120f, 0f, 0f, "Elite: Stabilität, Schutz und Regeneration; heilt dich."), false);
            e2.HealBase = 3000f; e2.HealCoefficient = 0.5f; e2.SelfBuffs.Add(B(EffectType.Stability, 5, 10f)); e2.SelfBuffs.Add(B(EffectType.Protection, 1, 6f)); e2.SelfBuffs.Add(B(EffectType.Regeneration, 1, 10f));

            // ---- Gefährten-Familien-Skills (F2)
            var pw = S("Pet_Wolf", "Rudelheulen", "Heulen", SkillDelivery.Self, 0f, 25f, 0f, 0f, "Wolf: Wut für dich und den Gefährten; Feinde in der Nähe werden verkrüppelt.");
            pw.Id = "pet_wolf_howl"; pw.Radius = 6f; pw.ShareWithAllies = true; pw.SelfBuffs.Add(B(EffectType.Fury, 1, 6f)); pw.TargetBuffs.Add(B(EffectType.Cripple, 1, 3f)); pw.SlotType = SkillSlotType.Utility;
            var pb = S("Pet_Bear", "Bärenbrüllen", "Brüllen", SkillDelivery.Self, 0f, 25f, 0f, 0f, "Bär: verspottet Feinde in der Nähe und gewährt dem Gefährten Schutz.");
            pb.Id = "pet_bear_roar"; pb.Radius = 6f; pb.TargetBuffs.Add(B(EffectType.Taunt, 1, 2f)); pb.SelfBuffs.Add(B(EffectType.Protection, 1, 5f)); pb.SlotType = SkillSlotType.Utility;
            var pf = S("Pet_Falcon", "Sturzflug", "Sturz", SkillDelivery.Melee, 0f, 20f, 3f, 1.0f, "Falke: Sturzflug auf das Ziel, Blindheit; du erhältst Eile.");
            pf.Id = "pet_falcon_dive"; pf.TargetBuffs.Add(B(EffectType.Blind, 1, 3f)); pf.SelfBuffs.Add(B(EffectType.Swiftness, 1, 6f)); pf.SlotType = SkillSlotType.Utility;
            foreach (var p in new[] { pw, pb, pf }) { p.StartsUnlocked = true; p.UnlockCost = 0; p.WeaponStyleOverride = ""; }

            d.WeaponSets.AddRange(new[] { setLb, setAx, setSw });
            d.StartWeapons = new[] { WeaponKind.Longbow, WeaponKind.SwordWarhorn };
            d.SkillPool.AddRange(new[] { h1, h2, h3, t1, t2, t3, g1, g2, g3, w1, w2, w3, e1, e2 });
            d.ExtraSkills.AddRange(new[] { pw, pb, pf });
            d.Palette = VfxPalette.Natur; d.SignatureSkill = "LB5_Dornenregen";
            d.Specializations = RangerTraits();
            return d;
        }
    }
}

namespace ProjectTyrant.EditorTools
{
    public static partial class ClassBuilder
    {
        /// <summary>Gefährten (geriggte Vierbeiner/Vogel) als Archetyp + Prefab: Wolf, Bär, Falke.</summary>
        static void BuildPets()
        {
            Pet("Wolf", "Wolf", 1.0f, 2.6f, 1.6f, 7f, p => CreatureBuilder.Quadruped(p, "PetWolf", new CreatureBuilder.Look { Body = new Color(0.45f, 0.42f, 0.38f), Belly = new Color(0.75f, 0.7f, 0.62f), Accent = new Color(0.3f, 0.6f, 0.25f), Eye = new Color(1.4f, 1.1f, 0.3f) }, 1.3f, 0.55f, 0.55f, 0.35f, 0.6f, true, false, true));
            Pet("Baer", "Bär", 1.4f, 3.2f, 2.0f, 5.5f, p => CreatureBuilder.Quadruped(p, "PetBaer", new CreatureBuilder.Look { Body = new Color(0.36f, 0.25f, 0.16f), Belly = new Color(0.5f, 0.38f, 0.26f), Accent = new Color(0.3f, 0.6f, 0.25f), Eye = new Color(0.2f, 0.15f, 0.1f), Scale = 1.35f }, 1.5f, 0.6f, 0.8f, 0.3f, 0.15f, true, false, true));
            Pet("Falke", "Falke", 0.7f, 2.2f, 1.3f, 8f, p => CreatureBuilder.Bird(p, "PetFalke", new CreatureBuilder.Look { Body = new Color(0.55f, 0.42f, 0.28f), Belly = new Color(0.9f, 0.85f, 0.75f), Accent = new Color(0.95f, 0.75f, 0.2f), Eye = new Color(1.5f, 1.1f, 0.2f), Scale = 0.9f }));
        }

        static void Pet(string id, string name, float hp, float coef, float interval, float speed, System.Func<Transform, GameObject> visual)
        {
            var a = Asset<EnemyArchetype>(Root + "/wildhueter/Pet_" + id + ".asset");
            a.DisplayName = name; a.Tier = EnemyTier.None; a.Team = Team.Ally; a.HealthFactor = hp; a.AttackCoefficient = coef; a.AttackRange = 2.4f; a.AttackInterval = interval;
            a.AttackWindup = NpcBrain.MinWindup; a.MoveSpeed = speed; a.AggroRange = 12f; a.LeashRange = 1000f; a.Scale = 1f; a.Experience = 0; a.ContributionValue = 0f; a.ArmorFactor = 1.6f;
            a.Palette = VfxPalette.Natur; a.Loot = null;
            string path = "Assets/ProjectTyrant/Prefabs/Characters/PT_Pet_" + id + ".prefab";
            var go = new GameObject("PT_Pet_" + id); go.layer = CharacterLayer;
            var vis = visual(go.transform);
            AddPlaceholder(vis, "pet_" + id.ToLowerInvariant(), "Tiergefährte " + name + ", stilisiert, Low-Poly");
            var col = go.AddComponent<CapsuleCollider>(); col.height = 1.2f; col.radius = 0.45f; col.center = new Vector3(0, 0.6f, 0);
            var c = go.AddComponent<Combatant>(); c.Team = Team.Ally; c.Tier = EnemyTier.None; c.DisplayName = name;
            var brain = go.AddComponent<NpcBrain>(); brain.Archetype = a; brain.Visual = vis.transform; brain.WanderRadius = 1.5f; brain.UseLeash = false; brain.DestroyOnDeath = false;
            go.AddComponent<NpcAbilities>(); go.AddComponent<HitFlash>();
            CharacterRigs.Drive(go, vis, Anim.AnimDriver.Role.Npc);
            SetLayerRecursive(go, CharacterLayer);
            a.Prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
    }
}
