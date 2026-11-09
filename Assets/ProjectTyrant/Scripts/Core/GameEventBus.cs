using System;
using UnityEngine;

namespace ProjectTyrant
{
    public enum Team { Player, Ally, Enemy, Neutral }

    public struct DamageInfo
    {
        public Combatant Source;
        public Combatant Target;
        public float Amount;
        public bool IsCondition;
        public bool Evaded;
        public bool Blocked;
        public bool Missed;
        public bool Crit;
        public bool Glancing;
        public Vector3 Point;
        public SkillDefinition Skill;
        /// <summary>Lauf 5: stärkster Zustand eines Zustands-Ticks (Farbe im Kampftext).</summary>
        public EffectType Condition;
    }

    /// <summary>Zentraler Event-Bus (reine C#-Events). Systeme kommunizieren hierüber statt über Singletons.</summary>
    public static class GameEventBus
    {
        public static event Action<DamageInfo> DamageDealt;
        public static event Action<Combatant, float> Healed;
        public static event Action<Combatant, Combatant> EntityKilled;      // victim, killer
        public static event Action<Combatant> PlayerDowned;
        public static event Action<Combatant> PlayerRallied;
        public static event Action<Combatant> PlayerDefeated;
        public static event Action<Combatant> PlayerRespawned;
        public static event Action<Combatant, SkillDefinition> SkillCast;
        public static event Action<ComboEntry, Vector3> ComboTriggered;
        public static event Action<string, Color> Notification;
        public static event Action<float> ContributionAdded;
        public static event Action EventStateChanged;
        public static event Action<Combatant, Combatant> CritLanded;
        public static event Action<Combatant> DodgePerformed;
        public static event Action<Combatant, int> LeveledUp;
        public static event Action<int, string> XpGained;
        public static event Action<string, string> AreaEntered;          // Name, Untertitel
        public static event Action<string, string, Color> Banner;        // Titel, Untertitel, Farbe
        public static event Action ProfileChanged;
        public static event Action<string> ZoneProgress;                 // Marker-Id
        public static event Action<string, int> ItemAcquired;            // Item-Id, Anzahl
        public static event Action<Combatant, SkillDefinition> HealSkillUsed;
        public static event Action<Combatant> WeaponSwapped;

        public static void RaiseDamage(DamageInfo d) => DamageDealt?.Invoke(d);
        /// <summary>Lauf 5: Aktivierung unterbrochen (Kampftext „Unterbrochen“, Splitter-Effekt, dumpfer Ton).</summary>
        public static event Action<Combatant, SkillDefinition> CastInterrupted;
        public static void RaiseInterrupted(Combatant c, SkillDefinition s) => CastInterrupted?.Invoke(c, s);
        public static void RaiseHealed(Combatant c, float amount) => Healed?.Invoke(c, amount);
        public static void RaiseKilled(Combatant victim, Combatant killer) => EntityKilled?.Invoke(victim, killer);
        public static void RaisePlayerDowned(Combatant c) => PlayerDowned?.Invoke(c);
        public static void RaisePlayerRallied(Combatant c) => PlayerRallied?.Invoke(c);
        public static void RaisePlayerDefeated(Combatant c) => PlayerDefeated?.Invoke(c);
        public static void RaisePlayerRespawned(Combatant c) => PlayerRespawned?.Invoke(c);
        public static void RaiseSkillCast(Combatant c, SkillDefinition s) => SkillCast?.Invoke(c, s);
        public static void RaiseCombo(ComboEntry e, Vector3 p) => ComboTriggered?.Invoke(e, p);
        public static void Notify(string msg, Color color) => Notification?.Invoke(msg, color);
        public static void Notify(string msg) => Notification?.Invoke(msg, Color.white);
        public static void RaiseContribution(float amount) => ContributionAdded?.Invoke(amount);
        public static void RaiseEventStateChanged() => EventStateChanged?.Invoke();

        public static void RaiseCrit(Combatant s, Combatant t) => CritLanded?.Invoke(s, t);
        public static void RaiseDodge(Combatant c) => DodgePerformed?.Invoke(c);
        public static void RaiseLevelUp(Combatant c, int lvl) => LeveledUp?.Invoke(c, lvl);
        public static void RaiseXp(int amount, string reason) => XpGained?.Invoke(amount, reason);
        public static void RaiseAreaEntered(string name, string sub) => AreaEntered?.Invoke(name, sub);
        public static void RaiseBanner(string title, string sub, Color c) => Banner?.Invoke(title, sub, c);
        public static void RaiseProfileChanged() => ProfileChanged?.Invoke();
        public static void RaiseZoneProgress(string id) => ZoneProgress?.Invoke(id);
        public static void RaiseItemAcquired(string id, int n) => ItemAcquired?.Invoke(id, n);
        public static void RaiseHealSkill(Combatant c, SkillDefinition s) => HealSkillUsed?.Invoke(c, s);
        public static void RaiseWeaponSwap(Combatant c) => WeaponSwapped?.Invoke(c);

        /// <summary>Entfernt alle Abonnenten (Domain-Reload-sicher, auch für Tests).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            DamageDealt = null; Healed = null; EntityKilled = null; PlayerDowned = null; PlayerRallied = null;
            PlayerDefeated = null; PlayerRespawned = null; SkillCast = null; ComboTriggered = null;
            Notification = null; ContributionAdded = null; EventStateChanged = null;
            CritLanded = null; DodgePerformed = null; LeveledUp = null; XpGained = null; AreaEntered = null; Banner = null;
            ProfileChanged = null; ZoneProgress = null; ItemAcquired = null; HealSkillUsed = null; WeaponSwapped = null;
        }
    }
}
