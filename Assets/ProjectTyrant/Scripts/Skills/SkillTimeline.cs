using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum SkillMoveRule { Free, Rooted, Leap, Lunge }

    /// <summary>
    /// Lauf 5 / M22: gemeinsame Zeitachse eines Skills (Daten, vom Builder abgeleitet). Zeiten relativ zum Aktivierungsbeginn.
    /// Aktivierung (blockierend bis zum letzten Treffer), Treffer-/Projektil-Frames, Nachwirkung (Aftercast), Bewegungs-Regel und drei Spuren:
    /// Animation (Clip + Zeitfaktor: Clip-Länge × Faktor = Aktivierung + Nachwirkung), VFX (Komposition je Phase) und SFX (je Ereignis).
    /// </summary>
    [Serializable]
    public class SkillTimeline
    {
        public float Activation;          // s bis zum letzten Treffer (0 = sofort)
        public float Aftercast;           // s Nachwirkung (blockiert nur die Warteschlange; Ausweichen bricht ab)
        public List<float> Hits = new List<float>();
        public List<float> Projectiles = new List<float>();
        public SkillMoveRule Movement = SkillMoveRule.Free;
        public float MoveDistance;
        public bool Instant;              // Sofort-Skill: läuft neben einer Aktivierung, unterbricht die Warteschlange nicht
        public bool Channel;              // mehrere Treffer über die Aktivierung verteilt (Kanal-Balken)
        [Header("Animation")] public string Clip; public float AnimScale = 1f;
        [Header("VFX (Komposition „Form/Palette/Größe“)")] public string VfxCharge, VfxRelease, VfxProjectile, VfxImpact, VfxAfter;
        [Header("SFX (Familie/Ereignis)")] public string SfxCharge, SfxRelease, SfxImpact;

        public bool IsBuilt => !string.IsNullOrEmpty(Clip);
        public float Total => Activation + Aftercast;
        public float FirstHit => Hits.Count > 0 ? Hits[0] : (Projectiles.Count > 0 ? Projectiles[0] : Activation);
        /// <summary>Abspieltempo des Clips (1/Zeitfaktor).</summary>
        public float AnimSpeed => AnimScale > 1e-4f ? 1f / AnimScale : 1f;

        /// <summary>
        /// Ableitung aus den Skill-Daten und den Clip-Markern: Zeitfaktor so, dass der Treffer-/Geschoss-Marker des Clips auf den ersten Treffer fällt;
        /// Nachwirkung = Rest des Clips × Faktor. Auto-Angriffe behalten ihre Taktung (Aktivierung + Nachwirkung = bisherige Aktivierungszeit).
        /// </summary>
        public static SkillTimeline Derive(float castTime, int hits, float hitInterval, bool projectile, bool autoAttack, SkillMoveRule move, float moveDistance,
                                           string clip, float clipLength, float clipHitMarker, float clipLastHitMarker)
        {
            var t = new SkillTimeline { Clip = clip, Movement = move, MoveDistance = moveDistance };
            hits = Mathf.Max(1, hits);
            float span = (hits - 1) * Mathf.Max(0f, hitInterval);
            clipLength = Mathf.Max(0.05f, clipLength);
            if (clipHitMarker <= 0f) clipHitMarker = clipLength * 0.5f;
            if (clipLastHitMarker < clipHitMarker) clipLastHitMarker = clipHitMarker;
            float first;
            if (autoAttack && castTime > 0f)
            {
                // Takt bleibt: Clip füllt genau die bisherige Aktivierungszeit (+ Kanal-Spanne)
                t.AnimScale = (castTime + span) / clipLength;
                first = clipHitMarker * t.AnimScale;
            }
            else if (castTime <= 0.05f)
            {
                t.Instant = castTime <= 0f && hits == 1;
                first = Mathf.Max(0f, castTime);
                t.AnimScale = Mathf.Clamp(Mathf.Max(0.05f, first) / clipHitMarker, 0.35f, 1f);
                if (first <= 0f) t.AnimScale = Mathf.Clamp(0.25f / clipLength, 0.35f, 1f);
            }
            else
            {
                first = castTime;
                // mehrere Treffer: letzter Clip-Treffer auf den letzten Treffer, sonst erster Treffer auf die Aktivierung
                t.AnimScale = hits > 1 ? Mathf.Clamp((first + span) / Mathf.Max(clipLastHitMarker, clipHitMarker), 0.3f, 3f)
                                       : Mathf.Clamp(first / clipHitMarker, 0.3f, 3f);
            }
            for (int i = 0; i < hits; i++) { float h = first + i * hitInterval; if (projectile) t.Projectiles.Add(h); else t.Hits.Add(h); }
            t.Activation = first + span;
            t.Channel = hits > 1;
            t.AnimScale = Mathf.Max(t.AnimScale, t.Activation / clipLength);   // Clip deckt immer die ganze Aktivierung
            t.Aftercast = Mathf.Max(0f, clipLength * t.AnimScale - t.Activation);
            return t;
        }

        /// <summary>Konsistenz: Treffer innerhalb der Aktivierung, Clip × Faktor = Aktivierung + Nachwirkung, alle drei Spuren belegt.</summary>
        public bool Validate(float clipLength, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(Clip)) { error = "keine Animation"; return false; }
            if (Hits.Count == 0 && Projectiles.Count == 0) { error = "kein Treffer-/Geschoss-Frame"; return false; }
            foreach (var h in Hits) if (h < -1e-4f || h > Activation + 1e-3f) { error = "Treffer " + h.ToString("0.00") + " außerhalb der Aktivierung " + Activation.ToString("0.00"); return false; }
            foreach (var h in Projectiles) if (h < -1e-4f || h > Activation + 1e-3f) { error = "Geschoss " + h.ToString("0.00") + " außerhalb der Aktivierung"; return false; }
            if (Mathf.Abs(clipLength * AnimScale - (Activation + Aftercast)) > 0.02f) { error = "Clip × Faktor " + (clipLength * AnimScale).ToString("0.00") + " ≠ Aktivierung + Nachwirkung " + Total.ToString("0.00"); return false; }
            if (string.IsNullOrEmpty(VfxRelease) || string.IsNullOrEmpty(VfxImpact)) { error = "VFX-Spur leer"; return false; }
            if (string.IsNullOrEmpty(SfxRelease) || string.IsNullOrEmpty(SfxImpact)) { error = "SFX-Spur leer"; return false; }
            return true;
        }
    }

    /// <summary>Auto-Angriffs-Kette (rein): 3 Glieder, läuft bei jedem abgeschlossenen Glied weiter, setzt nach 2 s Pause zurück.</summary>
    public class AutoChainModel
    {
        public const float ResetAfter = 2f;
        public int Length { get; private set; } = 3;
        public int Index { get; private set; }
        float _lastTime = float.NegativeInfinity;
        public AutoChainModel(int length = 3) { Length = Mathf.Max(1, length); }
        /// <summary>Aktuelles Glied zur Zeit now (setzt nach Pause zurück).</summary>
        public int Current(float now) { if (now - _lastTime > ResetAfter) Index = 0; return Index; }
        /// <summary>Glied abgeschlossen → nächstes.</summary>
        public void Completed(float now) { Current(now); Index = (Index + 1) % Length; _lastTime = now; }
        public void Reset() { Index = 0; _lastTime = float.NegativeInfinity; }
    }

    /// <summary>
    /// Warteschlange (rein, GW2): ein während Aktivierung/Nachwirkung gedrückter Skill wird eingereiht (der zuletzt gedrückte gewinnt) und startet,
    /// sobald der Spieler frei ist. Sofort-Skills laufen neben der Aktivierung und lassen die Warteschlange unberührt. Shift erzwingt den Start.
    /// </summary>
    public class SkillQueueModel<T> where T : class
    {
        public enum Decision { StartNow, Queued, StartAlongside, ForceStart }
        public T Queued { get; private set; }
        public float BusyUntil { get; private set; }
        public float AftercastFrom { get; private set; }
        public bool IsBusy(float now) => now < BusyUntil;

        public Decision Press(T skill, bool instant, bool force, float now)
        {
            if (force) { Queued = null; return Decision.ForceStart; }
            if (instant) return IsBusy(now) ? Decision.StartAlongside : Decision.StartNow;
            if (!IsBusy(now)) return Decision.StartNow;
            Queued = skill; return Decision.Queued;
        }

        /// <summary>Aktivierung beginnt: belegt bis Aktivierung + Nachwirkung.</summary>
        public void Begin(float now, float activation, float aftercast) { AftercastFrom = now + activation; BusyUntil = now + activation + aftercast; }
        /// <summary>Ausweichen/Unterbrechung: Aktivierung und Nachwirkung enden, Warteschlange leert sich.</summary>
        public void Cancel(float now) { BusyUntil = now; Queued = null; }
        /// <summary>Nachwirkung frei ab sofort (Bewegung bricht Nachwirkung nicht – nur Ausweichen).</summary>
        public T PopReady(float now) { if (IsBusy(now) || Queued == null) return null; var q = Queued; Queued = null; return q; }
        public void Clear() => Queued = null;
    }
}
