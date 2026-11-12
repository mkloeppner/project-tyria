using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>
    /// Welt-Lauf W1: Aufstieg/Sprungrätsel – Kontrollpunkte. Ein Absturz setzt nicht mehr automatisch zurück (Spieler-Wunsch: der Aufstieg
    /// muss abbrechbar sein). Nur wer auf dem Pfad besiegt wird, erscheint am letzten Kontrollpunkt; wer den Pfad verlässt (am Boden,
    /// abseits aller Pfadpunkte), beendet die Verfolgung und wählt nach einer Niederlage wie gewohnt eine Wegmarke.
    /// </summary>
    public class ClimbArea : MonoBehaviour
    {
        public static readonly List<ClimbArea> All = new List<ClimbArea>();
        public string Id, Name;
        public List<Vector3> Checkpoints = new List<Vector3>();
        public List<Vector3> Stages = new List<Vector3>();
        [Tooltip("Pfadpunkte (Serpentine, Vorsprünge, Kletterplatten, Kontrollpunkte); leer = Kontrollpunkte")] public List<Vector3> PathPoints = new List<Vector3>();
        [Tooltip("Waagerechter Abstand zum Pfad, ab dem der Spieler den Aufstieg verlassen hat (m)")] public float PathRadius = 18f;
        public float Radius = 200f;
        CheckpointTracker _t;
        float _hintAt, _lastVisit = -999f;
        void OnEnable() { All.Add(this); _t = new CheckpointTracker(Checkpoints); }
        void OnDisable() => All.Remove(this);
        public int LastCheckpoint => _t != null ? _t.Last : -1;
        public bool Tracking => _t != null && _t.Active;
        IList<Vector3> Path => PathPoints != null && PathPoints.Count > 0 ? PathPoints : Checkpoints;
        float PathR => PathPoints != null && PathPoints.Count > 0 ? PathRadius : Mathf.Max(PathRadius, 30f);

        /// <summary>Der zuletzt betretene Aufstieg mit aktivem Kontrollpunkt (für die Wiederbelebung nach einer Niederlage), sonst null.</summary>
        public static ClimbArea ActiveTracking()
        {
            ClimbArea best = null;
            foreach (var a in All) if (a != null && a.Tracking && (best == null || a._lastVisit > best._lastVisit)) best = a;
            return best;
        }

        /// <summary>Kontrollpunkt für die Wiederbelebung; danach bleibt die Verfolgung aktiv (der Spieler steht wieder auf dem Pfad).</summary>
        public bool TryRespawnPoint(out Vector3 at)
        {
            at = default;
            var p = _t != null ? _t.RespawnPoint : null;
            if (!p.HasValue) return false;
            at = p.Value; return true;
        }

        /// <summary>Verfolgung beenden (z. B. Schnellreise, Zonenwechsel, Admin).</summary>
        public void CancelTracking() { if (_t != null) _t.Cancel(); }

        void Update()
        {
            var gm = GameManager.Instance; var p = gm != null ? gm.Player : null;
            if (p == null || p.IsDead) return;   // Niederlage: Kontrollpunkt bleibt bis zur Wiederbelebung erhalten
            var pos = p.transform.position;
            var d = pos - transform.position; d.y = 0f;
            bool inArea = d.sqrMagnitude <= Radius * Radius;
            if (!inArea && !_t.Active) return;
            var pc = p.GetComponent<PlayerController>();
            var mc = MountController.Local;
            bool flying = mc != null && mc.Flight != null && mc.Flight.Airborne;
            bool grounded = pc != null && pc.IsGrounded && !flying;
            bool onPath = inArea && CheckpointTracker.NearPath(pos, Path, PathR);
            if (_t.Track(onPath, grounded, Time.deltaTime))
            {
                GameEventBus.Notify(Name + " verlassen – Kontrollpunkt verworfen", new Color(0.8f, 0.85f, 0.9f));
                return;
            }
            if (!onPath || flying) return;   // Flug zählt nicht als Kletterfortschritt
            if (_t.Visit(pos))
            {
                _lastVisit = Time.time;
                if (Time.time >= _hintAt) { _hintAt = Time.time + 2f; GameEventBus.Notify("Kontrollpunkt: " + Name + " (" + (_t.Last + 1) + "/" + Checkpoints.Count + ")", new Color(0.7f, 0.95f, 1f)); }
                var st = PlayerProfile.Instance != null ? PlayerProfile.Instance.State : null;
                if (_t.Last == Checkpoints.Count - 1 && st != null && !st.Flags.Contains("climb:" + Id))
                {
                    st.Flags.Add("climb:" + Id);
                    st.Achievements.Add("w1_summits");
                    GameEventBus.RaiseBanner(Name, "Gipfel erreicht", new Color(0.75f, 0.95f, 1f));
                }
            }
        }
    }
}
