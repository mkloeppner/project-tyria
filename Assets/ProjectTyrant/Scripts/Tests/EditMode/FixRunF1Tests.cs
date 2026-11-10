using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Fix-Lauf F1: Kamera-Zoom, A/D-Drehen, Bodenziel-Modi, Flächeneffekt-Ebenen, Auto-Angriffs-Ketten, Optionen.</summary>
    public class CameraZoomTests
    {
        [Test]
        public void Zoom_ClampsFootRange_StepsAndInvert()
        {
            var z = new CameraZoomModel(); z.Snap(6f);
            z.Scroll(1f); Assert.AreEqual(5.25f, z.Target, 1e-4f, "vorwärts = heran, 0,75 m je Raste");
            for (int i = 0; i < 40; i++) z.Scroll(1f); Assert.AreEqual(1.5f, z.Target, 1e-4f, "Minimum 1,5 m");
            for (int i = 0; i < 40; i++) z.Scroll(-1f); Assert.AreEqual(12f, z.Target, 1e-4f, "Maximum 12 m");
            z.Invert = true; z.Scroll(-1f); Assert.AreEqual(11.25f, z.Target, 1e-4f, "umgekehrt");
            z.Invert = false; z.Step = 5f; z.Scroll(1f); Assert.AreEqual(9.75f, z.Target, 1e-4f, "Raster auf 1,5 m begrenzt");
        }

        [Test]
        public void Zoom_MountRange_LockedAndNotches()
        {
            var z = new CameraZoomModel(); z.Snap(2f);
            z.SetMounted(true); Assert.AreEqual(2.5f, z.Target, 1e-4f, "beritten mindestens 2,5 m");
            for (int i = 0; i < 40; i++) z.Scroll(-1f); Assert.AreEqual(16f, z.Target, 1e-4f, "beritten bis 16 m");
            z.SetMounted(false); Assert.AreEqual(12f, z.Target, 1e-4f);
            z.Locked = true; z.Scroll(3f); Assert.AreEqual(12f, z.Target, 1e-4f, "Kamerafahrt sperrt");
            Assert.AreEqual(1f, CameraZoomModel.Notches(120f), 1e-4f, "Legacy ±120");
            Assert.AreEqual(1f, CameraZoomModel.Notches(1f), 1e-4f, "Input System normalisiert ±1");
            Assert.AreEqual(-2f, CameraZoomModel.Notches(-240f), 1e-4f);
        }

        [Test]
        public void Zoom_DampedNotInstant()
        {
            var z = new CameraZoomModel(); z.Snap(12f); z.Scroll(4f);
            z.Tick(1f / 60f); Assert.Greater(z.Distance, 9.5f, "nicht sofort");
            for (int i = 0; i < 30; i++) z.Tick(1f / 60f);
            Assert.AreEqual(9f, z.Distance, 0.05f, "nach 0,5 s angekommen");
        }
    }

    public class MovementModelTests
    {
        [Test]
        public void TurnMode_ChangesYawNotPosition()
        {
            var r = MovementModel.Compute(MovementModel.Mode.Turn, false, false, false, new Vector2(1f, 0f), 0f, 0f, Vector3.forward, 0.5f);
            Assert.IsTrue(r.Turning); Assert.AreEqual(90f, r.YawDelta, 1e-3f, "180°/s");
            Assert.Less(r.Wish.sqrMagnitude, 1e-6f, "A/D allein bewegt nicht");
            var w = MovementModel.Compute(MovementModel.Mode.Turn, false, false, false, new Vector2(0f, 1f), 0f, 90f, Vector3.forward, 0.1f);
            Assert.AreEqual(1f, w.Wish.x, 1e-3f, "W läuft entlang der Charakter-Blickrichtung, nicht der Kamera");
            var q = MovementModel.Compute(MovementModel.Mode.Turn, false, false, false, Vector2.zero, 1f, 0f, Vector3.forward, 0.1f);
            Assert.AreEqual(1f, q.Wish.x, 1e-3f, "Q/E bleiben seitwärts"); Assert.AreEqual(0f, q.YawDelta);
        }

        [Test]
        public void StrafeMode_MovesSidewaysWithoutYaw()
        {
            var r = MovementModel.Compute(MovementModel.Mode.Strafe, false, false, false, new Vector2(1f, 0f), 0f, 0f, Vector3.forward, 0.5f);
            Assert.IsFalse(r.Turning); Assert.AreEqual(0f, r.YawDelta); Assert.AreEqual(1f, r.Wish.x, 1e-3f); Assert.IsTrue(r.FaceWish);
        }

        [Test]
        public void RightMouseActionCamAndMount_OverrideTurn()
        {
            foreach (var (rmb, ac, mount) in new[] { (true, false, false), (false, true, false), (false, false, true) })
            {
                var r = MovementModel.Compute(MovementModel.Mode.Turn, rmb, ac, mount, new Vector2(1f, 0f), 0f, 0f, Vector3.forward, 0.5f);
                Assert.IsFalse(r.Turning); Assert.AreEqual(0f, r.YawDelta); Assert.AreEqual(1f, r.Wish.x, 1e-3f, "A/D seitwärts");
            }
        }
    }

    public class GroundTargetModelTests
    {
        [Test]
        public void Normal_PreviewThenClickOrSameKey()
        {
            var g = new GroundTargetModel();
            Assert.AreEqual(GroundTargetModel.Action.Preview, g.KeyDown(9, 0f)); Assert.IsTrue(g.PreviewVisible(0f));
            Assert.AreEqual(GroundTargetModel.Action.Cast, g.KeyDown(9, 0.5f), "dieselbe Taste wirkt");
            g.KeyDown(9, 1f); Assert.AreEqual(GroundTargetModel.Action.Cast, g.Click());
            g.KeyDown(9, 2f); Assert.AreEqual(GroundTargetModel.Action.None, g.KeyUp(9, 2.1f), "Loslassen wirkt im Normalmodus nicht");
            Assert.AreEqual(GroundTargetModel.Action.Cancel, g.Escape()); Assert.AreEqual(GroundTargetModel.State.Idle, g.Current);
        }

        [Test]
        public void PressRelease_TapCastsHoldShowsPreview()
        {
            var g = new GroundTargetModel(); g.SetMode(GroundTargetModel.Mode.PressRelease); g.HoldThreshold = 0.15f;
            g.KeyDown(5, 0f); Assert.IsFalse(g.PreviewVisible(0.1f), "vor der Schwelle keine Vorschau");
            Assert.IsTrue(g.IsTap(0f, 0.1f)); Assert.AreEqual(GroundTargetModel.Action.Cast, g.KeyUp(5, 0.1f), "Tippen wirkt");
            g.KeyDown(5, 1f); Assert.IsTrue(g.PreviewVisible(1.2f), "Halten zeigt Vorschau");
            Assert.AreEqual(GroundTargetModel.Action.None, g.KeyUp(6, 1.3f), "andere Taste");
            Assert.AreEqual(GroundTargetModel.Action.Cast, g.KeyUp(5, 1.4f), "Loslassen wirkt");
            g.KeyDown(5, 2f); Assert.AreEqual(GroundTargetModel.Action.Cancel, g.Escape(), "Esc beim Halten");
            Assert.AreEqual(GroundTargetModel.Action.None, g.KeyUp(5, 2.2f));
        }

        [Test]
        public void Instant_AndInstantWithRange()
        {
            var g = new GroundTargetModel(); g.SetMode(GroundTargetModel.Mode.Instant);
            Assert.AreEqual(GroundTargetModel.Action.Cast, g.KeyDown(9, 0f)); Assert.AreEqual(GroundTargetModel.State.Idle, g.Current);
            g.SetMode(GroundTargetModel.Mode.InstantWithRange);
            Assert.AreEqual(GroundTargetModel.Action.CastWithRangeFlash, g.KeyDown(9, 0f)); Assert.AreEqual(0.5f, GroundTargetModel.RangeFlashSeconds);
        }

        [Test]
        public void ModeChangeWithOpenPreview_Cancels_OtherSkillCancels()
        {
            var g = new GroundTargetModel(); g.KeyDown(9, 0f);
            Assert.AreEqual(GroundTargetModel.Action.Cancel, g.SetMode(GroundTargetModel.Mode.Instant)); Assert.AreEqual(GroundTargetModel.State.Idle, g.Current);
            g.SetMode(GroundTargetModel.Mode.Normal); g.KeyDown(9, 0f);
            Assert.AreEqual(GroundTargetModel.Action.Cancel, g.OtherSkill(3));
        }

        [Test]
        public void Resolve_ClampAbortAndSnap()
        {
            var g = new GroundTargetModel();
            Assert.IsTrue(g.Resolve(Vector3.zero, new Vector3(20, 0, 0), 10f, null, out var p, out bool c)); Assert.IsTrue(c); Assert.AreEqual(10f, p.x, 1e-3f, "am Reichweitenrand");
            g.ClampToRange = false; Assert.IsFalse(g.Resolve(Vector3.zero, new Vector3(20, 0, 0), 10f, null, out _, out _), "abbrechen");
            g.SnapToTarget = true; Assert.IsTrue(g.Resolve(Vector3.zero, new Vector3(3, 0, 0), 10f, new Vector3(0, 0, 8), out p, out _)); Assert.AreEqual(8f, p.z, 1e-3f, "auf Ziel eingerastet");
            Assert.IsTrue(g.Resolve(Vector3.zero, new Vector3(3, 0, 0), 10f, new Vector3(0, 0, 30), out p, out _)); Assert.AreEqual(3f, p.x, 1e-3f, "Ziel außer Reichweite: Cursor");
        }

        [Test]
        public void Caster_FastCastAliasMapsToInstant()
        {
            var go = new GameObject("GT"); try
            {
                go.AddComponent<Combatant>().Team = Team.Player; var caster = go.AddComponent<SkillCaster>(); caster.Init();
                caster.FastCastGroundTargets = true; Assert.AreEqual(GroundTargetModel.Mode.Instant, caster.Ground.CurrentMode);
                caster.FastCastGroundTargets = false; Assert.AreEqual(GroundTargetModel.Mode.Normal, caster.Ground.CurrentMode);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }

    public class AreaDisplayTests
    {
        [Test]
        public void Visibility_PerOwnerAndOption()
        {
            var p = AreaDisplay.For(AreaDisplay.Owner.Player, 1, 2); Assert.IsFalse(p.Outline); Assert.IsTrue(p.Effect);
            Assert.IsTrue(AreaDisplay.For(AreaDisplay.Owner.Player, 0, 2).Outline);
            var a = AreaDisplay.For(AreaDisplay.Owner.Ally, 1, 2); Assert.AreEqual(0.5f, a.Alpha, 1e-4f, "Verbündete Standard: reduziert");
            Assert.IsFalse(AreaDisplay.For(AreaDisplay.Owner.Ally, 0, 3).Effect, "Aus");
            Assert.IsFalse(AreaDisplay.For(AreaDisplay.Owner.Ally, 0, 1).Outline, "Nur Effekt");
            var e = AreaDisplay.For(AreaDisplay.Owner.Enemy, 1, 3); Assert.IsTrue(e.Outline && e.Effect && e.Alpha == 1f, "Gegner unberührt");
            Assert.AreEqual(AreaDisplay.Owner.Player, AreaDisplay.FromIntensity(SkillFx.TeamIntensity(Team.Player)));
            Assert.AreEqual(AreaDisplay.Owner.Ally, AreaDisplay.FromIntensity(SkillFx.TeamIntensity(Team.Ally)));
            Assert.AreEqual(AreaDisplay.Owner.Enemy, AreaDisplay.FromIntensity(SkillFx.TeamIntensity(Team.Enemy)));
        }

        [Test]
        public void EveryGroundComposition_HasOutlineAndEffectLayer()
        {
            var cat = VfxKitBuilder.Catalog; Assert.IsNotNull(cat);
            var bad = new List<string>(); int n = 0;
            foreach (var e in cat.Entries)
            {
                if (!VfxIds.TryParse(e.Key, out var shape, out _, out _) || (shape != VfxShape.GroundCircle && shape != VfxShape.Sigil)) continue;
                n++; var layers = e.Prefab.GetComponentsInChildren<VfxAreaLayer>(true);
                bool o = false, f = false; foreach (var l in layers) { if (l.Layer == AreaLayer.AreaOutline) o = true; else f = true; }
                if (!o || !f) bad.Add(e.Key);
            }
            Assert.AreEqual(2 * 27 * 4, n); Assert.IsEmpty(bad, string.Join(", ", bad));
        }

        [Test]
        public void EnemyTelegraphs_OnlyEnemyOwnerHidden()
        {
            var enemy = new GameObject("E").AddComponent<Combatant>(); enemy.Team = Team.Enemy;
            bool old = GroundTelegraph.ShowEnemyTelegraphs;
            try
            {
                GroundTelegraph.ShowEnemyTelegraphs = true;
                var t = GroundTelegraph.Spawn(enemy, Vector3.zero, 3f, 1f, 100f);
                var r = t.GetComponentInChildren<Renderer>();
                if (r != null) Assert.IsTrue(r.enabled, "Standard: immer anzeigen");
                Object.DestroyImmediate(t.gameObject);
            }
            finally { GroundTelegraph.ShowEnemyTelegraphs = old; Object.DestroyImmediate(enemy.gameObject); }
            Assert.IsTrue(new GameOptions().EnemyTelegraphs, "Standardwert an");
        }
    }

    public class AutoChainTests
    {
        [Test]
        public void Chain_AdvancesAndResetsAfter2s()
        {
            var c = new AutoChainModel(3);
            Assert.AreEqual(0, c.Current(0f)); c.Completed(0.5f); Assert.AreEqual(1, c.Current(1f)); c.Completed(1f);
            Assert.IsTrue(c.AtBonus(1.5f), "drittes Glied = Bonus"); c.Completed(1.5f); Assert.AreEqual(0, c.Current(2f), "von vorn");
            c.Completed(2f); Assert.AreEqual(0, c.Current(4.1f), "2 s Pause → zurück");
        }

        [Test]
        public void Chain_BreakOnSwapDodgeInterruptTargetLoss()
        {
            var c = new AutoChainModel(3); c.Completed(0f); c.Completed(0.5f);
            c.Break(); Assert.AreEqual(0, c.Current(0.6f)); Assert.AreEqual(1, c.ResetCount);
            Assert.IsFalse(c.AtBonus(0.6f), "Bonus nur auf Glied 3");
        }

        [Test]
        public void Caster_WeaponSwapBreaksChain()
        {
            var go = new GameObject("C"); try
            {
                go.AddComponent<Combatant>().Team = Team.Player; var caster = go.AddComponent<SkillCaster>(); caster.Init();
                var a = ScriptableObject.CreateInstance<WeaponSet>(); var b = ScriptableObject.CreateInstance<WeaponSet>(); a.Skills = new SkillDefinition[5]; b.Skills = new SkillDefinition[5];
                caster.SetWeaponSets(a, b); caster.Chain.Completed(Time.time); caster.Chain.Completed(Time.time);
                Assert.IsTrue(caster.TrySwapWeapons()); Assert.AreEqual(0, caster.Chain.Current(Time.time));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void EveryChainEligibleWeapon_HasThreeLinksWithBonus()
        {
            var lib = ClassBuilder.Library; Assert.IsNotNull(lib);
            var bad = new List<string>(); int n = 0;
            foreach (var c in lib.Classes)
            {
                var sets = new List<WeaponSet>(c.WeaponSets); foreach (var e in c.EliteSpecs) sets.Add(e.Weapon);
                foreach (var w in sets)
                {
                    if (!ClassBuilder.ChainEligible(c, w)) continue;
                    n++; var s = w.Skills[0]; string who = c.Id + "/" + w.DisplayName;
                    if (s.Chain == null || s.Chain.Length != 2) { bad.Add(who + ": nicht 3 Glieder"); continue; }
                    var l2 = s.Chain[0]; var l3 = s.Chain[1];
                    if (Mathf.Abs(l2.Coefficient - s.Coefficient) > 1e-4f) bad.Add(who + ": Glied 2 ≠ Basis");
                    float k = l3.Coefficient / s.Coefficient; if (k < 1.3f - 1e-4f || k > 1.5f + 1e-4f) bad.Add(who + ": Glied 3 ×" + k.ToString("0.00"));
                    if (!l3.ChainBonus || s.ChainBonus || l2.ChainBonus) bad.Add(who + ": Bonus-Markierung");
                    if (l3.TargetBuffs.Count + l3.SelfBuffs.Count <= s.TargetBuffs.Count + s.SelfBuffs.Count && l3.TargetCount == s.TargetCount) bad.Add(who + ": kein Bonuseffekt");
                    if (l2.ChainRoot != s || l3.ChainRoot != s) bad.Add(who + ": Kettenwurzel");
                    foreach (var link in new[] { s, l2, l3 }) if (link.Timeline == null || !link.Timeline.IsBuilt) bad.Add(who + "/" + link.DisplayName + ": Zeitachse");
                }
                foreach (var w in sets) if (w != null && w.Kind == WeaponKind.Longbow && w.Skills[0].Chain != null && w.Skills[0].Chain.Length > 0) bad.Add(c.Id + ": Langbogen ohne Kette (GW2)");
            }
            Assert.GreaterOrEqual(n, 16, "kettenfähige Waffen");
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void Tooltip_ShowsAllLinksWithFacts()
        {
            var lib = ClassBuilder.Library; var gs = lib.Get("waechter").WeaponSets[0].Skills[0];
            foreach (var link in new[] { gs, gs.Chain[0], gs.Chain[1] })
            {
                var tip = TooltipBuilder.Skill(link, null);
                int lines = 0; foreach (var l in tip.Lines) if (l.Text != null && (l.Text.Contains("1/3") || l.Text.Contains("2/3") || l.Text.Contains("3/3"))) lines++;
                Assert.AreEqual(3, lines, link.DisplayName + ": drei Glieder im Tooltip");
            }
            Assert.AreEqual(3, gs.Chain[1].ChainPosition(out int len)); Assert.AreEqual(3, len);
        }
    }

    public class OptionCatalogTests
    {
        [Test]
        public void EveryOption_HasTooltipAndField()
        {
            foreach (var d in OptionCatalog.All)
            {
                Assert.IsNotNull(OptionCatalog.Field(d.Field), d.Field);
                Assert.IsFalse(string.IsNullOrEmpty(d.Tooltip), d.Field + ": Tooltip"); Assert.IsTrue(d.Tooltip.EndsWith(".") || d.Tooltip.EndsWith("“") || d.Tooltip.EndsWith(")"), d.Field + ": ein Satz");
                Assert.Contains(d.Tab, OptionCatalog.Tabs);
            }
            foreach (var f in new[] { "TurnMode", "GroundTargetMode", "GroundHoldMs", "GroundSnapToTarget", "GroundClampToRange", "AreaOwn", "AreaAllies", "EnemyTelegraphs", "ZoomStep", "ZoomInvert", "CameraDefaultDistance" })
                Assert.IsTrue(OptionCatalog.Get(f).HasValue, f + " im Katalog");
        }

        [Test]
        public void Standard_SetsExactTableValues()
        {
            var o = new GameOptions { TurnMode = 1, GroundTargetMode = 3, GroundHoldMs = 280, GroundSnapToTarget = true, GroundClampToRange = false, AreaOwn = 1, AreaAllies = 3, EnemyTelegraphs = false, ZoomStep = 1.4f, ZoomInvert = true, CameraDefaultDistance = 11f, CameraCurrentDistance = 9f };
            OptionCatalog.ResetTab(o, "Steuerung"); OptionCatalog.ResetTab(o, "Spiel"); OptionCatalog.ResetTab(o, "Kamera");
            Assert.AreEqual(0, o.TurnMode, "Drehen"); Assert.AreEqual(0, o.GroundTargetMode, "Normal"); Assert.AreEqual(150, o.GroundHoldMs); Assert.IsFalse(o.GroundSnapToTarget); Assert.IsTrue(o.GroundClampToRange);
            Assert.AreEqual(0, o.AreaOwn, "Fläche und Effekt"); Assert.AreEqual(2, o.AreaAllies, "Reduziert"); Assert.IsTrue(o.EnemyTelegraphs);
            Assert.AreEqual(0.75f, o.ZoomStep, 1e-4f); Assert.IsFalse(o.ZoomInvert); Assert.AreEqual(6f, o.CameraDefaultDistance, 1e-4f); Assert.AreEqual(-1f, o.CameraCurrentDistance);
            Assert.IsFalse(o.FirstPersonAtMinZoom);
        }

        [Test]
        public void Options_RoundTripAllNewValues()
        {
            var o = new GameOptions { TurnMode = 1, GroundTargetMode = 2, GroundHoldMs = 220, GroundSnapToTarget = true, GroundClampToRange = false, AreaOwn = 1, AreaAllies = 0, EnemyTelegraphs = false, ZoomStep = 0.5f, ZoomInvert = true, CameraDefaultDistance = 8.5f, CameraCurrentDistance = 3.25f };
            var b = GameOptions.FromJson(o.ToJson());
            Assert.AreEqual(1, b.TurnMode); Assert.AreEqual(2, b.GroundTargetMode); Assert.AreEqual(220, b.GroundHoldMs); Assert.IsTrue(b.GroundSnapToTarget); Assert.IsFalse(b.GroundClampToRange);
            Assert.AreEqual(1, b.AreaOwn); Assert.AreEqual(0, b.AreaAllies); Assert.IsFalse(b.EnemyTelegraphs); Assert.AreEqual(0.5f, b.ZoomStep, 1e-4f); Assert.IsTrue(b.ZoomInvert);
            Assert.AreEqual(8.5f, b.CameraDefaultDistance, 1e-4f); Assert.AreEqual(3.25f, b.CameraCurrentDistance, 1e-4f);
            var c = GameOptions.FromJson(new GameOptions { GroundHoldMs = 5000, ZoomStep = 9f, CameraDefaultDistance = 0.1f }.ToJson());
            Assert.AreEqual(300, c.GroundHoldMs); Assert.AreEqual(1.5f, c.ZoomStep, 1e-4f); Assert.AreEqual(1.5f, c.CameraDefaultDistance, 1e-4f);
        }
    }
}
