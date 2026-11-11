using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Wachen-Patrouille auf den Wegen (Stufe 0–2): läuft die Wegpunkte ab, solange kein Kampf läuft.</summary>
    public class GuardPatrol : MonoBehaviour
    {
        public Vector3[] Points = new Vector3[0];
        int _i, _dir = 1; NpcBrain _b;
        void Awake() { _b = GetComponent<NpcBrain>(); }
        void Update()
        {
            if (_b == null || Points == null || Points.Length < 2 || _b.Self == null || _b.Self.IsDead) return;
            if (_b.State != NpcState.Idle) { _b.Objective = null; return; }
            var p = Points[_i];
            if (new Vector2(p.x - transform.position.x, p.z - transform.position.z).magnitude < 2f)
            {
                if (_i + _dir < 0 || _i + _dir >= Points.Length) _dir = -_dir;
                _i += _dir;
                _b.HomePosition = transform.position;   // Rückkehr nach Kämpfen zum letzten Wegpunkt
            }
            _b.Objective = Points[_i];
        }
    }
}
