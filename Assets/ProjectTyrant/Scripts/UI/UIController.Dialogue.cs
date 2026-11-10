using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Untertitel, Letterbox, Antwortoptionen (Charme/Würde/Wildheit-Icons), Sprechblase, „Überspringen?“, Chat-Log mit Emote-Eingabe.</summary>
    public partial class UIController
    {
        RectTransform _dlgRoot, _letterTop, _letterBottom, _subPanel, _choicePanel, _bubble, _skipPanel, _chatRoot;
        TextMeshProUGUI _subName, _subText, _subHint, _bubbleName, _bubbleText;
        Transform _bubbleAnchor; float _bubbleTimer;
        Action<bool> _skipAnswer;
        bool _hudWasHidden;
        readonly List<Button> _choiceButtons = new List<Button>();
        readonly List<(string text, float time)> _chat = new List<(string, float)>();
        TextMeshProUGUI _chatText; TMP_InputField _chatInput;
        public static int SubtitleSize = 26;   // Option „Untertitel-Größe“

        DialoguePlayer DlgPlayer => GameManager.Instance != null ? GameManager.Instance.Dialogue : null;

        void BuildDialogueUi()
        {
            _dlgRoot = UiFactory.Rect("Dialogue", OverlayLayer).Stretch();
            _letterTop = UiFactory.Image("LetterTop", _dlgRoot, Color.black).rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 110));
            _letterBottom = UiFactory.Image("LetterBottom", _dlgRoot, Color.black).rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 110));
            var sp = UiFactory.Image("Subtitle", _dlgRoot, new Color(0.02f, 0.02f, 0.03f, 0.72f), UiFactory.Sprite("ui_round"), true);
            _subPanel = sp.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(1180, 132));
            _subName = UiFactory.Text("Name", _subPanel, "", 22, TextAlignmentOptions.TopLeft, Color.white); _subName.rectTransform.Stretch(22, 22, 10, 96); _subName.fontStyle = FontStyles.Bold;
            _subText = UiFactory.Text("Text", _subPanel, "", SubtitleSize, TextAlignmentOptions.TopLeft, Color.white); _subText.rectTransform.Stretch(22, 22, 40, 28);
            _subHint = UiFactory.Text("Hint", _subPanel, "Leertaste/Klick: weiter · Esc: überspringen", 14, TextAlignmentOptions.BottomRight, new Color(0.7f, 0.7f, 0.75f)); _subHint.rectTransform.Stretch(22, 16, 100, 6);
            sp.gameObject.AddComponent<Button>().onClick.AddListener(() => DlgPlayer?.Next());
            _choicePanel = UiFactory.Rect("Choices", _dlgRoot).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 172), new Vector2(760, 200));
            var v = _choicePanel.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = 6; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false; v.childAlignment = TextAnchor.LowerCenter;
            var bb = UiFactory.Image("Bubble", _dlgRoot, new Color(0.97f, 0.95f, 0.88f, 0.95f), UiFactory.Sprite("ui_round"));
            _bubble = bb.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(420, 96));
            _bubbleName = UiFactory.Text("Name", _bubble, "", 16, TextAlignmentOptions.TopLeft, new Color(0.2f, 0.2f, 0.2f)); _bubbleName.rectTransform.Stretch(14, 14, 6, 70); _bubbleName.fontStyle = FontStyles.Bold;
            _bubbleText = UiFactory.Text("Text", _bubble, "", 17, TextAlignmentOptions.TopLeft, new Color(0.08f, 0.08f, 0.08f)); _bubbleText.rectTransform.Stretch(14, 14, 28, 8);
            var sk = UiFactory.Image("Skip", _dlgRoot, UiFactory.PanelColor, UiFactory.Sprite("ui_panel"), true);
            _skipPanel = sk.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 150));
            UiFactory.Text("Q", _skipPanel, "Szene überspringen?", 22, TextAlignmentOptions.Center, UiFactory.AccentColor).rectTransform.Stretch(10, 10, 14, 80);
            var yes = UiFactory.Button("Yes", _skipPanel, "Überspringen", () => AnswerSkip(true), 17); ((RectTransform)yes.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 16), new Vector2(180, 44));
            var no = UiFactory.Button("No", _skipPanel, "Weiter ansehen", () => AnswerSkip(false), 17); ((RectTransform)no.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 16), new Vector2(180, 44));
            _dlgRoot.gameObject.SetActive(true);
            SetDialogueVisible(false);
            BuildChat();
        }

        void SetDialogueVisible(bool on)
        {
            _letterTop.gameObject.SetActive(false); _letterBottom.gameObject.SetActive(false);
            _subPanel.gameObject.SetActive(on); _choicePanel.gameObject.SetActive(false); _bubble.gameObject.SetActive(false); _skipPanel.gameObject.SetActive(false);
        }

        public void DialogueOpen(SceneForm form, bool letterbox)
        {
            SetDialogueVisible(form != SceneForm.Bubble);
            _letterTop.gameObject.SetActive(letterbox); _letterBottom.gameObject.SetActive(letterbox);
            // Untertitel über dem unteren Balken statt halb darin (Letterbox 110 px), Antworten rücken mit
            _subPanel.anchoredPosition = new Vector2(0f, letterbox ? 122f : 28f); _choicePanel.anchoredPosition = new Vector2(0f, letterbox ? 266f : 172f);
            if (form != SceneForm.Bubble) { _hudWasHidden = HudHidden; SetHudHidden(true); }
            _subText.fontSize = SubtitleSize;
        }

        public void DialogueClose()
        {
            bool wasBubble = DlgPlayer != null && DlgPlayer.CurrentScene != null && DlgPlayer.CurrentScene.Form == SceneForm.Bubble;
            SetDialogueVisible(false);
            if (!wasBubble) SetHudHidden(_hudWasHidden);
            _bubbleTimer = 4f;
        }

        public void DialogueLine(string name, Color color, string text, SceneForm form, Transform anchor)
        {
            if (form == SceneForm.Bubble)
            {
                _bubble.gameObject.SetActive(true); _bubbleName.text = name; _bubbleName.color = color * 0.6f + Color.black * 0.4f; _bubbleText.text = text; _bubbleAnchor = anchor; _bubbleTimer = 99f;
                return;
            }
            _subName.text = name; _subName.color = color; _subText.text = text;
        }

        public void DialogueChoices(DialogueChoice ch, StoryTextTable t, StoryState s)
        {
            foreach (var b in _choiceButtons) if (b != null) Destroy(b.gameObject);
            _choiceButtons.Clear();
            if (ch == null) { _choicePanel.gameObject.SetActive(false); return; }
            _choicePanel.gameObject.SetActive(true);
            for (int i = 0; i < ch.Options.Count; i++)
            {
                int idx = i; var o = ch.Options[i]; var line = StoryModel.Resolve(t, o.Line, s);
                string icon = o.SetPact != Pact.None ? "● " : o.Tendency == Trait.Charme ? "♥ " : o.Tendency == Trait.Wuerde ? "♦ " : o.Tendency == Trait.Wildheit ? "▲ " : "• ";
                var b = UiFactory.Button("Opt" + i, _choicePanel, "<b>" + (i + 1) + "</b>  " + icon + (line != null ? line.Text : o.Line), () => DlgPlayer?.Choose(idx), 19);
                UiFactory.Layout(b, 52);
                var lbl = b.GetComponentInChildren<TextMeshProUGUI>(); if (lbl != null) { lbl.alignment = TextAlignmentOptions.Left; lbl.margin = new Vector4(14, 0, 10, 0); }
                b.GetComponent<Image>().color = o.Tendency == Trait.Charme ? new Color(0.45f, 0.22f, 0.3f) : o.Tendency == Trait.Wuerde ? new Color(0.22f, 0.3f, 0.45f) : o.Tendency == Trait.Wildheit ? new Color(0.45f, 0.3f, 0.15f) : UiFactory.ButtonColor;
                _choiceButtons.Add(b);
            }
        }

        public void ConfirmSkip(Action<bool> answer) { _skipAnswer = answer; _skipPanel.gameObject.SetActive(true); _skipPanel.SetAsLastSibling(); }
        void AnswerSkip(bool yes) { _skipPanel.gameObject.SetActive(false); var a = _skipAnswer; _skipAnswer = null; a?.Invoke(yes); }

        /// <summary>Esc während einer Szene: Überspringen-Abfrage (statt Spielmenü).</summary>
        bool DialogueEscape()
        {
            var d = DlgPlayer; if (d == null || !d.Playing || d.CurrentScene == null || d.CurrentScene.Form == SceneForm.Bubble) return false;
            if (_skipPanel.gameObject.activeSelf) { AnswerSkip(false); return true; }
            d.RequestSkip(); return true;
        }

        void UpdateDialogueUi()
        {
            var d = DlgPlayer;
            if (d != null && d.Playing && d.CurrentScene != null && d.CurrentScene.Form != SceneForm.Bubble && !_skipPanel.gameObject.activeSelf && !_choicePanel.gameObject.activeSelf)
            {
                var input = InputManager.Instance;
                bool space = input != null && input.JumpPressed;
                if (space) d.Next();
                if (_choicePanel.gameObject.activeSelf) for (int i = 0; i < 3; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) d.Choose(i);
            }
            if (d != null && d.Playing && _choicePanel.gameObject.activeSelf) for (int i = 0; i < _choiceButtons.Count; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) d.Choose(i);
            if (_bubble.gameObject.activeSelf)
            {
                _bubbleTimer -= Time.unscaledDeltaTime;
                var cam = Camera.main;
                if (_bubbleTimer <= 0f || cam == null) _bubble.gameObject.SetActive(false);
                else
                {
                    // Sprecher in Sicht und nah → Blase über ihm; sonst (Funk, hinter der Kamera, weit weg) über dem Helden, am Bildrand gehalten
                    Vector3 home = Player != null ? Player.transform.position + Vector3.up * 2.6f : cam.transform.position + cam.transform.forward * 5f;
                    Vector3 w = _bubbleAnchor != null ? _bubbleAnchor.position + Vector3.up * 2.4f : home;
                    var sp = cam.WorldToScreenPoint(w);
                    if (sp.z < 0.5f || sp.z > 35f || sp.x < 0f || sp.x > Screen.width || sp.y < 0f || sp.y > Screen.height) sp = cam.WorldToScreenPoint(home);
                    var scale = Root.localScale.x;
                    float x = Mathf.Clamp(sp.x / scale, 230f, Screen.width / scale - 230f), y = Mathf.Clamp(sp.y / scale, 180f, Screen.height / scale - 120f);
                    _bubble.anchoredPosition = new Vector2(x, y);
                }
            }
            UpdateChat();
        }

        // ------------------------------------------------------------ Chat-Log + Emotes
        void BuildChat()
        {
            _chatRoot = UiFactory.Rect("Chat", HudLayer).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(14, 250), new Vector2(520, 190));
            _chatText = UiFactory.Text("Log", _chatRoot, "", 15, TextAlignmentOptions.BottomLeft, Color.white); _chatText.rectTransform.Stretch(4, 4, 4, 40);
            _chatInput = UiFactory.InputField("Input", _chatRoot, "Enter: Chat · /winken /tanzen /jubeln /verbeugen /sitzen");
            ((RectTransform)_chatInput.transform).Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), Vector2.zero, new Vector2(0, 34));
            _chatInput.onSubmit.AddListener(SubmitChat);
            _chatInput.onSelect.AddListener(_ => InputManager.TextInputActive = true);
            _chatInput.onDeselect.AddListener(_ => InputManager.TextInputActive = false);
            _chatInput.gameObject.SetActive(false);
        }

        public void AddChatLine(string who, string text, Color c)
        {
            _chat.Add(("<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + who + ":</color> " + text, Time.unscaledTime));
            if (_chat.Count > 60) _chat.RemoveAt(0);
        }

        void UpdateChat()
        {
            bool open = _chatInput.gameObject.activeSelf;
            if (!open && !InputManager.TextInputActive && Input.GetKeyDown(KeyCode.Return) && (DlgPlayer == null || !DlgPlayer.Playing)) { _chatInput.gameObject.SetActive(true); _chatInput.text = ""; _chatInput.ActivateInputField(); open = true; }
            var sb = new System.Text.StringBuilder(); int shown = 0;
            for (int i = _chat.Count - 1; i >= 0 && shown < (open ? 14 : 6); i--)
            {
                float age = Time.unscaledTime - _chat[i].time; if (!open && age > 25f) break;
                sb.Insert(0, _chat[i].text + "\n"); shown++;
            }
            _chatText.text = sb.ToString();
        }

        void SubmitChat(string s)
        {
            _chatInput.DeactivateInputField(); _chatInput.gameObject.SetActive(false); InputManager.TextInputActive = false;
            if (string.IsNullOrWhiteSpace(s)) return;
            var e = EmoteModel.Parse(s);
            if (e != Emote.None) { EmoteRunner.Play(Player, e); AddChatLine("Du", "*" + e + "*", new Color(0.8f, 0.9f, 1f)); return; }
            AddChatLine(PlayerProfile.Instance != null ? PlayerProfile.Instance.State.CharacterName : "Du", s, new Color(0.9f, 0.9f, 0.9f));
        }
    }
}
