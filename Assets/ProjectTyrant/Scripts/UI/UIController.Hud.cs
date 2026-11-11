using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>HUD im GW2-Layout: Skill-Leiste mit HP-Kreis (Level), Ausdauer, Erfahrungsleiste, Effektleisten, Zielplakette, Banner, Prompts.</summary>
    public partial class UIController
    {
        class Bar { public RectTransform Fill; public Image FillImage; public TextMeshProUGUI Text; public GameObject Root; public float Last = -1f; }
        class Slot { public GameObject Root; public Image Bg, Icon; public TextMeshProUGUI Label, Key, Cd; public RectTransform CdOverlay; public Image Frame; public SkillDefinition Skill; public int LastCd = -1; public float LastFrac = -1f; public bool LastCasting; public float FailTimer; public bool LastQueued; public Vector2 BasePos; }
        class Floating { public TextMeshProUGUI Text; public Vector3 World; public float Age, Life; public Vector2 Drift; public float Rot, Arc = 70f; }
        class BannerItem { public string Title, Sub; public Color Color; }

        readonly System.Collections.Generic.List<Image> _endDividers = new System.Collections.Generic.List<Image>(); int _endSegments = -1;
        /// <summary>Ausweich-Leiste in Balken teilen (2 normal, 3 auf dem Greif bzw. mit Phantomtanz).</summary>
        void LayoutEnduranceDividers(EnduranceModel e)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(e.Max / Mathf.Max(1f, e.DodgeCost)));
            if (n == _endSegments) return; _endSegments = n;
            while (_endDividers.Count < n - 1) _endDividers.Add(UiFactory.Image("Divider", _end.Root.transform, new Color(0, 0, 0, 0.8f)));
            for (int i = 0; i < _endDividers.Count; i++)
            {
                bool on = i < n - 1; _endDividers[i].gameObject.SetActive(on); if (!on) continue;
                float x = (i + 1f) / n; _endDividers[i].rectTransform.Place(new Vector2(x, 0), new Vector2(x, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3, 0));
            }
            _end.Root.GetComponent<RectTransform>().sizeDelta = new Vector2(65f * n, 10f);   // je Balken gleich breit
        }
        Bar _flightPower;
        Bar _end, _xp, _cast, _downedBar, _targetHp, _eventBar, _channel;
        readonly Slot[] _slots = new Slot[SkillCaster.SlotCount];
        TextMeshProUGUI _swapText, _targetName, _crosshair, _help, _rewards, _evTitle, _evDesc, _evTimer, _evMedal, _downedHint, _comboHint;
        int _lastRealLevel = -1;
        TextMeshProUGUI _orbLevel, _orbHp, _prompt, _bannerTitle, _bannerSub, _areaText, _xpText;
        Image _orbFill;
        GameObject _targetRoot, _eventRoot, _bannerRoot, _areaRoot, _promptRoot;
        EffectBar _playerBoons, _playerConds, _targetBoons, _targetConds;
        float _comboTimer, _bannerTimer, _areaTimer;
        readonly Queue<BannerItem> _banners = new Queue<BannerItem>();
        readonly List<Floating> _floating = new List<Floating>();
        readonly Queue<TextMeshProUGUI> _floatPool = new Queue<TextMeshProUGUI>();
        static readonly string[] KeyLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        int _lastLevel = -1, _lastHp = -1, _lastMaxHp = -1, _lastSwapCd = -2, _lastSetIndex = -1;
        bool _lastDowned;
        Combatant _boundTarget;
        static readonly string[] CdStrings = BuildCd();

        static string[] BuildCd() { var a = new string[200]; for (int i = 0; i < a.Length; i++) a[i] = i == 0 ? "" : i.ToString(); return a; }

        public static string HelpTextFull => HelpText;
        const string HelpText = "<b>Steuerung</b>\nW/S laufen · A/D drehen (Rechtsklick halten: seitwärts) · Q/E seitwärts · Leertaste springen\nV oder Doppeltipp = Ausweichen · Rechtsklick halten = Kamera · Mausrad = Zoom\nC Action-Kamera · Tab Ziel · Esc Fenster/Ziel/Spielmenü\n1–5 Waffe · 6 Heilen · 7–9 Hilfe · 0 Elite · ^/Y Waffenwechsel · F1–F4 Klassenmechanik · F5 Elite-Mechanik\nX Reittier (halten = wählen) · Leertaste beritten = Fähigkeit · 1 beritten = Absitz-Angriff\nF Interagieren · I Inventar · H Held · M Karte · +/− Minimap\nAlt+Ziehen Stapel teilen · Shift Vergleich · Bodenziel-Modus und Shift+Skill sofort: Optionen › Steuerung · F9 Debug · F10 Hilfe";

        void BuildHud()
        {
            var hud = HudLayer;
            // ---- Skill-Leiste mit HP-Kreis in der Mitte ----
            var bar = UiFactory.Rect("SkillBar", hud).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(1000, 84));
            for (int i = 0; i < SkillCaster.SlotCount; i++)
            {
                float x = i < 5 ? -420f + i * 74f : 124f + (i - 5) * 74f + (i >= 6 ? 16f : 0f) + (i >= 9 ? 16f : 0f);
                _slots[i] = MakeSlot(bar, i, new Vector2(x, 0));
            }
            var orbBg = UiFactory.Image("HealthOrb", bar, new Color(0.08f, 0.02f, 0.02f, 0.95f), UiFactory.Sprite("ui_circle"), true);
            orbBg.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(104, 104));
            _orbFill = UiFactory.Image("Fill", orbBg.transform, new Color(0.8f, 0.12f, 0.1f), UiFactory.Sprite("ui_circle"));
            _orbFill.rectTransform.Stretch(5, 5, 5, 5);
            _orbFill.type = Image.Type.Filled; _orbFill.fillMethod = Image.FillMethod.Vertical; _orbFill.fillOrigin = 0;
            var orbRing = UiFactory.Image("Ring", orbBg.transform, new Color(0.9f, 0.75f, 0.45f), UiFactory.Sprite("ui_ring"));
            orbRing.rectTransform.Stretch(0, 0, 0, 0);
            _orbLevel = UiFactory.Text("Level", orbBg.transform, "1", 34, TextAlignmentOptions.Center);
            _orbLevel.rectTransform.Stretch(0, 0, 14, 30); _orbLevel.fontStyle = FontStyles.Bold;
            _orbHp = UiFactory.Text("Hp", orbBg.transform, "", 14, TextAlignmentOptions.Center);
            _orbHp.rectTransform.Stretch(0, 0, 62, 10);
            orbBg.gameObject.AddComponent<UiTooltipTrigger>().Provider = HealthTooltip;
            _swapText = UiFactory.Text("Swap", bar, "", 17, TextAlignmentOptions.Right);
            _swapText.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8, 0), new Vector2(240, 60));

            // ---- Ausdauer über dem Kreis, Erfahrung über der Leiste ----
            _end = MakeBar("Endurance", hud, new Vector2(0.5f, 0f), new Vector2(0, 138), new Vector2(130, 10), new Color(1f, 0.82f, 0.25f), 0);
            _flightPower = MakeBar("FlightPower", hud, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(130, 7), new Color(0.55f, 0.85f, 1f), 0);   // Himmelsschuppe: Flugkraft über der Ausweich-Leiste
            _flightPower.Root.SetActive(false);
            _endDividers.Add(UiFactory.Image("Divider", _end.Root.transform, new Color(0, 0, 0, 0.8f)));
            _xp = MakeBar("Experience", hud, new Vector2(0.5f, 0f), new Vector2(0, 108), new Vector2(980, 7), new Color(0.75f, 0.45f, 1f), 0);
            _xp.Root.GetComponent<Image>().raycastTarget = true;
            _xp.Root.AddComponent<UiTooltipTrigger>().Provider = XpTooltip;
            _xpText = UiFactory.Text("XpGain", hud, "", 15, TextAlignmentOptions.Center, new Color(0.85f, 0.7f, 1f));
            _xpText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(330, 116), new Vector2(300, 22));

            // ---- Effektleisten (Segen links, Zustände rechts) ----
            _playerBoons = new EffectBar(hud, "PlayerBoons", new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-75, 152), 34, true, true);
            _playerConds = new EffectBar(hud, "PlayerConditions", new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(75, 152), 34, false, false);

            _cast = MakeBar("CastBar", hud, new Vector2(0.5f, 0f), new Vector2(0, 270), new Vector2(520, 20), new Color(0.95f, 0.85f, 0.45f), 15);
            _cast.Root.SetActive(false);
            _channel = MakeBar("ChannelBar", hud, new Vector2(0.5f, 0f), new Vector2(0, 240), new Vector2(360, 18), new Color(0.55f, 0.85f, 1f), 14);
            _channel.Root.SetActive(false);
            _promptRoot = UiFactory.Image("Prompt", hud, new Color(0, 0, 0, 0.6f), UiFactory.Sprite("ui_round")).gameObject;
            ((RectTransform)_promptRoot.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(320, 34));
            _prompt = UiFactory.Text("Text", _promptRoot.transform, "", 19, TextAlignmentOptions.Center, Color.white);
            _prompt.rectTransform.Stretch();
            _promptRoot.SetActive(false);

            _downedBar = MakeBar("DownedBar", hud, new Vector2(0.5f, 0f), new Vector2(0, 320), new Vector2(460, 26), new Color(0.85f, 0.35f, 0.15f), 17);
            _downedHint = UiFactory.Text("DownedHint", _downedBar.Root.transform, "AM BODEN – 1 Angriff · 2 Unterbrechen · 3 Selbstheilung · 4 Bandagieren", 20, TextAlignmentOptions.Center);
            _downedHint.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0, 6), new Vector2(800, 34));
            _downedBar.Root.SetActive(false);

            // Lauf 5 / M23: Am-Boden-Vignette (radialer Rand, pulsiert mit sinkender Downed-HP schneller)
            _downedVignette = new GameObject("DownedVignette", typeof(RectTransform)).AddComponent<UnityEngine.UI.RawImage>();
            _downedVignette.transform.SetParent(hud, false); _downedVignette.transform.SetAsFirstSibling();
            _downedVignette.rectTransform.Stretch(); _downedVignette.raycastTarget = false;
            _downedVignette.texture = VignetteTexture(); _downedVignette.color = new Color(0.55f, 0.04f, 0.04f, 0f);
            _downedVignette.gameObject.SetActive(false);

            // ---- Zielplakette oben mittig ----
            var tbg = UiFactory.Image("TargetFrame", hud, new Color(0.05f, 0.05f, 0.1f, 0.6f), UiFactory.Sprite("ui_round"));
            _targetRoot = tbg.gameObject;
            tbg.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(480, 112));
            _targetName = UiFactory.Text("TargetName", tbg.transform, "", 21, TextAlignmentOptions.Center);
            _targetName.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -4), new Vector2(-16, 30));
            _targetHp = MakeBar("TargetHealth", (RectTransform)tbg.transform, new Vector2(0.5f, 1f), new Vector2(0, -54), new Vector2(440, 20), new Color(0.85f, 0.2f, 0.15f), 14);
            _targetBoons = new EffectBar((RectTransform)tbg.transform, "TargetBoons", new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-4, 6), 26, true, true);
            _targetConds = new EffectBar((RectTransform)tbg.transform, "TargetConditions", new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(4, 6), 26, false, false);
            _targetRoot.SetActive(false);

            // ---- Event-Tracker (bis M13) ----
            var ebg = UiFactory.Image("EventTracker", hud, new Color(0.08f, 0.06f, 0.04f, 0.6f), UiFactory.Sprite("ui_round"));
            _eventRoot = ebg.gameObject;
            var er = ebg.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20, -140), new Vector2(400, 150));
            _evTitle = UiFactory.Text("Title", er, "", 21, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.4f));
            _evTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(-20, 28));
            _evDesc = UiFactory.Text("Desc", er, "", 16, TextAlignmentOptions.TopLeft);
            _evDesc.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(-20, 50));
            _eventBar = MakeBar("Progress", er, new Vector2(0.5f, 0f), new Vector2(0, 34), new Vector2(370, 16), new Color(1f, 0.65f, 0.2f), 12);
            _evTimer = UiFactory.Text("Timer", er, "", 15, TextAlignmentOptions.Left);
            _evTimer.rectTransform.Place(new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(10, 6), new Vector2(0, 24));
            _evMedal = UiFactory.Text("Medal", er, "", 15, TextAlignmentOptions.Right);
            _evMedal.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 6), new Vector2(0, 24));
            _eventRoot.SetActive(false);

            // ---- Meldungen, Banner, Gebietsname ----
            _comboHint = UiFactory.Text("Combo", hud, "", 20, TextAlignmentOptions.Center);
            _comboHint.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(900, 34));
            _bannerRoot = UiFactory.Rect("Banner", hud).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1000, 110)).gameObject;
            _bannerTitle = UiFactory.Text("Title", _bannerRoot.transform, "", 46, TextAlignmentOptions.Center, UiFactory.AccentColor);
            _bannerTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 60)); _bannerTitle.fontStyle = FontStyles.Bold;
            _bannerSub = UiFactory.Text("Sub", _bannerRoot.transform, "", 21, TextAlignmentOptions.Center);
            _bannerSub.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(0, 34));
            _bannerRoot.SetActive(false);
            _areaRoot = UiFactory.Rect("AreaName", hud).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(800, 60)).gameObject;
            _areaText = UiFactory.Text("Text", _areaRoot.transform, "", 32, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.85f));
            _areaText.rectTransform.Stretch(); _areaText.fontStyle = FontStyles.Italic;
            _areaRoot.SetActive(false);

            _crosshair = UiFactory.Text("Crosshair", hud, "+", 34, TextAlignmentOptions.Center);
            _crosshair.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            _crosshair.gameObject.SetActive(false);
            _rewards = UiFactory.Text("Rewards", hud, "", 17, TextAlignmentOptions.Left);
            _rewards.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -16), new Vector2(560, 30));
            _help = UiFactory.Text("Help", hud, HelpText, 15, TextAlignmentOptions.BottomLeft, new Color(1, 1, 1, 0.8f));
            _help.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(600, 210));
        }

        Slot MakeSlot(RectTransform parent, int index, Vector2 pos)
        {
            var s = new Slot();
            var frame = UiFactory.Image("Slot" + index, parent, new Color(0, 0, 0, 0.75f), UiFactory.Sprite("ui_round"), true);
            var r = frame.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(68, 68));
            s.Root = r.gameObject; s.Frame = frame;
            s.Bg = UiFactory.Image("Bg", r, Color.gray, UiFactory.Sprite("ui_round")); s.Bg.rectTransform.Stretch(3, 3, 3, 3);
            s.Icon = UiFactory.Image("Icon", r, Color.white); s.Icon.rectTransform.Stretch(8, 8, 8, 8);
            s.Label = UiFactory.Text("Label", r, "", 16, TextAlignmentOptions.Bottom); s.Label.rectTransform.Stretch(2, 2, 2, 3); s.Label.fontStyle = FontStyles.Bold;
            s.CdOverlay = UiFactory.Image("Cooldown", r, new Color(0, 0, 0, 0.65f)).rectTransform;
            s.CdOverlay.anchorMin = Vector2.zero; s.CdOverlay.anchorMax = new Vector2(1, 0); s.CdOverlay.pivot = new Vector2(0.5f, 0);
            s.CdOverlay.offsetMin = new Vector2(3, 3); s.CdOverlay.offsetMax = new Vector2(-3, 3);
            s.Cd = UiFactory.Text("CdText", r, "", 24, TextAlignmentOptions.Center); s.Cd.rectTransform.Stretch();
            s.Key = UiFactory.Text("Key", r, KeyLabels[index], 14, TextAlignmentOptions.TopLeft); s.Key.rectTransform.Stretch(4, 0, 2, 0);
            int idx = index;
            frame.gameObject.AddComponent<UiTooltipTrigger>().Provider = () => _caster == null ? null : TooltipBuilder.Skill(_caster.GetSkillInSlot(idx), Player);
            frame.gameObject.AddComponent<UiClickRelay>().Left = () => { if (_caster != null) _caster.TryUseSlot(idx); };
            return s;
        }

        Bar MakeBar(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, float fontSize)
        {
            var b = new Bar();
            var bg = UiFactory.Image(name, parent, new Color(0.05f, 0.04f, 0.08f, 0.85f));
            var r = bg.rectTransform.Place(anchor, anchor, new Vector2(0.5f, 0f), pos, size);
            b.Root = r.gameObject;
            b.FillImage = UiFactory.Image("Fill", r, color);
            b.Fill = b.FillImage.rectTransform;
            b.Fill.anchorMin = Vector2.zero; b.Fill.anchorMax = Vector2.one; b.Fill.pivot = new Vector2(0, 0.5f);
            b.Fill.offsetMin = new Vector2(2, 2); b.Fill.offsetMax = new Vector2(-2, -2);
            if (fontSize > 0) { b.Text = UiFactory.Text("Text", r, "", fontSize, TextAlignmentOptions.Center); b.Text.rectTransform.Stretch(); }
            return b;
        }

        static void SetBar(Bar b, float frac, string text = null)
        {
            frac = Mathf.Clamp01(frac);
            if (Mathf.Abs(frac - b.Last) > 0.0005f) { b.Last = frac; b.Fill.anchorMax = new Vector2(frac, 1f); }
            if (b.Text != null && text != null && b.Text.text != text) b.Text.text = text;
        }

        void BindHud()
        {
            _playerBoons.Bind(Player); _playerConds.Bind(Player);
            if (Player != null) Player.HealthChanged += () => _lastHp = -1;
            _lastLevel = -1;
            RefreshRewards();
        }

        void OnEnable()
        {
            GameEventBus.Notification += OnNotification;
            GameEventBus.DamageDealt += OnDamage;
            GameEventBus.Healed += OnHealed;
            GameEventBus.Banner += OnBanner;
            GameEventBus.AreaEntered += OnAreaEntered;
            GameEventBus.XpGained += OnXp;
            GameEventBus.ProfileChanged += RefreshRewards;
            GameEventBus.CastInterrupted += OnInterruptedText;
            GameEventBus.ComboTriggered += OnComboText;
        }

        void OnDisable()
        {
            GameEventBus.Notification -= OnNotification;
            GameEventBus.DamageDealt -= OnDamage;
            GameEventBus.Healed -= OnHealed;
            GameEventBus.Banner -= OnBanner;
            GameEventBus.AreaEntered -= OnAreaEntered;
            GameEventBus.XpGained -= OnXp;
            GameEventBus.ProfileChanged -= RefreshRewards;
            GameEventBus.CastInterrupted -= OnInterruptedText;
            GameEventBus.ComboTriggered -= OnComboText;
            if (_boundCaster != null) _boundCaster.CastFailed -= OnCastFailed;
        }

        void UpdateHud()
        {
            if (Player == null) return;
            UpdateMechanicHud();   // Lauf 5 / M25
            bool downed = _downed != null && _downed.IsDowned;

            int hp = Mathf.CeilToInt(Player.Health), maxHp = Mathf.CeilToInt(Player.MaxHealth);
            if (hp != _lastHp || maxHp != _lastMaxHp)
            {
                _lastHp = hp; _lastMaxHp = maxHp;
                _orbFill.fillAmount = Player.HealthFraction;
                _orbHp.text = hp.ToString("N0");
            }
            int realLevel = PlayerProfile.Instance != null ? PlayerProfile.Instance.Progression.Level : Player.Level;
            if (Player.Level != _lastLevel || realLevel != _lastRealLevel)
            {
                _lastLevel = Player.Level; _lastRealLevel = realLevel;
                _orbLevel.text = realLevel == _lastLevel ? CdStrings[Mathf.Clamp(_lastLevel, 0, 199)] : realLevel + "<size=55%> (" + _lastLevel + ")</size>";   // Herabstufung: „30 (18)“
            }
            if (_pc != null) { SetBar(_end, _pc.Dodge.Endurance.Fraction); LayoutEnduranceDividers(_pc.Dodge.Endurance); }
            var mcf = MountController.Local != null ? MountController.Local.Flight : null;
            bool fp = mcf != null && mcf.P.Kind == ProjectTyrant.Flight.FlyerKind.Skyscale && !HudHidden;
            if (_flightPower.Root.activeSelf != fp) _flightPower.Root.SetActive(fp);
            if (fp) { SetBar(_flightPower, mcf.P.Endurance > 0f ? mcf.Endurance / mcf.P.Endurance : 0f); _flightPower.Root.GetComponent<RectTransform>().sizeDelta = new Vector2(_end.Root.GetComponent<RectTransform>().sizeDelta.x, 7f); }
            var prof = PlayerProfile.Instance;
            if (prof != null)
            {
                if (prof.Progression.Level >= AttributeSet.MaxLevel)
                {
                    var m = prof.Mastery;   // Lauf 4: ab Stufe 30 zeigt die Leiste den Beherrschungs-Fortschritt
                    SetBar(_xp, m.Selected.HasValue ? m.Progress(m.Selected.Value) : 0f);
                    _xp.FillImage.color = new Color(0.35f, 0.95f, 0.65f);
                }
                else SetBar(_xp, prof.Progression.XpFraction);
            }
            _playerBoons.Update(); _playerConds.Update();

            UpdateSlots(downed);
            UpdateCast();
            UpdateDowned(downed);
            UpdateTarget();
            UpdatePrompt();
            UpdateNotify();
            FlushHeals();
            UpdateFloating();
            UpdateCombatUi();

            var rig = ThirdPersonCameraRig.Current;
            _crosshair.gameObject.SetActive(rig != null && rig.ActionCam && !Windows.AnyOpen && GameOptions.Load().ActionCrosshair);
        }

        // Fix-Lauf F1: Ketten-Anzeige am Skill-1-Feld (1/3, 2/3, 3/3)
        TMPro.TextMeshProUGUI _chainBadge; string _chainBadgeText;
        void UpdateChainBadge(Slot slot, SkillDefinition s)
        {
            int pos = s.ChainPosition(out int len);
            string t = len > 1 ? pos + "/" + len : "";
            if (_chainBadge == null)
            {
                _chainBadge = UiFactory.Text("Chain", slot.Root.transform, "", 12, TMPro.TextAlignmentOptions.BottomRight, new Color(1f, 0.85f, 0.45f));
                _chainBadge.rectTransform.Stretch(2, 2, 3, 1); _chainBadge.fontStyle = TMPro.FontStyles.Bold; _chainBadge.raycastTarget = false;
            }
            if (t != _chainBadgeText) { _chainBadgeText = t; _chainBadge.text = t; _chainBadge.color = pos == len && len > 1 ? new Color(1f, 0.6f, 0.25f) : new Color(1f, 0.85f, 0.45f); }
        }

        void UpdateSlots(bool downed)
        {
            if (_caster == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                var s = _caster.GetSkillInSlot(i);
                bool visible = s != null && (!downed || i < 4);
                if (slot.Root.activeSelf != visible) slot.Root.SetActive(visible);
                if (!visible) { slot.Skill = null; continue; }
                if (slot.Skill != s)
                {
                    slot.Skill = s;
                    slot.Bg.color = s.IconColor * 0.75f + new Color(0, 0, 0, 0.25f);
                    slot.Icon.sprite = s.Icon; slot.Icon.enabled = s.Icon != null;
                    slot.Label.text = s.Icon != null ? "" : s.ShortLabel;
                    slot.LastCd = -1; slot.LastFrac = -1f;
                }
                if (i == 0) UpdateChainBadge(slot, s);
                float cd = _caster.GetCooldownRemaining(s);
                float frac = _caster.GetCooldownFraction(s);
                if (Mathf.Abs(frac - slot.LastFrac) > 0.002f) { slot.LastFrac = frac; slot.CdOverlay.anchorMax = new Vector2(1f, frac); }
                int cdInt = cd > 0f ? Mathf.CeilToInt(cd) : 0;
                if (cdInt != slot.LastCd) { slot.LastCd = cdInt; slot.Cd.text = CdStrings[Mathf.Clamp(cdInt, 0, 199)]; }
                bool casting = _caster.CastingSkill == s || _caster.AimingSkill == s;
                if (casting != slot.LastCasting)
                {
                    slot.LastCasting = casting;
                    slot.Frame.color = casting ? new Color(1f, 0.9f, 0.4f, 1f) : new Color(0, 0, 0, 0.75f);
                }
            }
            int swapCd = Mathf.CeilToInt(_caster.SwapCooldownRemaining);
            if (downed != _lastDowned || swapCd != _lastSwapCd || _caster.ActiveSetIndex != _lastSetIndex)
            {
                _lastDowned = downed; _lastSwapCd = swapCd; _lastSetIndex = _caster.ActiveSetIndex;
                if (downed) _swapText.text = "";
                else
                {
                    var set = _caster.ActiveSet;
                    var other = _caster.WeaponSets != null && _caster.WeaponSets.Length > 1 ? _caster.WeaponSets[1 - _caster.ActiveSetIndex] : null;
                    _swapText.text = "<b>" + (set != null ? set.DisplayName : "-") + "</b>\n<size=14>^/Y: " + (other != null ? other.DisplayName : "-") + (swapCd > 0 ? "  (" + swapCd + " s)" : "  bereit") + "</size>";
                }
            }
        }

        void UpdateCast()
        {
            if (_caster != null && _caster.IsAiming)
            {
                _cast.Root.SetActive(true);
                SetBar(_cast, 0f, "Zielen: " + _caster.AimingSkill.DisplayName + "  ·  Taste/Linksklick = wirken, Esc = abbrechen");
                return;
            }
            if (_caster == null || !_caster.IsCasting || _caster.CastTotal < 0.2f) { if (_cast.Root.activeSelf) _cast.Root.SetActive(false); return; }
            _cast.Root.SetActive(true);
            SetBar(_cast, _caster.CastProgress, _caster.CastingSkill.DisplayName);
        }

        UnityEngine.UI.RawImage _downedVignette; float _vignette;

        static Texture2D VignetteTexture()
        {
            const int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "PT_DownedVignette" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.25f, r))));
            }
            t.SetPixels32(px); t.Apply(false, true); return t;
        }

        void UpdateDowned(bool downed)
        {
            if (_downedBar.Root.activeSelf != downed) _downedBar.Root.SetActive(downed);
            if (_downedVignette != null)
            {
                _vignette = Mathf.MoveTowards(_vignette, downed ? 1f : 0f, Time.unscaledDeltaTime * (downed ? 3f : 1.5f));
                bool show = _vignette > 0.001f;
                if (_downedVignette.gameObject.activeSelf != show) _downedVignette.gameObject.SetActive(show);
                if (show)
                {
                    float hp = downed ? _downed.DownedHealthFraction : 1f;
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(7f, 2.5f, hp));
                    var c = _downedVignette.color; c.a = _vignette * (0.55f + 0.25f * pulse); _downedVignette.color = c;
                }
            }
            if (!downed) return;
            SetBar(_downedBar, _downed.DownedHealthFraction, "Downed-HP " + Mathf.CeilToInt(_downed.DownedHealth) + "   ·   " + Mathf.CeilToInt(_downed.TimeRemaining) + " s");
        }

        void UpdateTarget()
        {
            var t = _targeting != null ? _targeting.Current : null;
            if (_targetRoot.activeSelf != (t != null)) _targetRoot.SetActive(t != null);
            if (t != _boundTarget)
            {
                _boundTarget = t;
                _targetBoons.Bind(t); _targetConds.Bind(t);
                if (t != null)
                {
                    string tier = t.Tier == EnemyTier.Champion ? "<color=#ffb030>Champion</color> " : t.Tier == EnemyTier.Veteran ? "<color=#d0d0ff>Veteran</color> " : "";
                    int diff = t.Level - (Player != null ? Player.Level : 1);
                    string lc = diff >= 3 ? "#ff6050" : diff <= -5 ? "#a0a0a0" : "#ffffff";
                    // Balance B1: Namensfarbe wie GW2 – gelb = neutral (greift nur an, wenn angegriffen), rot = feindlich, grün = freundlich
                    var tb = t.GetComponent<NpcBrain>();
                    string nc = t.Team != Team.Enemy ? "#80e080" : tb != null && tb.IsYellow ? "#f2d24a" : "#ff6a55";
                    string kind = t.Team != Team.Enemy ? "" : tb != null && tb.IsYellow ? " <size=70%><color=#f2d24a>(neutral)</color></size>" : " <size=70%><color=#ff8a75>(feindlich)</color></size>";
                    _targetName.text = "<color=" + lc + ">" + t.Level + "</color>  " + tier + "<color=" + nc + ">" + t.DisplayName + "</color>" + kind;
                    _targetHp.Last = -1f;
                }
            }
            if (t == null) return;
            SetBar(_targetHp, t.HealthFraction);
            if (_targetHp.Text != null)
            {
                int th = Mathf.CeilToInt(t.Health);
                if (th != _lastTargetHp) { _lastTargetHp = th; _targetHp.Text.text = th.ToString("N0"); }
            }
            _targetBoons.Update(); _targetConds.Update();
            UpdateDefiance(t);
            UpdateEnemyCast(t);
        }

        partial void UpdateDefiance(Combatant t);

        int _lastTargetHp = -1;
        Interactable _lastPrompt;

        void UpdatePrompt()
        {
            var it = _interaction != null ? _interaction.Current : null;
            bool channel = _interaction != null && _interaction.IsChanneling;
            if (_channel.Root.activeSelf != channel) _channel.Root.SetActive(channel);
            if (channel) SetBar(_channel, _interaction.ChannelProgress, _interaction.Channeling.Prompt + " …");
            bool show = it != null && !channel && !HudHidden;
            if (_promptRoot.activeSelf != show) _promptRoot.SetActive(show);
            if (show && it != _lastPrompt) { _lastPrompt = it; _prompt.text = "<color=#ffd060>[F]</color> " + it.Prompt; }
            if (!show) _lastPrompt = null;
        }

        void RefreshRewards()
        {
            var p = PlayerProfile.Instance;
            if (p == null || _rewards == null) return;
            _rewards.text = WalletModel.Format(p.Wallet.Copper) + "   ·   Karma " + p.Wallet.Karma.ToString("N0") + "   ·   Heldenpunkte " + p.Progression.AvailableHeroPoints;
        }

        TooltipContent HealthTooltip()
        {
            if (Player == null) return null;
            var c = new TooltipContent { Title = "Lebenspunkte", TitleColor = new Color(1f, 0.5f, 0.45f), Subtitle = "Stufe " + Player.Level };
            c.Add(Mathf.CeilToInt(Player.Health).ToString("N0") + " / " + Mathf.CeilToInt(Player.MaxHealth).ToString("N0"));
            c.Add("Rüstung " + Mathf.RoundToInt(Player.Stats.Armor).ToString("N0") + "   ·   Krit-Chance " + (Player.Stats.CritChance * 100f).ToString("0.0") + " %");
            return c;
        }

        TooltipContent XpTooltip()
        {
            var p = PlayerProfile.Instance;
            if (p == null) return null;
            var c = new TooltipContent { Title = "Erfahrung", TitleColor = new Color(0.85f, 0.7f, 1f), Subtitle = "Stufe " + p.Progression.Level };
            c.Add(p.Progression.Xp.ToString("N0") + " / " + ProgressionModel.XpToNext(p.Progression.Level).ToString("N0") + " EP");
            c.Add("Heldenpunkte verfügbar: " + p.Progression.AvailableHeroPoints);
            return c;
        }

        // ---------------- Meldungen & Banner ----------------
        void OnNotification(string msg, Color color)
        {
            if (msg.StartsWith("Combo:")) { _comboHint.text = msg; _comboHint.color = color; _comboTimer = 2.5f; return; }
            Toasts?.Show(msg, color);   // Toast mit eigenem Hintergrund statt Text über Text
        }

        void OnBanner(string title, string sub, Color c) { _banners.Enqueue(new BannerItem { Title = title, Sub = sub, Color = c }); }

        void OnAreaEntered(string name, string sub)
        {
            _areaText.text = name + (string.IsNullOrEmpty(sub) ? "" : "\n<size=18>" + sub + "</size>");
            _areaTimer = 4f; _areaRoot.SetActive(true);
        }

        float _xpTimer;
        int _xpAccum;
        void OnXp(int amount, string reason) { _xpAccum += amount; _xpTimer = 2.5f; _xpText.text = "+" + _xpAccum + " EP"; }

        void UpdateNotify()
        {
            float dt = Time.unscaledDeltaTime;
            Toasts?.Tick(dt);
            if (_comboTimer > 0f) { _comboTimer -= dt; var cc = _comboHint.color; cc.a = Mathf.Clamp01(_comboTimer); _comboHint.color = cc; }
            if (_bannerTimer > 0f)
            {
                _bannerTimer -= dt;
                float a = Mathf.Clamp01(_bannerTimer) * Mathf.Clamp01((3.2f - _bannerTimer) * 4f);
                var c = _bannerTitle.color; c.a = a; _bannerTitle.color = c;
                var s = _bannerSub.color; s.a = a; _bannerSub.color = s;
                if (_bannerTimer <= 0f) _bannerRoot.SetActive(false);
            }
            else if (_banners.Count > 0 && !HudHidden)
            {
                var b = _banners.Dequeue();
                _bannerTitle.text = b.Title; _bannerTitle.color = b.Color; _bannerSub.text = b.Sub ?? ""; _bannerSub.color = Color.white;
                _bannerTimer = 3.2f; _bannerRoot.SetActive(true);
            }
            if (_areaTimer > 0f)
            {
                _areaTimer -= dt;
                var c = _areaText.color; c.a = Mathf.Clamp01(_areaTimer) * Mathf.Clamp01((4f - _areaTimer) * 3f); _areaText.color = c;
                if (_areaTimer <= 0f) _areaRoot.SetActive(false);
            }
            if (_xpTimer > 0f) { _xpTimer -= dt; var c = _xpText.color; c.a = Mathf.Clamp01(_xpTimer); _xpText.color = c; if (_xpTimer <= 0f) _xpAccum = 0; }
        }

        public void ToggleHelp() => _help.gameObject.SetActive(!_help.gameObject.activeSelf);

        // ---------------- Event-Tracker (Kompatibilität bis M13) ----------------
        public void SetEventTracker(bool visible, string title = "", string desc = "", float progress = 0f, float timeLeft = -1f, string medal = "", Color medalColor = default)
        {
            if (Tracker != null)
            {
                if (_eventRoot != null && _eventRoot.activeSelf) _eventRoot.SetActive(false);
                var d = EventDirector.Instance;
                Tracker.SetEntry(0, visible, title, desc, progress, timeLeft, medal, medalColor, d != null ? d.CurrentCenter : (Vector3?)null);
                return;
            }
            if (_eventRoot == null) return;
            if (_eventRoot.activeSelf != visible) _eventRoot.SetActive(visible);
            if (!visible) return;
            if (_evTitle.text != title) _evTitle.text = title;
            if (_evDesc.text != desc) _evDesc.text = desc;
            SetBar(_eventBar, progress);
            int t = timeLeft >= 0f ? Mathf.CeilToInt(timeLeft) : -1;
            if (t != _lastEvTimer) { _lastEvTimer = t; _evTimer.text = t >= 0 ? (t / 60) + ":" + (t % 60).ToString("00") : ""; }
            if (_evMedal.text != medal) _evMedal.text = medal;
            _evMedal.color = medalColor == default ? Color.white : medalColor;
        }

        int _lastEvTimer = -2;

        // ---------------- Kampftext ----------------
        void OnDamage(DamageInfo d)
        {
            if (Player == null || d.Target == null) return;
            bool outgoing = d.Source == Player;
            bool incoming = d.Target == Player;
            if (!outgoing && !incoming) return;
            string txt; Color col; float size = d.IsCondition ? 20 : 26;
            if (d.Evaded) { txt = incoming ? "Ausgewichen" : "Verfehlt"; col = new Color(0.8f, 0.9f, 1f); }
            else if (d.Blocked) { txt = "Block"; col = new Color(0.8f, 0.9f, 1f); }
            else if (d.Missed) { txt = "Verfehlt"; col = new Color(0.7f, 0.7f, 0.7f); }
            else if (incoming) { txt = "-" + Mathf.RoundToInt(d.Amount); col = new Color(1f, 0.35f, 0.3f); }
            else
            {
                txt = Mathf.RoundToInt(d.Amount).ToString();
                col = d.IsCondition ? ConditionTextColor(d.Condition) : d.Crit ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.95f, 0.8f);
                if (d.Crit) { txt += "!"; size = 36; }
                if (d.Glancing) col = new Color(0.75f, 0.7f, 0.6f);
            }
            SpawnFloating(d.Target.AimPoint + Vector3.up * 0.6f, txt, col, size, d.Crit && outgoing ? Random.Range(-12f, 12f) : 0f);
            // Fix-Lauf F1: Bonus des dritten Ketten-Glieds im Kampftext markieren (einmal je Treffer-Zeitpunkt)
            if (outgoing && !d.IsCondition && d.Skill != null && d.Skill.ChainBonus && Time.time - _lastChainText > 0.3f)
            { _lastChainText = Time.time; SpawnFloating(d.Target.AimPoint + Vector3.up * 1.3f, "Kettenabschluss", new Color(1f, 0.75f, 0.35f), 22); }
        }
        float _lastChainText = -1f;

        float _healAccum, _healTimer;

        void OnHealed(Combatant c, float amount)
        {
            if (c != Player || amount <= 0f) return;
            _healAccum += amount;
        }

        void FlushHeals()
        {
            _healTimer -= Time.deltaTime;
            if (_healTimer > 0f) return;
            _healTimer = 0.5f;
            if (_healAccum >= 1f && Player != null)
                SpawnFloating(Player.AimPoint + Vector3.up * 0.8f, "+" + Mathf.RoundToInt(_healAccum), new Color(0.4f, 1f, 0.4f), 22);
            _healAccum = 0f;
        }

        int _floatSkip;
        void SpawnFloating(Vector3 world, string text, Color c, float size, float rot = 0f)
        {
            if (HudHidden) return;
            // Lauf 5 / M24: Kampftext-Dichte (0 = nur Krits/große Zahlen, 1 = jede zweite kleine Zahl, 2 = alles)
            int dens = GameOptions.Load().CombatTextDensity;
            if (dens < 2 && size < 30f && rot == 0f && (dens == 0 || (++_floatSkip & 1) == 0)) return;
            TextMeshProUGUI t;
            if (_floatPool.Count > 0) t = _floatPool.Dequeue();
            else
            {
                t = UiFactory.Text("Float", HudLayer, "", 24, TextAlignmentOptions.Center);
                t.rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 40));
            }
            t.gameObject.SetActive(true);
            t.text = text; t.color = c; t.fontSize = size;
            t.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
            // Bogenflug: seitlich driften, steigen und am Ende leicht absinken
            _floating.Add(new Floating { Text = t, World = world, Life = rot != 0f ? 1.3f : 1.1f, Drift = new Vector2(Random.Range(-60f, 60f), 0f), Rot = rot, Arc = size >= 30 ? 95f : 70f });
        }

        void UpdateFloating()
        {
            var cam = Camera.main;
            float scale = Root.lossyScale.x > 0 ? 1f / Root.lossyScale.x : 1f;
            for (int i = _floating.Count - 1; i >= 0; i--)
            {
                var f = _floating[i];
                f.Age += Time.deltaTime;
                if (f.Age >= f.Life || cam == null)
                {
                    f.Text.gameObject.SetActive(false); _floatPool.Enqueue(f.Text); _floating.RemoveAt(i); continue;
                }
                Vector3 sp = cam.WorldToScreenPoint(f.World);
                if (sp.z < 0) { f.Text.gameObject.SetActive(false); continue; }
                f.Text.gameObject.SetActive(true);
                float k = f.Age / f.Life;
                float arc = 4f * k * (1f - k);   // Parabel 0 → 1 → 0
                f.Text.rectTransform.anchoredPosition = new Vector2(sp.x * scale, sp.y * scale) + f.Drift * k + Vector2.up * (f.Arc * arc + 30f * k);
                if (f.Rot != 0f) f.Text.rectTransform.localScale = Vector3.one * (1f + 0.25f * Mathf.Max(0f, 1f - k * 4f));
                var c = f.Text.color; c.a = 1f - k * k; f.Text.color = c;
            }
        }
    }
}
