using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum ComboResultType { AreaBuffAllies, AreaHealAllies, AreaDebuffEnemies, AreaDamageEnemies, ProjectileOnHitDebuff, ProjectileOnHitSelfBuff }

    [Serializable]
    public class ComboEntry
    {
        public ComboFieldType Field;
        public ComboFinisherType Finisher;
        public string ResultName = "Combo";
        public ComboResultType ResultType;
        public EffectApplication Buff;
        public float Amount;      // Heilung (Basis auf Level 80) bzw. Referenzwert
        public float Coefficient; // Schadens-Koeffizient (AreaDamageEnemies)
        public int Cleanse;        // entfernte Zustände (Wasser-Combos)
        public float Radius = 5f;
    }

    /// <summary>Feld × Finisher → Ergebnis. Datengetriebene Combo-Tabelle.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Combo Table", fileName = "ComboTable")]
    public class ComboTable : ScriptableObject
    {
        public List<ComboEntry> Entries = new List<ComboEntry>();

        public bool TryGet(ComboFieldType field, ComboFinisherType finisher, out ComboEntry entry)
        {
            entry = null;
            if (field == ComboFieldType.None || finisher == ComboFinisherType.None) return false;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Field == field && Entries[i].Finisher == finisher) { entry = Entries[i]; return true; }
            }
            return false;
        }

        /// <summary>Standardtabelle – wird vom Editor-Builder als Asset gespeichert und in Tests verglichen.</summary>
        public static List<ComboEntry> CreateDefaultEntries()
        {
            return new List<ComboEntry>
            {
                new ComboEntry { Field = ComboFieldType.Fire, Finisher = ComboFinisherType.Blast, ResultName = "Flammende Macht", ResultType = ComboResultType.AreaBuffAllies, Buff = new EffectApplication(EffectType.Might, 3, 20f), Radius = 6f },
                new ComboEntry { Field = ComboFieldType.Fire, Finisher = ComboFinisherType.Leap, ResultName = "Glutlandung", ResultType = ComboResultType.AreaDebuffEnemies, Buff = new EffectApplication(EffectType.Burning, 3, 4f), Radius = 4f },
                new ComboEntry { Field = ComboFieldType.Fire, Finisher = ComboFinisherType.Projectile, ResultName = "Brandgeschoss", ResultType = ComboResultType.ProjectileOnHitDebuff, Buff = new EffectApplication(EffectType.Burning, 1, 3f) },
                new ComboEntry { Field = ComboFieldType.Fire, Finisher = ComboFinisherType.Whirl, ResultName = "Funkenwirbel", ResultType = ComboResultType.AreaDamageEnemies, Amount = 180f, Coefficient = 0.6f, Buff = new EffectApplication(EffectType.Burning, 1, 3f), Radius = 5f },
                new ComboEntry { Field = ComboFieldType.Water, Finisher = ComboFinisherType.Blast, ResultName = "Heilende Woge", ResultType = ComboResultType.AreaHealAllies, Amount = 1200f, Cleanse = 1, Radius = 6f },
                new ComboEntry { Field = ComboFieldType.Water, Finisher = ComboFinisherType.Leap, ResultName = "Quellsprung", ResultType = ComboResultType.AreaHealAllies, Amount = 900f, Cleanse = 1, Radius = 3f },
                new ComboEntry { Field = ComboFieldType.Water, Finisher = ComboFinisherType.Projectile, ResultName = "Lebensgeschoss", ResultType = ComboResultType.ProjectileOnHitSelfBuff, Buff = new EffectApplication(EffectType.Regeneration, 1, 3f) },
                new ComboEntry { Field = ComboFieldType.Lightning, Finisher = ComboFinisherType.Blast, ResultName = "Sturmesschub", ResultType = ComboResultType.AreaBuffAllies, Buff = new EffectApplication(EffectType.Swiftness, 1, 8f), Radius = 6f },
                new ComboEntry { Field = ComboFieldType.Lightning, Finisher = ComboFinisherType.Projectile, ResultName = "Blitzgeschoss", ResultType = ComboResultType.ProjectileOnHitDebuff, Buff = new EffectApplication(EffectType.Vulnerability, 2, 6f) },
                new ComboEntry { Field = ComboFieldType.Lightning, Finisher = ComboFinisherType.Whirl, ResultName = "Funkenschlag", ResultType = ComboResultType.AreaDebuffEnemies, Buff = new EffectApplication(EffectType.Vulnerability, 4, 8f), Radius = 5f },
                new ComboEntry { Field = ComboFieldType.Smoke, Finisher = ComboFinisherType.Blast, ResultName = "Rauchschleier-Tarnung", ResultType = ComboResultType.AreaBuffAllies, Buff = new EffectApplication(EffectType.Stealth, 1, 3f), Radius = 5f },
                new ComboEntry { Field = ComboFieldType.Smoke, Finisher = ComboFinisherType.Leap, ResultName = "Schattensprung", ResultType = ComboResultType.AreaBuffAllies, Buff = new EffectApplication(EffectType.Stealth, 1, 2f), Radius = 2f },
            };
        }
    }
}
