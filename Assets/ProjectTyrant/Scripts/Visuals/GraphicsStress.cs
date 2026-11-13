using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G1/G7: Worst-Case-Test /gfx stress n – n Explosionsfelder (Telegrafen ohne Schaden) und 1,5 n Orbs um den Spieler, 20 s.</summary>
    public static class GraphicsStress
    {
        public static string Spawn(int n)
        {
            var gm = GameManager.Instance; if (gm == null || gm.Player == null) return "Kein Spieler";
            n = Mathf.Clamp(n, 1, 60);
            var c = gm.Player.transform.position; var rng = new System.Random(n);
            for (int i = 0; i < n; i++)
            {
                var p = c + new Vector3((float)rng.NextDouble() * 30f - 15f, 0, (float)rng.NextDouble() * 30f - 15f);
                var t = GroundTelegraph.Spawn(null, p, 3f + (float)rng.NextDouble() * 2f, 20f, 0f);
                Object.Destroy(t.gameObject, 20f);
            }
            int orbs = Mathf.RoundToInt(n * 1.5f);
            for (int i = 0; i < orbs; i++)
            {
                var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(o.GetComponent<Collider>());
                o.name = "PT_StressOrb"; o.transform.position = c + new Vector3((float)rng.NextDouble() * 30f - 15f, 1.2f, (float)rng.NextDouble() * 30f - 15f); o.transform.localScale = Vector3.one * 0.8f;
                var lib = VfxLibrary.Current; if (lib != null && lib.ProjectileGlow != null) o.GetComponent<Renderer>().sharedMaterial = lib.ProjectileGlow;
                var l = o.AddComponent<Light>(); l.type = LightType.Point; l.range = 4f; l.intensity = 1.5f; l.color = new Color(1f, 0.4f, 0.2f); l.shadows = LightShadows.None;
                Object.Destroy(o, 20f);
            }
            return n + " Explosionsfelder + " + orbs + " Orbs (20 s)";
        }
    }
}
