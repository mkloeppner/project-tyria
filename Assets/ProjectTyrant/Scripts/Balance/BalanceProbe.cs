using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    [Serializable] public class ProbeResult { public string Mode, Zone, Archetype, Label; public int Danger, Level, Count; public float Seconds; }
    [Serializable] public class ProbeLog { public List<ProbeResult> Results = new List<ProbeResult>(); }

    /// <summary>
    /// Balance B1: Trainingspuppen-Messungen je Stufe: Zeit-bis-Tod eines untätigen Spielers gegen n Gegner (Trash/Veteran) und Zeit-bis-Kill (Auto-Angriff + 2 Skills)
    /// gegen einen passiven Gegner. Ergebnisse unter Temp/PT_QA/balance/probes_&lt;Label&gt;.json.
    /// </summary>
    public class BalanceProbe : MonoBehaviour
    {
        public struct Job { public string Mode; public int Danger, Count, Level; public string Zone, Archetype; public Vector3 Spot; }
        public static readonly Queue<Job> Jobs = new Queue<Job>();
        public static ProbeLog Log = new ProbeLog();
        public static string Label = "probe";
        public static bool Running => _inst != null;
        static BalanceProbe _inst;
        public static float TimeScale = 3f;

        /// <summary>Archetyp je Stufe (Trash der Mess-Zone) – Veteran über Modus "ttdvet".</summary>
        public static string TrashFor(int danger) => danger <= 2 ? "Trash_Pluenderer" : danger == 3 ? "Moor_Schleicher" : danger == 4 ? "Glut_Kriecher" : "Tiefe_Tiefenschreck";
        public static string VeteranFor(int danger) => danger <= 2 ? "Veteran_Brecher" : danger == 3 ? "Moor_Koloss" : danger == 4 ? "Glut_Golem" : "Tiefe_BlinderJaeger";

        public static string StartTtd(string a)
        {
            var parts = (a ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int d = parts.Length > 0 && int.TryParse(parts[0], out int x) ? x : 0;
            int n = parts.Length > 1 && int.TryParse(parts[1], out int y) ? y : 3;
            var gm = GameManager.Instance; if (gm == null || gm.Player == null) return "Kein Spieler";
            Jobs.Enqueue(new Job { Mode = "ttd", Danger = d, Count = n, Level = gm.Player.Level, Archetype = TrashFor(d), Spot = gm.Player.transform.position });
            Run(); return "TTD-Messung Stufe " + d + " gegen " + n;
        }

        /// <summary>Volle Serie: je Stufe (Mess-Zelle) TTD 3 Trash, TTD 1 Veteran, TTK Trash, TTK Veteran.</summary>
        public static void QueueSeries(string label)
        {
            Label = label; Log = new ProbeLog();
            foreach (var c in BalancePlan.Cells)
            {
                Jobs.Enqueue(new Job { Mode = "ttd", Danger = c.Danger, Count = 3, Level = c.Level, Zone = c.Zone, Archetype = TrashFor(c.Danger), Spot = c.Start });
                if (c.Danger > 0) Jobs.Enqueue(new Job { Mode = "ttdvet", Danger = c.Danger, Count = 1, Level = c.Level, Zone = c.Zone, Archetype = VeteranFor(c.Danger), Spot = c.Start });
                Jobs.Enqueue(new Job { Mode = "ttk", Danger = c.Danger, Count = 1, Level = c.Level, Zone = c.Zone, Archetype = TrashFor(c.Danger), Spot = c.Start });
                if (c.Danger > 0) Jobs.Enqueue(new Job { Mode = "ttkvet", Danger = c.Danger, Count = 1, Level = c.Level, Zone = c.Zone, Archetype = VeteranFor(c.Danger), Spot = c.Start });
            }
            Run();
        }

        public static void Run()
        {
            if (_inst != null || Jobs.Count == 0) return;
            _inst = new GameObject("QA_BalanceProbe").AddComponent<BalanceProbe>();
            DontDestroyOnLoad(_inst.gameObject);   // überlebt Zonenwechsel
        }

        IEnumerator Start()
        {
            var gm = GameManager.Instance;
            var prof = PlayerProfile.Instance;
            int? savedLevel = prof != null ? prof.Progression.Level : (int?)null;
            if (prof != null) prof.SaveSuspended = true;
            while (Jobs.Count > 0)
            {
                var j = Jobs.Dequeue();
                if (!string.IsNullOrEmpty(j.Zone) && (ZoneRoot.Current == null || ZoneRoot.Current.Id != j.Zone))
                {
                    gm.ChangeZone(j.Zone, null);
                    float w = 0f; while ((ZoneRoot.Current == null || ZoneRoot.Current.Id != j.Zone) && w < 60f) { w += Time.unscaledDeltaTime; yield return null; }
                    yield return new WaitForSecondsRealtime(2f);
                }
                yield return RunJob(j);
            }
            Time.timeScale = 1f;
            try { Directory.CreateDirectory("Temp/PT_QA/balance"); File.WriteAllText("Temp/PT_QA/balance/probes_" + Label + ".json", JsonUtility.ToJson(Log, true)); } catch { }
            BalanceLoadout.Remove();
            if (prof != null) { prof.SaveSuspended = false; DebugCommands.Load(); }
            var sb = new System.Text.StringBuilder();
            foreach (var r in Log.Results) sb.AppendLine(r.Mode + " Stufe " + r.Danger + " (" + r.Zone + ", L" + r.Level + ", " + r.Count + "× " + r.Archetype + "): " + r.Seconds.ToString("0.0") + " s");
            Debug.Log("[ProjectTyrant] Messungen " + Label + ":\n" + sb);
            _inst = null; Destroy(gameObject);
        }

        IEnumerator RunJob(Job j)
        {
            var gm = GameManager.Instance; var p = gm.Player; if (p == null) yield break;
            var pc = p.GetComponent<PlayerController>(); var caster = p.GetComponent<SkillCaster>(); var downed = p.GetComponent<DownedController>();
            DebugCommands.SetLevel(j.Level);
            BalanceLoadout.Apply(p, j.Level);
            // freien Platz suchen: Startpunkt, Sicherheitszone vermeiden (sonst geben Gegner sofort auf)
            Vector3 spot = j.Spot;
            for (int i = 0; i < 12 && BalanceWorld.InSafezone(spot); i++) spot += new Vector3(9f, 0f, 6f);
            // Biom-Zonen: Messplatz neben dem Welt-Lager der Stufe, das dem Wunschpunkt am nächsten liegt (sicherer Boden, keine Lava/Höhlendecke)
            if (ZoneRoot.Current != null && ZoneRoot.Current.Definition != null && !ZoneRoot.Current.Definition.LegacyLayout)
            {
                NpcBrain camp = null; float bd = float.MaxValue;
                foreach (var nb in UnityEngine.Object.FindObjectsByType<NpcBrain>())
                {
                    if (!nb.WorldSpawn || nb.DangerLevel != j.Danger || nb.Self == null || nb.Self.Team != Team.Enemy) continue;
                    float d = Vector3.Distance(nb.HomePosition, j.Spot);
                    if (d < bd) { bd = d; camp = nb; }
                }
                if (camp != null) spot = camp.HomePosition + new Vector3(12f, 0f, 0f);
            }
            // Bodenpunkt vom NavMesh (Raycast von oben träfe in Höhlen die Decke; falsche Höhe → Rettung zur Wegmarke = Sicherheitszone)
            if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 40f, UnityEngine.AI.NavMesh.AllAreas)) spot = hit.position;
            pc.Teleport(spot + Vector3.up * 0.3f, Quaternion.identity);
            if (p.IsDead) p.Revive(1f); if (downed != null) downed.ResetState();
            p.SetHealth(p.MaxHealth); p.Effects.ClearConditions();
            ClearAround(spot, 30f);
            yield return new WaitForSecondsRealtime(0.5f);
            var arch = DataBuilderLookup(j.Archetype);
            if (arch == null) { Debug.LogWarning("[ProjectTyrant] Probe: Archetyp fehlt " + j.Archetype); yield break; }
            var spawned = new List<NpcBrain>();
            for (int i = 0; i < j.Count; i++)
            {
                var ang = i * Mathf.PI * 2f / j.Count;
                var b = NpcBrain.Spawn(arch, spot + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 3f, Quaternion.identity);
                if (b == null) continue;
                b.LevelOverride = j.Level; b.DangerOverride = j.Danger; b.ApplyArchetype(); b.HomePosition = b.transform.position; b.UseLeash = false;
                spawned.Add(b);
            }
            if (j.Mode == "flee") { yield return Flee(j, spawned, spot); yield break; }
            bool ttk = j.Mode.StartsWith("ttk");
            caster.AutoAttack = ttk;
            Time.timeScale = TimeScale;
            float t = 0f, start = -1f, limit = ttk ? 180f : 240f; float result = -1f;
            if (!ttk) foreach (var b in spawned) b.ForceAggro(p);
            else foreach (var b in spawned) { b.Suspended = true; p.GetComponent<TargetingSystem>()?.SetTarget(b.Self); }
            float hpStart = p.Health;
            while (t < limit)
            {
                t += Time.deltaTime;
                PlayerController.QaWishWorld = Vector3.zero;
                if (!ttk)
                {
                    if (start < 0f && p.Health < hpStart - 0.5f) start = t - 0.05f;
                    if (start < 0f) start = t > 0.1f && spawned.Exists(b => b.State == NpcState.Attack) ? t : start;
                    if (p.IsDowned || p.IsDead) { result = t - Mathf.Max(0f, start); break; }
                }
                else
                {
                    var b = spawned[0];
                    if (start < 0f) start = t;
                    pc.FacingOverride = b.transform.position;
                    if (Vector3.Distance(p.transform.position, b.transform.position) > 3f) PlayerController.QaWishWorld = (b.transform.position - p.transform.position).normalized;
                    if (!caster.IsCasting) { if (!caster.TryUseSlot(1, true)) if (!caster.TryUseSlot(2, true)) caster.TryUseSlot(0, true); }
                    if (b.Self.IsDead) { result = t - start; break; }
                }
                yield return null;
            }
            Time.timeScale = 1f;
            PlayerController.QaWishWorld = null; pc.FacingOverride = null; caster.AutoAttack = true;
            foreach (var b in spawned) if (b != null) Destroy(b.gameObject);
            Restore();
            if (downed != null && p.IsDowned) downed.Rally("Messung beendet");
            if (p.IsDead) p.Revive(1f);
            p.SetHealth(p.MaxHealth);
            Log.Results.Add(new ProbeResult { Mode = j.Mode, Zone = ZoneRoot.Current != null ? ZoneRoot.Current.Id : "", Archetype = j.Archetype, Danger = j.Danger, Level = j.Level, Count = j.Count, Seconds = result < 0f ? limit : result, Label = Label });
            if (BalanceTelemetry.Current != null) BalanceTelemetry.Record(ttk ? TelemetryKind.Ttk : TelemetryKind.Ttd, spot, j.Archetype, arch.Tier, j.Mode, result);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        /// <summary>
        /// Gegner in der Nähe ruhigstellen (Suspended + unsichtbar + nicht anvisierbar). Kein SetActive: das Ab-/Anschalten geriggter Figuren
        /// fror den Editor ein (KNOWN_ISSUES 58, Animation Rigging) – vermutlich Auslöser der Hänger am 10.11. um 08:30 und 12:10.
        /// </summary>
        /// <summary>Flucht-Probe: Gegner provozieren, 2 s kämpfen lassen, dann geradeaus weglaufen; Ergebnis = Sekunden bis zur Aufgabe (−1 = gestorben/verfolgt).</summary>
        IEnumerator Flee(Job j, List<NpcBrain> spawned, Vector3 spot)
        {
            var p = GameManager.Instance.Player; var pc = p.GetComponent<PlayerController>(); var downed = p.GetComponent<DownedController>();
            Time.timeScale = TimeScale;
            foreach (var b in spawned) { b.UseLeash = true; b.ForceAggro(p); }
            float t = 0f; while (t < 2f) { t += Time.deltaTime; PlayerController.QaWishWorld = Vector3.zero; yield return null; }
            var dir = spawned.Count > 0 ? (p.transform.position - spawned[0].transform.position) : Vector3.forward; dir.y = 0f; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
            float run = 0f, result = -1f;
            while (run < 60f)
            {
                run += Time.deltaTime;
                PlayerController.QaWishWorld = dir;
                if (p.IsDowned || p.IsDead) break;
                bool chased = false; foreach (var b in spawned) if (b != null && !b.Self.IsDead && b.Target == p && b.State != NpcState.Reset) chased = true;
                if (!chased) { result = run; break; }
                yield return null;
            }
            Time.timeScale = 1f; PlayerController.QaWishWorld = null;
            foreach (var b in spawned) if (b != null) Destroy(b.gameObject);
            Restore();
            if (downed != null && p.IsDowned) downed.Rally("Messung beendet");
            if (p.IsDead) p.Revive(1f);
            p.SetHealth(p.MaxHealth);
            Log.Results.Add(new ProbeResult { Mode = "flee", Zone = ZoneRoot.Current != null ? ZoneRoot.Current.Id : "", Archetype = j.Archetype, Danger = j.Danger, Level = j.Level, Count = j.Count, Seconds = result, Label = Label });
            yield return new WaitForSecondsRealtime(0.3f);
        }

        static void ClearAround(Vector3 c, float r)
        {
            foreach (var x in new List<Combatant>(Combatant.All))
            {
                if (x == null || x.Team != Team.Enemy || x.IsDead) continue;
                if ((x.transform.position - c).sqrMagnitude >= r * r) continue;
                var b = x.GetComponent<NpcBrain>(); if (b == null || b.Suspended) continue;
                b.Suspended = true; x.Targetable = false;
                foreach (var rr in x.GetComponentsInChildren<Renderer>()) rr.enabled = false;
                _hidden.Add(x.gameObject);
            }
        }
        static readonly List<GameObject> _hidden = new List<GameObject>();
        static void Restore()
        {
            foreach (var g in _hidden)
            {
                if (g == null) continue;
                var b = g.GetComponent<NpcBrain>(); if (b != null) b.Suspended = false;
                var x = g.GetComponent<Combatant>(); if (x != null) x.Targetable = true;
                foreach (var rr in g.GetComponentsInChildren<Renderer>()) rr.enabled = true;
            }
            _hidden.Clear();
        }
        void OnDestroy() => Restore();

        /// <summary>Archetyp-Asset zur Laufzeit (alle Archetypen hängen am GameManager-Katalog oder werden über die Szene gefunden).</summary>
        static EnemyArchetype DataBuilderLookup(string id)
        {
            foreach (var a in Resources.FindObjectsOfTypeAll<EnemyArchetype>()) if (a != null && (a.name == "Enemy_" + id || a.name == id)) return a;
            return null;
        }
    }
}
