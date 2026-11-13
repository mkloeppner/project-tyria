namespace ProjectTyrant
{
    /// <summary>Grafik-Lauf G7: Grafik-Optionen auf Stufe, Effekt-Schalter, Lichtempfindlich-Modus und Leistungsregler anwenden.</summary>
    public partial class GameOptions
    {
        public void ApplyGraphics()
        {
            Visuals.Photosensitive.Enabled = Photosensitive;
            Visuals.GraphicsSettingsRuntime.ApplyTier(Visuals.QualityTiers.FromUnityLevel(UnityEngine.QualitySettings.GetQualityLevel()));
            if (!GfxFog) Visuals.GraphicsSettingsRuntime.SetFeature("Nebel", false);
            if (!GfxOutline) Visuals.GraphicsSettingsRuntime.SetFeature("Outline", false);
            if (!GfxGodrays) Visuals.GraphicsSettingsRuntime.SetFeature("Lichtschaft", false);
            if (!GfxDistortion) Visuals.GraphicsSettingsRuntime.SetFeature("Verzerrung", false);
            if (!GfxWeather) Visuals.GraphicsSettingsRuntime.SetFeature("Wetter", false);
            if (!GfxAmbient) Visuals.GraphicsSettingsRuntime.SetFeature("Umgebung", false);
            Visuals.GraphicsSettingsRuntime.AutoGovernor = GfxAutoGovernor;
        }
    }
}
