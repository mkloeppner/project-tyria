using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Welt-Lauf W1: Schlüssel des Spielers (Besitz) und aktuell genutzte Fortbewegung (aufgesessen/fliegend).</summary>
    public static class PlayerKeys
    {
        public static Traversal Owned()
        {
            var p = PlayerProfile.Instance; if (p == null) return Traversal.None;
            var t = MountShop.Unlocked(p.State.Mounts);
            var m = p.Mastery;
            if ((t & Traversal.Skimmer) != 0 && m != null && m.Has(MasteryLine.Umwelt, 3)) t |= Traversal.LavaSkimmer;
            if (m != null && MasteryEffects.JadeBotRank(m) > 0) t |= Traversal.JadeBot;
            var gm = GameManager.Instance;
            if (gm != null && gm.Story != null && gm.Story.State != null && (gm.Story.State.Chapter > 10 || gm.Story.State.Finished)) t |= Traversal.CorruptionWard;
            return t;
        }

        /// <summary>Was schützt den Spieler gerade (aufgesessenes Reittier, Flug, dauerhafte Schutzwirkungen)?</summary>
        public static Traversal Active()
        {
            var t = Traversal.None;
            var mc = MountController.Local;
            if (mc != null && mc.Motor.IsMounted)
            {
                switch (mc.Motor.Mounted)
                {
                    case MountKind.Raptor: t |= Traversal.Raptor; break;
                    case MountKind.Springer: t |= Traversal.Springer; break;
                    case MountKind.Skimmer: t |= Traversal.Skimmer; break;
                    case MountKind.DuneRunner: t |= Traversal.DuneRunner; break;
                    case MountKind.Skiff: t |= Traversal.Boat; break;
                    case MountKind.Griffon: if (mc.Flight != null && mc.Flight.Airborne) t |= Traversal.Griffon; break;
                    case MountKind.Skyscale: if (mc.Flight != null && mc.Flight.Airborne) t |= Traversal.Skyscale; break;
                }
                var p = PlayerProfile.Instance;
                if ((t & Traversal.Skimmer) != 0 && p != null && p.Mastery != null && p.Mastery.Has(MasteryLine.Umwelt, 3)) t |= Traversal.LavaSkimmer;
            }
            if ((Owned() & Traversal.CorruptionWard) != 0) t |= Traversal.CorruptionWard;
            return t;
        }
    }

    /// <summary>Entdeckungsschleife: zeigt nach der Belohnung kurz das nächste Ziel (Lichtsäule in der Ferne).</summary>
    public static class NextGoalBeam
    {
        public static void Show(Vector3 at, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "PT_NextGoalBeam";
            go.transform.position = at + Vector3.up * 60f; go.transform.localScale = new Vector3(2.5f, 60f, 2.5f);
            var r = go.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) { var m = new Material(shader); m.color = new Color(1f, 0.9f, 0.5f, 1f); r.sharedMaterial = m; }
            Object.Destroy(go, 15f);
            if (!string.IsNullOrEmpty(name)) GameEventBus.Notify("In der Ferne leuchtet: " + name, new Color(1f, 0.9f, 0.5f));
        }
    }
}
