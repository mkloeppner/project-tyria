using UnityEngine;

namespace ProjectTyrant
{
    public partial class PlayerProfile
    {
        /// <summary>Reisekosten innerhalb der Zone oder in eine andere Zone (Aufschlag), Kommandant-Rabatt ab M18.</summary>
        public int ZoneTravelCost(Vector3 from, Vector3 to, bool otherZone) => WorldModel.TravelCost(from, to, State.Progression.Level, otherZone, TravelDiscount);
        public float TravelDiscount { get; set; }

        /// <summary>Zonenübergreifende Schnellreise: Kosten abbuchen, Zonenwechsel mit Ladebildschirm zur Wegmarke.</summary>
        public bool TravelToZone(ZoneDefinition zone, Vector3 waypoint, int cost)
        {
            var gm = GameManager.Instance;
            if (Player == null || gm == null || zone == null) return false;
            if (Player.IsDead || Player.IsDowned) { GameEventBus.Notify("Im Moment nicht möglich", new Color(1f, 0.6f, 0.4f)); return false; }
            if (!State.Wallet.TrySpendCopper(cost)) { GameEventBus.Notify("Nicht genug Münzen (" + WalletModel.Format(cost) + ")", new Color(1f, 0.6f, 0.4f)); return false; }
            gm.ChangeZone(zone.Id, null, waypoint + new Vector3(0f, 0.3f, -2.5f));
            GameEventBus.Notify("Gereist: " + zone.DisplayName, new Color(0.6f, 0.9f, 1f));
            return true;
        }

        /// <summary>Schnellreise: Kosten abbuchen, Ladeblende, Spawn neben der Wegmarke.</summary>
        public bool TravelTo(Vector3 waypoint, int cost, string label)
        {
            if (Player == null) return false;
            if (Player.IsDead || Player.IsDowned) { GameEventBus.Notify("Im Moment nicht möglich", new Color(1f, 0.6f, 0.4f)); return false; }
            if (!State.Wallet.TrySpendCopper(cost)) { GameEventBus.Notify("Nicht genug Münzen (" + WalletModel.Format(cost) + ")", new Color(1f, 0.6f, 0.4f)); return false; }
            var ui = UIController.Instance;
            System.Action go = () =>
            {
                if (ui != null) ui.Windows.CloseAll();
                var pc = Player.GetComponent<PlayerController>();
                var target = waypoint + new Vector3(0f, 0.3f, -2.5f);
                if (pc != null) pc.Teleport(target, Quaternion.identity); else Player.transform.position = target;
                State.Fog.Reveal(target, FogRevealRadius);
                GameEventBus.Notify("Gereist: " + label, new Color(0.6f, 0.9f, 1f));
                RequestSave("Reise");
            };
            if (ui != null) ui.FadeAction(go); else go();
            return true;
        }
    }
}
