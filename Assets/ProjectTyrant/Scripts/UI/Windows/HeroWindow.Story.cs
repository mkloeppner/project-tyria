using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Heldenfenster-Tab „Geschichte“ – Kapitel-Liste, aktueller Schritt mit Zielen, Tagebuch in den Worten des Helden, Figurengalerie, Kapitel wiederspielen.</summary>
    public partial class HeroWindow
    {
        public RectTransform StoryPage { get; private set; }
        RectTransform _chapterList, _galleryList;
        TextMeshProUGUI _stepText, _journalText;
        int _storyIndex = -1;
        int _storyVersion = -1;

        void BuildStoryTab()
        {
            StoryPage = AddTab("Geschichte");
            _storyIndex = _tabs.Count - 1;
            // Drei scrollbare Spalten: Kapitel · aktueller Schritt + Tagebuch · Figuren (26 Kapitel passen nicht mehr auf eine Seite)
            UiScroll.List("Chapters", StoryPage, out _chapterList, 3, 2).Place(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(300, -10));
            UiScroll.List("StepAndJournal", StoryPage, out var middle, 10, 8).Stretch(320, 320, 5, 5);
            _stepText = UiScroll.ListText("Step", middle, "", 16, Color.white);
            _journalText = UiScroll.ListText("Journal", middle, "", 15, new Color(0.88f, 0.85f, 0.78f));
            UiScroll.List("Gallery", StoryPage, out _galleryList, 4, 4).Place(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(300, -10));
        }

        public void SelectStory() { if (_storyIndex >= 0) Select(_storyIndex); }

        void RefreshStory()
        {
            var d = GameManager.Instance != null ? GameManager.Instance.Story : null;
            if (d == null || d.Database == null || d.State == null) { _stepText.text = "Keine Geschichte geladen."; return; }
            var s = d.State; int lvl = PlayerProfile.Instance != null ? PlayerProfile.Instance.State.Progression.Level : 1;
            int ver = s.Chapter * 100000 + s.Step * 1000 + s.Counters.Count * 10 + s.CompletedChapters.Count + lvl * 7;
            if (ver == _storyVersion) return; _storyVersion = ver;
            UiFactory.Clear(_chapterList);
            foreach (var ch in d.Database.Chapters)
            {
                bool done = s.CompletedChapters.Contains(ch.Number), cur = !s.Finished && ch.Number == s.Chapter;
                string mark = done ? "<color=#7CE38B>√</color>" : cur ? "<color=#7CE38B>►</color>" : "<color=#777777>·</color>";
                var chc = ch;
                var b = UiFactory.Button("Ch" + ch.Number, _chapterList, mark + " <b>" + ch.Number + "</b>  " + ch.Title + "\n<size=12><color=#AAAAAA>" + StoryDirector.ZoneName(ch.Zone) + " · ab Stufe " + ch.MinLevel + (done ? " · wiederspielen" : "") + "</color></size>", () => { if (done) Replay(d, chc); }, 15);
                UiFactory.Layout(b, 46);
                var lbl = b.GetComponentInChildren<TextMeshProUGUI>(); if (lbl != null) lbl.alignment = TextAlignmentOptions.Left;
                b.GetComponent<Image>().color = cur ? new Color(0.16f, 0.32f, 0.18f) : UiFactory.ButtonColor;
            }
            var st = d.Step; var chapter = d.Chapter;
            var sb = new System.Text.StringBuilder();
            if (s.Finished) sb.Append("<b>Die Chronik des Tyrannen ist erzählt.</b>\n");
            else if (st != null && chapter != null)
            {
                sb.Append("<color=#7CE38B><b>Kapitel " + chapter.Number + ": " + chapter.Title + "</b></color>\n<b>" + st.Title + "</b>  <color=#AAAAAA>(" + StoryDirector.ZoneName(st.Zone) + ")</color>\n" + st.Description + "\n\n");
                foreach (var l in d.ObjectiveLines()) sb.Append(l).Append('\n');
                if (!StoryModel.Available(d.Database, s, lvl)) sb.Append("\n<color=#FF8866>Ab Stufe " + chapter.MinLevel + "</color>");
            }
            sb.Append("\n<size=13><color=#AAAAAA>Herkunft: " + s.OriginE + " · Charakterzug: " + s.TraitE + " · Leitstern: " + s.LodestarE + (s.PactE != Pact.None ? " · Bündnis: " + s.PactE : "") + " · Späher gesprochen: " + s.Scouts.Count + "</color></size>");
            _stepText.text = sb.ToString();
            var j = StoryModel.Journal(d.Database, s);
            _journalText.text = "<b>Tagebuch</b>\n" + (j.Count == 0 ? "<i>Noch nichts geschrieben.</i>" : string.Join("\n\n", j.ToArray()));
            UiFactory.Clear(_galleryList);
            var head = UiFactory.Text("GalleryHead", _galleryList, "<b>Figuren</b>", 16, TextAlignmentOptions.Left, UiFactory.AccentColor); UiFactory.Layout(head, 24);
            int maxCh = s.Finished ? 99 : s.Chapter;
            foreach (var sp in d.Database.Cast.Speakers)
            {
                if (sp.Id == "held" || sp.UnlockChapter > maxCh || sp.UnlockChapter <= 0) continue;
                var t = UiFactory.Text("Fig_" + sp.Id, _galleryList, "<color=#" + ColorUtility.ToHtmlStringRGB(sp.Color) + "><b>" + sp.DisplayName + "</b></color> <size=12>– " + sp.Role + "</size>\n<size=12>" + sp.Bio + "</size>", 14, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.9f, 0.9f));
            }
        }

        /// <summary>Kapitel wiederspielen: die Szenen des Kapitels in Reihenfolge (ohne Belohnung, jederzeit überspringbar).</summary>
        void Replay(StoryDirector d, StoryChapter ch)
        {
            var ids = new List<string>();
            foreach (var st in StoryModel.StepsFor(ch, d.State)) { if (!string.IsNullOrEmpty(st.SceneStart)) ids.Add(st.SceneStart); foreach (var o in st.Objectives) if (!string.IsNullOrEmpty(o.Scene)) ids.Add(o.Scene); if (!string.IsNullOrEmpty(st.SceneEnd)) ids.Add(st.SceneEnd); }
            Window.Close();
            PlayChain(d, ids, 0);
        }

        static void PlayChain(StoryDirector d, List<string> ids, int i) { if (i >= ids.Count) return; d.PlayScene(ids[i], () => PlayChain(d, ids, i + 1)); }
    }
}
