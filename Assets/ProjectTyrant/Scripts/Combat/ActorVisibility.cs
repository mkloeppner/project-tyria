using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Figuren aus- und einblenden, OHNE GameObject.SetActive. Geriggte Figuren (Animation Rigging im eigenen PlayableGraph) verlieren beim
    /// Deaktivieren/Reaktivieren ihre TransformStreamHandles → Burst-Ausnahmen in RigSyncSceneToStreamJob und Editor-Absturz
    /// („Overflow in memory allocator“, 10.11.2026, ausgelöst von den Moorschleichern in Nebelmoor). Stattdessen: Renderer, Collider,
    /// Anvisierbarkeit und Gehirn abschalten.
    /// </summary>
    public static class ActorVisibility
    {
        public static void SetHidden(GameObject go, bool hidden)
        {
            if (go == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = !hidden;
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) col.enabled = !hidden;
            var c = go.GetComponent<Combatant>(); if (c != null) c.Targetable = !hidden;
            var b = go.GetComponent<NpcBrain>(); if (b != null) b.Suspended = hidden;
        }

        /// <summary>Nur die Darstellung (Renderer) einer Teil-Hierarchie, z. B. das Visual eines Gegners.</summary>
        public static void SetRenderersVisible(Transform root, bool visible)
        {
            if (root == null) return;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        }
    }
}
