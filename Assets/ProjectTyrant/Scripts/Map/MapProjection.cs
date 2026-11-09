using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Reine Welt↔Karten-Transformation. Karten-UV (0..1) entspricht der Zone (−Half..+Half in X/Z), Norden = +Z = oben.
    /// Minimap: Spieler im Zentrum, optional mit der Kamera gedreht (Blickrichtung zeigt nach oben).
    /// </summary>
    public static class MapProjection
    {
        /// <summary>Kantenlänge der aktuellen Zone (Lauf 4: je Zone, zentriert um 0/0). Startwert = Startzone.</summary>
        public static float ZoneSize { get; private set; } = Procedural.ZoneLayout.Size;
        public static float Half => ZoneSize * 0.5f;
        public static void SetZoneSize(float size) { if (size > 1f) ZoneSize = size; }

        /// <summary>Minimap-Zoomstufen in Pixel je Meter (Stufe 0 = ganze Zone).</summary>
        public static readonly float[] MinimapZoom = { 1.1f, 1.6f, 2.4f, 3.6f, 5.2f };

        public static Vector2 WorldToUv(Vector3 world) => new Vector2((world.x + Half) / ZoneSize, (world.z + Half) / ZoneSize);
        public static Vector3 UvToWorld(Vector2 uv, float y = 0f) => new Vector3(uv.x * ZoneSize - Half, y, uv.y * ZoneSize - Half);

        /// <summary>Dreht einen Kartenvektor (x rechts, y oben) um <paramref name="degrees"/> gegen den Uhrzeigersinn.</summary>
        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        /// <summary>Welt → lokale Minimap-Pixel (relativ zum Zentrum). Mit Rotation zeigt die Kamerablickrichtung (yaw) nach oben.</summary>
        public static Vector2 WorldToMinimap(Vector3 world, Vector3 center, float pixelsPerMeter, float cameraYaw, bool rotate)
        {
            var d = new Vector2(world.x - center.x, world.z - center.z) * pixelsPerMeter;
            return rotate ? Rotate(d, cameraYaw) : d;
        }

        /// <summary>Umkehrung von <see cref="WorldToMinimap"/> (Klick auf die Minimap → Weltposition).</summary>
        public static Vector3 MinimapToWorld(Vector2 local, Vector3 center, float pixelsPerMeter, float cameraYaw, bool rotate)
        {
            var d = (rotate ? Rotate(local, -cameraYaw) : local) / pixelsPerMeter;
            return new Vector3(center.x + d.x, center.y, center.z + d.y);
        }

        /// <summary>Klemmt einen Punkt auf den Rand eines Rechtecks (halbe Größe), z. B. für Event-Marker außerhalb der Minimap.</summary>
        public static Vector2 ClampToRect(Vector2 p, Vector2 halfSize, out bool clamped)
        {
            clamped = Mathf.Abs(p.x) > halfSize.x || Mathf.Abs(p.y) > halfSize.y;
            if (!clamped) return p;
            float sx = Mathf.Abs(p.x) > 1e-4f ? halfSize.x / Mathf.Abs(p.x) : float.MaxValue;
            float sy = Mathf.Abs(p.y) > 1e-4f ? halfSize.y / Mathf.Abs(p.y) : float.MaxValue;
            return p * Mathf.Min(sx, sy);
        }

        public static bool InsideRect(Vector2 p, Vector2 halfSize) => Mathf.Abs(p.x) <= halfSize.x && Mathf.Abs(p.y) <= halfSize.y;

        // ---------- Weltkarte (Pan/Zoom) ----------
        /// <summary>UV → lokale Pixel im Weltkarten-Ausschnitt (Ursprung Mitte). mapPixels = Kantenlänge der Karte bei Zoom 1.</summary>
        public static Vector2 UvToView(Vector2 uv, Vector2 viewCenterUv, float mapPixels, float zoom) => (uv - viewCenterUv) * (mapPixels * zoom);
        public static Vector2 ViewToUv(Vector2 local, Vector2 viewCenterUv, float mapPixels, float zoom) => viewCenterUv + local / (mapPixels * zoom);

        /// <summary>Zoomt um einen Bildschirmpunkt (der Punkt unter dem Mauszeiger bleibt fest).</summary>
        public static Vector2 ZoomAround(Vector2 viewCenterUv, Vector2 pivotLocal, float mapPixels, float oldZoom, float newZoom)
        {
            var pivotUv = ViewToUv(pivotLocal, viewCenterUv, mapPixels, oldZoom);
            return pivotUv - pivotLocal / (mapPixels * newZoom);
        }

        /// <summary>Begrenzt die Ansicht, sodass die Karte die Fläche nicht verlässt (wenn größer als der Ausschnitt).</summary>
        public static Vector2 ClampView(Vector2 viewCenterUv, Vector2 viewSize, float mapPixels, float zoom)
        {
            var halfUv = viewSize * 0.5f / (mapPixels * zoom);
            float x = halfUv.x >= 0.5f ? 0.5f : Mathf.Clamp(viewCenterUv.x, halfUv.x, 1f - halfUv.x);
            float y = halfUv.y >= 0.5f ? 0.5f : Mathf.Clamp(viewCenterUv.y, halfUv.y, 1f - halfUv.y);
            return new Vector2(x, y);
        }
    }

    /// <summary>Schnellreise-Kosten (GW2-ähnlich: Basis + Distanz × Stufenfaktor), in Kupfer.</summary>
    public static class TravelModel
    {
        public const int BaseCost = 10;
        public static int Cost(Vector3 from, Vector3 to, int level)
        {
            float d = new Vector2(to.x - from.x, to.z - from.z).magnitude;
            return Mathf.RoundToInt(BaseCost + d * 0.25f * (1f + level * 0.15f));
        }
    }
}
