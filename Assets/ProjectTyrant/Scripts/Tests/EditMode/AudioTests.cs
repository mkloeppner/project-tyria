using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 5 / M24: Audio-Analyse (Spitze, RMS, DC, Naht, Spektrum), Komponist, Mixer, Stimmen, Musik-Zustände, Vollständigkeit, Optionen/Menü.</summary>
    public class AudioTests
    {
        static SoundBank Bank { get { var b = AudioBuilder.Bank; Assert.IsNotNull(b, "SoundBank fehlt (Rebuild)"); return b; } }

        static float[] Read(AudioClip c) { Assert.IsNotNull(c); return WavIO.Read(AssetDatabase.GetAssetPath(c), out _); }

        [Test]
        public void EveryClip_PeakRmsDcAndDuration_WithinCategory()
        {
            var bad = new List<string>();
            foreach (var e in Bank.Entries)
            {
                var spec = SoundCategories.Spec(e.Category);
                foreach (var c in e.Clips)
                {
                    var st = AudioAnalysis.Analyze(Read(c));
                    if (st.PeakDb > -0.95f) bad.Add(c.name + " Spitze " + st.PeakDb.ToString("0.0"));
                    if (Mathf.Abs(st.RmsDb - spec.TargetRmsDb) > 3f) bad.Add(c.name + " RMS " + st.RmsDb.ToString("0.0") + " (Ziel " + spec.TargetRmsDb + ")");
                    if (Mathf.Abs(st.Dc) > 0.002f) bad.Add(c.name + " DC " + st.Dc.ToString("0.0000"));
                    if (st.Seconds < spec.MinSeconds * 0.9f || st.Seconds > spec.MaxSeconds * 1.1f) bad.Add(c.name + " Dauer " + st.Seconds.ToString("0.00"));
                }
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void SpectralCentroid_InCategoryBand_AndBassStemsLow()
        {
            var bad = new List<string>();
            foreach (var e in Bank.Entries)
            {
                var spec = SoundCategories.Spec(e.Category);
                foreach (var st in e.Stats) if (st.CentroidHz < spec.MinCentroid || st.CentroidHz > spec.MaxCentroid) bad.Add(e.Id + " " + st.CentroidHz.ToString("0") + " Hz");
            }
            foreach (var m in Bank.Music)
            {
                var bass = AudioAnalysis.SpectralCentroid(Read(m.Stems[(int)Stem.Bass]));
                if (bass >= 300f) bad.Add(m.Key + " Bass-Stem " + bass.ToString("0") + " Hz");
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void Loops_EndsArePhaseContinuous()
        {
            var bad = new List<string>();
            foreach (var e in Bank.Entries) if (e.Loop) foreach (var c in e.Clips) { float s = AudioAnalysis.LoopSeam(Read(c)); if (s >= 0.01f) bad.Add(c.name + " " + s.ToString("0.0000")); }
            foreach (var m in Bank.Music) foreach (var c in m.Stems) { float s = AudioAnalysis.LoopSeam(Read(c)); if (s >= 0.01f) bad.Add(c.name + " " + s.ToString("0.0000")); }
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void MusicStems_SameLength_LoopsBetween60And90Seconds()
        {
            Assert.AreEqual(22, Bank.Music.Count, "10 Zonen × (Erkundung, Kampf) + Boss + Menü (Lauf 6: sechs neue Zonen)");
            foreach (var m in Bank.Music)
            {
                int n = -1;
                foreach (var c in m.Stems) { var d = Read(c); if (n < 0) n = d.Length; Assert.AreEqual(n, d.Length, m.Key + ": Stems nicht synchron"); }
                float sec = n / (float)Synth.Rate;
                Assert.That(sec, Is.InRange(59f, 91f), m.Key);
                float bar = 240f / m.Bpm; Assert.AreEqual(m.Bars * bar, sec, 0.01f, m.Key + ": ganze Takte");
            }
            Assert.AreEqual(10 * 7, Bank.Stingers.Count, "7 Stinger je Zone (10 Zonen seit Lauf 6)");
            foreach (var s in Bank.Stingers) Assert.That(s.Stats.Seconds, Is.InRange(2f, 4.2f), s.Key);
        }

        [Test]
        public void Composer_NotesStayInZoneMode_AndTempoPerState()
        {
            foreach (var z in new[] { "sonnfurt", "nebelmoor", "frostkamm", "gluetoede", "boss", "menu" })
            {
                var zm = ZoneMusic.For(z);
                foreach (MusicKind k in System.Enum.GetValues(typeof(MusicKind)))
                {
                    var song = MusicComposer.Compose(zm, k);
                    foreach (var n in song.Notes) if (n.Stem != Stem.Percussion) Assert.IsTrue(MusicComposer.InScale(zm, n.Midi), z + "/" + k + ": Note " + n.Midi + " nicht in " + zm.Mode);
                    if (k == MusicKind.Explore && z != "boss" && z != "menu") Assert.That(song.Bpm, Is.InRange(70f, 90f));
                    if (k == MusicKind.Combat && z != "boss" && z != "menu") Assert.That(song.Bpm, Is.InRange(120f, 140f));
                    Assert.AreEqual(0, song.Bars % 4, "ganze Phrasen");
                }
            }
            Assert.AreEqual(MusicMode.Lydian, ZoneMusic.For("sonnfurt").Mode);
            Assert.AreEqual(MusicMode.Dorian, ZoneMusic.For("nebelmoor").Mode);
            Assert.AreEqual(MusicMode.Aeolian, ZoneMusic.For("frostkamm").Mode); Assert.IsTrue(ZoneMusic.For("frostkamm").OpenFifths);
            Assert.AreEqual(MusicMode.Phrygian, ZoneMusic.For("gluetoede").Mode);
        }

        [Test]
        public void Composer_MainThemeRecursInEveryZone()
        {
            foreach (var z in SoundRecipes.Zones)
            {
                var zm = ZoneMusic.For(z); var song = MusicComposer.Compose(zm, MusicKind.Explore);
                var mel = song.Notes.FindAll(n => n.Stem == Stem.Melody && n.Bar < 4);
                Assert.GreaterOrEqual(mel.Count, MusicComposer.ThemeDegrees.Length, z);
                for (int i = 0; i < MusicComposer.ThemeDegrees.Length; i++) Assert.AreEqual(MusicComposer.Degree(zm, MusicComposer.ThemeDegrees[i], 1), mel[i].Midi, z + ": Thema Ton " + i);
            }
        }

        [Test]
        public void Mixer_HierarchySnapshotsAndHierarchyOfLevels()
        {
            Assert.AreEqual(MixGroup.Effects, MixerModel.Parent(MixGroup.Combat));
            Assert.AreEqual(MixGroup.Effects, MixerModel.Parent(MixGroup.Movement));
            Assert.AreEqual(MixGroup.Master, MixerModel.Parent(MixGroup.Music));
            Assert.IsNull(MixerModel.Parent(MixGroup.Master));
            var m = new MixerModel();
            foreach (var n in new[] { MixerModel.Erkundung, MixerModel.Kampf, MixerModel.Boss, MixerModel.Menue, MixerModel.Downed, MixerModel.Unterwasser }) Assert.IsTrue(m.Snapshots.ContainsKey(n), n);
            m.Update(1f);
            float exploreMusic = m.GainDb(MixGroup.Music);
            Assert.GreaterOrEqual(SoundCategories.Spec(SoundCategory.Music).TargetRmsDb + exploreMusic - (SoundCategories.Spec(SoundCategory.UI).TargetRmsDb + m.GainDb(MixGroup.UI)), 18f, "UI ≥ 18 dB unter Musik");
            m.TransitionTo(MixerModel.Kampf, 0.5f); m.Update(0.25f); m.Update(0.5f);
            Assert.Greater(m.GainDb(MixGroup.Music), exploreMusic);
            m.TransitionTo(MixerModel.Menue, 0.2f); m.Update(1f);
            Assert.Less(m.Cutoff(MixGroup.Ambience), 1000f, "Menü: Tiefpass auf Welt");
            Assert.Less(m.GainDb(MixGroup.Music), exploreMusic, "Menü: Musik leiser");
            m.TransitionTo(MixerModel.Downed, 0.2f); m.Update(1f);
            Assert.Less(m.Cutoff(MixGroup.Combat), 1000f, "Downed dumpf");
            Assert.AreEqual(MixerModel.Open, m.Cutoff(MixGroup.UI));
        }

        [Test]
        public void Mixer_DuckingAndUserVolumes()
        {
            var m = new MixerModel(); m.Update(1f);
            float music = m.GainDb(MixGroup.Music), fx = m.GainDb(MixGroup.Combat);
            m.DuckForStinger(3f); m.Update(0.1f);
            Assert.AreEqual(music + MixerModel.StingerDuckDb, m.GainDb(MixGroup.Music), 0.05f);
            m.MenuOpen = true; for (int i = 0; i < 30; i++) m.Update(0.1f);
            Assert.AreEqual(fx + MixerModel.MenuDuckDb, m.GainDb(MixGroup.Combat), 0.2f);
            Assert.AreEqual(music, m.GainDb(MixGroup.Music), 0.05f, "Stinger-Ducking endet");
            m.SetUserVolume(MixGroup.Effects, 0f);
            Assert.AreEqual(0f, m.Gain(MixGroup.Combat)); Assert.AreEqual(0f, m.Gain(MixGroup.Movement));
            m.SetUserVolume(MixGroup.Master, 0.5f);
            Assert.AreEqual(Synth.LinToDb(0.25f), m.GainDb(MixGroup.Music) - music, 0.1f);
        }

        [Test]
        public void Voices_PriorityStealing_AndTotalBudget()
        {
            var a = new VoiceAllocator(3);
            Assert.AreEqual(0, a.Request(VoicePriority.Step, 0f, 1f));
            Assert.AreEqual(1, a.Request(VoicePriority.Step, 0.1f, 1f));
            Assert.AreEqual(2, a.Request(VoicePriority.Hit, 0.2f, 1f));
            Assert.AreEqual(0, a.Request(VoicePriority.PlayerSkill, 0.3f, 1f), "Spieler-Skill stiehlt den ältesten Schritt");
            Assert.AreEqual(1, a.Request(VoicePriority.Step, 0.35f, 1f), "gleiche Priorität ersetzt die älteste gleiche");
            var full = new VoiceAllocator(1); full.Request(VoicePriority.PlayerSkill, 0f, 5f);
            Assert.AreEqual(-1, full.Request(VoicePriority.AmbientDetail, 1f, 1f), "Ambient-Detail entfällt");
            Assert.AreEqual(0, full.Request(VoicePriority.Step, 6f, 1f), "abgelaufene Stimme ist frei");
            int total = AudioPool.EffectVoices + AudioPool.UiVoices + MusicDirector.Channels + AmbienceDirector.Channels;
            Assert.LessOrEqual(total, AudioPool.MaxVoices); Assert.AreEqual(32, AudioPool.MaxVoices);
        }

        [Test]
        public void RoundRobin_NeverRepeatsVariantBackToBack()
        {
            var rr = new RoundRobin(3); int last = -1;
            for (int i = 0; i < 200; i++) { int v = rr.Next("x", 3); Assert.AreNotEqual(last, v); Assert.That(v, Is.InRange(0, 2)); last = v; }
            Assert.AreEqual(0, rr.Next("y", 1));
        }

        [Test]
        public void MusicState_CombatAfter3s_ReleaseAfter8s_BossAndMenu()
        {
            var m = new MusicStateModel();
            for (int i = 0; i < 25; i++) m.Update(0.1f, true, false, false, false);
            Assert.AreEqual(MusicState.Explore, m.State, "2,5 s: noch Erkundung");
            for (int i = 0; i < 6; i++) m.Update(0.1f, true, false, false, false);
            Assert.AreEqual(MusicState.Combat, m.State);
            for (int i = 0; i < 79; i++) m.Update(0.1f, false, false, false, false);
            Assert.AreEqual(MusicState.Combat, m.State, "7,9 s nach Kampfende: noch Kampf");
            for (int i = 0; i < 3; i++) m.Update(0.1f, false, false, false, false);
            Assert.AreEqual(MusicState.Explore, m.State);
            for (int i = 0; i < 40; i++) m.Update(0.1f, true, true, false, false);
            Assert.AreEqual(MusicState.Boss, m.State);
            m.Update(0.1f, true, true, true, false); Assert.AreEqual(MusicState.Menu, m.State);
            Assert.AreEqual(0f, MusicDirector.StemLevel(Stem.Melody, MusicState.Death, 1f));
            Assert.Greater(MusicDirector.StemLevel(Stem.Percussion, MusicState.Combat, 1f), MusicDirector.StemLevel(Stem.Percussion, MusicState.Explore, 1f));
        }

        [Test]
        public void Completeness_EverySkillUiElementAndGameEventHasASound()
        {
            var bank = Bank; var missing = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:SkillDefinition"))
            {
                var s = AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var tl = s != null ? s.Timeline : null; if (tl == null || !tl.IsBuilt) continue;
                foreach (var id in new[] { tl.SfxCharge, tl.SfxRelease, tl.SfxImpact }) if (!string.IsNullOrEmpty(id) && bank.Get(id) == null) missing.Add(s.name + " → " + id);
            }
            foreach (var u in SoundRecipes.UiIds) if (bank.Get("UI/" + u) == null) missing.Add("UI/" + u);
            foreach (var id in GameSoundMap.FixedEvents) if (bank.Get(id) == null) missing.Add(id);
            foreach (var kv in GameSoundMap.Banners) if (bank.Get(kv.Value.Sfx) == null) missing.Add("Banner " + kv.Key);
            foreach (var kv in GameSoundMap.Notifications) if (bank.Get(kv.Value.Sfx) == null) missing.Add("Hinweis " + kv.Key);
            foreach (var s in SoundRecipes.Surfaces) if (bank.Get("Step/" + s) == null) missing.Add("Step/" + s);
            foreach (EffectType e in System.Enum.GetValues(typeof(EffectType))) if (bank.Get("Effect/" + e) == null) missing.Add("Effect/" + e);
            foreach (ItemRarity r in System.Enum.GetValues(typeof(ItemRarity))) if (bank.Get("Loot/" + r) == null) missing.Add("Loot/" + r);
            foreach (var f in SoundRecipes.Factions) foreach (var k in new[] { "Attack", "Hurt", "Death" }) if (bank.Get("Vox/" + f + "/" + k) == null) missing.Add("Vox/" + f + "/" + k);
            foreach (var z in SoundRecipes.Zones) { if (bank.Get("Amb/" + z) == null) missing.Add("Amb/" + z); if (bank.GetMusic(z + "/Explore") == null || bank.GetMusic(z + "/Combat") == null) missing.Add("Musik " + z); }
            foreach (var z in new[] { "sonnfurt", "nebelmoor", "frostkamm", "gluetoede" }) foreach (var id in new[] { AmbienceDirector.PickOneShot(z, false), AmbienceDirector.PickOneShot(z, true) }) if (bank.Get(id) == null) missing.Add(id);
            foreach (StingerKind k in System.Enum.GetValues(typeof(StingerKind))) foreach (var z in SoundRecipes.Zones) if (bank.GetStinger(z + "/" + k) == null) missing.Add("Stinger " + z + "/" + k);
            Assert.IsEmpty(missing, string.Join("\n", missing));
        }

        [Test]
        public void Options_RoundTripClampAndMenuStateMachine()
        {
            var o = new GameOptions { Master = 2f, Music = 0.3f, CameraSensitivity = 9f, EffectDensity = 5, HitStop = false, ColorblindTelegraphs = true };
            var back = GameOptions.FromJson(o.ToJson());
            Assert.AreEqual(1f, back.Master); Assert.AreEqual(0.3f, back.Music, 1e-4f); Assert.AreEqual(2.5f, back.CameraSensitivity);
            Assert.AreEqual(2, back.EffectDensity); Assert.IsFalse(back.HitStop); Assert.IsTrue(back.ColorblindTelegraphs);

            var m = new MenuStateModel(); var seen = new List<MenuState>(); m.StateChanged += s => seen.Add(s);
            m.Escape(true); Assert.AreEqual(MenuState.Game, m.State, "offenes Fenster: Esc schließt erst das Fenster");
            m.Escape(false); Assert.AreEqual(MenuState.Menu, m.State);
            m.OpenOptions(); Assert.AreEqual(MenuState.Options, m.State);
            m.Escape(false); Assert.AreEqual(MenuState.Menu, m.State, "Optionen → zurück ins Menü");
            m.Escape(false); Assert.AreEqual(MenuState.Game, m.State);
            CollectionAssert.AreEqual(new[] { MenuState.Menu, MenuState.Options, MenuState.Menu, MenuState.Game }, seen);
        }

        [Test]
        public void SoundMap_SurfacesHitMaterialsAndBanners()
        {
            Assert.AreEqual("Snow", GameSoundMap.Surface("Terrain", "frostkamm", false));
            Assert.AreEqual("Wood", GameSoundMap.Surface("Bridge_Plank", "sonnfurt", false));
            Assert.AreEqual("Stone", GameSoundMap.Surface("RuinWall", "nebelmoor", false));
            Assert.AreEqual("Water", GameSoundMap.Surface("Terrain", "gluetoede", true));
            Assert.AreEqual("Ash", GameSoundMap.Surface(null, "gluetoede", false));
            Assert.AreEqual("Hit/Elemental", GameSoundMap.HitMaterial(VfxPalette.Frost, false, false));
            Assert.AreEqual("Hit/Flesh", GameSoundMap.HitMaterial(null, true, false));
            Assert.AreEqual("Hit/Armor", GameSoundMap.HitMaterial(VfxPalette.Blut, false, true));
            Assert.AreEqual(StingerKind.Victory, GameSoundMap.ForBanner("Ereignis erfolgreich").Value.Stinger);
            Assert.AreEqual("Prog/LevelUp", GameSoundMap.ForBanner("Stufe 12").Value.Sfx);
            Assert.AreEqual(StingerKind.Travel, GameSoundMap.ForNotification("Gereist: Nebelmoor").Value.Stinger);
            Assert.IsNull(GameSoundMap.ForNotification("irgendwas"));
            Assert.AreEqual(-4f, SkillFx.TeamVolumeDb(Team.Enemy)); Assert.AreEqual(0f, SkillFx.TeamVolumeDb(Team.Player));
        }

        [Test]
        public void Synth_DspBasics_Deterministic()
        {
            var a = SoundRecipes.Render(SoundRecipes.Find("Hit/Flesh"), 1); var b = SoundRecipes.Render(SoundRecipes.Find("Hit/Flesh"), 1);
            CollectionAssert.AreEqual(a, b, "gleicher Seed → gleicher Klang");
            var c = SoundRecipes.Render(SoundRecipes.Find("Hit/Flesh"), 2); Assert.AreNotEqual(a.Length == c.Length && System.Linq.Enumerable.SequenceEqual(a, c), true, "Varianten unterscheiden sich");
            var sine = Synth.Buffer(1f); Synth.AddTone(sine, 0, 1f, Synth.Wave.Sine, t => 1000f, 0.5f, new Adsr(0f, 0f, 1f, 0f));
            Assert.AreEqual(1000f, AudioAnalysis.SpectralCentroid(sine), 60f);
            var lp = (float[])sine.Clone(); Synth.Filter(lp, Synth.FilterType.LowPass, 200f);
            Assert.Less(AudioAnalysis.Analyze(lp).RmsDb, AudioAnalysis.Analyze(sine).RmsDb - 12f, "Tiefpass dämpft 1 kHz");
            Synth.Normalize(sine, -12f, -1f); var st = AudioAnalysis.Analyze(sine);
            Assert.AreEqual(-12f, st.RmsDb, 0.6f); Assert.LessOrEqual(st.PeakDb, -0.99f);
        }
    }
}
