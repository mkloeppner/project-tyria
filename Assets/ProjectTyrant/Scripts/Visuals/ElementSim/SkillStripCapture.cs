using System.Collections;
using System.Linq;
using UnityEngine;

namespace ProjectTyrant.Visuals.ElementSim
{
    /// <summary>
    /// VFX-Lauf V1: Phasen-Aufnahmen und 2-Sekunden-Bildstreifen (8 Bilder) je Schaustück-Skill in Zeitlupe 0,25.
    /// Ziel: Temp/PT_QA/V1/&lt;tag&gt;/&lt;Klasse&gt;/&lt;Skill&gt;_&lt;Phase&gt;.png und _strip_0…7.png. „vorher“ wird nie überschrieben.
    /// Bewegungs-Metrik danach im Editor (`MotionReport`).
    /// </summary>
    public static class SkillStripCapture
    {
        public static readonly string[] Phases = { "Aufladen", "Auswurf", "Flug", "Einschlag", "Nachwirkung" };
        /// <summary>Zeitpunkte der Phasen in Effektzeit (s) ab Wirken.</summary>
        public static readonly float[] PhaseTimes = { 0.05f, 0.2f, 0.35f, 0.6f, 1.2f };
        public const int StripFrames = 8; public const float StripSeconds = 2f, SlowMotion = 0.25f;
        public static bool Running { get; private set; }
        public static string Status = "";
        /// <summary>Optional: nur Skills, deren Name diesen Text enthält (Nachaufnahmen eines Elements).</summary>
        public static string Filter = "";

        public static string Root(string tag) => "Temp/PT_QA/V1/" + tag;

        public static string Start(string tag)
        {
            var gm = GameManager.Instance; var lib = GraphicsLibrary.Current;
            if (gm == null || lib == null || gm.Player == null) return "Kein Spieler oder keine Grafik-Bibliothek";
            if (Running) return "Aufnahme läuft: " + Status;
            tag = string.IsNullOrEmpty(tag) ? "vorher" : tag;
            gm.StartCoroutine(Run(lib, tag));
            return lib.ShowcaseSkills.Count + " Skills × (5 Phasen + 8 Streifen) → " + Root(tag);
        }

        static bool Shot(string tag, string cls, string name)
        {
            string dir = Root(tag) + "/" + cls; System.IO.Directory.CreateDirectory(dir);
            string path = dir + "/" + name + ".png";
            if (tag == "vorher" && System.IO.File.Exists(path)) return false;
            ScreenCapture.CaptureScreenshot(path); return true;
        }

        static IEnumerator Run(GraphicsLibrary lib, string tag)
        {
            Running = true; ZoneGate.QaBypass = true;
            var gm = GameManager.Instance; var pc = gm.Player.GetComponent<PlayerController>();
            var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.enabled && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            foreach (var c in canvases) c.enabled = false;
            float oldScale = Time.timeScale; int shots = 0; string lastClass = null;
            Vector3 homePos = gm.Player.transform.position; Quaternion homeRot = gm.Player.transform.rotation;
            for (int i = 0; i < lib.ShowcaseSkills.Count; i++)
            {
                var s = lib.ShowcaseSkills[i]; if (s == null) continue;
                if (!string.IsNullOrEmpty(Filter) && !s.name.Contains(Filter)) continue;
                string cls = i < lib.ShowcaseClasses.Count && !string.IsNullOrEmpty(lib.ShowcaseClasses[i]) ? lib.ShowcaseClasses[i] : "allgemein";
                Status = "Skill " + (i + 1) + "/" + lib.ShowcaseSkills.Count + " " + s.name;
                var mark = lib.Bookmark("klassen/Showcase_" + cls);
                if (mark != null && cls != lastClass && pc != null && gm.CurrentZone != null && gm.CurrentZone.Id == "sonnfurt")
                {
                    var fwd = Quaternion.Euler(mark.Euler) * Vector3.forward; fwd.y = 0f; fwd.Normalize();
                    var spot = mark.Position + fwd * 8f;
                    if (Physics.Raycast(spot + Vector3.up * 30f, Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore)) spot = hit.point;
                    pc.Teleport(spot, Quaternion.LookRotation(Vector3.Cross(Vector3.up, fwd)));
                    for (int f = 0; f < 5; f++) yield return null;
                    lastClass = cls;
                }
                var p = gm.Player.transform; var cam = Camera.main;
                GraphicsAdminCommands.FreezeCamera(true);
                Vector3 from = mark != null && lastClass == cls ? mark.Position + Vector3.up * 1.5f : p.position - p.forward * 6f + p.right * 3.5f + Vector3.up * 3.2f;
                var look = p.position + p.forward * 3.5f + Vector3.up;
                if (cam != null) { cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(look - from)); cam.fieldOfView = 55f; }
                for (int f = 0; f < 3; f++) yield return null;

                // Durchgang 1: fünf Phasen
                Time.timeScale = SlowMotion;
                VfxPlayer.Play(s, p.position + Vector3.up * 1.2f, p.position + p.forward * 7f, p);
                float t = 0f;
                for (int ph = 0; ph < Phases.Length; ph++)
                {
                    yield return new WaitForSeconds(PhaseTimes[ph] - t); t = PhaseTimes[ph];
                    if (Shot(tag, cls, s.name + "_" + Phases[ph])) shots++;
                }
                Time.timeScale = 1f; yield return new WaitForSeconds(1.5f);

                // Durchgang 2: Bildstreifen über 2 s Effektzeit
                Time.timeScale = SlowMotion;
                VfxPlayer.Play(s, p.position + Vector3.up * 1.2f, p.position + p.forward * 7f, p);
                for (int f = 0; f < StripFrames; f++)
                {
                    yield return new WaitForSeconds(StripSeconds / StripFrames);
                    if (Shot(tag, cls, s.name + "_strip_" + f)) shots++;
                }
                Time.timeScale = 1f; yield return new WaitForSeconds(1.5f);
            }
            if (pc != null && lastClass != null) pc.Teleport(homePos, homeRot);
            Time.timeScale = oldScale;
            foreach (var c in canvases) if (c != null) c.enabled = true;
            GraphicsAdminCommands.FreezeCamera(false);
            Status = "fertig: " + shots + " Aufnahmen → " + Root(tag);
            Debug.Log("[ProjectTyrant] V1-Bildstreifen " + Status);
            ZoneGate.QaBypass = false; Running = false;
        }
    }
}
