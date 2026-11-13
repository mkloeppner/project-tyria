using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G1/G2: Laufzeit-Steuerung von Tageszeit und Wetter (am GameManager-Objekt, kein eigenes Singleton). G1: Modelle und Admin-Anbindung;
    /// G2/G4 setzen daraus Licht, Himmel, Nebel und Wetter-Effekte.
    /// </summary>
    public partial class GraphicsDirector : MonoBehaviour
    {
        public static GraphicsDirector Instance => GameManager.Instance != null ? GameManager.Instance.GetComponent<GraphicsDirector>() : null;
        public readonly TimeOfDayModel Time = new TimeOfDayModel();
        public WeatherModel Weather { get; private set; }
        public ZoneArtProfile Profile { get; private set; }
        string _zone;

        void Update()
        {
            Time.Tick(UnityEngine.Time.unscaledDeltaTime);
            var gm = GameManager.Instance;
            var z = gm != null ? gm.CurrentZone : null;
            if (z != null && z.Id != _zone) OnZone(z);
            if (Weather != null) Weather.Tick(UnityEngine.Time.unscaledDeltaTime);
            Tick();
        }

        void OnZone(ZoneDefinition z)
        {
            _zone = z.Id;
            var lib = GraphicsLibrary.Current;
            Profile = lib != null ? lib.Profile(z.Id) : null;
            Weather = new WeatherModel(Profile != null ? Profile.Weathers : null, z.Seed + 77);
            ZoneChanged();
        }

        partial void ZoneChanged();
        partial void Tick();
        public void ApplyNow() => Tick();
    }
}
