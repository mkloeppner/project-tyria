using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Fix-Lauf F1 – Auto-Angriffs-Ketten aus einer Waffen-Vorlage: jede Nah- und Fernkampfwaffe (außer Langbogen, Kanal-Angriffen und den
    /// Einstimmungs-Angriffen des Elementarwebers) hat Skill 1 als Kette aus 3 Gliedern – Glied 1 und 2 Basis-Koeffizient, Glied 3 +40 % mit Bonus je Waffe.
    /// </summary>
    public static partial class ClassBuilder
    {
        public const float ChainBonusFactor = 1.4f;

        public struct ChainBonusDef { public EffectApplication Target, Self; public int Targets; public string Text; }

        public static ChainBonusDef BonusFor(WeaponKind k)
        {
            switch (k)
            {
                case WeaponKind.Greatsword: return new ChainBonusDef { Self = B(EffectType.Might, 1, 8f), Targets = 3, Text = "trifft bis zu 3 Ziele, 1 Macht" };
                case WeaponKind.Staff: return new ChainBonusDef { Target = B(EffectType.Vulnerability, 2, 6f), Targets = 3, Text = "durchschlägt (bis zu 3 Ziele), 2 Verwundbarkeit" };
                case WeaponKind.DaggerDagger: case WeaponKind.DaggerWarhorn: return new ChainBonusDef { Target = B(EffectType.Bleeding, 2, 6f), Text = "2 Blutung" };
                case WeaponKind.AxeTorch: case WeaponKind.AxeDagger: return new ChainBonusDef { Self = B(EffectType.Fury, 1, 4f), Text = "Wut (4 s)" };
                case WeaponKind.Hammer: return new ChainBonusDef { Target = B(EffectType.Weakness, 1, 3f), Text = "Schwäche (3 s)" };
                case WeaponKind.ScepterFocus: case WeaponKind.ScepterTorch: case WeaponKind.ScepterPistol: case WeaponKind.ScepterWarhorn: case WeaponKind.ScepterDagger: return new ChainBonusDef { Target = B(EffectType.Burning, 1, 3f), Text = "Brennen (3 s)" };
                case WeaponKind.SwordWarhorn: case WeaponKind.SwordPistol: case WeaponKind.SwordShield: case WeaponKind.SwordFocus: return new ChainBonusDef { Target = B(EffectType.Cripple, 1, 2f), Text = "Verkrüppelung (2 s)" };
                case WeaponKind.Shortbow: return new ChainBonusDef { Target = B(EffectType.Poison, 1, 4f), Text = "Gift (4 s)" };
                case WeaponKind.Rifle: return new ChainBonusDef { Target = B(EffectType.Vulnerability, 3, 6f), Text = "3 Verwundbarkeit" };
            }
            return new ChainBonusDef { Target = B(EffectType.Vulnerability, 1, 5f), Text = "1 Verwundbarkeit" };
        }

        /// <summary>Darf diese Waffe eine Kette haben? (Langbogen: Distanz-Bonus statt Kette wie GW2; Kanäle; Einstimmungs-Angriffe: eigener Einzel-Auto-Angriff je Element.)</summary>
        public static bool ChainEligible(ClassDefinition c, WeaponSet w)
        {
            if (w == null || w.Skills == null || w.Skills.Length == 0 || w.Skills[0] == null) return false;
            if (w.Kind == WeaponKind.Longbow || w.Kind == WeaponKind.None) return false;
            if (c != null && c.Mechanic == ClassMechanicKind.Attunement) return false;
            return w.Skills[0].Hits <= 1;
        }

        static void NormalizeChains(ClassLibrary lib)
        {
            foreach (var c in lib.Classes)
            {
                Begin(c.Id, c.Color, c.Palette);
                var sets = new List<WeaponSet>(c.WeaponSets); foreach (var e in c.EliteSpecs) if (e != null && e.Weapon != null) sets.Add(e.Weapon);
                foreach (var w in sets)
                {
                    if (!ChainEligible(c, w)) continue;
                    var s1 = w.Skills[0]; _dirty.Add(s1);
                    bool ranged = s1.Delivery == SkillDelivery.Projectile;
                    // Durchschnitt der Kette bleibt (gleiche Zahlen wie vorher): Basis = Mittel × 3 / (2 + 1,4)
                    float avg = s1.Chain != null && s1.Chain.Length == 2 ? (s1.Coefficient + s1.Chain[0].Coefficient + s1.Chain[1].Coefficient) / 3f : s1.Coefficient;
                    float baseCoef = Mathf.Round(avg * 3f / (2f + ChainBonusFactor) * 1000f) / 1000f;
                    s1.Coefficient = baseCoef;
                    if (s1.Chain == null || s1.Chain.Length < 2)
                    {
                        _pal = s1.Palette; _style = s1.WeaponStyleOverride;
                        string root = s1.DisplayName;
                        var keepBuffs = new List<EffectApplication>(s1.TargetBuffs);
                        Chain(s1, root + (ranged ? " · Folgeschuss" : " · Folgehieb"), root + (ranged ? " · Abschlussschuss" : " · Abschlusshieb"), baseCoef, baseCoef, baseCoef * ChainBonusFactor, B(EffectType.Vulnerability, 1, 1f));
                        s1.TargetBuffs = keepBuffs;
                        _style = null;
                    }
                    var b = s1.Chain[0]; var f = s1.Chain[1]; _dirty.Add(b); _dirty.Add(f);
                    b.Coefficient = baseCoef; f.Coefficient = baseCoef * ChainBonusFactor;
                    b.TargetBuffs = new List<EffectApplication>(s1.TargetBuffs); b.SelfBuffs = new List<EffectApplication>(s1.SelfBuffs); b.TargetCount = s1.TargetCount; b.ChainBonus = false;
                    var bonus = BonusFor(w.Kind);
                    f.TargetBuffs = new List<EffectApplication>(s1.TargetBuffs); f.SelfBuffs = new List<EffectApplication>(s1.SelfBuffs);
                    if (bonus.Target.Stacks > 0) f.TargetBuffs.Add(bonus.Target);
                    if (bonus.Self.Stacks > 0) f.SelfBuffs.Add(bonus.Self);
                    f.TargetCount = bonus.Targets > 0 ? bonus.Targets : s1.TargetCount;
                    f.ChainBonus = true; s1.ChainBonus = false; b.ChainRoot = s1; f.ChainRoot = s1; s1.ChainRoot = null;
                    foreach (var x in new[] { b, f }) { x.Weapon = s1.Weapon; x.Delivery = s1.Delivery; x.Range = s1.Range; x.CastTime = s1.CastTime; x.ProjectileSpeed = s1.ProjectileSpeed; x.Palette = s1.Palette; x.PaletteExplicit = s1.PaletteExplicit; x.WeaponStyleOverride = s1.WeaponStyleOverride; x.ClassId = s1.ClassId; x.InitiativeCost = 0; x.Cooldown = 0f; }
                    b.Description = "Zweites Glied der Kette (" + Mathf.RoundToInt(baseCoef * 100) + " % Koeffizient).";
                    f.Description = "Kettenabschluss: +" + Mathf.RoundToInt((ChainBonusFactor - 1f) * 100) + " % Schaden, " + bonus.Text + ".";
                }
            }
            _style = null;
        }
    }
}
