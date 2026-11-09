using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Heldenherausforderung (10 Heldenpunkte): Kommunion (5-s-Kanal, Schaden unterbricht), Kampf (Veteran „Herausforderer“;
    /// Niederlage oder Entfernen setzt zurück) oder Relikt (3 Gegner + Zustandsdruck bis alle besiegt).
    /// </summary>
    public class HeroChallenge : ZoneObject
    {
        public Procedural.ZoneContent.ChallengeKind Kind;
        public EnemyArchetype Challenger, Minion;
        public float LeashRadius = 30f;
        readonly List<Combatant> _foes = new List<Combatant>();
        bool _active;
        float _curseTimer;

        void Reset() { PromptText = "Kommunizieren"; Range = 3f; }

        public bool Active => _active;
        public override string Prompt => Kind == Procedural.ZoneContent.ChallengeKind.Communion ? "Kommunizieren" : Kind == Procedural.ZoneContent.ChallengeKind.Combat ? "Herausforderer rufen" : "Relikt berühren";
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !Done && !_active;

        public override void Interact(Combatant player)
        {
            if (Done) return;
            switch (Kind)
            {
                case Procedural.ZoneContent.ChallengeKind.Communion: Complete(); break;
                case Procedural.ZoneContent.ChallengeKind.Combat: StartFight(Challenger, 1, "Ein Herausforderer erscheint!"); break;
                case Procedural.ZoneContent.ChallengeKind.Relic: StartFight(Minion, 3, "Der Fluch des Relikts erwacht!"); break;
            }
        }

        void StartFight(EnemyArchetype a, int count, string msg)
        {
            if (a == null || a.Prefab == null) { Complete(); return; }
            _active = true; _foes.Clear();
            for (int i = 0; i < count; i++)
            {
                float ang = i * Mathf.PI * 2f / count;
                var pos = transform.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 5f;
                pos.y = ZoneRoot.GroundHeight(pos.x, pos.z) + 0.1f;
                var b = NpcBrain.Spawn(a, pos, Quaternion.identity, transform);
                if (b == null) continue;
                var c = b.GetComponent<Combatant>();
                if (Kind == Procedural.ZoneContent.ChallengeKind.Combat) c.DisplayName = "Herausforderer";
                _foes.Add(c);
            }
            _curseTimer = 1f;
            GameEventBus.RaiseBanner("Heldenherausforderung", msg, new Color(0.4f, 0.8f, 1f));
        }

        void Update()
        {
            if (!_active) return;
            var p = PlayerC;
            bool lost = p == null || p.IsDead || p.IsDowned || !PlayerWithin(LeashRadius);
            if (lost) { ResetFight("Herausforderung gescheitert – zurückgesetzt"); return; }
            if (Kind == Procedural.ZoneContent.ChallengeKind.Relic && (_curseTimer -= Time.deltaTime) <= 0f)
            {
                _curseTimer = 4f;
                p.Effects.Apply(EffectType.Bleeding, 2, 6f);
                p.Effects.Apply(EffectType.Poison, 1, 5f);
            }
            for (int i = 0; i < _foes.Count; i++) if (_foes[i] != null && !_foes[i].IsDead) return;
            _active = false;
            Cleanup(3f);
            Complete();
        }

        public void ResetFight(string reason)
        {
            _active = false;
            Cleanup(0f);
            if (reason != null) GameEventBus.Notify(reason, new Color(1f, 0.6f, 0.4f));
        }

        void Cleanup(float delay)
        {
            foreach (var f in _foes) if (f != null) Destroy(f.gameObject, delay);
            _foes.Clear();
        }
    }
}
