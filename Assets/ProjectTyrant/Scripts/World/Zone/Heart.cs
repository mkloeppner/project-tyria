using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Herz (GW2-Aufgabe): Herz-NPC, Fortschritt 0–100 % über Kills im Radius und Aufgaben-Objekte; zählt auch während Events.</summary>
    public class Heart : ZoneObject
    {
        public string NpcName;
        void Reset() { PromptText = "Sprechen"; Range = 3f; }

        protected override void OnEnable() { base.OnEnable(); GameEventBus.EntityKilled += OnKilled; }
        protected override void OnDisable() { base.OnDisable(); GameEventBus.EntityKilled -= OnKilled; }

        public float Progress => PlayerProfile.Instance == null ? 0f : ZoneRules.HeartProgress(PlayerProfile.Instance.State.Hearts, PlayerProfile.Instance.State.Completed, Id);
        public Vector3 Center => Definition != null ? Definition.Position : transform.position;
        public float Radius => Definition != null ? Definition.Radius : 20f;

        public bool Contains(Vector3 pos) { var d = pos - Center; d.y = 0f; return d.sqrMagnitude <= Radius * Radius; }

        void OnKilled(Combatant victim, Combatant killer)
        {
            var prof = PlayerProfile.Instance;
            if (prof == null || Done || victim == null || victim.Team != Team.Enemy || !Contains(victim.transform.position)) return;
            if (killer != prof.Player && !victim.Attackers.Contains(prof.Player)) return;
            prof.AddHeartProgress(Definition, Definition.KillValue, "Gegner besiegt");
        }

        public override string Prompt => "Sprechen – " + NpcName;
        public override bool CanInteract(Combatant player) => isActiveAndEnabled;
        public override void Interact(Combatant player)
        {
            EventDirector.Instance?.NotifyHeart(Id);   // Herz-Interaktion als Ereignis-Auslöser
            if (Done) GameEventBus.Notify(NpcName + ": „Danke für deine Hilfe! Sieh dir meine Waren an.“", new Color(1f, 0.85f, 0.5f));
            else GameEventBus.Notify(NpcName + ": „" + (Definition != null ? Definition.HeartTask : "") + "“", new Color(1f, 0.85f, 0.5f));
        }

        public override TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            var c = base.MarkerTooltip(info);
            if (c == null) return null;
            c.Add("Fortschritt: " + Mathf.RoundToInt(Progress * 100f) + " %", new Color(1f, 0.85f, 0.4f));
            return c;
        }
    }
}
