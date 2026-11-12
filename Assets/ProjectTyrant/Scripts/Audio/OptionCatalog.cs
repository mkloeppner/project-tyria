using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProjectTyrant
{
    /// <summary>
    /// Fix-Lauf F1: Katalog aller Optionen (Feld in <see cref="GameOptions"/>, Tab, Beschriftung, Tooltip mit einem Satz Erklärung).
    /// Grundlage für Tooltips, den „Standard“-Button je Tab und den datengetriebenen Test „jede Einstellung hat einen Tooltip“.
    /// </summary>
    public static class OptionCatalog
    {
        public struct Def { public string Field, Tab, Label, Tooltip; public string[] Choices; }
        static Def D(string field, string tab, string label, string tip, params string[] choices) => new Def { Field = field, Tab = tab, Label = label, Tooltip = tip, Choices = choices.Length > 0 ? choices : null };

        public static readonly string[] Tabs = { "Audio", "Kamera", "Grafik", "Spiel", "Steuerung" };

        public static readonly List<Def> All = new List<Def>
        {
            D("Master", "Audio", "Gesamt", "Gesamtlautstärke aller Klänge."),
            D("Music", "Audio", "Musik", "Lautstärke der Musik (Erkundung, Kampf, Stinger)."),
            D("Ambience", "Audio", "Umgebung", "Lautstärke der Umgebungsgeräusche (Wind, Wasser, Tiere)."),
            D("Effects", "Audio", "Effekte", "Lautstärke von Kampf-, Bewegungs- und Weltgeräuschen."),
            D("Ui", "Audio", "Benutzeroberfläche", "Lautstärke der Klänge von Fenstern und Schaltflächen."),
            D("ZoomStep", "Kamera", "Zoom-Geschwindigkeit", "Meter Kameraabstand je Mausrad-Raste (0,25–1,5 m)."),
            D("ZoomInvert", "Kamera", "Zoomrichtung umkehren", "Mausrad nach vorn zoomt heraus statt heran."),
            D("CameraDefaultDistance", "Kamera", "Standard-Kameraabstand", "Abstand in Metern, mit dem die Kamera ohne gespeicherten Wert startet (1,5–12 m)."),
            D("CameraSensitivity", "Kamera", "Empfindlichkeit", "Drehgeschwindigkeit der Kamera mit gehaltener rechter Maustaste und in der Action-Kamera."),
            D("CameraImpulse", "Kamera", "Kamera-Impuls bei Treffern", "Kurzes Wackeln der Kamera bei schweren Treffern."),
            D("ActionCrosshair", "Kamera", "Action-Kamera-Fadenkreuz", "Zeigt in der Action-Kamera ein Fadenkreuz in der Bildmitte."),
            D("Quality", "Grafik", "Qualitätsstufe", "Niedrig, Mittel, Hoch oder Ultra (Schatten, Umgebungsverdeckung, Auflösung, Effekt-Lagen); beim ersten Start automatisch erkannt."),
            D("Photosensitive", "Grafik", "Lichtempfindlich", "Deckelt Blitze, Leuchtspitzen und Bloom, halbiert Verzerrungen und schaltet Kamerawackeln ab."),
            D("GfxFog", "Grafik", "Nebel", "Höhennebel mit Sonnenstreuung; aus spart Leistung, Fernsicht wird klarer."),
            D("GfxOutline", "Grafik", "Konturen", "Dunkle Silhouetten-Linien im Nahbereich."),
            D("GfxGodrays", "Grafik", "Lichtschäfte", "Sonnenstrahlen, wenn die Sonne im Bild ist."),
            D("GfxDistortion", "Grafik", "Hitzeflimmern und Schockwellen", "Bildverzerrung über Lava und Wüste und bei Explosionen."),
            D("GfxWeather", "Grafik", "Wetter-Partikel", "Regen, Schnee, Sand und Asche; Licht und Nebel folgen dem Wetter auch ohne Partikel."),
            D("GfxAmbient", "Grafik", "Umgebungs-Effekte", "Glühwürmchen, Pollen, Funken und Blätter in der Welt."),
            D("GfxAutoGovernor", "Grafik", "Automatische Leistungsanpassung", "Reduziert Umgebungs- und Wetter-Effekte, wenn die Bildrate unter 45 fällt (Telegraphen bleiben immer)."),
            D("ViewDistance", "Grafik", "Sichtweite", "Wie weit die Kamera rendert."),
            D("EffectDensity", "Grafik", "Effekt-Dichte", "Partikelmenge der Effekte (Voll, Reduziert, Minimal)."),
            D("Shadows", "Grafik", "Schatten", "Echtzeit-Schatten an oder aus."),
            D("AutoLoot", "Spiel", "Auto-Beute", "Beute in der Nähe wird nach dem Kampf automatisch aufgesammelt."),
            D("CombatTextDensity", "Spiel", "Kampftext-Dichte", "Wie viele Schadenszahlen angezeigt werden."),
            D("HitStop", "Spiel", "Hit-Stop", "Kurzes Einfrieren bei schweren Treffern für mehr Wucht."),
            D("ColorblindTelegraphs", "Spiel", "Telegraphen: Farbenblind-Modus", "Gegner-Telegraphen in Blau–Gelb statt Rot–Orange."),
            D("UiScale", "Spiel", "UI-Skalierung", "Größe der Benutzeroberfläche."),
            D("MinimapRotate", "Spiel", "Minimap dreht mit", "Die Minimap dreht sich mit der Kamera."),
            D("AreaOwn", "Spiel", "Flächeneffekte eigener Skills", "„Nur Effekt“ blendet Ring, Rand und Füllscheibe eigener Flächen aus; Combo-Felder behalten einen dünnen, pulsierenden Elementrand.", "Fläche und Effekt", "Nur Effekt"),
            D("AreaAllies", "Spiel", "Flächeneffekte Verbündeter", "Darstellung der Flächen von Wachen, Gefährte und Truppe.", "Voll", "Nur Effekt", "Reduziert", "Aus"),
            D("SubtitleSize", "Spiel", "Untertitel-Größe", "Schriftgröße der Untertitel in Szenen (Untertitel sind immer an, Sprecher farbkodiert)."),
            D("TextSpeed", "Spiel", "Textgeschwindigkeit", "Wie lange Zeilen ohne Sprachdatei stehen bleiben (höher = schneller)."),
            D("AutoAdvanceDialogue", "Spiel", "Dialoge automatisch fortsetzen", "An: Zeilen laufen mit der Sprache weiter. Aus: Leertaste oder Klick für jede Zeile."),
            D("StoryHints", "Spiel", "Hinweise anzeigen", "Lern-Hinweise der Persönlichen Geschichte beim ersten Auftreten einer Mechanik."),
            D("EnemyTelegraphs", "Spiel", "Gegner-Telegraphen", "Warnung: Telegraphen zeigen, wo ein Gegner-Angriff einschlägt – ausgeschaltet fehlt diese Spielinformation."),
            D("TurnMode", "Steuerung", "A/D-Tasten", "Drehen: A/D drehen den Charakter, Q/E gehen seitwärts, rechte Maustaste macht A/D zu Seitwärts (GW2-Standard). Seitwärts: bisheriges Verhalten.", "Drehen", "Seitwärts"),
            D("GroundTargetMode", "Steuerung", "Bodenziel-Modus", "Wie Bodenziel-Skills gewirkt werden: mit Vorschau und Mausklick, beim Loslassen der Taste, sofort am Cursor oder sofort mit kurzer Reichweitenanzeige.", "Normal (Maus bestätigt)", "Drücken und Loslassen", "Sofort", "Sofort mit Reichweitenanzeige"),
            D("GroundHoldMs", "Steuerung", "Halte-Schwelle (ms)", "Ab dieser Haltedauer zeigt „Drücken und Loslassen“ die Vorschau; kürzeres Tippen wirkt sofort am Cursor (100–300 ms)."),
            D("GroundSnapToTarget", "Steuerung", "Bodenziel am Ziel einrasten", "Die Vorschau springt auf das anvisierte Ziel, solange es in Reichweite ist."),
            D("GroundClampToRange", "Steuerung", "Auf maximale Reichweite begrenzen", "An: außerhalb der Reichweite wird am Reichweitenrand gewirkt. Aus: der Skill wird abgebrochen."),
        };

        public static Def? Get(string field) { foreach (var d in All) if (d.Field == field) return d; return null; }

        public static FieldInfo Field(string name) => typeof(GameOptions).GetField(name, BindingFlags.Public | BindingFlags.Instance);

        /// <summary>„Standard“ je Tab: setzt genau die Felder dieses Tabs auf die Werkseinstellung zurück.</summary>
        public static int ResetTab(GameOptions o, string tab)
        {
            var def = new GameOptions(); int n = 0;
            foreach (var d in All)
            {
                if (d.Tab != tab) continue;
                var f = Field(d.Field); if (f == null) continue;
                f.SetValue(o, f.GetValue(def)); n++;
            }
            if (tab == "Kamera") o.CameraCurrentDistance = -1f;   // gespeicherten Abstand verwerfen → Standardabstand
            o.Clamp();
            return n;
        }
    }
}
