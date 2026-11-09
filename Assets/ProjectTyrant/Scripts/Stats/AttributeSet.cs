using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Die neun GW2-Attribute.</summary>
    public enum Attr { Power, Precision, Toughness, Vitality, Ferocity, ConditionDamage, HealingPower, Expertise, Concentration }

    /// <summary>Sammelt Beiträge aller Quellen (Basis, Ausrüstung, Eigenschaften, Effekte).</summary>
    public class AttributeAccumulator
    {
        public const int Count = 9;
        public readonly float[] Flat = new float[Count];
        public readonly float[] Percent = new float[Count];
        public float Defense;          // Verteidigung der Rüstungsteile
        public float CritChanceBonus;  // additiv (0.2 = +20 %)
        public float CritDamageBonus;  // additiv (0.1 = +10 %)
        public float HealthPercent;    // +% Lebenspunkte
        /// <summary>Umwandlungen (Anteil von „From“ als „To“), angewandt nach allen Quellen.</summary>
        public readonly List<(Attr from, Attr to, float fraction)> Conversions = new List<(Attr, Attr, float)>();
        public void Add(Attr a, float v) => Flat[(int)a] += v;
        public void AddPercent(Attr a, float v) => Percent[(int)a] += v;
        public void Clear()
        {
            Array.Clear(Flat, 0, Count); Array.Clear(Percent, 0, Count);
            Defense = 0f; CritChanceBonus = 0f; CritDamageBonus = 0f; HealthPercent = 0f; Conversions.Clear();
        }
    }

    public interface IAttributeSource
    {
        void Contribute(AttributeAccumulator acc, int level);
    }

    /// <summary>
    /// Reines Attribut-Modell: Basis (Level) + Ausrüstung + Eigenschaften + Effekte.
    /// Abgeleitete Werte nach GW2-Formeln (Krit-Chance, Krit-Schaden, Rüstung, Lebenspunkte, Dauer-Boni).
    /// </summary>
    public class AttributeSet
    {
        /// <summary>Level-Cap (Lauf 4): GW2-Stufe 80 auf 30 komprimiert – Stufe 30 entspricht den GW2-Werten für 80.</summary>
        public const int MaxLevel = 30;
        public const float ClassBaseHealthL1 = 1300f;
        public const float ClassBaseHealthL80 = 9212f;

        int _level = 1;
        public int Level { get => _level; set { int v = Mathf.Clamp(value, 1, MaxLevel); if (v != _level) { _level = v; MarkDirty(); } } }
        /// <summary>Primärattribute nutzen die Spieler-Basis; NPCs können einen Faktor setzen.</summary>
        public float PrimaryBaseFactor = 1f;
        public bool UsePlayerBase = true;

        readonly List<IAttributeSource> _sources = new List<IAttributeSource>();
        readonly AttributeAccumulator _acc = new AttributeAccumulator();
        readonly float[] _values = new float[AttributeAccumulator.Count];
        bool _dirty = true;

        public event Action Changed;

        public static float PrimaryBase(int level) => Mathf.Lerp(37f, 1000f, (Mathf.Clamp(level, 1, MaxLevel) - 1) / (float)(MaxLevel - 1));
        /// <summary>Skalierungsfaktor 0..1 relativ zu Level 80 (für level-abhängige Basiswerte).</summary>
        public static float LevelScale(int level) => PrimaryBase(level) / 1000f;

        public void AddSource(IAttributeSource s) { if (s != null && !_sources.Contains(s)) { _sources.Add(s); MarkDirty(); } }
        public void RemoveSource(IAttributeSource s) { if (_sources.Remove(s)) MarkDirty(); }
        public void MarkDirty() { _dirty = true; Changed?.Invoke(); }

        void Recalculate()
        {
            if (!_dirty) return;
            _dirty = false;
            _acc.Clear();
            float pb = PrimaryBase(_level) * PrimaryBaseFactor;
            _acc.Add(Attr.Power, pb); _acc.Add(Attr.Precision, pb); _acc.Add(Attr.Toughness, pb); _acc.Add(Attr.Vitality, pb);
            for (int i = 0; i < _sources.Count; i++) _sources[i].Contribute(_acc, _level);
            for (int i = 0; i < AttributeAccumulator.Count; i++)
                _values[i] = Mathf.Max(0f, _acc.Flat[i] * (1f + _acc.Percent[i]));
            if (_acc.Conversions.Count > 0)
            {
                Array.Copy(_values, _preConversion, _values.Length);
                foreach (var c in _acc.Conversions) _values[(int)c.to] += _preConversion[(int)c.from] * c.fraction;
            }
        }
        readonly float[] _preConversion = new float[AttributeAccumulator.Count];

        public float Get(Attr a) { Recalculate(); return _values[(int)a]; }
        public float Defense { get { Recalculate(); return _acc.Defense; } }

        /// <summary>5 % beim Grundwert, +1 % je 21 Präzision über Basis (auf Level 80 normiert).</summary>
        public float CritChance
        {
            get
            {
                Recalculate();
                float basePrec = PrimaryBase(_level) * PrimaryBaseFactor;
                float per = 21f * Mathf.Max(0.05f, LevelScale(_level));
                return Mathf.Clamp01(0.05f + (Get(Attr.Precision) - basePrec) / per / 100f + _acc.CritChanceBonus);
            }
        }

        /// <summary>150 % + Wildheit/15 %.</summary>
        public float CritDamage { get { Recalculate(); return 1.5f + Get(Attr.Ferocity) / 1500f + _acc.CritDamageBonus; } }
        public float Armor => Get(Attr.Toughness) + Defense;
        public float MaxHealth
        {
            get
            {
                Recalculate();
                float baseHp = Mathf.Lerp(ClassBaseHealthL1, ClassBaseHealthL80, (_level - 1) / (float)(MaxLevel - 1));
                return (baseHp + Get(Attr.Vitality) * 10f) * (1f + _acc.HealthPercent);
            }
        }
        /// <summary>Zustandsdauer-Bonus (Sachkenntnis/15 %), max. +100 %.</summary>
        public float ConditionDurationBonus => Mathf.Min(1f, Get(Attr.Expertise) / 1500f);
        /// <summary>Segensdauer-Bonus (Konzentration/15 %), max. +100 %.</summary>
        public float BoonDurationBonus => Mathf.Min(1f, Get(Attr.Concentration) / 1500f);

        public static string AttrName(Attr a)
        {
            switch (a)
            {
                case Attr.Power: return "Kraft";
                case Attr.Precision: return "Präzision";
                case Attr.Toughness: return "Zähigkeit";
                case Attr.Vitality: return "Vitalität";
                case Attr.Ferocity: return "Wildheit";
                case Attr.ConditionDamage: return "Zustandsschaden";
                case Attr.HealingPower: return "Heilkraft";
                case Attr.Expertise: return "Sachkenntnis";
                case Attr.Concentration: return "Konzentration";
            }
            return a.ToString();
        }
    }

    /// <summary>Einfache feste Attributquelle (Nahrung, Debug, NPC-Profile).</summary>
    public class FlatAttributeSource : IAttributeSource
    {
        public readonly float[] Values = new float[AttributeAccumulator.Count];
        public float Defense;
        public void Contribute(AttributeAccumulator acc, int level)
        {
            for (int i = 0; i < Values.Length; i++) acc.Flat[i] += Values[i];
            acc.Defense += Defense;
        }
    }
}
