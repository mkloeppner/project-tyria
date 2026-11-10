using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Balance-Daten (Profile 0–5, erwarteter Spieler-Stand). Ein Asset, von jeder ZoneRoot referenziert.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Balance/Config")]
    public class BalanceConfig : ScriptableObject
    {
        public DangerProfile[] Levels = new DangerProfile[DangerTable.MaxLevel + 1];
        public ExpectedPlayer Player = new ExpectedPlayer();
        public DangerSettings Get(int level)
        {
            level = Mathf.Clamp(level, 0, DangerTable.MaxLevel);
            return Levels != null && level < Levels.Length && Levels[level] != null ? Levels[level].Settings : DangerTable.Default(level);
        }
    }
}
