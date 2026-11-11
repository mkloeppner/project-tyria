using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Welt-Lauf W1: Aufstieg/Sprungrätsel – Kontrollpunkte; Sturz setzt an den letzten Kontrollpunkt zurück (nicht an die Wegmarke).</summary>
    public class ClimbArea : MonoBehaviour
    {
        public static readonly List<ClimbArea> All = new List<ClimbArea>();
        public string Id, Name;
        public List<Vector3> Checkpoints = new List<Vector3>();
        public List<Vector3> Stages = new List<Vector3>();
        public float Radius = 200f;
        CheckpointTracker _t;
        float _hintAt;
        void OnEnable() { All.Add(this); _t = new CheckpointTracker(Checkpoints); }
        void OnDisable() => All.Remove(this);
        public int LastCheckpoint => _t != null ? _t.Last : -1;

        void Update()
        {
            var gm = GameManager.Instance; var p = gm != null ? gm.Player : null;
            if (p == null || p.IsDead) return;
            var pos = p.transform.position;
            var d = pos - transform.position; d.y = 0f;
            if (d.sqrMagnitude > Radius * Radius) return;
            var mc = MountController.Local;
            if (mc != null && mc.Flight != null && mc.Flight.Airborne) return;   // Flug zählt nicht als Sturz
            if (_t.Visit(pos))
            {
                if (Time.time >= _hintAt) { _hintAt = Time.time + 2f; GameEventBus.Notify("Kontrollpunkt: " + Name + " (" + (_t.Last + 1) + "/" + Checkpoints.Count + ")", new Color(0.7f, 0.95f, 1f)); }
                var st = PlayerProfile.Instance != null ? PlayerProfile.Instance.State : null;
                if (_t.Last == Checkpoints.Count - 1 && st != null && !st.Flags.Contains("climb:" + Id))
                {
                    st.Flags.Add("climb:" + Id);
                    st.Achievements.Add("w1_summits");
                    GameEventBus.RaiseBanner(Name, "Gipfel erreicht", new Color(0.75f, 0.95f, 1f));
                }
            }
            var pc = p.GetComponent<PlayerController>();
            if (pc != null && pc.Airborne && pc.VerticalVelocity < -12f && _t.ShouldReset(pos, out var reset))
            {
                pc.Teleport(reset, p.transform.rotation); pc.ResetFallTracking();
                GameEventBus.Notify("Abgestürzt – zurück zum Kontrollpunkt", new Color(1f, 0.8f, 0.5f));
            }
        }
    }
}
