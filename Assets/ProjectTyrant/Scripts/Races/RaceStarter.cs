using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 11: Rennleiter (F) – startet ein Reittier-Rennen über die Checkpoint-Ringe (<see cref="RaceModel"/>); Boost-Pads, Bestzeit, Titel „Dünenreiter“.</summary>
    public class RaceStarter : Interactable, IMapMarkerProvider
    {
        public string RaceId = "rennen", Title = "Rennen";
        public List<Transform> Checkpoints = new List<Transform>();
        public List<Transform> BoostPads = new List<Transform>();
        public float TimeLimit = 180f, GoldTime = 90f;
        RaceModel _m; float _boostT;
        static readonly Dictionary<string, RaceModel> _models = new Dictionary<string, RaceModel>();

        protected override void OnEnable() { base.OnEnable(); MapMarkerRegistry.Register(this); }
        protected override void OnDisable() { base.OnDisable(); MapMarkerRegistry.Unregister(this); }
        public override string Prompt => "Rennleiter – " + Title;

        public override void Interact(Combatant player)
        {
            var mc = MountController.Local;
            if (mc == null || !mc.Motor.IsMounted) { GameEventBus.Notify("Sitz zuerst auf ein Reittier auf", new Color(1f, 0.8f, 0.5f)); return; }
            if (!_models.TryGetValue(RaceId, out _m)) { var cps = new Vector3[Checkpoints.Count]; for (int i = 0; i < cps.Length; i++) cps[i] = Checkpoints[i].position; _models[RaceId] = _m = new RaceModel(RaceId, cps, TimeLimit); }
            if (_m.Start(mc.Motor.Mounted)) GameEventBus.RaiseBanner(Title, "Los! " + _m.Checkpoints.Length + " Checkpoints · " + Mathf.RoundToInt(TimeLimit) + " s" + (_m.BestTimes.TryGetValue(mc.Motor.Mounted, out var b) ? " · Bestzeit " + b.ToString("0.0") + " s" : ""), new Color(1f, 0.85f, 0.4f));
        }

        void Update()
        {
            if (_m == null || _m.Phase != RaceModel.State.Running) return;
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null; if (p == null) return;
            var pc = p.GetComponent<PlayerController>();
            foreach (var bp in BoostPads) if (bp != null && Vector3.Distance(bp.position, p.transform.position) < 4f) _boostT = RaceModel.BoostTime;
            if (_boostT > 0f) { _boostT -= Time.deltaTime; if (pc != null && pc.SpeedOverride > 0f) pc.SpeedOverride *= RaceModel.BoostFactor; }
            int before = _m.Next;
            bool done = _m.Tick(Time.deltaTime, p.transform.position);
            if (_m.Next != before && !done) GameEventBus.Notify("Checkpoint " + _m.Next + "/" + _m.Checkpoints.Length + " · " + _m.Time.ToString("0.0") + " s", new Color(0.8f, 1f, 0.6f));
            if (done)
            {
                var prof = PlayerProfile.Instance;
                GameEventBus.RaiseBanner("Ziel!", _m.Time.ToString("0.0") + " s" + (_m.Time <= GoldTime ? " · Gold!" : ""), new Color(1f, 0.85f, 0.4f));
                if (prof != null) { prof.GrantReward(100, 3000, 0, "Rennen"); RegionWallet.Add(prof.State, RegionCurrency.BountyTokens, _m.Time <= GoldTime ? 3 : 1); prof.State.Achievements.Add("race_both"); if (_m.Time <= GoldTime) prof.State.Achievements.Add("race_gold"); }
            }
            else if (_m.Phase == RaceModel.State.TimedOut) GameEventBus.Notify("Zeit abgelaufen", new Color(1f, 0.5f, 0.4f));
        }

        public void CollectMarkers(List<MapMarkerInfo> into) => into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Vendor, State = MarkerState.Open, Position = transform.position, Label = "Rennleiter: " + Title, Owner = this });
        public TooltipContent MarkerTooltip(MapMarkerInfo info) => (Object)info.Owner == this ? TooltipBuilder.MapMarker(info, info.Label) : null;
    }
}
