using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 11: instabile Eigenschaften eines Kopfgeld-Ziels zur Laufzeit (Muster-Baukasten: Segen, Zustände, Beschwörungen, Trotzleiste).</summary>
    public class BountyTarget : MonoBehaviour
    {
        public readonly List<BountyModifier> Modifiers = new List<BountyModifier>();
        public Combatant Self { get; private set; }
        float _t; bool _orbDone; Vector3 _last;

        void Start()
        {
            Self = GetComponent<Combatant>();
            if (Self == null) return;
            Self.SetHealth(Self.MaxHealth);
            if (Modifiers.Contains(BountyModifier.Schnelligkeit)) Self.Effects.Apply(new EffectApplication(EffectType.Quickness, 1, 9999f), Self);
            if (Modifiers.Contains(BountyModifier.Zorn)) Self.Damaged += d => Self.Effects.Apply(new EffectApplication(EffectType.Might, 1, 6f), Self);
            _last = transform.position;
        }

        void Update()
        {
            if (Self == null || Self.IsDead) return;
            _t += Time.deltaTime;
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            float d = p != null ? Vector3.Distance(p.transform.position, transform.position) : 999f;
            if (Modifiers.Contains(BountyModifier.HeilungBeiBewegung) && Vector3.Distance(_last, transform.position) > 0.5f) { Self.SetHealth(Mathf.Min(Self.MaxHealth, Self.Health + Self.MaxHealth * 0.004f)); _last = transform.position; }
            if (_t < 5f) return;
            _t = 0f;
            if (p == null || d > 30f) return;
            foreach (var m in Modifiers)
                switch (m)
                {
                    case BountyModifier.Blindheitsnebel: if (d < 10f) p.Effects.Apply(new EffectApplication(EffectType.Blind, 1, 2f), Self); break;
                    case BountyModifier.FesselFelder: if (d < 8f) p.Effects.Apply(new EffectApplication(EffectType.Immobilize, 1, 1f), Self); break;
                    case BountyModifier.KettenBlitz: if (d < 15f) DamageCalculator.DealDirect(Self, p, 1.5f); break;
                    case BountyModifier.ReittierAbwurf: var mc = MountController.Local; if (mc != null && mc.Motor.IsMounted && d < 6f) { mc.ApplyDismount(); GameEventBus.Notify("Abgeworfen!", new Color(1f, 0.5f, 0.4f)); } break;
                    case BountyModifier.TrotzFenster: var bar = DefianceBar.Of(Self); if (bar != null && !bar.Model.Broken) GameEventBus.Notify("Trotzleisten-Fenster: jetzt Kontrolle!", new Color(0.6f, 0.9f, 1f)); break;
                    case BountyModifier.Spiegelbilder: case BountyModifier.Orbkranz:
                        if (m == BountyModifier.Orbkranz && (_orbDone || Self.Health > Self.MaxHealth * 0.5f)) break;
                        if (m == BountyModifier.Orbkranz) _orbDone = true;
                        var brain = GetComponent<NpcBrain>();
                        if (brain != null && brain.Archetype != null && brain.Archetype.SummonArchetype != null && brain.Archetype.SummonArchetype.Prefab != null)
                            for (int i = 0; i < 2; i++) Instantiate(brain.Archetype.SummonArchetype.Prefab, ZoneAnchors.GroundAt(transform.position + Quaternion.Euler(0, i * 180f, 0) * Vector3.forward * 4f + Vector3.up * 2f), Quaternion.identity);
                        break;
                }
        }
    }
}
