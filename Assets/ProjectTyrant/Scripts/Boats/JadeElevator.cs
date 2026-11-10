using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 11: Aufzug (Jadehafen, Jade-Geräte). F an der Plattform: nächste Ebene. Stadt-Aufzüge frei, Welt-Aufzüge brauchen Jadebatterien.</summary>
    public class JadeElevator : Interactable
    {
        public float[] Levels = { 0f, 18f, 36f };
        public bool NeedsJade;
        public Transform Platform;
        ElevatorModel _m;

        void Awake() { _m = new ElevatorModel(Levels, NeedsJade); }
        public override string Prompt => NeedsJade ? "Jade-Aufzug (Jadebatterien)" : "Aufzug – nächste Ebene";

        public override void Interact(Combatant player)
        {
            var bot = player != null ? player.GetComponent<JadeBotController>() : null;
            if (!_m.Call((_m.Current + 1) % Levels.Length, bot != null ? bot.Model : null)) { GameEventBus.Notify("Der Aufzug braucht Jadebatterien (Jadegeist-Beherrschung 3)", new Color(0.6f, 1f, 0.8f)); return; }
        }

        void Update()
        {
            if (_m == null || Platform == null) return;
            float before = _m.Height;
            _m.Tick(Time.deltaTime);
            float dy = _m.Height - before;
            var lp = Platform.localPosition; lp.y = _m.Height; Platform.localPosition = lp;
            if (Mathf.Abs(dy) < 1e-5f) return;
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            if (p == null) return;
            var d = p.transform.position - Platform.position; d.y = 0f;
            if (d.magnitude < 3f && Mathf.Abs(p.transform.position.y - Platform.position.y) < 2.5f)
            {
                var pc = p.GetComponent<PlayerController>(); if (pc != null) pc.Teleport(p.transform.position + Vector3.up * dy, p.transform.rotation);
            }
        }
    }
}
