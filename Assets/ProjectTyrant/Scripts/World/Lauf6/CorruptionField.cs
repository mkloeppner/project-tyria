using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6 / M30: Verderbnis-Fleck im Dämmerwald (Welt-Zustand): wächst mit der Zeit, weicht nach bestandenen Verderbnis-Ereignissen zurück.
    /// Im Fleck Qual-Stapel, außer im Licht einer Laterne (Wächterstein-Laterne oder Laternen-Bündel) oder mit Umwelt-Beherrschung „Giftresistenz“.
    /// </summary>
    public class CorruptionField : MonoBehaviour
    {
        public static readonly List<CorruptionField> All = new List<CorruptionField>();
        public float BaseRadius = 14f;   // vom Builder gesetzt (serialisiert)
        public CorruptionModel Model { get; private set; } = new CorruptionModel();
        void Awake() { Model = new CorruptionModel(BaseRadius); }
        public string[] SuppressedBy = { "wald_laternen", "wald_priester" };
        public Transform Visual;
        float _tick;

        void OnEnable() { All.Add(this); Hook(); }
        void OnDisable() { All.Remove(this); var ed = EventDirector.Instance; if (ed != null && ed.Scheduler != null) ed.Scheduler.Ended -= OnEnded; }
        void Hook() { var ed = EventDirector.Instance; if (ed != null && ed.Scheduler != null) { ed.Scheduler.Ended -= OnEnded; ed.Scheduler.Ended += OnEnded; } }

        void OnEnded(EventRunner r)
        {
            if (r == null || r.Def == null || System.Array.IndexOf(SuppressedBy, r.Def.Id) < 0) return;
            if (r.State == RunnerState.Succeeded) { Model.EventSucceeded(); GameEventBus.Notify("Die Verderbnis weicht zurück", new Color(0.8f, 0.7f, 1f)); }
            else Model.EventFailed();
        }

        void Update()
        {
            if (EventDirector.Instance != null && Time.frameCount % 120 == 0) Hook();
            Model.Tick(Time.deltaTime);
            if (Visual != null) Visual.localScale = new Vector3(Model.Radius * 2f, Visual.localScale.y, Model.Radius * 2f);
            if ((_tick -= Time.deltaTime) > 0f) return; _tick = 1f;
            var gm = GameManager.Instance; var p = gm != null ? gm.Player : null; if (p == null || p.IsDead) return;
            var d = p.transform.position - transform.position; d.y = 0f;
            bool light = CorruptionWard.Covers(p.transform.position) || (gm.Story != null && gm.Story.Bundle.Current == "laterne");
            var prof = PlayerProfile.Instance; bool mastery = prof != null && MasteryEffects.PoisonMistImmune(prof.Mastery);
            int n = Model.TormentPerSecond(d.magnitude, light, mastery);
            if (n > 0) p.Effects.Apply(new EffectApplication(EffectType.Torment, n, 3f), null);
        }
    }
}
