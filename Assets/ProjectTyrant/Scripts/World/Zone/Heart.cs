using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Herz (GW2-Aufgabe): Herz-NPC, Fortschritt 0–100 % über Kills im Radius und Aufgaben-Objekte; zählt auch während Events.</summary>
    public class Heart : ZoneObject
    {
        public string NpcName;
        [Tooltip("Lauf 6: erlaubte Beitragswege (leer = alle)")] public HeartWay[] Ways = new HeartWay[0];
        [Tooltip("Lauf 6: Emote-Aufgabe (z. B. /jubeln vor dem NPC)")] public Emote EmoteWanted;
        [Tooltip("Lauf 6: Aufgaben-Icons im Pop-up")] public string TaskSummary;
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
            prof.AddHeartWay(Definition, HeartWay.Kill, Definition.KillValue, "Gegner besiegt");
        }

        public override string Prompt => "Sprechen – " + NpcName;
        public override bool CanInteract(Combatant player) => isActiveAndEnabled;
        public override void Interact(Combatant player)
        {
            EventDirector.Instance?.NotifyHeart(Id);   // Herz-Interaktion als Ereignis-Auslöser
            var story = GameManager.Instance != null ? GameManager.Instance.Story : null;   // Lauf 6: Herz-NPC spricht seine Zeile (Sprechblase mit Stimme)
            if (story != null && !Done && story.Database != null && story.Database.Scene("HZ_" + Id) != null && !story.Busy) { story.PlayScene("HZ_" + Id, null); return; }
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
