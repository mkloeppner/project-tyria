using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Fix-Lauf F1: einmalige Lern-Hinweise (Account-weit, PlayerPrefs „PT_Hint_&lt;Schlüssel&gt;“).</summary>
    public static class LearnHints
    {
        public static bool Show(string key, string text)
        {
            if (!Application.isPlaying) return false;
            string k = "PT_Hint_" + key;
            if (PlayerPrefs.GetInt(k, 0) == 1) return false;
            PlayerPrefs.SetInt(k, 1);
            GameEventBus.Notify("Hinweis: " + text, new Color(0.7f, 0.95f, 1f));
            return true;
        }
    }
}
