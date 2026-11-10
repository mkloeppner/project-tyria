using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Rudel-Verhalten je Gefahrenstufe.</summary>
    public enum PackMode { None, Partner, Group, Ambush, Caller }

    /// <summary>Balance B1: Kontroll-Effekte („Fesseln“), die Gegner einer Stufe einsetzen dürfen.</summary>
    [Flags] public enum ControlKinds { None = 0, Cripple = 1, Chill = 2, Immobilize = 4, Pull = 8, Daze = 16, Dismount = 32 }

    /// <summary>
    /// Balance B1: Werte einer Gefahrenstufe (eine Zeile der Tabelle „Zielbild A“). Reichweiten in GW2-Einheiten (wie im Auftrag),
    /// zur Laufzeit über <see cref="Units"/> in Meter umgerechnet. Reines Datenobjekt – auch für Tests ohne Assets.
    /// </summary>
    [Serializable]
    public class DangerSettings
    {
        public int Level;
        [Range(0, 1)] public float NeutralShare;
        public float AggroRange, Leash, PursuitSpeed, MaxPursuit;
        [Tooltip("Sichtlinie verloren → Aufgabe nach s")] public float LosForget;
        [Tooltip("Zögern vor der Verfolgung (s)")] public float Hesitation;
        [Range(0, 1)] public float ControlChance;
        public ControlKinds Controls;
        public PackMode Pack;
        [Tooltip("Rudel-Aggro-Radius (GW2-Einheiten, 0 = nur Partner)")] public float PackRadius;
        public int MaxGroupSize;
        public float Respawn;
        [Tooltip("Dichte-Faktor (1 = Standard)")] public float Density;
        [Tooltip("Veteranen: 0 keine · 1 einzeln an Lagern · 2 Lager+Events · 3 Patrouillen · 4 Champion-Patrouillen · 5 Eliten")] public int VeteranRule;
        [Tooltip("Fluchtgnade: Spieler 5 s ohne Schaden und weiter entfernt (GW2-Einheiten, 0 = aus)")] public float MercyDistance;
        [Tooltip("Aufgabe bei aufgesessenem Spieler ab Entfernung (GW2-Einheiten, 0 = nur Jäger verfolgen weiter)")] public float MountGiveUp;
        [Tooltip("Telegraph-Mindestaufladung (s)")] public float MinTelegraph;
        [Tooltip("Gegner-Schaden je Treffer (Anteil Spieler-HP), Telegraph-Angriffe dürfen höher")] public float HitShare, TelegraphHitShare;
        [Tooltip("Zeit-bis-Kill Trash (s), Zielkorridor")] public float TtkTrashMin, TtkTrashMax;
        [Tooltip("Zeit-bis-Tod gegen 3 Trash (s), Untergrenze")] public float TtdThreeTrash;
        [Tooltip("Zeit-bis-Tod gegen 1 Veteran (s), 0 = keine Veteranen")] public float TtdVeteran;
        public float TtkVeteranMin, TtkVeteranMax;
        public bool NpcRevive, GuardsHelp, GiveUpHint;
        [Tooltip("Downed: 0 Gnade (Abbruch nach 6 s) · 1 bevorzugt Wachen · 2 Standard · 3 gezielt (Stomp)")] public int DownedMode;
        [Tooltip("Hinterhalt-Gegner / Rufer erlaubt")] public bool Ambushes, Callers;
        [Tooltip("Eile-Phasen der Verfolger")] public bool HastePhases;
        [Tooltip("max. Event-Stufe in dieser Gefahrenstufe")] public int MaxEventTier, MinEventTier;

        public DangerSettings Clone() => (DangerSettings)MemberwiseClone();
    }

    /// <summary>Balance B1: GW2-Einheiten ↔ Meter (1 Einheit = 1 Zoll = 0,0254 m; Nahkampf 130 ≈ 3,3 m, Fernkampf 1.200 ≈ 30 m).</summary>
    public static class Units
    {
        public const float MetersPerUnit = 0.0254f;
        public static float M(float gw2Units) => gw2Units * MetersPerUnit;
        public static float U(float meters) => meters / MetersPerUnit;
    }

    /// <summary>Balance B1: die sechs Gefahrenstufen 0–5 aus „Zielbild A/B/D“ als Standardwerte (Builder schreibt daraus die DangerProfile-Assets).</summary>
    public static class DangerTable
    {
        public const int MaxLevel = 5;
        public static readonly string[] Names = { "Dorfrand", "Umland", "Wildnis", "Grenzland", "Feindland", "Verderbt" };

        public static DangerSettings Default(int level)
        {
            level = Mathf.Clamp(level, 0, MaxLevel);
            var s = new DangerSettings { Level = level };
            switch (level)
            {
                case 0:
                    Set(s, 0.80f, 450, 800, 0.85f, 8, 3, 1f, 0f, ControlKinds.None, PackMode.None, 0, 1, 120, 0.5f, 0);
                    s.MercyDistance = 600; s.MountGiveUp = 400; s.MinTelegraph = 1.2f; s.HitShare = 0.06f; s.TelegraphHitShare = 0.06f;
                    s.TtkTrashMin = 3; s.TtkTrashMax = 4; s.TtdThreeTrash = 30; s.TtdVeteran = 0; s.TtkVeteranMin = 0; s.TtkVeteranMax = 0;
                    s.NpcRevive = true; s.GuardsHelp = true; s.GiveUpHint = true; s.DownedMode = 0; s.MinEventTier = 0; s.MaxEventTier = 0;
                    break;
                case 1:
                    Set(s, 0.60f, 550, 1100, 0.92f, 12, 3, 1f, 0.10f, ControlKinds.Cripple, PackMode.Partner, 0, 2, 90, 0.7f, 1);
                    s.MercyDistance = 600; s.MountGiveUp = 400; s.MinTelegraph = 1.2f; s.HitShare = 0.08f; s.TelegraphHitShare = 0.08f;
                    s.TtkTrashMin = 4; s.TtkTrashMax = 5; s.TtdThreeTrash = 25; s.TtdVeteran = 25; s.TtkVeteranMin = 20; s.TtkVeteranMax = 30;
                    s.NpcRevive = true; s.GuardsHelp = true; s.GiveUpHint = true; s.DownedMode = 1; s.MinEventTier = 0; s.MaxEventTier = 1;
                    break;
                case 2:
                    Set(s, 0.40f, 650, 1400, 1.00f, 18, 3, 0f, 0.25f, ControlKinds.Cripple | ControlKinds.Chill, PackMode.Group, 300, 3, 75, 1f, 2);
                    s.MountGiveUp = 400; s.MinTelegraph = 1.0f; s.HitShare = 0.10f; s.TelegraphHitShare = 0.10f;
                    s.TtkTrashMin = 5; s.TtkTrashMax = 6; s.TtdThreeTrash = 20; s.TtdVeteran = 20; s.TtkVeteranMin = 25; s.TtkVeteranMax = 35;
                    s.GuardsHelp = true; s.DownedMode = 2; s.MinEventTier = 1; s.MaxEventTier = 2;
                    break;
                case 3:
                    Set(s, 0.20f, 750, 1700, 1.05f, 25, 6, 0f, 0.40f, ControlKinds.Cripple | ControlKinds.Chill | ControlKinds.Immobilize, PackMode.Group, 300, 4, 60, 1.2f, 3);
                    s.MountGiveUp = 400; s.MinTelegraph = 1.0f; s.HitShare = 0.12f; s.TelegraphHitShare = 0.12f;
                    s.TtkTrashMin = 5; s.TtkTrashMax = 7; s.TtdThreeTrash = 16; s.TtdVeteran = 16; s.TtkVeteranMin = 30; s.TtkVeteranMax = 40;
                    s.DownedMode = 2; s.MinEventTier = 2; s.MaxEventTier = 3;
                    break;
                case 4:
                    Set(s, 0.10f, 850, 2100, 1.10f, 35, 6, 0f, 0.60f, ControlKinds.Cripple | ControlKinds.Chill | ControlKinds.Immobilize | ControlKinds.Pull | ControlKinds.Daze, PackMode.Ambush, 300, 6, 45, 1.4f, 4);
                    s.MountGiveUp = 0; s.MinTelegraph = 0.75f; s.HitShare = 0.15f; s.TelegraphHitShare = 0.30f;
                    s.TtkTrashMin = 6; s.TtkTrashMax = 8; s.TtdThreeTrash = 12; s.TtdVeteran = 12; s.TtkVeteranMin = 35; s.TtkVeteranMax = 45;
                    s.DownedMode = 3; s.Ambushes = true; s.HastePhases = true; s.MinEventTier = 3; s.MaxEventTier = 4;
                    break;
                default:
                    Set(s, 0.00f, 1000, 2500, 1.15f, 60, 6, 0f, 0.80f, ControlKinds.Cripple | ControlKinds.Chill | ControlKinds.Immobilize | ControlKinds.Pull | ControlKinds.Daze | ControlKinds.Dismount, PackMode.Caller, 300, 8, 30, 1.6f, 5);
                    s.MountGiveUp = 0; s.MinTelegraph = 0.75f; s.HitShare = 0.18f; s.TelegraphHitShare = 0.40f;
                    s.TtkTrashMin = 7; s.TtkTrashMax = 9; s.TtdThreeTrash = 9; s.TtdVeteran = 10; s.TtkVeteranMin = 40; s.TtkVeteranMax = 50;
                    s.DownedMode = 3; s.Ambushes = true; s.Callers = true; s.HastePhases = true; s.MinEventTier = 3; s.MaxEventTier = 4;
                    break;
            }
            return s;
        }

        static void Set(DangerSettings s, float neutral, float aggro, float leash, float speed, float maxPursuit, float los, float hesitate, float ctrl, ControlKinds kinds, PackMode pack, float packR, int group, float respawn, float density, int vet)
        {
            s.NeutralShare = neutral; s.AggroRange = aggro; s.Leash = leash; s.PursuitSpeed = speed; s.MaxPursuit = maxPursuit; s.LosForget = los; s.Hesitation = hesitate;
            s.ControlChance = ctrl; s.Controls = kinds; s.Pack = pack; s.PackRadius = packR; s.MaxGroupSize = group; s.Respawn = respawn; s.Density = density; s.VeteranRule = vet;
        }

        /// <summary>Fessel-Abklingzeit je Gegner (nie mehr als eine Fessel je 6 s).</summary>
        public const float ControlIcd = 6f;
        /// <summary>Kampfende nach 5 s ohne Schaden (GW2).</summary>
        public const float CombatEndDelay = 5f;
        /// <summary>Separation: Mindestabstand Gegner ↔ Spieler (m), Separation nur unter Gegnern.</summary>
        public const float MinPlayerGap = 0.8f;

        /// <summary>Fessel-Art erlaubt? (Stufe 1: nur Verkrüppelung).</summary>
        public static bool Allows(DangerSettings s, ControlKinds k) => s != null && (s.Controls & k) == k;

        public static ControlKinds KindOf(EffectType t)
        {
            switch (t)
            {
                case EffectType.Cripple: return ControlKinds.Cripple;
                case EffectType.Chill: return ControlKinds.Chill;
                case EffectType.Immobilize: return ControlKinds.Immobilize;
                case EffectType.Stun: return ControlKinds.Daze;
            }
            return ControlKinds.None;
        }
    }
}
