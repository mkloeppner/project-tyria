using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6 / M32: Belagerung der Feste nach <see cref="SiegeModel"/> – Tore fallen mit bestandenen Phasen, schließen sich bei Scheitern oder Neustart des Metas.</summary>
    public class SiegeDirector : MonoBehaviour
    {
        public static SiegeDirector Current { get; private set; }
        public readonly SiegeModel Model = new SiegeModel();
        public GameObject[] Gates = new GameObject[0];
        public string[] GateEvents = new string[0];

        void OnEnable() { Current = this; Hook(); Apply(); }
        void OnDisable() { if (Current == this) Current = null; var ed = EventDirector.Instance; if (ed != null && ed.Scheduler != null) ed.Scheduler.Ended -= OnEnded; }
        void Hook() { var ed = EventDirector.Instance; if (ed != null && ed.Scheduler != null) { ed.Scheduler.Ended -= OnEnded; ed.Scheduler.Ended += OnEnded; } }
        void Update() { if (Time.frameCount % 120 == 0) Hook(); }

        void OnEnded(EventRunner r)
        {
            if (r == null || r.Def == null || r.Def.MetaId != "belagerung") return;
            if (Model.Completed && r.Def.Id == SiegeModel.Phases[0]) Model.Reset();   // neuer Zyklus: Tore wieder zu
            if (r.State == RunnerState.Succeeded) Model.Succeed(r.Def.Id); else Model.Fail(r.Def.Id);
            if (Model.Completed) GameEventBus.RaiseBanner("Die Feste ist gefallen", "Belagerung bestanden – die Tore bleiben bis zum nächsten Zyklus offen", new Color(1f, 0.7f, 0.5f));
            Apply();
        }

        void Apply()
        {
            for (int i = 0; i < Gates.Length; i++) if (Gates[i] != null) Gates[i].SetActive(i >= Model.GatesDown);
        }
    }
}
