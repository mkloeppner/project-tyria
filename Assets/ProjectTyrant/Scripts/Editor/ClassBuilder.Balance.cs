using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// M27 Balancing: Schaden je Klasse und Spezialisierung auf ±15 % des Wächters (gleiches Gear, Duell-Bot im Messmodus).
    /// GW2-nah über die Skill-Werte selbst: Koeffizienten, Feld-Ticks und die Dauer schädigender Zustände werden je Klasse
    /// (alle Skills der Klasse inkl. Gefährte) und zusätzlich je Spezialisierung (nur ihre eigenen Skills) skaliert.
    /// Der Wächter ist die Referenz (Faktor 1, Werte unverändert).
    /// </summary>
    public static partial class ClassBuilder
    {
        /// <summary>Faktor je Klasse (Messung M27 → Kommentar in DECISIONS).</summary>
        public static readonly Dictionary<string, float> ClassDamage = new Dictionary<string, float>
        {
            // Messung M27 (Duell-Bot, Messmodus, 60 s × 2 Durchgänge, Maximum je Eintrag): Wächter 769 · Wildhüter 656 · Elementarweber 348 · Schattenklinge 487 DPS
            { "waechter", 1f }, { "wildhueter", 1.15f }, { "elementarweber", 2.0f }, { "schattenklinge", 1.55f },
        };

        /// <summary>Zusatzfaktor je Elite-Spezialisierung (wirkt nur auf deren eigene Skills).</summary>
        public static readonly Dictionary<string, float> EliteDamage = new Dictionary<string, float>
        {
            // Messung M27 vor Faktoren: Glutrichter 849 · Bollwerkmeister 369 · Sturmherold 674 · Dornenschild 503
            { "glutrichter", 0.9f }, { "bollwerkmeister", 2.0f }, { "sturmherold", 1.15f }, { "dornenschild", 1.5f },
            // Hainwächter 696 · Bestienbund 710 · Dornenwächter 1015 (nach 0,65: 953, nach 0,55: 866–915) · Sturmfalke 905 (Gefährte trägt 35–40 % und wird nur mit dem Klassenfaktor skaliert)
            { "hainwaechter", 0.95f }, { "bestienbund", 0.95f }, { "dornenwaechter", 0.5f }, { "sturmfalke", 0.65f },
            // Sturmrufer 148 · Weber 424 · Katalysator 450 · Glutbinder 388
            { "sturmrufer", 2.0f }, { "weber", 0.9f }, { "katalysator", 0.85f }, { "glutbinder", 1.0f },
            // Wegelagerer 349 · Phantomtänzer 345 · Schattenhüter 314 · Klingenwirbel 483
            { "wegelagerer", 1.4f }, { "phantomtaenzer", 1.45f }, { "schattenhueter", 1.55f }, { "klingenwirbel", 1.0f },
        };

        public static float ClassFactor(string id) => id != null && ClassDamage.TryGetValue(id, out var f) ? f : 1f;
        public static float EliteFactor(string id) => id != null && EliteDamage.TryGetValue(id, out var f) ? f : 1f;

        static bool DamagingCondition(EffectType t) => t == EffectType.Bleeding || t == EffectType.Burning || t == EffectType.Poison || t == EffectType.Torment || t == EffectType.Confusion;

        /// <summary>Schadenswerte eines Skills skalieren (Koeffizient, Feld-Tick, Dauer schädigender Zustände).</summary>
        public static void ScaleDamage(SkillDefinition s, float f)
        {
            if (s == null || Mathf.Approximately(f, 1f)) return;
            s.Coefficient = Mathf.Round(s.Coefficient * f * 1000f) / 1000f;
            s.FieldTickCoefficient = Mathf.Round(s.FieldTickCoefficient * f * 1000f) / 1000f;
            if (s.TargetBuffs != null)
                for (int i = 0; i < s.TargetBuffs.Count; i++)
                {
                    var b = s.TargetBuffs[i];
                    if (DamagingCondition(b.Type)) { b.Duration = Mathf.Round(b.Duration * f * 100f) / 100f; s.TargetBuffs[i] = b; }
                }
            _dirty.Add(s);
        }

        static void ApplyBalance(ClassLibrary lib)
        {
            foreach (var c in lib.Classes)
            {
                if (c == null) continue;
                float fc = ClassFactor(c.Id);
                var core = new HashSet<SkillDefinition>();
                foreach (var s in c.AllSkills()) if (core.Add(s)) ScaleDamage(s, fc);
                if (c.Id == "wildhueter")
                    foreach (var n in new[] { "Wolf", "Baer", "Falke" })
                    {
                        var a = PetArchetypeAsset(n); if (a == null) continue;
                        a.AttackCoefficient = Mathf.Round(a.AttackCoefficient * fc * 1000f) / 1000f; _dirty.Add(a);
                    }
                foreach (var e in c.EliteSpecs)
                {
                    if (e == null) continue;
                    float fe = fc * EliteFactor(e.Id);
                    var seen = new HashSet<SkillDefinition>();
                    foreach (var s in e.AllSkills())
                    {
                        if (s == null || !seen.Add(s)) continue;
                        // Elite-eigene Skills: Klassen- und Spezialisierungsfaktor; bereits als Kern-Skill skaliert → nur Spezialisierungsfaktor
                        ScaleDamage(s, core.Contains(s) ? EliteFactor(e.Id) : fe);
                    }
                }
            }
        }

        static EnemyArchetype PetArchetypeAsset(string n) => AssetDatabase.LoadAssetAtPath<EnemyArchetype>(Root + "/wildhueter/Pet_" + n + ".asset");
    }
}
