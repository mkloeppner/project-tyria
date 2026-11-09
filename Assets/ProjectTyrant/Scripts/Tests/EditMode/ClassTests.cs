using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 5 / M25: Klassen-Mechaniken (Gefährte, Einstimmung, Initiative, Tarnung, Stehlen), Klassen-Vollständigkeit, Account/Charakter-Speicherstand.</summary>
    public class ClassTests
    {
        static ClassLibrary Lib { get { var l = ClassBuilder.Library; Assert.IsNotNull(l, "ClassLibrary fehlt (Rebuild)"); return l; } }

        [Test]
        public void Attunement_SwapCooldownGlobalAndThemes()
        {
            var m = new AttunementModel(); var log = new List<string>(); m.Swapped += (a, b) => log.Add(a + ">" + b);
            Assert.AreEqual(Element.Fire, m.Current);
            Assert.IsFalse(m.Swap(Element.Fire), "bereits eingestimmt");
            Assert.IsTrue(m.Swap(Element.Water));
            Assert.IsFalse(m.Swap(Element.Air), "globaler Takt 1 s");
            m.Tick(1.01f); Assert.IsTrue(m.Swap(Element.Air));
            m.Tick(1.01f); Assert.IsFalse(m.Swap(Element.Water), "verlassene Einstimmung lädt 10 s");
            Assert.AreEqual(AttunementModel.Recharge - 1.01f, m.CooldownOf(Element.Water), 0.02f);
            m.Tick(9f); Assert.IsTrue(m.Swap(Element.Water));
            CollectionAssert.AreEqual(new[] { "Fire>Water", "Water>Air", "Air>Water" }, log);
            Assert.AreEqual(ComboFieldType.Fire, AttunementModel.FieldOf(Element.Fire)); Assert.AreEqual(VfxPalette.Erde, AttunementModel.PaletteOf(Element.Earth));
        }

        [Test]
        public void Initiative_CostsAndRegeneration()
        {
            var m = new InitiativeModel();
            Assert.AreEqual(12, m.Points);
            Assert.IsTrue(m.Spend(5)); Assert.IsTrue(m.Spend(6)); Assert.AreEqual(1, m.Points);
            Assert.IsFalse(m.CanSpend(3)); Assert.IsFalse(m.Spend(3));
            m.Tick(2.0f); Assert.AreEqual(3, m.Points, "1 Punkt je Sekunde");
            m.Tick(100f); Assert.AreEqual(InitiativeModel.Max, m.Points, "Obergrenze 12");
            Assert.IsTrue(m.CanSpend(0));
        }

        [Test]
        public void Stealth_AttackEndsAndRevealedBlocks()
        {
            var m = new StealthModel();
            Assert.IsTrue(m.Enter(3f)); Assert.IsTrue(m.InStealth);
            m.Tick(1f); Assert.IsTrue(m.Attack(), "Tarnungsangriff"); Assert.IsFalse(m.InStealth); Assert.IsTrue(m.IsRevealed);
            Assert.IsFalse(m.Enter(4f), "Enthüllt verhindert Tarnung");
            m.Tick(3.01f); Assert.IsFalse(m.IsRevealed); Assert.IsTrue(m.Enter(4f));
            m.Tick(4.01f); Assert.IsFalse(m.InStealth, "läuft ab"); Assert.IsFalse(m.Attack(), "kein Tarnungsangriff ohne Tarnung");
        }

        [Test]
        public void Pet_SwapCooldownDeathAndRevive()
        {
            var m = new PetModel(); int summons = 0; m.Summoned += f => summons++;
            Assert.AreEqual(PetFamily.Wolf, m.Current);
            Assert.IsTrue(m.Swap(false)); Assert.AreEqual(PetFamily.Bear, m.Current); Assert.AreEqual(0f, m.SwapRemaining, "außerhalb des Kampfes sofort");
            Assert.IsTrue(m.Swap(true)); Assert.AreEqual(PetModel.SwapCooldown, m.SwapRemaining);
            Assert.IsFalse(m.Swap(true), "20 s Abklingzeit im Kampf");
            m.Died(); Assert.IsTrue(m.Dead); m.Tick(19f); Assert.IsTrue(m.Dead);
            m.Tick(1.1f); Assert.IsFalse(m.Dead, "kehrt nach 20 s zurück");
            Assert.AreEqual(3, summons);
            m.SetPair(PetFamily.Falcon, PetFamily.Wolf); Assert.IsTrue(PetModel.Flying(PetFamily.Falcon)); Assert.Greater(PetModel.HealthFactor(PetFamily.Bear), PetModel.HealthFactor(PetFamily.Falcon));
        }

        [Test]
        public void Steal_RangeCooldownAndFactionSkill()
        {
            var m = new StolenSkillModel();
            Assert.IsFalse(m.Steal("Moor", 30f), "außer Reichweite (22,5 m)");
            Assert.IsTrue(m.Steal("Frost", 10f)); Assert.AreEqual("stolen_frost", m.Held);
            Assert.IsFalse(m.CanSteal(5f, out _), "Abklingzeit");
            Assert.AreEqual("stolen_frost", m.UseHeld()); Assert.IsNull(m.Held);
            m.Tick(StolenSkillModel.StealRecharge); Assert.IsTrue(m.CanSteal(5f, out _));
            var thief = Lib.Get("schattenklinge");
            foreach (var f in new[] { "Moor", "Frost", "Glut", "Beast", "Bandit" }) Assert.IsNotNull(thief.ExtraSkills.Find(s => s.Id == StolenSkillModel.SkillFor(f)), f);
        }

        [Test]
        public void Classes_CompleteLikeTheGuardian()
        {
            Assert.AreEqual(4, Lib.Classes.Count);
            foreach (var c in Lib.Classes)
            {
                if (c.Id != "waechter") Assert.AreEqual(3, c.WeaponSets.Count, c.Id + ": 3 Waffensets");
                int weapon = 0; foreach (var w in c.WeaponSets) weapon += w.Skills.Length;
                if (c.Id != "waechter") Assert.GreaterOrEqual(weapon, 15, c.Id);
                Assert.AreEqual(3, c.SkillPool.FindAll(s => s.SlotType == SkillSlotType.Heal).Count, c.Id + ": 3 Heil");
                var utils = c.SkillPool.FindAll(s => s.SlotType == SkillSlotType.Utility);
                Assert.AreEqual(9, utils.Count, c.Id + ": 9 Hilfe");
                var cats = new HashSet<UtilityCategory>(); foreach (var u in utils) cats.Add(u.Category);
                Assert.AreEqual(3, cats.Count, c.Id + ": 3 Typen");
                foreach (var cat in cats) { var tiers = new HashSet<int>(); foreach (var u in utils) if (u.Category == cat) tiers.Add(u.Tier); Assert.AreEqual(3, tiers.Count, c.Id + " " + cat + ": Tier 1–3"); }
                Assert.AreEqual(2, c.SkillPool.FindAll(s => s.SlotType == SkillSlotType.Elite).Count, c.Id + ": 2 Elite");
                Assert.AreEqual(1, c.SkillPool.FindAll(s => s.SlotType == SkillSlotType.Heal && s.StartsUnlocked).Count, c.Id + ": 1 Heilung frei");
                Assert.GreaterOrEqual(utils.FindAll(s => s.StartsUnlocked).Count, 1, c.Id + ": ≥ 1 Hilfe frei");
                Assert.AreEqual(3, c.Specializations.Count, c.Id + ": 3 Linien");
                foreach (var sp in c.Specializations) { Assert.AreEqual(3, sp.Minors.Length); Assert.AreEqual(9, sp.Majors.Length); foreach (var t in sp.Majors) Assert.IsNotNull(t, sp.Id); }
                if (c.Id != "waechter") { Assert.AreNotEqual(ClassMechanicKind.None, c.Mechanic, c.Id); Assert.IsFalse(string.IsNullOrEmpty(c.ResourceName)); }
                foreach (var k in c.StartWeapons) Assert.IsTrue(c.CanWield(k), c.Id + " Startwaffe " + k);
            }
        }

        [Test]
        public void EverySkillOfEveryClass_HasTimelineAnimationVfxSoundAndTooltip()
        {
            var clips = ClipBuilder.HumanoidSet; var vfx = VfxKitBuilder.Catalog; var bank = AudioBuilder.Bank;
            Assert.IsNotNull(clips); Assert.IsNotNull(vfx); Assert.IsNotNull(bank);
            var missing = new List<string>(); int n = 0;
            foreach (var c in Lib.Classes)
                foreach (var s in c.AllSkills())
                {
                    n++; var tl = s.Timeline; string who = c.Id + "/" + s.DisplayName;
                    if (tl == null || !tl.IsBuilt) { missing.Add(who + ": Zeitachse"); continue; }
                    if (clips.Get(tl.Clip) == null) missing.Add(who + ": Clip " + tl.Clip);
                    foreach (var id in new[] { tl.VfxRelease, tl.VfxImpact, tl.VfxCharge, tl.VfxProjectile }) if (!string.IsNullOrEmpty(id) && vfx.Get(id) == null) missing.Add(who + ": VFX " + id);
                    foreach (var id in new[] { tl.SfxCharge, tl.SfxRelease, tl.SfxImpact }) if (!string.IsNullOrEmpty(id) && bank.Get(id) == null) missing.Add(who + ": SFX " + id);
                    if (string.IsNullOrEmpty(tl.SfxRelease)) missing.Add(who + ": SFX-Auswurf leer");
                    var tip = TooltipBuilder.Skill(s, null);
                    if (tip == null || string.IsNullOrEmpty(tip.Title) || tip.Lines.Count == 0) missing.Add(who + ": Tooltip");
                }
            Assert.Greater(n, 150);
            Assert.IsEmpty(missing, string.Join("\n", missing));
        }

        [Test]
        public void Weaver_TemplatesCoverAllAttunements()
        {
            var w = Lib.Get("elementarweber");
            Assert.AreEqual(ClassMechanicKind.Attunement, w.Mechanic); Assert.IsFalse(w.SwapInCombat);
            var names = new HashSet<string>();
            foreach (var set in w.WeaponSets)
            {
                Assert.AreEqual(20, set.Attunement.Length, set.DisplayName);
                for (int e = 0; e < 4; e++) for (int slot = 0; slot < 5; slot++)
                {
                    var s = set.SkillFor(slot, (Element)e);
                    Assert.AreEqual((Element)e, s.Element); Assert.AreEqual(AttunementModel.PaletteOf((Element)e), s.Palette);
                    Assert.IsTrue(names.Add(s.DisplayName), "Name doppelt: " + s.DisplayName);
                }
            }
            Assert.AreEqual(3, w.SkillPool.FindAll(s => s.Special == SkillSpecial.Conjure && s.ConjureSet != null && s.ConjureSet.Skills.Length == 5).Count);
        }

        [Test]
        public void Thief_InitiativeInsteadOfCooldown_StealthAttackPerSet()
        {
            var t = Lib.Get("schattenklinge");
            foreach (var set in t.WeaponSets)
            {
                Assert.IsNotNull(set.StealthSkill, set.DisplayName); Assert.IsTrue(set.StealthSkill.StealthAttack);
                Assert.AreEqual(0, set.Skills[0].InitiativeCost, "Skill 1 kostenlos");
                for (int i = 1; i < 5; i++) { Assert.Greater(set.Skills[i].InitiativeCost, 0, set.Skills[i].DisplayName); Assert.AreEqual(0f, set.Skills[i].Cooldown, set.Skills[i].DisplayName); }
            }
            Assert.AreNotEqual(t.WeaponSets[0].Skills[2].DisplayName, t.WeaponSets[1].Skills[2].DisplayName, "Doppelwaffen-Skill je Kombination");
        }

        [Test]
        public void Account_MigrationCreateDeleteAndSplit()
        {
            var legacy = new SaveGameDto { Version = 2, Level = 17, Copper = 12345, Karma = 77, CurrentZone = "nebelmoor", MasteryXp = 900 };
            legacy.Mounts.Add("Raptor"); legacy.SelectedMount = "Raptor";
            var acc = AccountModel.MigrateLegacy(legacy);
            Assert.AreEqual(1, acc.Characters.Count); Assert.AreEqual("waechter", acc.Characters[0].ClassId); Assert.AreEqual(17, acc.Characters[0].Level);
            Assert.AreEqual(0, acc.Selected); Assert.AreEqual(12345, acc.Copper); CollectionAssert.Contains(acc.Mounts, "Raptor");
            Assert.AreEqual("waechter", legacy.ClassId, "Charakter 1 = Wächter");

            int s = AccountModel.Create(acc, "Lyra Dornpfad", "wildhueter", "#2E7D4F", "#C8A040", out string why);
            Assert.AreEqual(1, s, why);
            Assert.AreEqual(-1, AccountModel.Create(acc, "lyra dornpfad", "schattenklinge", "#000000", "#FFFFFF", out why), "Name doppelt");
            Assert.AreEqual(-1, AccountModel.Create(acc, "X", "schattenklinge", "#000000", "#FFFFFF", out why), "zu kurz");
            for (int i = 0; i < 3; i++) Assert.GreaterOrEqual(AccountModel.Create(acc, "Held Nummer " + "ABC"[i], "elementarweber", "#000000", "#FFFFFF", out why), 0);
            Assert.AreEqual(-1, AccountModel.FreeSlot(acc)); Assert.AreEqual(-1, AccountModel.Create(acc, "Zu Viele", "waechter", "#000000", "#FFFFFF", out why), "max. 5");
            Assert.IsFalse(AccountModel.Delete(acc, 1, "Lyra", out why), "Name muss genau stimmen");
            Assert.IsTrue(AccountModel.Delete(acc, 1, "Lyra Dornpfad", out why));
            Assert.AreEqual(1, AccountModel.FreeSlot(acc));

            // Account-Ebene gewinnt beim Laden, Charakter-Ebene bleibt eigen
            var ch = AccountModel.NewCharacter(AccountModel.Get(acc, 2));
            Assert.AreEqual("elementarweber", ch.ClassId); Assert.AreEqual(ProfileState.StartZoneId, ch.CurrentZone);
            AccountModel.ApplyAccount(acc, ch);
            Assert.AreEqual(12345, ch.Copper); CollectionAssert.Contains(ch.Mounts, "Raptor");
            ch.Copper = 20000; AccountModel.ExtractAccount(ch, acc); Assert.AreEqual(20000, acc.Copper);
            var json = JsonUtility.ToJson(acc); var back = JsonUtility.FromJson<AccountDto>(json);
            Assert.AreEqual(acc.Characters.Count, back.Characters.Count); Assert.AreEqual(AccountDto.CurrentVersion, back.Version);
        }

        [Test]
        public void Equipment_ClassBindingForWeapons()
        {
            var eq = new EquipmentModel();
            var bow = ScriptableObject.CreateInstance<ItemDefinition>(); bow.Type = ItemType.Weapon; bow.Weapon = WeaponKind.Longbow; bow.RequiredLevel = 1;
            var old = EquipmentModel.WeaponAllowed;
            try
            {
                var guardian = Lib.Get("waechter"); var ranger = Lib.Get("wildhueter");
                EquipmentModel.WeaponAllowed = k => guardian.CanWield(k);
                Assert.IsFalse(eq.CanEquip(bow, EquipSlot.Weapon1, 10, out string why)); StringAssert.Contains("Klasse", why);
                EquipmentModel.WeaponAllowed = k => ranger.CanWield(k);
                Assert.IsTrue(eq.CanEquip(bow, EquipSlot.Weapon1, 10, out why), why);
                Assert.IsTrue(Lib.Get("elementarweber").CanWield(WeaponKind.Staff) && guardian.CanWield(WeaponKind.Staff), "Stab: Wächter und Elementarweber");
            }
            finally { EquipmentModel.WeaponAllowed = old; Object.DestroyImmediate(bow); }
        }

        [Test]
        public void Attunement_RejectsOutOfRangeElement()
        {
            // F-Tasten liefern 0–3; ein fehlerhafter Index (z. B. 4) darf keine Exception werfen (Fund aus dem Klassen-Duell).
            var a = new AttunementModel();
            Assert.IsFalse(a.CanSwap((Element)4, out var why)); Assert.IsNotNull(why);
            Assert.IsFalse(a.Swap((Element)7));
            Assert.AreEqual(0f, a.CooldownOf((Element)4));
            Assert.AreEqual(Element.Fire, a.Current);
        }
    }
}
