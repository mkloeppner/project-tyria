using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    public enum ModuleShape { Box, Cylinder, Sphere }
    public enum ModuleRole { Structure, Accent, Glow, Floor, Ramp, Ledge, Detail, Glass, Rock, Foliage }

    /// <summary>Ein Baustein eines Wahrzeichens (lokal: y = 0 Boden, −z = Vorderseite/Eingang).</summary>
    public struct LandmarkModule
    {
        public string Name;
        public ModuleShape Shape;
        public Vector3 Pos, Scale, Euler;
        public ModuleRole Role;
        public bool Solid, Interior, Silhouette;
        public float Top => Pos.y + (Shape == ModuleShape.Cylinder ? Scale.y : Scale.y * 0.5f) ;
    }

    public class LandmarkLayout
    {
        public LandmarkKind Kind;
        public float Height;
        public readonly List<LandmarkModule> Modules = new List<LandmarkModule>();
        public readonly Dictionary<string, Vector3> Anchors = new Dictionary<string, Vector3>();
        public Bounds Interior;          // Innenraum (für Portal-Culling); size 0 = keiner
        public Vector3 Entrance;
        public float Footprint;
        public bool HasInterior => Interior.size.sqrMagnitude > 1f;
        public float MaxTop { get { float m = 0f; foreach (var x in Modules) m = Mathf.Max(m, x.Top); return m; } }
    }

    /// <summary>
    /// Welt-Lauf W1: Wahrzeichen-Baukasten (rein, deterministisch). Erzeugt aus Typ und Höhe die Bausteine (Primitive), Innenräume (hohle Hüllen mit Tür),
    /// Rampen (≤ 32°, zu Fuß begehbar), Springer-Vorsprünge (≤ 13 m Abstand) und benannte Anker für Aufstiegsstufen und Belohnungen.
    /// </summary>
    public static class LandmarkKit
    {
        public const float MaxRampDeg = 32f, SpringerStep = 13f, DoorWidth = 4f, DoorHeight = 5f;

        public static LandmarkLayout Build(LandmarkKind kind, float height, int seed)
        {
            var L = new LandmarkLayout { Kind = kind, Height = height };
            var rnd = new System.Random(seed);
            switch (kind)
            {
                case LandmarkKind.Cathedral: Cathedral(L, height); break;
                case LandmarkKind.Pyramid: Pyramid(L, height); break;
                case LandmarkKind.Citadel: Citadel(L, height); break;
                case LandmarkKind.Pagoda: Pagoda(L, height); break;
                case LandmarkKind.GiantTree: GiantTree(L, height); break;
                case LandmarkKind.Colossus: Colossus(L, height); break;
                case LandmarkKind.Lighthouse: Lighthouse(L, height); break;
                case LandmarkKind.GlacierPalace: GlacierPalace(L, height); break;
                case LandmarkKind.SunkenCity: SunkenCity(L, height, rnd); break;
                case LandmarkKind.SkyIsland: SkyIsland(L, height); break;
                default: Massif(L, height); break;
            }
            if (!L.Anchors.ContainsKey("Eingang")) L.Anchors["Eingang"] = L.Entrance;
            return L;
        }

        // ---------------- Bausteine ----------------
        static void Add(LandmarkLayout L, string n, ModuleShape s, Vector3 p, Vector3 sc, ModuleRole r, bool solid = true, bool interior = false, bool sil = false, Vector3 e = default)
            => L.Modules.Add(new LandmarkModule { Name = n, Shape = s, Pos = p, Scale = sc, Role = r, Solid = solid, Interior = interior, Silhouette = sil, Euler = e });
        /// <summary>Quader mit Mittelpunkt c.</summary>
        static void Box(LandmarkLayout L, string n, Vector3 c, Vector3 size, ModuleRole r = ModuleRole.Structure, bool sil = false, bool interior = false, Vector3 e = default)
            => Add(L, n, ModuleShape.Box, c, size, r, true, interior, sil, e);
        /// <summary>Zylinder mit Boden bei b (Unity-Zylinder: Höhe = 2 · scale.y).</summary>
        static void Cyl(LandmarkLayout L, string n, Vector3 b, float radius, float h, ModuleRole r = ModuleRole.Structure, bool sil = false, bool solid = true)
            => Add(L, n, ModuleShape.Cylinder, b + Vector3.up * h * 0.5f, new Vector3(radius * 2f, h * 0.5f, radius * 2f), r, solid, false, sil);

        /// <summary>Hohle Halle (Boden bei b.y): vier Wände, Tür vorne (−z), Dach. Innenraum zählt zum Culling.</summary>
        static void Hall(LandmarkLayout L, string n, Vector3 b, Vector3 size, float wall = 1.5f, bool roof = true, ModuleRole role = ModuleRole.Structure, bool sil = true)
        {
            float hx = size.x * 0.5f, hz = size.z * 0.5f, h = size.y;
            Box(L, n + "_Back", b + new Vector3(0, h * 0.5f, hz - wall * 0.5f), new Vector3(size.x, h, wall), role, sil);
            Box(L, n + "_Left", b + new Vector3(-hx + wall * 0.5f, h * 0.5f, 0), new Vector3(wall, h, size.z), role, sil);
            Box(L, n + "_Right", b + new Vector3(hx - wall * 0.5f, h * 0.5f, 0), new Vector3(wall, h, size.z), role, sil);
            float side = (size.x - DoorWidth) * 0.5f;
            Box(L, n + "_FrontL", b + new Vector3(-(DoorWidth * 0.5f + side * 0.5f), h * 0.5f, -hz + wall * 0.5f), new Vector3(side, h, wall), role, sil);
            Box(L, n + "_FrontR", b + new Vector3(DoorWidth * 0.5f + side * 0.5f, h * 0.5f, -hz + wall * 0.5f), new Vector3(side, h, wall), role, sil);
            if (h > DoorHeight) Box(L, n + "_Lintel", b + new Vector3(0, (h + DoorHeight) * 0.5f, -hz + wall * 0.5f), new Vector3(DoorWidth, h - DoorHeight, wall), role, sil);
            if (roof) Box(L, n + "_Roof", b + new Vector3(0, h + wall * 0.5f, 0), new Vector3(size.x, wall, size.z), role, sil);
            Box(L, n + "_Floor", b + new Vector3(0, 0.15f, 0), new Vector3(size.x - wall * 2f, 0.3f, size.z - wall * 2f), ModuleRole.Floor, false, true);
            var ib = new Bounds(b + new Vector3(0, h * 0.5f, 0), new Vector3(size.x - wall * 2f, h, size.z - wall * 2f));
            if (L.HasInterior) L.Interior.Encapsulate(ib); else L.Interior = ib;
        }

        /// <summary>Rampe von a nach b (Mittellinie, Oberkante), zu Fuß begehbar, wenn die Steigung ≤ <see cref="MaxRampDeg"/>.</summary>
        static void Ramp(LandmarkLayout L, string n, Vector3 a, Vector3 b, float width, bool interior = false)
        {
            var d = b - a; float hor = new Vector2(d.x, d.z).magnitude; float len = d.magnitude;
            float pitch = -Mathf.Atan2(d.y, hor) * Mathf.Rad2Deg, yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Add(L, n, ModuleShape.Box, (a + b) * 0.5f + Vector3.down * 0.25f, new Vector3(width, 0.5f, len), ModuleRole.Ramp, true, interior, false, new Vector3(pitch, yaw, 0f));
        }

        public static float RampDegrees(LandmarkModule m) => Mathf.Abs(m.Euler.x > 180f ? m.Euler.x - 360f : m.Euler.x);

        /// <summary>Kletter-Vorsprünge (Springer): Plattformen von y0 bis y1 im Abstand ≤ <see cref="SpringerStep"/>, spiralförmig um c.</summary>
        static Vector3 Ledges(LandmarkLayout L, string n, Vector3 c, float radius, float y0, float y1, float startAngle = 0f)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt((y1 - y0) / SpringerStep));
            Vector3 last = c;
            for (int i = 1; i <= count; i++)
            {
                float y = Mathf.Lerp(y0, y1, i / (float)count);
                float a = startAngle + i * 0.5f;
                last = c + new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                Box(L, n + "_" + i, last + Vector3.down * 0.4f, new Vector3(5f, 0.8f, 5f), ModuleRole.Ledge);
            }
            return last;
        }

        static void Spire(LandmarkLayout L, string n, Vector3 b, float radius, float h, ModuleRole r = ModuleRole.Accent)
        {
            int seg = 5; float y = b.y;
            for (int i = 0; i < seg; i++)
            {
                float hh = h / seg, rr = radius * (1f - i / (float)seg) + 0.3f;
                Cyl(L, n + "_" + i, new Vector3(b.x, y, b.z), rr, hh, r, true);
                y += hh;
            }
        }

        // ---------------- Typen ----------------
        static void Cathedral(LandmarkLayout L, float H)
        {
            float len = Mathf.Max(44f, H * 0.5f), wid = Mathf.Max(20f, H * 0.2f), nh = Mathf.Max(18f, H * 0.26f);
            Hall(L, "Nave", new Vector3(0, 0, 0), new Vector3(wid, nh, len));
            Hall(L, "Transept", new Vector3(0, 0, len * 0.22f), new Vector3(wid * 2f, nh * 0.85f, wid * 0.8f), 1.5f, true, ModuleRole.Structure);
            // Galerie innen (über Rampe zu Fuß)
            float gal = Mathf.Min(nh - 4f, 9f);
            Box(L, "Gallery", new Vector3(-wid * 0.5f + 3.5f, gal - 0.25f, len * 0.05f), new Vector3(4f, 0.5f, len * 0.7f), ModuleRole.Floor, false, true);
            // Rampe entlang der rechten Wand, dann Brücke zur Galerie
            float run = gal / Mathf.Tan(28f * Mathf.Deg2Rad);
            Ramp(L, "GalleryRamp", new Vector3(wid * 0.5f - 3.5f, 0.3f, -len * 0.44f), new Vector3(wid * 0.5f - 3.5f, gal, -len * 0.44f + run), 3f, true);
            Box(L, "GalleryBridge", new Vector3(0, gal - 0.25f, -len * 0.44f + run + 2f), new Vector3(wid - 4f, 0.5f, 4f), ModuleRole.Floor, false, true);
            for (int i = 0; i < 6; i++) Cyl(L, "Pillar_" + i, new Vector3((i % 2 == 0 ? -1 : 1) * wid * 0.25f, 0, -len * 0.3f + i / 2 * len * 0.25f), 0.8f, nh - 1f, ModuleRole.Detail);
            L.Anchors["Galerie"] = new Vector3(-wid * 0.5f + 3.5f, gal, len * 0.05f);
            // Türme vorne mit Spitzen; Strebepfeiler-Vorsprünge (Springer) am linken Turm
            float tw = Mathf.Max(8f, wid * 0.42f), th = H * 0.8f;
            foreach (int s in new[] { -1, 1 })
            {
                Box(L, "Tower" + s, new Vector3(s * (wid * 0.5f + tw * 0.5f - 1f), th * 0.5f, -len * 0.5f + tw * 0.5f), new Vector3(tw, th, tw), ModuleRole.Structure, true);
                Spire(L, "Spire" + s, new Vector3(s * (wid * 0.5f + tw * 0.5f - 1f), th, -len * 0.5f + tw * 0.5f), tw * 0.5f, H - th);
            }
            var tc = new Vector3(-(wid * 0.5f + tw * 0.5f - 1f), 0, -len * 0.5f + tw * 0.5f);
            var top = Ledges(L, "Buttress", tc, tw * 0.5f + 3.5f, 6f, th * 0.92f, 2.4f);
            L.Anchors["Turmgalerie"] = top;
            Box(L, "TowerPlatform", tc + new Vector3(0, th + 0.25f, 0), new Vector3(tw + 2f, 0.5f, tw + 2f), ModuleRole.Ledge);
            L.Anchors["Turmspitze"] = tc + new Vector3(0, th + 0.6f, 0);
            Add(L, "Rose", ModuleShape.Cylinder, new Vector3(0, nh * 0.72f, -len * 0.5f - 0.4f), new Vector3(wid * 0.45f, 0.2f, wid * 0.45f), ModuleRole.Glow, false, false, false, new Vector3(90f, 0, 0));
            // Krypta hinter dem Chor
            Hall(L, "Crypt", new Vector3(0, 0, len * 0.5f + 9f), new Vector3(14f, 6f, 14f), 1.2f, true, ModuleRole.Structure, false);
            L.Anchors["Krypta"] = new Vector3(0, 0.4f, len * 0.5f + 9f);
            L.Entrance = new Vector3(0, 0, -len * 0.5f - 2f);
            L.Anchors["Hauptschiff"] = new Vector3(0, 0.4f, 0);
            L.Footprint = Mathf.Max(len, wid * 2f + tw * 2f) * 0.5f + 16f;
        }

        static void Pyramid(LandmarkLayout L, float H)
        {
            int n = Mathf.Max(5, Mathf.CeilToInt(H / 12f));
            float step = H / n, B = H * 1.5f;
            float hallH = step * 2f;
            // Unterste zwei Stufen hohl: Eingangshalle
            Hall(L, "Hall", Vector3.zero, new Vector3(B * 0.55f, hallH, B * 0.55f), 2f, false, ModuleRole.Structure, false);
            float ring = (B - B * 0.55f) * 0.5f;
            for (int k = 0; k < 2; k++)
            {
                float side = B * (1f - k / (float)n * 0.85f);
                float y = k * step + step * 0.5f;
                // Ringstufe um die Halle (vier Blöcke)
                float inner = B * 0.55f, w = (side - inner) * 0.5f;
                Box(L, "StepF" + k, new Vector3(0, y, -(inner * 0.5f + w * 0.5f)), new Vector3(side, step, w), ModuleRole.Structure, true);
                Box(L, "StepB" + k, new Vector3(0, y, inner * 0.5f + w * 0.5f), new Vector3(side, step, w), ModuleRole.Structure, true);
                Box(L, "StepL" + k, new Vector3(-(inner * 0.5f + w * 0.5f), y, 0), new Vector3(w, step, inner), ModuleRole.Structure, true);
                Box(L, "StepR" + k, new Vector3(inner * 0.5f + w * 0.5f, y, 0), new Vector3(w, step, inner), ModuleRole.Structure, true);
            }
            // Eingangstunnel durch die Ringstufe
            L.Modules.RemoveAll(m => m.Name == "StepF0");
            float side0 = B, inner0 = B * 0.55f, w0 = (side0 - inner0) * 0.5f;
            float sideW = (side0 - DoorWidth) * 0.5f;
            Box(L, "StepF0L", new Vector3(-(DoorWidth * 0.5f + sideW * 0.5f), step * 0.5f, -(inner0 * 0.5f + w0 * 0.5f)), new Vector3(sideW, step, w0), ModuleRole.Structure, true);
            Box(L, "StepF0R", new Vector3(DoorWidth * 0.5f + sideW * 0.5f, step * 0.5f, -(inner0 * 0.5f + w0 * 0.5f)), new Vector3(sideW, step, w0), ModuleRole.Structure, true);
            if (step > DoorHeight) Box(L, "StepF0T", new Vector3(0, (step + DoorHeight) * 0.5f, -(inner0 * 0.5f + w0 * 0.5f)), new Vector3(DoorWidth, step - DoorHeight, w0), ModuleRole.Structure, true);
            Box(L, "HallRoof", new Vector3(0, hallH + 0.5f, 0), new Vector3(B * 0.55f, 1f, B * 0.55f), ModuleRole.Structure, true);
            for (int k = 2; k < n; k++)
            {
                float side = B * (1f - k / (float)n * 0.85f);
                Box(L, "Step" + k, new Vector3(0, k * step + step * 0.5f, 0), new Vector3(side, step, side), ModuleRole.Structure, true);
            }
            // Königskammer: erhöhte Plattform in der Halle mit Rampe
            Box(L, "KingDais", new Vector3(0, 1.5f, B * 0.15f), new Vector3(10f, 3f, 8f), ModuleRole.Floor, false, true);
            Ramp(L, "KingRamp", new Vector3(0, 0.3f, B * 0.15f - 10f), new Vector3(0, 3f, B * 0.15f - 4f), 3f, true);
            L.Anchors["Halle"] = new Vector3(0, 0.4f, -B * 0.1f);
            L.Anchors["Königskammer"] = new Vector3(0, 3.2f, B * 0.15f);
            L.Anchors["Schatzkammer"] = new Vector3(B * 0.18f, 0.4f, B * 0.18f);
            // Rampe zu Fuß bis zur halben Höhe (Vorderseite), Springer-Flanke, Spitze mit Kristall
            int half = n / 2;
            float halfY = half * step, frontZ = -B * (1f - (half - 1) / (float)n * 0.85f) * 0.5f;
            float run = halfY / Mathf.Tan(28f * Mathf.Deg2Rad);
            Ramp(L, "FootRamp", new Vector3(B * 0.2f, 0.2f, frontZ - run), new Vector3(B * 0.2f, halfY, frontZ + 1f), 6f);
            L.Anchors["Halbe Höhe"] = new Vector3(B * 0.2f, halfY + 0.3f, frontZ + 3f);
            L.Anchors["Flanke"] = new Vector3(0, (n - 1) * step + 0.3f, 0);
            Cyl(L, "TopCap", new Vector3(0, H, 0), B * 0.075f * 0.9f, 1.5f, ModuleRole.Accent);
            Add(L, "Crystal", ModuleShape.Sphere, new Vector3(0, H + 4f, 0), new Vector3(5f, 7f, 5f), ModuleRole.Glow, true, false, true);
            L.Anchors["Spitze"] = new Vector3(0, H + 1.6f, 0);
            Hall(L, "TopChamber", new Vector3(0, (n - 1) * step, 0), new Vector3(Mathf.Min(10f, B * 0.12f), Mathf.Min(step - 0.5f, 6f), Mathf.Min(10f, B * 0.12f)), 1f, false, ModuleRole.Structure, false);
            L.Anchors["Spitzenkammer"] = new Vector3(0, (n - 1) * step + 0.4f, 0);
            L.Entrance = new Vector3(0, 0, -B * 0.5f - 2f);
            L.Footprint = B * 0.5f + run * 0.3f + 12f;
        }

        static void Citadel(LandmarkLayout L, float H)
        {
            float S = Mathf.Max(60f, H * 0.9f), wallH = 12f, t = 3f;
            Hall(L, "Walls", Vector3.zero, new Vector3(S, wallH, S), t, false, ModuleRole.Structure, true);
            foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            {
                Cyl(L, "CornerTower", new Vector3(c.x * S * 0.5f, 0, c.y * S * 0.5f), 5f, wallH + 10f, ModuleRole.Structure, true);
                Spire(L, "CornerSpire", new Vector3(c.x * S * 0.5f, wallH + 10f, c.y * S * 0.5f), 5.5f, 8f);
            }
            for (int i = 0; i < 8; i++) Box(L, "Merlon_" + i, new Vector3(-S * 0.4f + i * S * 0.115f, wallH + 1f, -S * 0.5f + t * 0.5f), new Vector3(2f, 2f, t), ModuleRole.Accent);
            L.Anchors["Zinnen"] = new Vector3(S * 0.25f, wallH + 0.3f, S * 0.5f - t * 0.5f);
            L.Anchors["Hof"] = new Vector3(0, 0.4f, -S * 0.25f);
            // Bergfried (hohl unten, massiver Turm darüber)
            float kw = Mathf.Max(18f, S * 0.28f);
            Hall(L, "KeepHall", new Vector3(0, 0, S * 0.12f), new Vector3(kw, 10f, kw), 1.5f, true, ModuleRole.Structure, true);
            Box(L, "Keep", new Vector3(0, 10f + (H - 10f) * 0.5f, S * 0.12f), new Vector3(kw * 0.8f, H - 10f, kw * 0.8f), ModuleRole.Structure, true);
            Box(L, "KeepRoof", new Vector3(0, H + 0.4f, S * 0.12f), new Vector3(kw, 0.8f, kw), ModuleRole.Ledge);
            L.Anchors["Halle"] = new Vector3(0, 0.4f, S * 0.12f);
            L.Anchors["Bergfried-Dach"] = new Vector3(0, H + 1f, S * 0.12f);
            Hall(L, "Dungeon", new Vector3(S * 0.3f, 0, S * 0.3f), new Vector3(10f, 5f, 10f), 1f, true, ModuleRole.Structure, false);
            L.Anchors["Kerker"] = new Vector3(S * 0.3f, 0.4f, S * 0.3f);
            // Wehrgang über Rampe zu Fuß (innen an der Mauer)
            float run = wallH / Mathf.Tan(28f * Mathf.Deg2Rad);
            Ramp(L, "WallRamp", new Vector3(-S * 0.5f + t + 2f, 0.2f, -S * 0.3f), new Vector3(-S * 0.5f + t + 2f, wallH, -S * 0.3f + run), 3f);
            Box(L, "WallWalk", new Vector3(-S * 0.5f + t + 2f, wallH - 0.25f, -S * 0.3f + run + 6f), new Vector3(4f, 0.5f, 12f), ModuleRole.Floor);
            L.Anchors["Wehrgang"] = new Vector3(-S * 0.5f + t + 2f, wallH, -S * 0.3f + run + 6f);
            L.Entrance = new Vector3(0, 0, -S * 0.5f - 2f);
            L.Footprint = S * 0.5f + 10f;
        }

        static void Pagoda(LandmarkLayout L, float H)
        {
            int n = H >= 60f ? 9 : 5;
            float th = H / n, b0 = Mathf.Max(22f, H * 0.32f);
            for (int i = 0; i < n; i++)
            {
                float s = b0 * (1f - i / (float)n * 0.6f), y = i * th;
                if (i < 3) Hall(L, "Tier" + i, new Vector3(0, y, 0), new Vector3(s, th - 1.2f, s), 1f, false, ModuleRole.Structure, true);
                else Box(L, "Tier" + i, new Vector3(0, y + (th - 1.2f) * 0.5f, 0), new Vector3(s, th - 1.2f, s), ModuleRole.Structure, true);
                Box(L, "Roof" + i, new Vector3(0, y + th - 0.6f, 0), new Vector3(s * 1.35f, 1.2f, s * 1.35f), ModuleRole.Accent, true);
            }
            // Innen: Rampen Ebene 1 → 3 (zu Fuß)
            for (int i = 0; i < 2; i++)
            {
                float s = b0 * (1f - (i + 1) / (float)n * 0.6f);
                float run = th / Mathf.Tan(28f * Mathf.Deg2Rad);
                float x = (i % 2 == 0 ? 1 : -1) * (s * 0.5f - 3f);
                Ramp(L, "InnerRamp" + i, new Vector3(x, i * th + 0.3f, -run * 0.5f), new Vector3(x, (i + 1) * th, run * 0.5f), 2.5f, true);
            }
            L.Anchors["Ebene 1"] = new Vector3(0, 0.4f, 0);
            L.Anchors["Ebene 3"] = new Vector3(0, 2 * th + 0.4f, 0);
            Spire(L, "Finial", new Vector3(0, H, 0), 1.5f, Mathf.Max(6f, H * 0.08f), ModuleRole.Glow);
            L.Anchors["Spitze"] = new Vector3(0, H + 0.4f, b0 * 0.15f);
            L.Anchors["Balkon"] = new Vector3(b0 * 0.45f, 4 * th, 0);
            L.Entrance = new Vector3(0, 0, -b0 * 0.5f - 2f);
            L.Footprint = b0 * 0.7f + 10f;
        }

        static void GiantTree(LandmarkLayout L, float H)
        {
            float R = Mathf.Max(8f, H * 0.05f), hallH = 14f;
            // Wurzelhalle: Ring aus 10 Stammsegmenten mit Lücke (Tür) vorne
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f + Mathf.PI * 0.1f;
                if (i == 7) continue;   // Tür nach −z
                var p = new Vector3(Mathf.Cos(a) * R, hallH * 0.5f, Mathf.Sin(a) * R);
                Box(L, "RootWall_" + i, p, new Vector3(R * 0.68f, hallH, 2.4f), ModuleRole.Structure, true, false, new Vector3(0, -a * Mathf.Rad2Deg + 90f, 0));
            }
            Cyl(L, "Trunk", new Vector3(0, hallH, 0), R, H * 0.82f - hallH, ModuleRole.Structure, true);
            var ib = new Bounds(new Vector3(0, hallH * 0.5f, 0), new Vector3(R * 1.6f, hallH, R * 1.6f));
            L.Interior = ib;
            Box(L, "HallFloor", new Vector3(0, 0.15f, 0), new Vector3(R * 1.6f, 0.3f, R * 1.6f), ModuleRole.Floor, false, true);
            L.Anchors["Wurzelhalle"] = new Vector3(0, 0.4f, 0);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                var outer = new Vector3(Mathf.Cos(a) * (R + 16f), 0f, Mathf.Sin(a) * (R + 16f));
                var inner = new Vector3(Mathf.Cos(a) * (R + 1f), 6f, Mathf.Sin(a) * (R + 1f));
                Ramp(L, "Root_" + i, outer, inner, 4f);
            }
            var last = Ledges(L, "Branch", Vector3.zero, R + 5f, hallH + 4f, H * 0.7f, 0.3f);
            L.Anchors["Äste"] = Vector3.Lerp(new Vector3(0, hallH, 0), last, 0.5f);
            Box(L, "CrownVillage", new Vector3(0, H * 0.72f, 0), new Vector3(R * 4f, 1f, R * 4f), ModuleRole.Floor, false);
            L.Anchors["Kronendorf"] = new Vector3(R * 1.4f, H * 0.72f + 0.6f, 0);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Add(L, "Crown_" + i, ModuleShape.Sphere, new Vector3(Mathf.Cos(a) * R * 2.2f, H * 0.86f, Mathf.Sin(a) * R * 2.2f), Vector3.one * R * 3.2f, ModuleRole.Foliage, false, false, true);
            }
            Add(L, "CrownTop", ModuleShape.Sphere, new Vector3(0, H * 0.94f, 0), Vector3.one * R * 3f, ModuleRole.Foliage, true, false, true);
            L.Anchors["Krone"] = new Vector3(0, H * 0.94f + R * 1.5f, 0);
            L.Entrance = new Vector3(0, 0, -R - 3f);
            L.Footprint = R + 22f;
        }

        static void Colossus(LandmarkLayout L, float H)
        {
            // Halb liegender Steinriese, an den Hang gelehnt: Länge H, Kopf auf ≈ 0,6 H
            float len = H, tilt = 50f, wid = H * 0.22f;
            float rad = tilt * Mathf.Deg2Rad;
            var torsoC = new Vector3(0, Mathf.Sin(rad) * len * 0.3f + wid * 0.25f, len * 0.3f * Mathf.Cos(rad) - len * 0.1f);
            Box(L, "Torso", torsoC, new Vector3(wid, wid * 0.55f, len * 0.6f), ModuleRole.Rock, true, false, new Vector3(-tilt, 0, 0));
            var headC = new Vector3(0, Mathf.Sin(rad) * len * 0.62f + wid * 0.35f, Mathf.Cos(rad) * len * 0.62f - len * 0.1f);
            Add(L, "Head", ModuleShape.Sphere, headC, Vector3.one * wid * 0.62f, ModuleRole.Rock, true, false, true);
            Add(L, "EyeL", ModuleShape.Sphere, headC + new Vector3(-wid * 0.12f, wid * 0.05f, -wid * 0.28f), Vector3.one * 2.2f, ModuleRole.Glow, false);
            Add(L, "EyeR", ModuleShape.Sphere, headC + new Vector3(wid * 0.12f, wid * 0.05f, -wid * 0.28f), Vector3.one * 2.2f, ModuleRole.Glow, false);
            L.Anchors["Kopf"] = headC + Vector3.up * wid * 0.33f;
            // Arm als Rampe (zu Fuß) bis zur Brust, Schulter mit Springer
            var shoulder = new Vector3(-wid * 0.62f, Mathf.Sin(rad) * len * 0.45f + wid * 0.2f, Mathf.Cos(rad) * len * 0.45f - len * 0.1f);
            var chestTop = new Vector3(wid * 0.55f, Mathf.Sin(rad) * len * 0.22f + wid * 0.45f, Mathf.Cos(rad) * len * 0.22f - len * 0.1f);
            float run = chestTop.y / Mathf.Tan(26f * Mathf.Deg2Rad);
            Ramp(L, "Arm", new Vector3(wid * 0.55f, 0.2f, chestTop.z - run), chestTop, 5f);
            L.Anchors["Brust"] = chestTop + Vector3.up * 0.4f;
            Box(L, "Shoulder", shoulder, new Vector3(wid * 0.4f, wid * 0.4f, wid * 0.4f), ModuleRole.Rock, true);
            L.Anchors["Schulter"] = shoulder + Vector3.up * wid * 0.21f;
            Ledges(L, "ShoulderSteps", new Vector3(-wid * 0.9f, 0, shoulder.z - wid), 3f, 4f, shoulder.y + wid * 0.2f, 0f);
            // Brustkammer (zu Fuß durch eine Tür am Fuß)
            Hall(L, "ChestChamber", new Vector3(0, 0, -len * 0.12f), new Vector3(16f, 8f, 16f), 2f, true, ModuleRole.Rock, true);
            L.Anchors["Brustkammer"] = new Vector3(0, 0.4f, -len * 0.12f);
            for (int s = -1; s <= 1; s += 2) Box(L, "Leg" + s, new Vector3(s * wid * 0.28f, wid * 0.2f, -len * 0.28f), new Vector3(wid * 0.3f, wid * 0.4f, len * 0.3f), ModuleRole.Rock, true);
            L.Entrance = new Vector3(0, 0, -len * 0.12f - 10f);
            L.Footprint = len * 0.6f;
            L.Height = Mathf.Max(L.Height, headC.y + wid * 0.31f);
        }

        static void Lighthouse(LandmarkLayout L, float H)
        {
            float R = Mathf.Max(7f, H * 0.11f);
            // Hohler Turm aus 12 Segmenten, Tür vorne
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                if (i == 9) { Box(L, "TowerDoorTop", new Vector3(Mathf.Cos(a) * R, (H + DoorHeight) * 0.5f, Mathf.Sin(a) * R), new Vector3(R * 0.55f, H - DoorHeight, 1.6f), ModuleRole.Structure, true, false, new Vector3(0, -a * Mathf.Rad2Deg + 90f, 0)); continue; }
                Box(L, "TowerWall_" + i, new Vector3(Mathf.Cos(a) * R, H * 0.5f, Mathf.Sin(a) * R), new Vector3(R * 0.55f, H, 1.6f), ModuleRole.Structure, true, false, new Vector3(0, -a * Mathf.Rad2Deg + 90f, 0));
            }
            L.Interior = new Bounds(new Vector3(0, H * 0.5f, 0), new Vector3(R * 1.6f, H, R * 1.6f));
            // Wendelrampe innen
            int turns = Mathf.CeilToInt(H / 10f); float yy = 0.2f, rr = R - 2.8f, floorTop = H * 0.92f + 0.3f;
            Vector3 hatch = new Vector3(rr, 0, 0);
            for (int i = 0; i < turns * 4; i++)
            {
                float a0 = i * Mathf.PI * 0.5f, a1 = (i + 1) * Mathf.PI * 0.5f;
                float y1 = Mathf.Min(floorTop, yy + 2.6f);
                Ramp(L, "Spiral_" + i, new Vector3(Mathf.Cos(a0) * rr, yy, Mathf.Sin(a0) * rr), new Vector3(Mathf.Cos(a1) * rr, y1, Mathf.Sin(a1) * rr), 2.4f, true);
                yy = y1; hatch = new Vector3(Mathf.Cos(a1) * rr, 0, Mathf.Sin(a1) * rr);
                if (yy >= floorTop) break;
            }
            // Laternen-Boden mit Luke über dem Ende der Wendelrampe (sonst endet die Treppe unter der Decke)
            HatchFloor(L, "LanternFloor", H * 0.92f, R * 1.1f, hatch, 1.6f);
            Cyl(L, "Lantern", new Vector3(0, H * 0.92f + 0.3f, 0), R * 0.55f, H * 0.08f, ModuleRole.Glow, true, true);
            Spire(L, "Cap", new Vector3(0, H, 0), R * 0.8f, 6f);
            L.Anchors["Laterne"] = new Vector3(R * 0.85f, H * 0.92f + 0.4f, 0);
            Box(L, "OuterGallery", new Vector3(0, 13.5f, 0), new Vector3(R * 2.8f, 0.6f, R * 2.8f), ModuleRole.Ledge);
            L.Anchors["Außengalerie"] = new Vector3(R * 1.3f, 14f, 0);
            L.Anchors["Treppe"] = new Vector3(rr, 0.4f, 0);
            L.Entrance = new Vector3(0, 0, -R - 3f);
            L.Footprint = R * 1.6f + 10f;
        }

        /// <summary>Quadratischer Boden (Halbkante s, Mitte y) mit quadratischer Luke (Halbkante w) um h.</summary>
        static void HatchFloor(LandmarkLayout L, string n, float y, float s, Vector3 h, float w)
        {
            float x0 = Mathf.Clamp(h.x - w, -s, s), x1 = Mathf.Clamp(h.x + w, -s, s), z0 = Mathf.Clamp(h.z - w, -s, s), z1 = Mathf.Clamp(h.z + w, -s, s);
            void B(string k, float ax, float bx, float az, float bz) { if (bx - ax > 0.05f && bz - az > 0.05f) Box(L, n + "_" + k, new Vector3((ax + bx) * 0.5f, y, (az + bz) * 0.5f), new Vector3(bx - ax, 0.6f, bz - az), ModuleRole.Floor); }
            B("W", -s, x0, -s, s); B("E", x1, s, -s, s); B("S", x0, x1, -s, z0); B("N", x0, x1, z1, s);
        }

        static void GlacierPalace(LandmarkLayout L, float H)
        {
            float S = Mathf.Max(30f, H * 0.9f);
            Hall(L, "IceHall", Vector3.zero, new Vector3(S, H * 0.45f, S * 0.8f), 2f, true, ModuleRole.Glass, true);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Cyl(L, "IcePillar_" + i, new Vector3(Mathf.Cos(a) * S * 0.75f, 0, Mathf.Sin(a) * S * 0.65f), 2.5f, H * (0.6f + 0.4f * ((i * 37) % 10) / 10f), ModuleRole.Glass, true);
            }
            Spire(L, "IceSpire", new Vector3(0, H * 0.45f, 0), S * 0.18f, H * 0.55f, ModuleRole.Glass);
            // Eisbrücken über den Eissee (Plattformen ≤ 4 m Abstand, zu Fuß)
            for (int i = 0; i < 6; i++) Box(L, "IceBridge_" + i, new Vector3(S * 0.6f + i * 5f, 0.6f, 0), new Vector3(4f, 0.6f, 4f), ModuleRole.Glass, false);
            L.Anchors["Halle"] = new Vector3(0, 0.4f, 0);
            L.Anchors["Eisbrücke"] = new Vector3(S * 0.6f + 25f, 1f, 0);
            L.Anchors["Decke"] = new Vector3(0, H * 0.45f + 2.2f, 0);
            L.Entrance = new Vector3(0, 0, -S * 0.4f - 2f);
            L.Footprint = S * 0.8f + 30f;
        }

        static void SunkenCity(LandmarkLayout L, float H, System.Random r)
        {
            // halb versunkene Häuser (Dächer als Inseln, Abstände 10–13 m: Raptor), ein Turm (Springer), eine begehbare Halle am Ufer
            for (int i = 0; i < 9; i++)
            {
                float x = (i % 3 - 1) * 18f + (float)(r.NextDouble() * 4 - 2), z = (i / 3 - 1) * 18f + (float)(r.NextDouble() * 4 - 2);
                float hh = 6f + (float)r.NextDouble() * 8f;
                Box(L, "House_" + i, new Vector3(x, hh * 0.5f - 3f, z), new Vector3(9f, hh, 9f), ModuleRole.Structure, true, false, new Vector3(0, (float)r.NextDouble() * 15f, (float)(r.NextDouble() * 6 - 3)));
            }
            Cyl(L, "BellTower", new Vector3(0, -3f, 30f), 5f, H, ModuleRole.Structure, true);
            Spire(L, "BellSpire", new Vector3(0, H - 3f, 30f), 5.5f, 8f);
            Ledges(L, "TowerSteps", new Vector3(0, -3f, 30f), 8.5f, 4f, H - 3f, 1f);
            L.Anchors["Turm"] = new Vector3(0, H - 2.6f, 30f);
            L.Anchors["Dächer"] = new Vector3(18f, 8f, 0f);
            Hall(L, "ShoreHall", new Vector3(0, 0, -36f), new Vector3(14f, 7f, 12f), 1.2f, true, ModuleRole.Structure, false);
            L.Anchors["Halle"] = new Vector3(0, 0.4f, -36f);
            L.Entrance = new Vector3(0, 0, -44f);
            L.Footprint = 48f;
        }

        static void SkyIsland(LandmarkLayout L, float H)
        {
            // Schwebende Insel mit Tempel; Höhe H = Unterkante der Insel über Grund
            float R = 22f;
            Add(L, "IslandRock", ModuleShape.Sphere, new Vector3(0, H - 6f, 0), new Vector3(R * 2f, 18f, R * 2f), ModuleRole.Rock, true, false, true);
            Cyl(L, "IslandTop", new Vector3(0, H, 0), R, 1.5f, ModuleRole.Rock, true);
            Hall(L, "Sanctum", new Vector3(0, H + 1.5f, 0), new Vector3(16f, 9f, 16f), 1.2f, true, ModuleRole.Structure, true);
            for (int i = 0; i < 6; i++) { float a = i / 6f * Mathf.PI * 2f; Cyl(L, "TemplePillar_" + i, new Vector3(Mathf.Cos(a) * 14f, H + 1.5f, Mathf.Sin(a) * 14f), 1f, 10f, ModuleRole.Accent, true); }
            Spire(L, "WindOrgan", new Vector3(12f, H + 1.5f, 12f), 1.4f, 16f, ModuleRole.Glow);
            L.Anchors["Tempel"] = new Vector3(0, H + 1.9f, 0);
            L.Anchors["Windorgel"] = new Vector3(12f, H + 18f, 12f);
            L.Entrance = new Vector3(0, H + 1.5f, -10f);
            L.Footprint = R + 8f;
            L.Height = H + 20f;
        }

        static void Massif(LandmarkLayout L, float H)
        {
            // Landform: Bauwerk nur Gipfelkreuz/Schrein (Gelände kommt aus der Terrain-Operation)
            Cyl(L, "SummitCairn", new Vector3(0, H, 0), 1.6f, 3f, ModuleRole.Rock, true);
            for (int i = 0; i < 9; i++) { float a = i / 9f * Mathf.PI * 2f; Box(L, "Standing_" + i, new Vector3(Mathf.Cos(a) * 14f, H + 3.5f, Mathf.Sin(a) * 14f), new Vector3(2.2f, 7f, 1.4f), ModuleRole.Rock, true, false, new Vector3(0, -a * Mathf.Rad2Deg, 0)); }
            Add(L, "SummitBeacon", ModuleShape.Sphere, new Vector3(0, H + 4f, 0), Vector3.one * 2f, ModuleRole.Glow, false, false, true);
            L.Anchors["Gipfel"] = new Vector3(2f, H + 0.3f, 0);
            // Felsband: Springer-Vorsprünge außen am Tafelberg (Radius 26 m + Rand), Rast auf halber Höhe
            Ledges(L, "Felsband", Vector3.zero, 31f, 1f, H, Mathf.PI * 1.5f);
            int cnt = Mathf.Max(1, Mathf.CeilToInt((H - 1f) / SpringerStep)), mid = Mathf.Max(1, cnt / 2);
            float ma = Mathf.PI * 1.5f + mid * 0.5f;
            L.Anchors["Felsband"] = new Vector3(Mathf.Cos(ma) * 31f, Mathf.Lerp(1f, H, mid / (float)cnt) + 0.1f, Mathf.Sin(ma) * 31f);
            L.Entrance = Vector3.zero;
            L.Footprint = 10f;
        }
    }
}
