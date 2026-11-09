using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 4 / M18: Beherrschungen – EP ab Stufe 30, Punkte aus Einblicken (1), Zonenabschluss (2), Weltboss/Meta (1) und Erfolgen (1),
    /// Wirkungen an Reittier, Gleiter, Umwelt-Objekte und Kommandant (Auto-Beute, Reisekosten).
    /// </summary>
    public partial class PlayerProfile
    {
        [Header("Beherrschungen (Lauf 4)")]
        public MasteryLineDefinition[] MasteryLines = new MasteryLineDefinition[0];
        bool _masteryReady;

        public MasteryModel Mastery { get { EnsureMastery(); return State.Mastery; } }

        public MasteryLineDefinition MasteryLineDef(MasteryLine l) { foreach (var d in MasteryLines) if (d != null && d.Line == l) return d; return null; }

        ProfileState _masteryFor;

        void EnsureMastery()
        {
            if (_masteryFor != State)
            {
                // Laden ersetzt den ProfileState → Kosten neu setzen und Ereignis umhängen
                if (_masteryFor != null) _masteryFor.Mastery.RankLearned -= OnRankLearned;
                _masteryFor = State;
                foreach (var d in MasteryLines) if (d != null) State.Mastery.SetCosts(d.Line, d.Ranks);
                State.Mastery.RankLearned += OnRankLearned;
                TravelDiscount = MasteryEffects.TravelDiscount(State.Mastery);
            }
            if (_masteryReady) return;
            _masteryReady = true;
            MountController.MountHealthFactor = () => MasteryEffects.MountHealth(State.Mastery);
            MountController.LeapFactor = () => MasteryEffects.Leap(State.Mastery);
            MountController.ChargeFactor = () => MasteryEffects.Charge(State.Mastery);
            MountController.SpeedFactor = () => MasteryEffects.MountSpeed(State.Mastery);
            MountController.LavaDamageFactor = () => MasteryEffects.LavaFactor(State.Mastery);
            LootBag.AutoLoot = () => MasteryEffects.AutoLoot(State.Mastery);
            TravelDiscount = MasteryEffects.TravelDiscount(State.Mastery);
            MasteryInsight.Found += OnInsightFound;
            ZoneCompleted += OnZoneCompletedMastery;
            MountUnlocked += OnMountUnlockedMastery;
            GameEventBus.AreaEntered += OnAreaEnteredMastery;
        }

        partial void TeardownWorld()
        {
            if (!_masteryReady) return;
            if (_masteryFor != null) _masteryFor.Mastery.RankLearned -= OnRankLearned;
            MasteryInsight.Found -= OnInsightFound;
            ZoneCompleted -= OnZoneCompletedMastery;
            MountUnlocked -= OnMountUnlockedMastery;
            GameEventBus.AreaEntered -= OnAreaEnteredMastery;
        }

        void OnRankLearned(MasteryLine l, int rank)
        {
            TravelDiscount = MasteryEffects.TravelDiscount(State.Mastery);
            var def = MasteryLineDef(l);
            string name = def != null && rank - 1 < def.Ranks.Length ? def.Ranks[rank - 1].Name : MasteryModel.LineName(l) + " " + rank;
            string eff = def != null && rank - 1 < def.Ranks.Length ? def.Ranks[rank - 1].Effect : "";
            GameEventBus.RaiseBanner("Beherrschung erlernt", name + " · " + eff, new Color(0.45f, 1f, 0.75f));
            MasteryLearned?.Invoke(l, rank);
            GameEventBus.RaiseProfileChanged();
            RequestSave("Beherrschung");
        }

        public static event System.Action<MasteryLine, int> MasteryLearned;

        /// <summary>Beherrschungspunkt(e) vergeben (Einblick, Zonenabschluss, Weltboss/Meta, Erfolg).</summary>
        public void AwardMasteryPoints(int n, string reason)
        {
            if (n <= 0) return;
            Mastery.AddPoints(n);
            GameEventBus.Notify("+" + n + " Beherrschungspunkt" + (n > 1 ? "e" : "") + " · " + reason, new Color(0.45f, 1f, 0.75f));
            GameEventBus.RaiseProfileChanged();
            RequestSave("Beherrschungspunkt");
        }

        /// <summary>Erfolg einmalig (Id in Completed als „ach:…“).</summary>
        public void Achievement(string id, string label)
        {
            string key = "ach:" + id;
            if (State.Completed.Contains(key)) return;
            State.Completed.Add(key);
            GameEventBus.RaiseBanner("Erfolg", label, new Color(1f, 0.82f, 0.4f));
            AwardMasteryPoints(1, "Erfolg: " + label);
        }

        void OnInsightFound(ZoneObjectDefinition d) => AwardMasteryPoints(1, d != null ? d.DisplayName : "Einblick");
        void OnZoneCompletedMastery(string zone) => AwardMasteryPoints(2, "Zonenabschluss");

        void OnMountUnlockedMastery(MountKind k)
        {
            Achievement("first_mount", "Erstes Reittier");
            if (State.Mounts.Count >= 3) Achievement("all_mounts", "Herr der Reittiere");
        }

        void OnAreaEnteredMastery(string name, string sub)
        {
            int zones = 0; foreach (var _ in State.FogZones) zones++;
            if (zones >= 4) Achievement("explorer", "Kontinent-Entdecker (4 Zonen)");
        }

        public void SelectMasteryLine(MasteryLine l)
        {
            if (Mastery.IsMaxed(l)) return;
            Mastery.Selected = l;
            GameEventBus.RaiseProfileChanged();
            RequestSave("Beherrschung gewählt");
        }
    }
}
