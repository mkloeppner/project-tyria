using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Verborgene Truhe: nur mit „Umweltkunde 4“ sichtbar und zu öffnen; einmalig (Id in Completed).</summary>
    public class HiddenChest : Interactable
    {
        public string ChestId;
        public LootTable Loot;
        Renderer[] _renderers;
        bool _visible = true;

        void Awake() { _renderers = GetComponentsInChildren<Renderer>(true); }
        void Reset() { PromptText = "Öffnen"; Range = 2.8f; }

        bool Opened => PlayerProfile.Instance != null && PlayerProfile.Instance.State.Completed.Contains("chest:" + ChestId);
        bool Sees => PlayerProfile.Instance != null && MasteryEffects.SeesHiddenChests(PlayerProfile.Instance.Mastery);

        void Update()
        {
            bool v = Sees && !Opened;
            if (v == _visible) return;
            _visible = v;
            foreach (var r in _renderers) if (r != null) r.enabled = v;
        }

        public override string Prompt => "Verborgene Truhe öffnen";
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && Sees && !Opened;

        public override void Interact(Combatant player)
        {
            var prof = PlayerProfile.Instance;
            if (prof == null || !CanInteract(player)) return;
            prof.State.Completed.Add("chest:" + ChestId);
            if (Loot != null) prof.GrantLoot(LootRoller.Roll(Loot, prof.Items, new System.Random(ChestId.GetHashCode()), prof.State.Progression.Level), "Verborgene Truhe");
            prof.GrantReward(500, 2500, 0, "Verborgene Truhe");
            GameEventBus.RaiseBanner("Verborgene Truhe", "Umweltkunde zahlt sich aus", new Color(0.45f, 1f, 0.75f));
            prof.RequestSave("Truhe");
        }
    }
}
