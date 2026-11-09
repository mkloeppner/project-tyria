using System;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M24: Optionen (PT_Options). Audio, Kamera, Grafik, Spiel. Gespeichert als JSON in PlayerPrefs „PT_Options“
    /// (Account-Ebene; mit der Account/Charakter-Trennung in M25 wandert der Wert in den Account-Speicherstand). Jede Änderung wirkt sofort.
    /// </summary>
    [Serializable]
    public class GameOptions
    {
        // Audio 0..1
        public float Master = 1f, Music = 0.8f, Ambience = 0.8f, Effects = 0.9f, Ui = 0.8f;
        // Kamera
        public float CameraDistance = 1f, CameraSensitivity = 1f; public bool CameraImpulse = true, ActionCrosshair = true;
        // Grafik
        public int Quality = -1; public float ViewDistance = 1f; public int EffectDensity; public bool Shadows = true;
        // Spiel
        public bool AutoLoot = true; public int CombatTextDensity = 2; public bool HitStop = true, ColorblindTelegraphs; public float UiScale = 1f; public bool MinimapRotate;

        public const string Key = "PT_Options";
        public static GameOptions Current { get; private set; }
        public static event Action<GameOptions> Changed;

        public static GameOptions Load()
        {
            if (Current != null) return Current;
            var json = PlayerPrefs.GetString(Key, "");
            GameOptions o = null;
            if (!string.IsNullOrEmpty(json)) { try { o = JsonUtility.FromJson<GameOptions>(json); } catch { o = null; } }
            if (o == null)
            {
                o = new GameOptions();   // Migration der Einzel-Schlüssel aus M22/M23
                o.HitStop = PlayerPrefs.GetInt("PT_HitStop", 1) == 1; o.CameraImpulse = PlayerPrefs.GetInt("PT_CamImpulse", 1) == 1; o.EffectDensity = PlayerPrefs.GetInt("PT_EffectDensity", 0);
            }
            o.Clamp(); Current = o; return o;
        }

        public static GameOptions FromJson(string json) { var o = JsonUtility.FromJson<GameOptions>(json); o.Clamp(); return o; }
        public string ToJson() => JsonUtility.ToJson(this);

        public void Clamp()
        {
            Master = Mathf.Clamp01(Master); Music = Mathf.Clamp01(Music); Ambience = Mathf.Clamp01(Ambience); Effects = Mathf.Clamp01(Effects); Ui = Mathf.Clamp01(Ui);
            CameraDistance = Mathf.Clamp(CameraDistance, 0.6f, 1.6f); CameraSensitivity = Mathf.Clamp(CameraSensitivity, 0.3f, 2.5f);
            ViewDistance = Mathf.Clamp(ViewDistance, 0.5f, 1.5f); EffectDensity = Mathf.Clamp(EffectDensity, 0, 2); CombatTextDensity = Mathf.Clamp(CombatTextDensity, 0, 2);
            UiScale = Mathf.Clamp(UiScale, 0.8f, 1.3f);
        }

        /// <summary>Speichern und sofort anwenden.</summary>
        public void Save()
        {
            Clamp(); Current = this;
            PlayerPrefs.SetString(Key, ToJson());
            PlayerPrefs.SetInt("PT_HitStop", HitStop ? 1 : 0); PlayerPrefs.SetInt("PT_CamImpulse", CameraImpulse ? 1 : 0); PlayerPrefs.SetInt("PT_EffectDensity", EffectDensity);
            PlayerPrefs.Save();
            Apply();
        }

        public void Apply()
        {
            CombatFeel.HitStopEnabled = HitStop; CombatFeel.CameraImpulseEnabled = CameraImpulse;
            VfxPool.EffectDensity = (VfxPool.Density)EffectDensity;
            GroundTelegraph.Colorblind = ColorblindTelegraphs;
            if (Quality >= 0 && Quality < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != Quality) QualitySettings.SetQualityLevel(Quality, true);
            QualitySettings.shadows = Shadows ? ShadowQuality.All : ShadowQuality.Disable;
            var cam = Camera.main; if (cam != null) cam.farClipPlane = 600f * ViewDistance;
            var rig = ThirdPersonCameraRig.Current; if (rig != null) { rig.Sensitivity = 0.15f * CameraSensitivity; rig.MaxZoom = 1.8f * CameraDistance; }
            var pool = AudioPool.Current; if (pool != null) pool.LoadOptions();
            Changed?.Invoke(this);
        }
    }

    /// <summary>Lauf 5 / M24: Spielmenü-Zustandsmaschine (Esc wie GW2): Spiel ↔ Menü ↔ Optionen; Esc schließt erst Kontext/Fenster.</summary>
    public enum MenuState { Game, Menu, Options }

    public class MenuStateModel
    {
        public MenuState State { get; private set; } = MenuState.Game;
        public event Action<MenuState> StateChanged;

        /// <summary>Esc: ohne offenes Fenster öffnet das Spielmenü; im Menü zurück ins Spiel; in den Optionen zurück ins Menü.</summary>
        public void Escape(bool windowOrContextOpen)
        {
            switch (State)
            {
                case MenuState.Game: if (!windowOrContextOpen) Set(MenuState.Menu); break;
                case MenuState.Menu: Set(MenuState.Game); break;
                default: Set(MenuState.Menu); break;
            }
        }

        public void OpenOptions() { if (State != MenuState.Options) Set(MenuState.Options); }
        public void Resume() => Set(MenuState.Game);
        void Set(MenuState s) { if (s == State) return; State = s; StateChanged?.Invoke(s); }
    }
}
