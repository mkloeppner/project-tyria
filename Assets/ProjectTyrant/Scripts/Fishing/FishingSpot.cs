using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 11: Angelstelle (Fischschwarm). F: Angeln beginnen – Leertaste halten = Wurf, loslassen; nach dem Biss Leertaste halten = einholen,
    /// Spannung im grünen Bereich halten (<see cref="FishingModel"/>). Fang landet in der Sammlung (Profil) und im Erfolgsfortschritt.
    /// </summary>
    public class FishingSpot : Interactable
    {
        public string Zone;
        FishingModel _m; bool _active; float _msgT;

        public override string Prompt => _active ? "Angeln beenden" : "Angeln";

        public override void Interact(Combatant player)
        {
            if (_active) { _active = false; return; }
            _active = true; _m = new FishingModel(Random.Range(0, 99999));
            var mc = MountController.Local; _m.FromBoat = mc != null && mc.Motor.Mounted == MountKind.Skiff;
            _m.RodTier = 1 + Mathf.Min(2, PlayerProfile.Instance != null ? PlayerProfile.Instance.State.Achievements.Done.Count / 4 : 0);
            _m.BeginCast();
            GameEventBus.Notify("Leertaste halten: Wurf · loslassen: auswerfen", new Color(0.7f, 0.9f, 1f));
        }

        void Update()
        {
            if (!_active || _m == null) return;
            var input = InputManager.Instance; if (input == null) return;
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            if (p == null || Vector3.Distance(p.transform.position, transform.position) > Range + 6f) { _active = false; return; }
            float dt = Time.deltaTime;
            switch (_m.State)
            {
                case FishingModel.Phase.Casting: if (input.JumpHeld) _m.HoldCast(dt); if (input.JumpReleased) { _m.ReleaseCast(); GameEventBus.Notify("Ausgeworfen: " + _m.Distance.ToString("0") + " m", new Color(0.7f, 0.9f, 1f)); } break;
                case FishingModel.Phase.Waiting:
                    bool night = System.DateTime.Now.Hour < 6 || System.DateTime.Now.Hour >= 21;
                    if (_m.Wait(dt, FishCatalog.For(Zone), night)) GameEventBus.Notify("Biss! Leertaste halten zum Einholen – Spannung im grünen Bereich halten", new Color(1f, 0.9f, 0.4f));
                    break;
                case FishingModel.Phase.Hooked:
                    _m.Fight(dt, input.JumpHeld);
                    _msgT -= dt; if (_msgT <= 0f) { _msgT = 0.6f; GameEventBus.Notify("Spannung " + Mathf.RoundToInt(_m.Tension * 100) + " % · Fortschritt " + Mathf.RoundToInt(_m.Progress * 100) + " %", _m.Tension >= FishingModel.SafeLow && _m.Tension <= FishingModel.SafeHigh ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.5f, 0.4f)); }
                    if (_m.State == FishingModel.Phase.Caught && _m.Hooked.HasValue)
                    {
                        var f = _m.Hooked.Value; var prof = PlayerProfile.Instance;
                        GameEventBus.RaiseBanner("Gefangen: " + f.Name, f.Rarity.ToString(), new Color(0.6f, 0.9f, 1f));
                        if (prof != null) { prof.State.Completed.Add(f.Id); prof.State.Achievements.Add("fish_50"); if (f.Rarity == FishRarity.Legendaer) prof.State.Achievements.Add("fish_legend"); if (FishCatalog.Complete(Zone, prof.State.Completed)) prof.State.Achievements.Add("fishcol_" + Zone, 99); prof.GrantReward(20, 200, 0, "Angeln"); }
                        _active = false;
                    }
                    else if (_m.State == FishingModel.Phase.Lost) { GameEventBus.Notify("Der Fisch ist entkommen", new Color(1f, 0.6f, 0.4f)); _active = false; }
                    break;
            }
        }
    }
}
