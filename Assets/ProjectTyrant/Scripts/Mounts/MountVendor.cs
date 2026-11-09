using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Stallmeister (F): verkauft ein Reittier, sobald das Herz der Zone erfüllt ist und das Gold reicht.</summary>
    public class MountVendor : Interactable, IMapMarkerProvider
    {
        public MountDefinition Mount;
        public string NpcName = "Stallmeister";

        void Reset() { PromptText = "Sprechen"; Range = 3.2f; }
        protected override void OnEnable() { base.OnEnable(); MapMarkerRegistry.Register(this); }
        protected override void OnDisable() { base.OnDisable(); MapMarkerRegistry.Unregister(this); }

        public override string Prompt => NpcName + " – " + (Mount != null ? Mount.DisplayName : "Reittier");
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && Mount != null;

        public override void Interact(Combatant player)
        {
            var prof = PlayerProfile.Instance; var ui = UIController.Instance;
            if (prof == null || Mount == null) return;
            var c = new Color(1f, 0.85f, 0.5f);
            switch (prof.CanBuyMount(Mount))
            {
                case MountShop.Verdict.AlreadyOwned: GameEventBus.Notify(NpcName + ": „Dein " + Mount.DisplayName + " ist gut versorgt. Drücke X zum Aufsitzen.“", c); break;
                case MountShop.Verdict.HeartMissing: GameEventBus.Notify(NpcName + ": „Hilf erst den Leuten hier (Herz erfüllen), dann vertraue ich dir einen " + Mount.DisplayName + " an.“", c); break;
                case MountShop.Verdict.NotEnoughGold: GameEventBus.Notify(NpcName + ": „Ein " + Mount.DisplayName + " kostet " + WalletModel.Format(Mount.PriceCopper) + ".“", c); break;
                default:
                    if (ui != null && ui.Dialog != null) ui.Dialog.Confirm("Reittier: " + Mount.DisplayName, Mount.Description + "\n\nLeertaste: " + Mount.AbilityText + "\nKosten: " + WalletModel.Format(Mount.PriceCopper), () => prof.BuyMount(Mount), "Kaufen");
                    else prof.BuyMount(Mount);
                    break;
            }
        }

        public void CollectMarkers(System.Collections.Generic.List<MapMarkerInfo> into)
        {
            into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Vendor, State = MarkerState.Open, Position = transform.position, Label = NpcName + " (" + (Mount != null ? Mount.DisplayName : "") + ")", Owner = this });
        }

        public TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if ((Object)info.Owner != this || Mount == null) return null;
            var c = TooltipBuilder.MapMarker(info, "Reittier: " + Mount.DisplayName + " · " + WalletModel.Format(Mount.PriceCopper));
            var prof = PlayerProfile.Instance;
            if (prof != null && !prof.OwnsMount(Mount.Kind) && prof.CanBuyMount(Mount) == MountShop.Verdict.HeartMissing) c.Add("Voraussetzung: Herz der Zone erfüllen", new Color(1f, 0.6f, 0.45f));
            return c;
        }
    }
}
