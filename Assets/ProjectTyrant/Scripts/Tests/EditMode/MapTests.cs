using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    public class MapProjectionTests
    {
        [Test]
        public void WorldUv_RoundTrip_AndKnownPlaces()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.5f), MapProjection.WorldToUv(Vector3.zero));
            Assert.AreEqual(Vector2.zero, MapProjection.WorldToUv(new Vector3(-100, 5, -100)));
            Assert.AreEqual(Vector2.one, MapProjection.WorldToUv(new Vector3(100, 0, 100)));
            // Hub (−60/−60) liegt unten links, Rabenschanze (58/58) oben rechts, Elmhain (58/−55) unten rechts
            var hub = MapProjection.WorldToUv(new Vector3(-60, 0, -60));
            var ruins = MapProjection.WorldToUv(new Vector3(58, 0, -55));
            Assert.AreEqual(0.2f, hub.x, 1e-4f); Assert.AreEqual(0.2f, hub.y, 1e-4f);
            Assert.Greater(ruins.x, 0.5f); Assert.Less(ruins.y, 0.5f);
            var w = new Vector3(12.5f, 0, -33.25f);
            var back = MapProjection.UvToWorld(MapProjection.WorldToUv(w));
            Assert.AreEqual(w.x, back.x, 1e-3f); Assert.AreEqual(w.z, back.z, 1e-3f);
        }

        [Test]
        public void Minimap_NorthUp_And_CameraRotation()
        {
            var c = new Vector3(10, 0, 10);
            var north = MapProjection.WorldToMinimap(c + new Vector3(0, 0, 5), c, 2f, 0f, false);
            Assert.AreEqual(0f, north.x, 1e-4f); Assert.AreEqual(10f, north.y, 1e-4f);
            var east = MapProjection.WorldToMinimap(c + new Vector3(5, 0, 0), c, 2f, 0f, false);
            Assert.AreEqual(10f, east.x, 1e-4f); Assert.AreEqual(0f, east.y, 1e-4f);
            // Kamera schaut nach Osten (yaw 90°): Punkt im Osten erscheint oben
            var ahead = MapProjection.WorldToMinimap(c + new Vector3(5, 0, 0), c, 2f, 90f, true);
            Assert.AreEqual(0f, ahead.x, 1e-4f); Assert.AreEqual(10f, ahead.y, 1e-4f);
            // beliebiger Winkel: Blickrichtung zeigt immer nach oben
            float yaw = 37f; var dir = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0, Mathf.Cos(yaw * Mathf.Deg2Rad));
            var p = MapProjection.WorldToMinimap(c + dir * 8f, c, 1f, yaw, true);
            Assert.AreEqual(0f, p.x, 1e-3f); Assert.AreEqual(8f, p.y, 1e-3f);
            var inv = MapProjection.MinimapToWorld(p, c, 1f, yaw, true);
            Assert.AreEqual((c + dir * 8f).x, inv.x, 1e-3f); Assert.AreEqual((c + dir * 8f).z, inv.z, 1e-3f);
        }

        [Test]
        public void ClampToRect_KeepsDirection()
        {
            var half = new Vector2(140, 115);
            var inside = MapProjection.ClampToRect(new Vector2(10, 10), half, out bool c1);
            Assert.IsFalse(c1); Assert.AreEqual(new Vector2(10, 10), inside);
            var p = MapProjection.ClampToRect(new Vector2(1000, 500), half, out bool c2);
            Assert.IsTrue(c2);
            Assert.AreEqual(140f, p.x, 1e-3f); Assert.AreEqual(70f, p.y, 1e-3f);
            Assert.AreEqual(5, MapProjection.MinimapZoom.Length, "5 Zoomstufen");
            for (int i = 1; i < 5; i++) Assert.Greater(MapProjection.MinimapZoom[i], MapProjection.MinimapZoom[i - 1]);
        }

        [Test]
        public void WorldMap_ZoomAroundPointer_And_ClampView()
        {
            var center = new Vector2(0.4f, 0.6f);
            var pointer = new Vector2(120, -80);
            var uvUnder = MapProjection.ViewToUv(pointer, center, 800f, 2f);
            var nc = MapProjection.ZoomAround(center, pointer, 800f, 2f, 3.5f);
            var uvAfter = MapProjection.ViewToUv(pointer, nc, 800f, 3.5f);
            Assert.AreEqual(uvUnder.x, uvAfter.x, 1e-5f); Assert.AreEqual(uvUnder.y, uvAfter.y, 1e-5f);
            var clamped = MapProjection.ClampView(new Vector2(0.01f, 0.99f), new Vector2(800, 800), 800f, 2f);
            Assert.AreEqual(0.25f, clamped.x, 1e-5f); Assert.AreEqual(0.75f, clamped.y, 1e-5f);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), MapProjection.ClampView(new Vector2(0.1f, 0.9f), new Vector2(800, 800), 800f, 1f));
        }
    }

    public class FogTests
    {
        [Test]
        public void Reveal_Radius60_And_Persistence()
        {
            var fog = new FogModel(128, 200f);
            Assert.AreEqual(0f, fog.RevealedFraction);
            int n = fog.Reveal(new Vector3(-60, 0, -60), 60f);
            Assert.Greater(n, 0);
            Assert.IsTrue(fog.IsRevealed(new Vector3(-60, 0, -60)));
            Assert.IsTrue(fog.IsRevealed(new Vector3(-60 + 55, 0, -60)), "innerhalb 60 m");
            Assert.IsFalse(fog.IsRevealed(new Vector3(-60 + 70, 0, -60)), "außerhalb 60 m");
            Assert.AreEqual(0, fog.Reveal(new Vector3(-60, 0, -60), 60f), "Wiederholung deckt nichts Neues auf");
            float frac = fog.RevealedFraction;
            // Persistenz über den Speicherstand
            var s = new ProfileState();
            s.Fog.Reveal(new Vector3(58, 0, -55), 60f);
            var json = JsonUtility.ToJson(s.ToDto());
            var t = new ProfileState();
            t.LoadDto(JsonUtility.FromJson<SaveGameDto>(json), null, null);
            Assert.AreEqual(s.Fog.RevealedCount, t.Fog.RevealedCount);
            Assert.IsTrue(t.Fog.IsRevealed(new Vector3(58, 0, -55)));
            Assert.IsFalse(t.Fog.IsRevealed(new Vector3(-60, 0, 60)));
            Assert.Greater(frac, 0.1f); Assert.Less(frac, 0.5f);
        }

        [Test]
        public void FogTexture_UpdatesOnlyOnChange()
        {
            var fog = new FogModel(16, 200f);
            var tex = new FogTexture(16, new Color(0, 0, 0, 0.8f));
            try
            {
                Assert.IsTrue(tex.Update(fog));
                Assert.IsFalse(tex.Update(fog), "ohne Änderung kein Upload");
                Assert.AreEqual(0.8f, tex.Texture.GetPixel(8, 8).a, 0.01f);
                fog.Reveal(Vector3.zero, 40f);   // 3×3-Nachbarschaft vollständig aufgedeckt (weiche Kanten)
                Assert.IsTrue(tex.Update(fog));
                Assert.AreEqual(0f, tex.Texture.GetPixel(8, 8).a, 0.01f);
                Assert.AreEqual(0.8f, tex.Texture.GetPixel(0, 0).a, 0.01f);
            }
            finally { Object.DestroyImmediate(tex.Texture); }
        }
    }

    public class ZoneCompletionTests
    {
        [Test]
        public void Count_FromMarkers()
        {
            var list = new List<MapMarkerInfo>
            {
                new MapMarkerInfo { Kind = MapMarkerKind.Waypoint, State = MarkerState.Open },
                new MapMarkerInfo { Kind = MapMarkerKind.Waypoint, State = MarkerState.Contested },
                new MapMarkerInfo { Kind = MapMarkerKind.Waypoint, State = MarkerState.Undiscovered },
                new MapMarkerInfo { Kind = MapMarkerKind.PointOfInterest, State = MarkerState.Done },
                new MapMarkerInfo { Kind = MapMarkerKind.PointOfInterest, State = MarkerState.Open },
                new MapMarkerInfo { Kind = MapMarkerKind.Heart, State = MarkerState.Done },
                new MapMarkerInfo { Kind = MapMarkerKind.Vendor, State = MarkerState.Open },
            };
            var c = ZoneCompletion.Count(list);
            Assert.AreEqual(new Vector2Int(2, 3), c.Waypoints, "umkämpft zählt als entdeckt");
            Assert.AreEqual(new Vector2Int(1, 2), c.Pois);
            Assert.AreEqual(new Vector2Int(1, 1), c.Hearts);
            Assert.AreEqual(6, c.Total, "Händler zählen nicht");
            Assert.AreEqual(4f / 6f, c.Percent, 1e-4f);
            Assert.IsFalse(c.Complete);
        }

        [Test]
        public void TravelCost_GrowsWithDistanceAndLevel()
        {
            var a = Vector3.zero;
            int near = TravelModel.Cost(a, new Vector3(10, 0, 0), 5);
            int far = TravelModel.Cost(a, new Vector3(150, 0, 0), 5);
            int farHigh = TravelModel.Cost(a, new Vector3(150, 0, 0), 15);
            Assert.GreaterOrEqual(near, TravelModel.BaseCost);
            Assert.Greater(far, near); Assert.Greater(farHigh, far);
            Assert.AreEqual(near, TravelModel.Cost(a, new Vector3(10, 30, 0), 5), "Höhe zählt nicht");
        }
    }
}
