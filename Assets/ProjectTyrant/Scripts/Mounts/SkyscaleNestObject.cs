using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 10: Objekte der Himmelsschuppen-Sammlung in der Welt (Ei, Glutstein, Brutpflegerin).</summary>
    public class SkyscaleNestObject : Interactable, IMapMarkerProvider
    {
        public enum Role { Egg, Stone, Keeper }
        public Role Kind;
        public int Index;
        public string NpcName = "Brutpflegerin Ilva";
        static readonly Color Talk = new Color(0.75f, 0.88f, 1f);

        protected override void OnEnable() { base.OnEnable(); MapMarkerRegistry.Register(this); }
        protected override void OnDisable() { base.OnDisable(); MapMarkerRegistry.Unregister(this); }

        HashSet<string> Done => PlayerProfile.Instance != null ? PlayerProfile.Instance.State.Completed : null;

        bool Visible
        {
            get
            {
                var c = Done; if (c == null) return Kind == Role.Keeper;
                switch (Kind)
                {
                    case Role.Egg: return !c.Contains(SkyscaleCollection.Egg);
                    case Role.Stone: return SkyscaleCollection.Step(c) == 1 && !c.Contains(SkyscaleCollection.StoneId(Index));
                    default: return true;
                }
            }
        }

        bool _shown = true;
        void Update()
        {
            bool v = Visible;
            if (v == _shown) return;
            _shown = v;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = v;   // keine SetActive-Umschaltung (Rig-Sicherheit)
        }

        public override string Prompt => Kind == Role.Egg ? "Himmelsschuppen-Ei bergen" : Kind == Role.Stone ? "Glutstein aufheben" : NpcName + " – Himmelsschuppe";
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && Visible;

        public override void Interact(Combatant player)
        {
            var prof = PlayerProfile.Instance; var c = Done; if (prof == null || c == null) return;
            switch (Kind)
            {
                case Role.Egg:
                    if (SkyscaleCollection.TakeEgg(c) == SkyscaleCollection.Result.Ok)
                    { GameEventBus.RaiseBanner("Himmelsschuppe 1/3", "Ein warmes Ei – bring es zur Brutpflegerin", Talk); SkillExecutor.VfxPulse(transform.position, 2f, Talk); }
                    break;
                case Role.Stone:
                    if (SkyscaleCollection.PickStone(c, Index) == SkyscaleCollection.Result.Ok)
                        GameEventBus.Notify("Glutstein " + SkyscaleCollection.StonesFound(c) + "/" + SkyscaleCollection.Stones, new Color(1f, 0.7f, 0.4f));
                    break;
                default:
                    var step = SkyscaleCollection.Step(c);
                    if (step == 0) GameEventBus.Notify(NpcName + ": „Auf dem höchsten Gipfel liegt ein verlassener Horst. Mit dem Greif kommst du hinauf.“", Talk);
                    else if (step == 1)
                    {
                        var r = SkyscaleCollection.HatchEgg(c);
                        if (r == SkyscaleCollection.Result.Ok) GameEventBus.RaiseBanner("Himmelsschuppe 2/3", "Das Junge ist geschlüpft!", Talk);
                        else GameEventBus.Notify(NpcName + ": „Das Ei braucht Wärme – " + SkyscaleCollection.StonesFound(c) + " von " + SkyscaleCollection.Stones + " Glutsteinen. Sie glühen an den Hängen ringsum.“", Talk);
                    }
                    else if (step == 2)
                    {
                        var r = SkyscaleCollection.BuySaddle(c, prof.Wallet);
                        if (r == SkyscaleCollection.Result.Ok) { prof.UnlockMount(MountKind.Skyscale); GameEventBus.RaiseBanner("Himmelsschuppe", "Neues Reittier – X halten zum Wählen", Talk); }
                        else GameEventBus.Notify(NpcName + ": „Ein Sattel kostet " + WalletModel.Format(SkyscaleCollection.SaddleCopper) + ".“", Talk);
                    }
                    else GameEventBus.Notify(NpcName + ": „Deine Himmelsschuppe wächst prächtig. Übe das Wandhaften!“", Talk);
                    break;
            }
            GameEventBus.RaiseProfileChanged(); prof.RequestSave("Himmelsschuppe");
        }

        public void CollectMarkers(List<MapMarkerInfo> into)
        {
            if (Kind != Role.Keeper) return;   // Ei und Glutsteine erkundet man selbst
            into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Vendor, State = MarkerState.Open, Position = transform.position, Label = NpcName + " (Himmelsschuppe)", Owner = this });
        }

        public TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if ((Object)info.Owner != this) return null;
            var c = TooltipBuilder.MapMarker(info, Done != null ? SkyscaleCollection.StepText(Done) : "Sammlung Himmelsschuppe");
            return c;
        }
    }
}
