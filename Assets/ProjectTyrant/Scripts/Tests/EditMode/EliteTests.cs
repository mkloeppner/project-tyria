using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 5 / M26: Elite-Spezialisierungen – Vollständigkeit (datengetrieben über alle Skills), nur eine aktiv, dritter Platz, Freischaltung, Mechanik-Varianten.</summary>
    public class EliteTests
    {
        static ClassLibrary Lib { get { var l = ClassBuilder.Library; Assert.IsNotNull(l, "ClassLibrary fehlt (Rebuild)"); return l; } }

        [Test]
        public void EliteSpecs_CompleteForEveryClass()
        {
            var palettes = new HashSet<VfxPalette>(); var bank = AudioBuilder.Bank; var vfx = VfxKitBuilder.Catalog; var items = AssetDbItems();
            int total = 0;
            foreach (var c in Lib.Classes)
            {
                Assert.AreEqual(4, c.EliteSpecs.Count, c.Id + ": 4 Elite-Spezialisierungen");
                foreach (var e in c.EliteSpecs)
                {
                    total++; string who = c.Id + "/" + e.Id;
                    Assert.AreEqual(c.Id, e.ClassId, who);
                    Assert.AreNotEqual(EliteMechanic.None, e.Mechanic, who + ": Mechanik");
                    Assert.IsFalse(string.IsNullOrEmpty(e.MechanicLabel) || string.IsNullOrEmpty(e.MechanicSummary), who + ": Mechanik-Text");
                    // Waffe: 5 Skills (Elementarweber: 5 je Einstimmung), Waffe führbar nur mit Spezialisierung ab Schritt 2
                    Assert.IsNotNull(e.Weapon, who + ": Waffe"); Assert.AreEqual(5, e.Weapon.Skills.Length, who);
                    foreach (var s in e.Weapon.Skills) Assert.IsNotNull(s, who);
                    if (c.Mechanic == ClassMechanicKind.Attunement) { Assert.AreEqual(20, e.Weapon.Attunement.Length, who); foreach (var s in e.Weapon.Attunement) Assert.IsNotNull(s, who); }
                    if (c.Mechanic == ClassMechanicKind.Initiative) { Assert.IsNotNull(e.Weapon.StealthSkill, who + ": Tarnungsangriff"); for (int i = 1; i < 5; i++) Assert.Greater(e.Weapon.Skills[i].InitiativeCost, 0, who + ": Initiative"); }
                    Assert.IsTrue(c.CanWield(e.Weapon.Kind, e, 2) && c.SetFor(e.Weapon.Kind, e, 2) == e.Weapon, who + ": Waffe ab Schritt 2");
                    if (c.SetFor(e.Weapon.Kind) == null) Assert.IsFalse(c.CanWield(e.Weapon.Kind, e, 1), who + ": Waffe vor Schritt 2 gesperrt");
                    Assert.IsTrue(items.Contains(ItemIds.Recruit(e.Weapon.Kind)), who + ": Rekrutenwaffe " + ItemIds.Recruit(e.Weapon.Kind));
                    // Skilltyp: 1 Heil, 3 Hilfe (Stufe 1–3), 1 Elite – alle vom Typ der Spezialisierung
                    Assert.AreEqual(5, e.TypeSkills.Count, who);
                    Assert.AreEqual(1, e.TypeSkills.FindAll(s => s.SlotType == SkillSlotType.Heal).Count, who + ": Heil");
                    var u = e.TypeSkills.FindAll(s => s.SlotType == SkillSlotType.Utility); Assert.AreEqual(3, u.Count, who + ": Hilfe");
                    var tiers = new HashSet<int>(); foreach (var s in u) { tiers.Add(s.Tier); Assert.AreEqual(e.SkillType, s.Category, who + ": Typ " + s.DisplayName); }
                    Assert.AreEqual(3, tiers.Count, who + ": Stufe 1–3");
                    Assert.AreEqual(1, e.TypeSkills.FindAll(s => s.SlotType == SkillSlotType.Elite).Count, who + ": Elite");
                    // Linie: 3 Neben + 9 Haupt mit Text und Wirkung
                    Assert.AreEqual(3, e.Minors.Length, who); Assert.AreEqual(9, e.Majors.Length, who);
                    foreach (var t in e.Minors) { Assert.IsNotNull(t, who); Assert.IsFalse(t.Major); Assert.Greater(t.Modifiers.Count, 0, who + "/" + t.DisplayName); Assert.IsFalse(string.IsNullOrEmpty(t.Description), who); }
                    for (int i = 0; i < 9; i++) { var t = e.Majors[i]; Assert.IsNotNull(t, who); Assert.IsTrue(t.Major); Assert.AreEqual(i / 3 + 1, t.Tier, who); Assert.Greater(t.Modifiers.Count, 0, who + "/" + t.DisplayName); Assert.IsFalse(string.IsNullOrEmpty(t.Description), who); }
                    // Signatur: eigene Palette (Farbe + Sound-Motiv), Mechanik-Skills, Signatur-Skill
                    Assert.IsTrue(palettes.Add(e.Palette), who + ": Palette doppelt " + e.Palette);
                    Assert.IsNotNull(bank.Get(VfxIds.Sfx(e.Palette, "Impact")), who + ": Sound-Motiv");
                    Assert.IsNotNull(vfx.Get(VfxIds.Compose(VfxShape.Aura, e.Palette, VfxSize.L)), who + ": Aura-Signatur");
                    Assert.IsNotNull(FindById(e, e.SignatureSkill), who + ": Signatur-Skill " + e.SignatureSkill);
                }
            }
            Assert.AreEqual(16, total);
        }

        static SkillDefinition FindById(EliteSpecDefinition e, string sig) { foreach (var s in e.AllSkills()) if (s != null && (s.Id == e.ClassId + "_" + sig || s.Id.EndsWith(sig))) return s; return null; }

        static HashSet<string> AssetDbItems()
        {
            var set = new HashSet<string>();
            foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition")) { var d = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)); if (d != null) set.Add(d.Id); }
            return set;
        }

        [Test]
        public void EverySkillOfEveryEliteSpec_HasTimelineAnimationVfxSoundAndTooltip()
        {
            var clips = ClipBuilder.HumanoidSet; var vfx = VfxKitBuilder.Catalog; var bank = AudioBuilder.Bank;
            var missing = new List<string>(); int n = 0;
            foreach (var c in Lib.Classes)
                foreach (var e in c.EliteSpecs)
                    foreach (var s in e.AllSkills())
                    {
                        n++; var tl = s.Timeline; string who = e.Id + "/" + s.DisplayName;
                        if (tl == null || !tl.IsBuilt) { missing.Add(who + ": Zeitachse"); continue; }
                        if (clips.Get(tl.Clip) == null) missing.Add(who + ": Clip " + tl.Clip);
                        foreach (var id in new[] { tl.VfxRelease, tl.VfxImpact, tl.VfxCharge, tl.VfxProjectile }) if (!string.IsNullOrEmpty(id) && vfx.Get(id) == null) missing.Add(who + ": VFX " + id);
                        foreach (var id in new[] { tl.SfxCharge, tl.SfxRelease, tl.SfxImpact }) if (!string.IsNullOrEmpty(id) && bank.Get(id) == null) missing.Add(who + ": SFX " + id);
                        if (string.IsNullOrEmpty(tl.SfxRelease)) missing.Add(who + ": SFX-Auswurf leer");
                        var tip = TooltipBuilder.Skill(s, null);
                        if (tip == null || string.IsNullOrEmpty(tip.Title) || tip.Lines.Count == 0) missing.Add(who + ": Tooltip");
                    }
            Assert.Greater(n, 250, "Elite-Skills gesamt");
            Assert.IsEmpty(missing, string.Join("\n", missing));
        }

        // ------------------------------------------------------------------ Regeln: dritter Platz, nur eine aktiv, Freischaltung
        static SpecializationDefinition Core(string id) { var s = ScriptableObject.CreateInstance<SpecializationDefinition>(); s.Id = id; Fill(s); return s; }
        static EliteSpecDefinition El(string id) { var s = ScriptableObject.CreateInstance<EliteSpecDefinition>(); s.Id = id; Fill(s); return s; }
        static void Fill(SpecializationDefinition s)
        {
            for (int i = 0; i < 3; i++) { var t = ScriptableObject.CreateInstance<TraitDefinition>(); t.Id = s.Id + "_m" + i; s.Minors[i] = t; }
            for (int i = 0; i < 9; i++) { var t = ScriptableObject.CreateInstance<TraitDefinition>(); t.Id = s.Id + "_M" + i; t.Major = true; s.Majors[i] = t; }
        }

        [Test]
        public void Elite_OnlyOneActive_OccupiesThirdSlot_UnlockAtLevel30()
        {
            var a = Core("a"); var b = Core("b"); var c = Core("c"); var x = El("x"); var y = El("y");
            var m = new TraitModel(); m.SetSpecializations(new SpecializationDefinition[] { a, b, c, x, y });
            var p = new ProgressionModel(); p.SetLevel(29);
            var lx = m.Get("x"); var ly = m.Get("y");
            Assert.IsFalse(m.CanUnlockTier(lx, p, out var why)); StringAssert.Contains("30", why);
            Assert.IsFalse(m.SetActiveElite("x", out _), "erst freischalten");
            p.SetLevel(30); int hp = p.AvailableHeroPoints;
            Assert.IsTrue(m.UnlockTier(lx, p, out _)); Assert.AreEqual(hp - EliteSpecDefinition.StepCost, p.AvailableHeroPoints, "Schritt kostet 10");
            Assert.IsTrue(m.UnlockMajor(lx, 1, 2, p, out _)); Assert.AreEqual(hp - EliteSpecDefinition.StepCost, p.AvailableHeroPoints, "Haupteigenschaft im Schritt enthalten");
            Assert.IsTrue(m.UnlockTier(m.Get("c"), p, out _));
            // ohne Elite: drei Kernlinien aktiv
            Assert.IsTrue(m.IsLineActive(m.Get("c"))); Assert.IsFalse(m.IsLineActive(lx));
            Assert.Contains(c.Minors[0], new List<TraitDefinition>(m.ActiveTraits()));
            // Elite belegt Platz 3
            Assert.IsTrue(m.SetActiveElite("x", out _));
            Assert.IsTrue(m.IsLineActive(m.Get("a")) && m.IsLineActive(m.Get("b")));
            Assert.IsFalse(m.IsLineActive(m.Get("c")), "dritte Kernlinie ruht");
            var act = new List<TraitDefinition>(m.ActiveTraits());
            Assert.IsFalse(act.Contains(c.Minors[0])); Assert.IsTrue(act.Contains(x.Minors[0])); Assert.IsTrue(act.Contains(x.Major(1, 2)));
            // nur eine aktiv
            m.UnlockTier(ly, p, out _);
            Assert.IsTrue(m.SetActiveElite("y", out _)); Assert.AreEqual("y", m.ActiveElite);
            Assert.IsFalse(m.IsLineActive(lx)); Assert.IsTrue(m.IsLineActive(ly));
            Assert.IsFalse(m.SetActiveElite("a", out _), "Kernlinie ist keine Elite");
            // ablegen und erstatten
            Assert.IsTrue(m.SetActiveElite(null, out _)); Assert.IsNull(m.ActiveElite); Assert.IsTrue(m.IsLineActive(m.Get("c")));
            m.SetActiveElite("x", out _);
            int refund = m.Refund(p); Assert.AreEqual(2 * EliteSpecDefinition.StepCost + TraitModel.TierCost, refund); Assert.IsNull(m.ActiveElite);
            // drei Schritte, dann vollständig
            for (int i = 0; i < 3; i++) Assert.IsTrue(m.UnlockTier(lx, p, out _));
            Assert.IsFalse(m.CanUnlockTier(lx, p, out _));
            Assert.AreEqual(30, 3 * EliteSpecDefinition.StepCost, "30 Heldenpunkte je Spezialisierung");
        }

        [Test]
        public void Elite_SaveRoundTripAndMissingLineDropsActive()
        {
            var a = Core("a"); var b = Core("b"); var c = Core("c"); var x = El("x");
            var st = new ProfileState(); st.Traits.SetSpecializations(new SpecializationDefinition[] { a, b, c, x });
            st.Progression.SetLevel(30); st.Traits.UnlockTier(st.Traits.Get("x"), st.Progression, out _); st.Traits.SetActiveElite("x", out _);
            var dto = st.ToDto(); Assert.AreEqual("x", dto.EliteSpec);
            var json = JsonUtility.ToJson(dto); var back = JsonUtility.FromJson<SaveGameDto>(json);
            var st2 = new ProfileState(); st2.LoadDto(back, null, new List<SkillDefinition>());
            st2.Traits.SetSpecializations(new SpecializationDefinition[] { a, b, c, x });
            Assert.AreEqual("x", st2.Traits.ActiveElite); Assert.AreEqual(1, st2.Traits.ActiveEliteSteps);
            // Klasse ohne diese Spezialisierung → Elite fällt weg (Platz 3 wieder Kernlinie)
            st2.Traits.SetSpecializations(new SpecializationDefinition[] { a, b, c });
            Assert.IsNull(st2.Traits.ActiveElite);
            // Lauf-4-/M25-Stand ohne Feld
            var old = JsonUtility.FromJson<SaveGameDto>(json.Replace("\"EliteSpec\":\"x\"", "\"EliteSpec\":\"\""));
            var st3 = new ProfileState(); st3.LoadDto(old, null, new List<SkillDefinition>()); Assert.IsNull(st3.Traits.ActiveElite);
        }

        // ------------------------------------------------------------------ Mechanik-Varianten (reine Modelle)
        [Test]
        public void Hainform_FillsByHealing_Avatar15s()
        {
            var h = new HainformModel();
            Assert.IsFalse(h.Enter());
            h.AddHealing(10000f, 10000f); Assert.AreEqual(40f, h.Gauge, 0.01f, "voller Lebensbalken = 40 %");
            h.Tick(10f, true); Assert.AreEqual(55f, h.Gauge, 0.01f, "1,5 %/s im Kampf");
            h.AddHealing(20000f, 10000f); Assert.AreEqual(100f, h.Gauge, 0.01f);
            Assert.IsTrue(h.Enter()); Assert.IsTrue(h.Active);
            h.AddHealing(10000f, 10000f); h.Tick(7.5f, true); Assert.AreEqual(50f, h.Gauge, 0.1f, "leert sich über 15 s");
            h.Tick(8f, true); Assert.IsFalse(h.Active); Assert.AreEqual(HainformModel.Recharge, h.Cooldown, 0.01f);
            h.AddHealing(30000f, 10000f); Assert.IsFalse(h.CanEnter, "Abklingzeit");
        }

        [Test]
        public void Malice_MarkHitsFinisherAndKneel()
        {
            var m = new MaliceModel();
            Assert.IsFalse(m.OnHit(7, 0f), "ohne Markierung keine Bosheit");
            m.Mark(7);
            Assert.IsTrue(m.OnHit(7, 0f)); Assert.IsFalse(m.OnHit(7, 0.2f), "Takt 0,5 s"); Assert.IsFalse(m.OnHit(8, 1f), "anderes Ziel");
            for (int i = 1; i < 10; i++) m.OnHit(7, i);
            Assert.AreEqual(MaliceModel.Max, m.Malice);
            Assert.AreEqual(1.75f, m.Finish(), 0.001f); Assert.AreEqual(0, m.Malice); Assert.AreEqual(-1, m.MarkedId);
            Assert.AreEqual(1f, m.RangeFactor); m.ToggleKneel(); Assert.IsTrue(m.Kneeling); Assert.AreEqual(1.5f, m.RangeFactor);
        }

        [Test]
        public void Overload_After5s_OncePerAttunement_PenaltyOnLeave()
        {
            var o = new OverloadModel();
            o.Tick(4.9f); Assert.IsFalse(o.Ready); o.Tick(0.2f); Assert.IsTrue(o.Ready);
            Assert.IsTrue(o.Use()); Assert.IsFalse(o.Use()); Assert.AreEqual(OverloadModel.ExtraRecharge, o.PenaltyOnLeave);
            o.OnSwap(); Assert.AreEqual(0f, o.PenaltyOnLeave); Assert.IsFalse(o.Ready);
            var a = new AttunementModel(); a.Swap(Element.Water); a.AddRecharge(Element.Fire, o.PenaltyOnLeave + 5f);
            Assert.AreEqual(AttunementModel.Recharge + 5f, a.CooldownOf(Element.Fire), 0.01f);
        }

        [Test]
        public void DualAttunement_SixCombosSymmetric()
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++)
            {
                int k = DualAttunementModel.ComboIndex((Element)i, (Element)j);
                if (i == j) { Assert.AreEqual(-1, k); continue; }
                Assert.AreEqual(k, DualAttunementModel.ComboIndex((Element)j, (Element)i), "symmetrisch"); seen.Add(k);
            }
            Assert.AreEqual(6, seen.Count); foreach (var k in seen) Assert.IsTrue(k >= 0 && k < 6);
            var d = new DualAttunementModel(); Assert.IsFalse(d.IsDual);
            d.Swap(Element.Air); Assert.AreEqual(Element.Air, d.Primary); Assert.AreEqual(Element.Fire, d.Secondary); Assert.AreEqual(1, d.Combo);
            d.Swap(Element.Air); Assert.AreEqual(Element.Fire, d.Secondary, "gleiche Einstimmung ändert nichts");
        }

        [Test]
        public void Shroud_SecondHealthBar_AbsorbDrainExit()
        {
            var s = new ShroudModel();
            s.Gain(40f); Assert.IsFalse(s.Enter(10000f), "ab 50 %");
            s.Gain(60f); Assert.AreEqual(100f, s.Force);
            Assert.IsTrue(s.Enter(10000f)); Assert.AreEqual(6000f, s.ShroudHealth, 0.1f);
            Assert.AreEqual(0f, s.Absorb(1000f)); Assert.AreEqual(5000f, s.ShroudHealth, 0.1f);
            s.Tick(2f); Assert.AreEqual(5000f - 6000f * 0.05f * 2f, s.ShroudHealth, 0.5f, "5 %/s Abbau");
            float rest = s.Absorb(10000f); Assert.Greater(rest, 0f); Assert.IsFalse(s.Active); Assert.AreEqual(ShroudModel.Recharge, s.Cooldown, 0.01f);
            Assert.AreEqual(500f, s.Absorb(500f), "außerhalb: kein Schutz");
        }

        [Test]
        public void Energy_AndAxeRecall()
        {
            var e = new EnergyModel();
            for (int i = 0; i < 40; i++) e.OnHit(i * 0.3f);
            Assert.AreEqual(EnergyModel.Max, e.Energy);
            Assert.IsTrue(e.Sphere()); Assert.IsFalse(e.Sphere(), "Abklingzeit"); e.Tick(10f); Assert.IsTrue(e.Sphere()); Assert.AreEqual(10f, e.Energy, 0.01f);
            e.OnHit(100f); e.OnHit(100.1f); Assert.AreEqual(11f, e.Energy, 0.01f, "Takt 0,25 s");
            var a = new AxeRecallModel(); Assert.IsFalse(a.Recall()); a.Throw(); a.Tick(3f); Assert.IsTrue(a.Recall()); Assert.IsFalse(a.Thrown);
            a.Throw(); a.Tick(6.1f); Assert.IsFalse(a.Thrown, "fällt nach 6 s zurück");
        }
    }
}
