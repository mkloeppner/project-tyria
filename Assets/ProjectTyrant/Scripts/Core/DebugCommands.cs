using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Befehle des Debug-Panels (F9). Auch für Live-Checks per execute_code nutzbar.</summary>
    public static partial class DebugCommands
    {
        public static PlayerProfile Profile => PlayerProfile.Instance;
        public static Combatant Player => GameManager.Instance != null ? GameManager.Instance.Player : null;

        public static string SetLevel(int level)
        {
            if (Profile == null) return "Kein Profil";
            Profile.Progression.SetLevel(level);
            if (Player != null) { Player.Level = Profile.EffectiveLevel; Player.Stats.MarkDirty(); Player.SetHealth(Player.MaxHealth); }
            GameEventBus.RaiseProfileChanged();
            return "Level " + Profile.Progression.Level;
        }

        public static string GiveXp(int xp) { if (Profile == null) return "-"; Profile.GrantXp(xp, "Debug"); return "+" + xp + " EP"; }
        public static string GiveHeroPoints(int n) { if (Profile == null) return "-"; Profile.Progression.GrantHeroPoints(n); return "+" + n + " Heldenpunkte"; }
        public static string GiveGold(int gold) { if (Profile == null) return "-"; Profile.Wallet.AddCopper(WalletModel.FromGold(gold)); return "+" + gold + " Gold"; }
        public static string GiveKarma(int k) { if (Profile == null) return "-"; Profile.Wallet.AddKarma(k); return "+" + k + " Karma"; }

        public static bool GodMode { get; private set; }
        public static string ToggleGodMode()
        {
            GodMode = !GodMode;
            if (Player != null)
            {
                Player.ManualInvulnerable = GodMode;
                var pc = Player.GetComponent<PlayerController>();
                if (pc != null) pc.GodMode = GodMode;
            }
            return "Gott-Modus " + (GodMode ? "an" : "aus");
        }

        public static string Save() { if (Profile == null) return "-"; Profile.Save("Debug"); return "Gespeichert: " + SaveSystem.CurrentPath; }
        public static string Load() { if (Profile == null) return "-"; return Profile.Load() ? "Geladen" : "Kein Speicherstand"; }
        public static string NewGame() { if (Profile == null) return "-"; Profile.NewGame(); return "Neues Spiel"; }
        public static string DeleteSave() { SaveSystem.Delete(); return "Speicherstand gelöscht"; }

        public static string Teleport(Vector3 pos)
        {
            if (Player == null) return "-";
            var pc = Player.GetComponent<PlayerController>();
            pc.Teleport(ZoneAnchors.GroundAt(pos + Vector3.up * 3f) + Vector3.up * 0.2f, Player.transform.rotation);
            return "Teleport " + pos.ToString("F0");
        }

        public static string ApplyEffect(EffectType t, int stacks, float duration)
        {
            if (Player == null) return "-";
            Player.Effects.Apply(t, stacks, duration, Player);
            return EffectLibrary.Name(t) + " ×" + stacks;
        }

        /// <summary>Weitere Befehle (Items, Marker, Events) ergänzen sich in späteren Milestones.</summary>
        public static readonly List<(string label, System.Func<string, string> run)> Extra = new List<(string, System.Func<string, string>)>();
    }
}
