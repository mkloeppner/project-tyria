using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G1/G7: Vorher-/Nachher-Aufnahmen für Skills (je 2 Phasen), Champions bzw. Veteranen (Boss-Stellvertreter je Biom) und Reittiere.
    /// Ziel: Temp/PT_QA/G1/&lt;tag&gt;/_skills/&lt;klasse&gt;, _bosse, _reittiere. „vorher“-Dateien werden nie überschrieben.
    /// </summary>
    public static class ShowcaseCapture
    {
        public static readonly string[] BossZones = { "sonnfurt", "nebelmoor", "frostkamm", "gluetoede", "silberkueste", "duenenmeer", "kristalltiefe", "gruene_flut", "tyrannenfeste" };
        public static bool Running { get; private set; }
        public static string Status = "";

        public static string Start(string tag)
        {
            var gm = GameManager.Instance; var lib = GraphicsLibrary.Current;
            if (gm == null || lib == null || gm.Player == null) return "Kein Spieler oder keine Grafik-Bibliothek";
            if (Running) return "Aufnahme läuft: " + Status;
            gm.StartCoroutine(Run(lib, string.IsNullOrEmpty(tag) ? "vorher" : tag));
            return lib.ShowcaseSkills.Count + " Skills, " + BossZones.Length + " Champions, " + lib.Mounts.Count + " Reittiere → Temp/PT_QA/G1/" + tag;
        }

        static string Root(string tag) => "Temp/PT_QA/G1/" + tag;

        static bool Shot(string tag, string sub, string name)
        {
            string dir = Root(tag) + "/" + sub; System.IO.Directory.CreateDirectory(dir);
            string path = dir + "/" + name + ".png";
            if (tag == "vorher" && System.IO.File.Exists(path)) return false;   // Vorher-Aufnahmen bleiben unangetastet
            ScreenCapture.CaptureScreenshot(path);
            return true;
        }

        static void Frame(Vector3 from, Vector3 lookAt)
        {
            var cam = Camera.main; if (cam == null) return;
            GraphicsAdminCommands.FreezeCamera(true);
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(lookAt - from)); cam.fieldOfView = 55f;
        }

        static string ClassOf(SkillDefinition s, GraphicsLibrary lib)
        {
            int i = lib.ShowcaseSkills.IndexOf(s);
            return i >= 0 && i < lib.ShowcaseClasses.Count && !string.IsNullOrEmpty(lib.ShowcaseClasses[i]) ? lib.ShowcaseClasses[i] : "allgemein";
        }

        static IEnumerator Run(GraphicsLibrary lib, string tag)
        {
            Running = true; ZoneGate.QaBypass = true;
            var gm = GameManager.Instance;
            var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.enabled && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            foreach (var c in canvases) c.enabled = false;
            int shots = 0;

            // Skills: Spieler am Klassen-Schaustück-Lesezeichen (freie Fläche), Kamera = Lesezeichen, Wirkrichtung quer durchs Bild;
            // Phase a = Aufladen/Auswurf, Phase b = Einschlag/Nachwirkung
            var pc = gm.Player.GetComponent<PlayerController>();
            Vector3 homePos = gm.Player.transform.position; Quaternion homeRot = gm.Player.transform.rotation;
            string lastClass = null;
            for (int i = 0; i < lib.ShowcaseSkills.Count; i++)
            {
                var s = lib.ShowcaseSkills[i]; if (s == null) continue;
                Status = "Skill " + (i + 1) + "/" + lib.ShowcaseSkills.Count + " " + s.name;
                string cls = ClassOf(s, lib);
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
                var p = gm.Player.transform;
                Vector3 target = p.position + p.forward * 7f;
                if (mark != null && lastClass == cls) Frame(mark.Position + Vector3.up * 1.5f, p.position + p.forward * 3.5f + Vector3.up * 1f);
                else Frame(p.position - p.forward * 6f + p.right * 3.5f + Vector3.up * 3.2f, p.position + p.forward * 3.5f + Vector3.up * 1f);
                for (int f = 0; f < 3; f++) yield return null;
                VfxPlayer.Play(s, p.position + Vector3.up * 1.2f, target, p);
                yield return new WaitForSeconds(0.15f);
                if (Shot(tag, "_skills/" + ClassOf(s, lib), s.name + "_a")) shots++;
                yield return new WaitForSeconds(0.45f);
                if (Shot(tag, "_skills/" + ClassOf(s, lib), s.name + "_b")) shots++;
                yield return new WaitForSeconds(1.2f);   // Effekte abklingen lassen
            }

            if (pc != null && lastClass != null) { pc.Teleport(homePos, homeRot); for (int f = 0; f < 3; f++) yield return null; }

            // Reittiere: Prefab vor dem Spieler, Verhalten aus (keine Eingaben), danach entfernt
            foreach (var m in lib.Mounts)
            {
                if (m == null || m.Prefab == null) continue;
                Status = "Reittier " + m.name;
                var p = gm.Player.transform;
                Vector3 at = p.position + p.forward * 6f;
                var go = Object.Instantiate(m.Prefab, at, Quaternion.LookRotation(-p.forward + p.right * 0.8f));
                foreach (var b in go.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
                foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
                var bounds = new Bounds(at, Vector3.one);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
                float d = Mathf.Max(4f, bounds.extents.magnitude * 2.4f);
                Frame(bounds.center - p.forward * 0.2f * d + (p.right * 0.9f - p.forward * 0.1f).normalized * d + Vector3.up * d * 0.35f, bounds.center);
                for (int f = 0; f < 4; f++) yield return null;
                if (Shot(tag, "_reittiere", m.name)) shots++;
                yield return null;
                Object.Destroy(go);
            }

            // Champions als Boss-Stellvertreter je Biom (Weltbosse folgen dem UTC-Fahrplan und sind nicht jederzeit da)
            foreach (var zone in BossZones)
            {
                Status = "Champion " + zone;
                if (gm.CurrentZone == null || gm.CurrentZone.Id != zone)
                {
                    gm.ChangeZone(zone, null, null);
                    for (int i = 0; i < 900 && (gm.CurrentZone == null || gm.CurrentZone.Id != zone); i++) yield return null;
                    yield return new WaitForSecondsRealtime(2f);
                }
                if (gm.CurrentZone == null || gm.CurrentZone.Id != zone) { Debug.LogWarning("[ProjectTyrant] Showcase: Zone " + zone + " nicht erreicht"); continue; }
                var foes = Object.FindObjectsByType<NpcBrain>().Where(b => b != null && b.Archetype != null && b.Archetype.Team == Team.Enemy && b.Archetype.Tier >= EnemyTier.Veteran).ToList();
                var champ = foes.OrderByDescending(b => (int)b.Archetype.Tier).ThenBy(b => (b.transform.position - gm.Player.transform.position).sqrMagnitude).FirstOrDefault();
                if (champ == null) { Debug.Log("[ProjectTyrant] Showcase: weder Champion noch Veteran in " + zone + " (Zone " + (gm.CurrentZone != null ? gm.CurrentZone.Id : "–") + ")"); continue; }
                for (int f = 0; f < 3; f++) yield return null;
                var t = champ.transform; var dir = (Quaternion.Euler(0, 35f, 0) * t.forward).normalized;
                Frame(t.position + dir * 9f + Vector3.up * 4f, t.position + Vector3.up * 1.4f);
                for (int f = 0; f < 4; f++) yield return null;
                if (Shot(tag, "_bosse", zone + "_" + champ.Archetype.name)) shots++;
                yield return null;
            }

            foreach (var c in canvases) if (c != null) c.enabled = true;
            GraphicsAdminCommands.FreezeCamera(false);
            Status = "fertig: " + shots + " Aufnahmen → " + Root(tag);
            Debug.Log("[ProjectTyrant] Showcase-Aufnahmen " + Status);
            ZoneGate.QaBypass = false; Running = false;
        }
    }
}
