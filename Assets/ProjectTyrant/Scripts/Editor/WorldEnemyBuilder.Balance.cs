using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Balance B1: neutrale Tiere der Startzone (gelber Name, greifen nur an, wenn angegriffen) – prozedural, Platzhalter.</summary>
    public static partial class WorldEnemyBuilder
    {
        static void BuildBalanceCreatures(LootTable trashLoot)
        {
            var boar = Arch("Wild_Eber");
            Stats(boar, "Wildeber", EnemyTier.Trash, 0.9f, 3f, 2.2f, 2f, 0.6f, 5f, 11f, 1f, 45, 8f);
            boar.NeutralCreature = true; boar.Loot = trashLoot;
            boar.Prefab = Prefab("PT_Enemy_Wild_Eber", boar, p => CreatureBuilder.Quadruped(p, "Boar", new CreatureBuilder.Look { Body = C(0.36f, 0.27f, 0.2f), Belly = C(0.5f, 0.4f, 0.3f), Accent = C(0.9f, 0.85f, 0.75f), Eye = new Color(0.15f, 0.1f, 0.08f), Scale = 0.8f }, 1.1f, 0.45f, 0.6f, 0.1f, 0.15f, true, false, true),
                "stylized painterly fantasy wild boar with small tusks and bristly brown fur, game-ready low poly, hand-painted texture");
            var stag = Arch("Wild_Hirsch");
            Stats(stag, "Rothirsch", EnemyTier.Trash, 0.85f, 2.8f, 2.4f, 2.2f, 0.6f, 5.5f, 11f, 1f, 45, 8f);
            stag.NeutralCreature = true; stag.Loot = trashLoot;
            stag.Prefab = Prefab("PT_Enemy_Wild_Hirsch", stag, p => CreatureBuilder.Quadruped(p, "Stag", new CreatureBuilder.Look { Body = C(0.58f, 0.38f, 0.22f), Belly = C(0.85f, 0.75f, 0.6f), Accent = C(0.75f, 0.65f, 0.5f), Eye = new Color(0.1f, 0.08f, 0.06f), Scale = 1f }, 1.25f, 0.8f, 0.55f, 0.5f, 0.1f, true, true, false),
                "stylized painterly fantasy red deer stag with antlers, game-ready low poly, hand-painted texture");
            UnityEditor.EditorUtility.SetDirty(boar); UnityEditor.EditorUtility.SetDirty(stag);
        }
    }
}
