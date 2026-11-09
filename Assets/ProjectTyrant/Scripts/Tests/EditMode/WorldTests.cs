using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 4 / M15: Herabstufung, Portal-Zuordnung, Save-Migration, Chunk-Zerlegung, Reisekosten.</summary>
    public class WorldTests
    {
        static ZoneDefinition Zone(string id, int min, int max, params PortalDef[] portals)
        {
            var z = ScriptableObject.CreateInstance<ZoneDefinition>();
            z.Id = id; z.LevelMin = min; z.LevelMax = max; z.Portals = new List<PortalDef>(portals);
            return z;
        }

        static PortalDef P(string id, string zone, string target) => new PortalDef { Id = id, TargetZone = zone, TargetPortal = target };

        [Test]
        public void LevelCap_Is30_And30EqualsFullAttributes()
        {
            Assert.AreEqual(30, AttributeSet.MaxLevel);
            Assert.AreEqual(1000f, AttributeSet.PrimaryBase(30), 0.01f);
            Assert.AreEqual(37f, AttributeSet.PrimaryBase(1), 0.01f);
            Assert.AreEqual(1000f, AttributeSet.PrimaryBase(80), 0.01f, "Über dem Cap wird geklemmt");
        }

        [Test]
        public void Downleveling_ZoneMaxPlusOne_OnlyDown()
        {
            Assert.AreEqual(16, WorldModel.EffectiveLevel(30, 15));
            Assert.AreEqual(12, WorldModel.EffectiveLevel(12, 15), "Unter dem Gebietsmaximum keine Änderung");
            Assert.AreEqual(26, WorldModel.EffectiveLevel(30, 25));
            Assert.AreEqual(30, WorldModel.EffectiveLevel(30, 30));
            Assert.AreEqual(22, WorldModel.EffectiveLevel(22, 0), "0 = keine Herabstufung");
        }

        [Test]
        public void Downleveling_XpFactorAndItemScale()
        {
            Assert.AreEqual(0.5f, WorldModel.XpFactor(30, 16));
            Assert.AreEqual(1f, WorldModel.XpFactor(20, 16));
            Assert.AreEqual(1f, WorldModel.ItemScale(10, 16), "Niedrigere Items werden nicht verstärkt");
            float s = WorldModel.ItemScale(30, 16);
            Assert.AreEqual(AttributeSet.PrimaryBase(16) / 1000f, s, 1e-4f);
            Assert.Less(s, 1f);
        }

        [Test]
        public void Portals_ResolveBidirectionally()
        {
            var a = Zone("a", 1, 15, P("a_b", "b", "b_a"));
            var b = Zone("b", 15, 25, P("b_a", "a", "a_b"), P("b_c", "c", "c_b"));
            var c = Zone("c", 25, 30, P("c_b", "b", "b_c"));
            var zones = new List<ZoneDefinition> { a, b, c };
            Assert.IsTrue(WorldModel.ResolvePortal(zones, "a", "a_b", out var tz, out var tp));
            Assert.AreEqual("b", tz.Id); Assert.AreEqual("b_a", tp.Id);
            Assert.IsTrue(WorldModel.ResolvePortal(zones, "c", "c_b", out tz, out tp));
            Assert.AreEqual("b_c", tp.Id);
            Assert.IsFalse(WorldModel.ResolvePortal(zones, "a", "missing", out tz, out tp));
            Assert.IsEmpty(WorldModel.ValidatePortals(zones));
        }

        [Test]
        public void Portals_ValidationFindsBrokenLinks()
        {
            var a = Zone("a", 1, 15, P("a_b", "b", "b_a"));
            var b = Zone("b", 15, 25, P("b_a", "a", "a_x"));   // zeigt auf nicht existierendes Portal
            var errors = WorldModel.ValidatePortals(new List<ZoneDefinition> { a, b });
            Assert.IsNotEmpty(errors);
        }

        [Test]
        public void SaveMigration_Version1LoadsIntoStartZone()
        {
            // Lauf-3-Stand: kein Version-, CurrentZone- oder ZoneFogs-Feld
            var oldFog = new FogModel(128, 200f);
            oldFog.Reveal(new Vector3(-60, 0, -60), 20f);
            string json = "{\"Level\":12,\"Xp\":340,\"HeroPoints\":48,\"Copper\":5432,\"HasPosition\":true,\"PosX\":-58.5,\"PosY\":1.2,\"PosZ\":-61.0,"
                          + "\"Fog\":\"" + oldFog.Serialize() + "\",\"ZoneChestGranted\":true,\"Completed\":[\"wp_sonnfurt\"]}";
            var dto = JsonUtility.FromJson<SaveGameDto>(json);
            Assert.Less(dto.Version, SaveGameDto.CurrentVersion);
            var st = new ProfileState();
            st.LoadDto(dto, null, null);
            Assert.AreEqual(ProfileState.StartZoneId, st.CurrentZone);
            Assert.AreEqual(12, st.Progression.Level);
            Assert.AreEqual(5432, st.Wallet.Copper);
            Assert.IsTrue(st.HasPosition);
            Assert.AreEqual(-58.5f, st.Position.x, 0.01f);
            Assert.AreEqual(oldFog.RevealedCount, st.FogFor(ProfileState.StartZoneId).RevealedCount);
            Assert.IsTrue(st.Completed.Contains("zonechest:sonnfurt"));
            Assert.IsTrue(st.Completed.Contains("wp_sonnfurt"));
        }

        [Test]
        public void SaveVersion2_RoundTripsZonesAndFogPerZone()
        {
            var st = new ProfileState();
            st.RegisterZoneSize("nebelmoor", 1000f);
            st.FogFor(ProfileState.StartZoneId).Reveal(new Vector3(0, 0, 0), 15f);
            st.FogFor("nebelmoor").Reveal(new Vector3(100, 0, -300), 60f);
            st.CurrentZone = "nebelmoor";
            var dto = st.ToDto();
            Assert.AreEqual(SaveGameDto.CurrentVersion, dto.Version);
            var back = new ProfileState();
            back.LoadDto(JsonUtility.FromJson<SaveGameDto>(JsonUtility.ToJson(dto)), null, null);
            Assert.AreEqual("nebelmoor", back.CurrentZone);
            Assert.AreEqual(1000f, back.FogFor("nebelmoor").WorldSize, 0.01f);
            Assert.AreEqual(st.FogFor("nebelmoor").RevealedCount, back.FogFor("nebelmoor").RevealedCount);
            Assert.AreEqual(st.FogFor(ProfileState.StartZoneId).RevealedCount, back.FogFor(ProfileState.StartZoneId).RevealedCount);
            Assert.AreEqual(back.FogFor("nebelmoor").RevealedCount, back.Fog.RevealedCount, "Fog = Nebel der aktuellen Zone");
        }

        [Test]
        public void ChunkSplit_CoversZoneWithoutOverlap()
        {
            var chunks = WorldModel.SplitChunks(1000f, 128f);
            Assert.AreEqual(64, chunks.Count, "ceil(1000/128) = 8 → 8×8");
            float area = 0f;
            foreach (var r in chunks)
            {
                area += r.width * r.height;
                Assert.GreaterOrEqual(r.xMin, -500f - 1e-3f); Assert.LessOrEqual(r.xMax, 500f + 1e-3f);
                Assert.Greater(r.width, 0f);
            }
            Assert.AreEqual(1000f * 1000f, area, 1f);
            for (int i = 0; i < chunks.Count; i++)
                for (int j = i + 1; j < chunks.Count; j++)
                {
                    var o = Rect.MinMaxRect(Mathf.Max(chunks[i].xMin, chunks[j].xMin), Mathf.Max(chunks[i].yMin, chunks[j].yMin), Mathf.Min(chunks[i].xMax, chunks[j].xMax), Mathf.Min(chunks[i].yMax, chunks[j].yMax));
                    Assert.IsFalse(o.width > 0.01f && o.height > 0.01f, "Überlappung " + i + "/" + j);
                }
            Assert.AreEqual(4, WorldModel.SplitChunks(200f, 128f).Count);
        }

        [Test]
        public void TravelCost_CrossZoneHasSurcharge()
        {
            int same = WorldModel.TravelCost(Vector3.zero, new Vector3(50, 0, 0), 20, false);
            int other = WorldModel.TravelCost(Vector3.zero, new Vector3(50, 0, 0), 20, true);
            Assert.AreEqual(TravelModel.BaseCost + WorldModel.ZoneSurcharge + 120, other);
            Assert.Greater(other, same);
            Assert.AreEqual(Mathf.RoundToInt(other * 0.5f), WorldModel.TravelCost(Vector3.zero, Vector3.zero, 20, true, 0.5f));
        }

        [Test]
        public void BiomeTerrain_DeterministicAndFlattened()
        {
            var b = ScriptableObject.CreateInstance<BiomeProfile>();
            b.Amplitude = 20f; b.CanyonDepth = 5f;
            var t1 = new BiomeTerrain(b, 77, 800f); var t2 = new BiomeTerrain(b, 77, 800f); var t3 = new BiomeTerrain(b, 78, 800f);
            Assert.AreEqual(t1.Height(123f, -45f), t2.Height(123f, -45f));
            Assert.AreNotEqual(t1.Height(123f, -45f), t3.Height(123f, -45f));
            t1.Flatten(new Vector2(10, 10), 20f, 5f);
            Assert.AreEqual(5f, t1.Height(10f, 10f), 1e-3f);
            Assert.Greater(t1.Height(399f, 0f), t1.RawHeight(380f, 0f) - 1f, "Randgebirge steigt zum Rand an");
        }
    }
}
