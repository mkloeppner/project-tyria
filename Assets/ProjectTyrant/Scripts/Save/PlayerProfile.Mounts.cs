using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 4 / M17: Reittiere kaufen, wählen; „Benötigt: …“-Prüfung für Karten-Marker.</summary>
    public partial class PlayerProfile
    {
        [Header("Reittiere (Lauf 4)")]
        public MountDefinition[] MountDefinitions = new MountDefinition[0];

        public MountDefinition Mount(MountKind k) { foreach (var m in MountDefinitions) if (m != null && m.Kind == k) return m; return null; }
        public bool OwnsMount(MountKind k) => k != MountKind.None && State.Mounts.Contains(k.ToString());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RegisterRequirementHook()
        {
            ZoneObject.RequirementCheck = t => { var p = Instance; return p != null && (MountShop.Unlocked(p.State.Mounts) & t) == t; };
        }

        public MountShop.Verdict CanBuyMount(MountDefinition m) =>
            m == null ? MountShop.Verdict.HeartMissing : MountShop.Check(m.Kind, State.Mounts, State.Completed, m.RequiredHeart, State.Wallet.Copper, m.PriceCopper);

        public bool BuyMount(MountDefinition m)
        {
            if (CanBuyMount(m) != MountShop.Verdict.Ok || !State.Wallet.TrySpendCopper(m.PriceCopper)) return false;
            State.Mounts.Add(m.Kind.ToString());
            if (State.SelectedMount == MountKind.None) State.SelectedMount = m.Kind;
            GameEventBus.RaiseBanner("Reittier freigeschaltet", m.DisplayName + " · X antippen zum Aufsitzen, X halten zum Wählen", new Color(1f, 0.82f, 0.4f));
            MountUnlocked?.Invoke(m.Kind);
            GameEventBus.RaiseProfileChanged();
            RequestSave("Reittier");
            return true;
        }

        public void SelectMount(MountKind k)
        {
            if (!OwnsMount(k)) return;
            State.SelectedMount = k;
            GameEventBus.RaiseProfileChanged();
            RequestSave("Reittier gewählt");
        }

        /// <summary>Debug/Test: Reittier ohne Kauf freischalten.</summary>
        public void UnlockMount(MountKind k)
        {
            if (k == MountKind.None || OwnsMount(k)) return;
            State.Mounts.Add(k.ToString());
            if (State.SelectedMount == MountKind.None) State.SelectedMount = k;
            MountUnlocked?.Invoke(k);
            GameEventBus.RaiseProfileChanged();
        }

        public static event System.Action<MountKind> MountUnlocked;
    }
}
