using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Herz-Aufgabe: Aufheben (sofort), Reparieren oder Bandagieren (Kanal). Verschwindet und kehrt nach einer Weile zurück.</summary>
    public class HeartTask : Interactable
    {
        public Heart Heart;
        public Procedural.ZoneContent.TaskKind Kind;
        public float RespawnTime = 25f;
        [Tooltip("Lauf 6: Beitragsweg (Sammeln, Benutzen, Wiederbeleben, Tiere, Entschärfen …)")] public HeartWay Way = HeartWay.Collect;
        public string Label;
        GameObject _visual;
        float _respawn;
        bool _used;

        void Awake() { _visual = transform.childCount > 0 ? transform.GetChild(0).gameObject : null; }

        public override string Prompt => !string.IsNullOrEmpty(Label) ? Label : Kind == Procedural.ZoneContent.TaskKind.Pickup ? "Aufheben" : Kind == Procedural.ZoneContent.TaskKind.Repair ? "Reparieren" : "Bandagieren";
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !_used && Heart != null && !Heart.Done;

        public override void Interact(Combatant player)
        {
            if (_used || Heart == null || PlayerProfile.Instance == null) return;
            _used = true; _respawn = RespawnTime;
            if (_visual != null) _visual.SetActive(false);
            var way = Way == HeartWay.Collect && Kind == Procedural.ZoneContent.TaskKind.Repair ? HeartWay.Use : Way == HeartWay.Collect && Kind == Procedural.ZoneContent.TaskKind.Bandage ? HeartWay.Revive : Way;
            PlayerProfile.Instance.AddHeartWay(Heart.Definition, way, Heart.Definition.TaskValue, Prompt);
        }

        void Update()
        {
            if (!_used) return;
            if ((_respawn -= Time.deltaTime) > 0f) return;
            _used = false;
            if (_visual != null) _visual.SetActive(true);
        }
    }
}
