using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    public enum TelemetryKind { Death, CombatStart, CombatEnd, EscapeAttempt, EscapeResult, Kill, PlayerDowned, Rally, EventStart, EventEnd, NpcRevive, GiveUp, Ttk, Ttd }

    [Serializable]
    public class TelemetryRecord
    {
        public float T;
        public TelemetryKind Kind;
        public float X, Z;
        public string Zone, Region, Enemy, Cause, EventId, Result;
        public int Danger, PlayerLevel, EnemyTier, EventTier;
        public float Value;
    }

    [Serializable]
    public class TelemetrySession
    {
        public string Id, Profile, Zone, Start;
        public int Seed, Danger;
        public float Seconds;
        public List<TelemetryRecord> Records = new List<TelemetryRecord>();

        public int Count(TelemetryKind k, string result = null) { int n = 0; foreach (var r in Records) if (r.Kind == k && (result == null || r.Result == result)) n++; return n; }
        public int Deaths => Count(TelemetryKind.Death);
        public int EscapeAttempts => Count(TelemetryKind.EscapeAttempt);
        public int EscapeSuccess => Count(TelemetryKind.EscapeResult, "aufgegeben");
        public float EscapeRate => EscapeAttempts == 0 ? float.NaN : EscapeSuccess / (float)EscapeAttempts;
        public int EventsStarted(int tier) { int n = 0; foreach (var r in Records) if (r.Kind == TelemetryKind.EventEnd && r.EventTier == tier) n++; return n; }
        public int EventsWon(int tier) { int n = 0; foreach (var r in Records) if (r.Kind == TelemetryKind.EventEnd && r.EventTier == tier && r.Result == "erfolg") n++; return n; }
        public float EventRate(int tier) { int s = EventsStarted(tier); return s == 0 ? float.NaN : EventsWon(tier) / (float)s; }
        /// <summary>Tode je 30 Minuten (normiert auf die Session-Länge).</summary>
        public float DeathsPer30 => Seconds <= 0f ? 0f : Deaths * 1800f / Seconds;
        public float MedianValue(TelemetryKind k)
        {
            var v = new List<float>(); foreach (var r in Records) if (r.Kind == k) v.Add(r.Value);
            return Median(v);
        }
        public static float Median(List<float> v)
        {
            if (v.Count == 0) return float.NaN;
            v.Sort(); int m = v.Count / 2;
            return v.Count % 2 == 1 ? v[m] : (v[m - 1] + v[m]) * 0.5f;
        }
    }

    /// <summary>
    /// Balance B1: Telemetrie (nur Editor/Development Build) – Tode, Kampfbeginn/-ende, Fluchtversuche und Ergebnis, Zeit-bis-Kill/-Tod, Events, Downed/Rally, NPC-Wiederbelebung.
    /// Statische Sammelstelle ohne Singleton; Ausgabe als JSON unter Temp/PT_QA/balance/telemetry/.
    /// </summary>
    public static class BalanceTelemetry
    {
        public static bool Enabled => Application.isEditor || Debug.isDebugBuild;
        public static TelemetrySession Current { get; private set; }
        public static readonly List<TelemetrySession> Finished = new List<TelemetrySession>();
        public static string Folder = "Temp/PT_QA/balance/telemetry";
        static float _t0;
        /// <summary>Kontext-Lieferant (Zone, Gebiet, Stufe, Spieler-Level) – vom Spiel gesetzt, in Tests ersetzbar.</summary>
        public static Func<Vector3, (string zone, string region, int danger)> Context = p => ("", "", 0);
        public static Func<int> PlayerLevel = () => 1;
        public static Func<float> Clock = () => Time.time;

        public static void Begin(string profile, string zone, int seed, int danger = -1)
        {
            if (!Enabled) return;
            if (Current != null) End();
            _t0 = Clock();
            Current = new TelemetrySession { Id = profile + "_" + zone + "_s" + seed + "_" + DateTime.Now.ToString("HHmmss"), Profile = profile, Zone = zone, Seed = seed, Danger = danger, Start = DateTime.Now.ToString("s") };
        }

        public static TelemetrySession End(bool write = true)
        {
            var s = Current;
            if (s == null) return null;
            s.Seconds = Clock() - _t0;
            Current = null;
            Finished.Add(s);
            if (write) Write(s);
            return s;
        }

        public static TelemetryRecord Record(TelemetryKind kind, Vector3 pos, string enemy = null, EnemyTier tier = EnemyTier.None, string cause = null, float value = 0f, string result = null, string eventId = null, int eventTier = -1)
        {
            if (Current == null) return null;
            var c = Context(pos);
            var r = new TelemetryRecord { T = Clock() - _t0, Kind = kind, X = pos.x, Z = pos.z, Zone = c.zone, Region = c.region, Danger = c.danger, PlayerLevel = PlayerLevel(), Enemy = enemy, EnemyTier = (int)tier, Cause = cause, Value = value, Result = result, EventId = eventId, EventTier = eventTier };
            Current.Records.Add(r);
            return r;
        }

        public static string Write(TelemetrySession s)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string path = Path.Combine(Folder, s.Id + ".json");
                File.WriteAllText(path, JsonUtility.ToJson(s, true));
                return path;
            }
            catch (Exception e) { Debug.LogWarning("[ProjectTyrant] Telemetrie nicht geschrieben: " + e.Message); return null; }
        }

        public static TelemetrySession Read(string path) => JsonUtility.FromJson<TelemetrySession>(File.ReadAllText(path));

        public static List<TelemetrySession> ReadAll(string folder = null)
        {
            var l = new List<TelemetrySession>();
            folder = folder ?? Folder;
            if (!Directory.Exists(folder)) return l;
            foreach (var f in Directory.GetFiles(folder, "*.json")) { try { l.Add(Read(f)); } catch { } }
            return l;
        }
    }

    /// <summary>
    /// Balance B1: Fluchtversuch-Erkennung (rein). Ein Versuch beginnt, wenn sich der Spieler im Kampf 2 s lang ohne Angriff von den Verfolgern entfernt;
    /// er wird bis zu 10 s (bzw. bis zum Ergebnis) bewertet: Verfolger geben auf / Spieler stirbt / Spieler kämpft weiter.
    /// </summary>
    public class EscapeTracker
    {
        public const float Window = 2f, Judge = 10f;
        float _since = -1f; bool _attempt;
        public bool InAttempt => _attempt;

        /// <summary>Gibt true zurück, sobald ein Versuch beginnt (einmal je Versuch).</summary>
        public bool Tick(float now, bool inCombat, bool movingAway, bool attacked)
        {
            if (!inCombat || attacked || !movingAway) { if (attacked) _attempt = false; _since = -1f; return false; }
            if (_since < 0f) _since = now;
            if (!_attempt && now - _since >= Window) { _attempt = true; return true; }
            return false;
        }

        /// <summary>Versuch abschließen (aufgegeben / gestorben / kämpft weiter).</summary>
        public void Close() { _attempt = false; _since = -1f; }
    }
}
