using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 11: Reittier-Rennen (rein, getestet). Checkpoints in Reihenfolge, Zeitlimit, Bestzeit je Reittier, Boost-Pads (+50 % für 3 s),
    /// Abkürzungen (Sandportale) nur mit dem Dünenläufer, Geisterfahrer = aufgezeichnete Bestzeit-Spur.
    /// </summary>
    public class RaceModel
    {
        public enum State { Idle, Running, Finished, TimedOut }
        public string Id; public Vector3[] Checkpoints; public float CheckpointRadius = 8f, TimeLimit = 180f;
        public State Phase = State.Idle;
        public int Next; public float Time; public MountKind Mount;
        public readonly Dictionary<MountKind, float> BestTimes = new Dictionary<MountKind, float>();
        public readonly List<Vector3> Recording = new List<Vector3>();
        public List<Vector3> Ghost = new List<Vector3>();
        public const float GhostSampleInterval = 0.5f, BoostFactor = 1.5f, BoostTime = 3f;
        float _sample;

        public RaceModel(string id, Vector3[] checkpoints, float limit) { Id = id; Checkpoints = checkpoints; TimeLimit = limit; }

        public bool Start(MountKind mount)
        {
            if (mount == MountKind.None || Phase == State.Running) return false;
            Mount = mount; Phase = State.Running; Next = 0; Time = 0f; Recording.Clear(); _sample = 0f; return true;
        }

        /// <summary>Pro Frame: Zeit, Aufzeichnung, Checkpoint-Prüfung. Rückgabe true = Ziel erreicht.</summary>
        public bool Tick(float dt, Vector3 pos)
        {
            if (Phase != State.Running) return false;
            Time += dt; _sample += dt;
            if (_sample >= GhostSampleInterval) { _sample = 0f; Recording.Add(pos); }
            if (Time > TimeLimit) { Phase = State.TimedOut; return false; }
            if (Vector3.Distance(pos, Checkpoints[Next]) <= CheckpointRadius)
            {
                Next++;
                if (Next >= Checkpoints.Length) { Finish(); return true; }
            }
            return false;
        }

        void Finish()
        {
            Phase = State.Finished;
            if (!BestTimes.TryGetValue(Mount, out var best) || Time < best) { BestTimes[Mount] = Time; Ghost = new List<Vector3>(Recording); }
        }

        /// <summary>Abkürzung: Sandportal überspringt Checkpoints – nur mit dem Dünenläufer.</summary>
        public bool UseShortcut(int fromCheckpoint, int toCheckpoint)
        {
            if (Phase != State.Running || Mount != MountKind.DuneRunner || fromCheckpoint != Next || toCheckpoint <= fromCheckpoint || toCheckpoint >= Checkpoints.Length) return false;
            Next = toCheckpoint; return true;
        }

        public Vector3 GhostAt(float t) { if (Ghost.Count == 0) return Vector3.zero; int i = Mathf.Clamp(Mathf.FloorToInt(t / GhostSampleInterval), 0, Ghost.Count - 1); return Ghost[i]; }
    }
}
