using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M25: Klassenmechanik-Leiste über der Skill-Leiste (wie GW2): F1–F4 mit Beschriftung und Abklingzeit, Ressource
    /// (Initiative als 12 Punkte, Einstimmungs-Rad, Gefährten-Leben).
    /// </summary>
    public partial class UIController
    {
        class MechSlot { public RectTransform Root; public Image Bg, Cd, Frame; public TextMeshProUGUI Label, Key, CdText; }
        RectTransform _mechRoot; readonly MechSlot[] _mech = new MechSlot[5]; Image[] _pips; TextMeshProUGUI _mechRes; Bar _petBar; ClassMechanicHost _host;
        ClassMechanicKind _builtKind = (ClassMechanicKind)(-1); EliteSpecDefinition _builtElite; bool _builtOnce;

        void BuildMechanicHud()
        {
            _mechRoot = UiFactory.Rect("ClassMechanic", HudLayer).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-250, 196), new Vector2(380, 74));
            for (int i = 0; i < 5; i++)
            {
                var s = new MechSlot();
                s.Frame = UiFactory.Image("F" + (i + 1), _mechRoot, new Color(0, 0, 0, 0.75f), UiFactory.Sprite("ui_round"), true);
                s.Root = s.Frame.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(i * 62 + (i == 4 ? 10 : 0), 0), new Vector2(56, 44));
                s.Bg = UiFactory.Image("Bg", s.Root, Color.gray, UiFactory.Sprite("ui_round")); s.Bg.rectTransform.Stretch(2, 2, 2, 2);
                s.Label = UiFactory.Text("Label", s.Root, "", 12, TextAlignmentOptions.Bottom, Color.white); s.Label.rectTransform.Stretch(2, 2, 2, 2); s.Label.fontStyle = FontStyles.Bold;
                s.Cd = UiFactory.Image("Cd", s.Root, new Color(0, 0, 0, 0.65f)); s.Cd.rectTransform.anchorMin = Vector2.zero; s.Cd.rectTransform.anchorMax = new Vector2(1, 0); s.Cd.rectTransform.offsetMin = new Vector2(2, 2); s.Cd.rectTransform.offsetMax = new Vector2(-2, 2);
                s.CdText = UiFactory.Text("CdText", s.Root, "", 18, TextAlignmentOptions.Center); s.CdText.rectTransform.Stretch();
                s.Key = UiFactory.Text("Key", s.Root, "F" + (i + 1), 11, TextAlignmentOptions.TopLeft, new Color(1f, 0.9f, 0.6f)); s.Key.rectTransform.Stretch(3, 0, 1, 0);
                int f = i;
                s.Frame.gameObject.AddComponent<UiClickRelay>().Left = () => { if (_host != null) _host.UseMechanic(f); };
                s.Frame.gameObject.AddComponent<UiTooltipTrigger>().Provider = () => MechTooltip(f);
                _mech[i] = s;
            }
            _mechRes = UiFactory.Text("Resource", _mechRoot, "", 14, TextAlignmentOptions.Left, new Color(1f, 0.92f, 0.75f));
            _mechRes.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 48), new Vector2(380, 22));
            _pips = new Image[InitiativeModel.Max];
            for (int i = 0; i < _pips.Length; i++) { _pips[i] = UiFactory.Image("Pip" + i, _mechRoot, new Color(0.75f, 0.55f, 1f), UiFactory.Sprite("ui_round")); _pips[i].rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(250 + (i % 6) * 21, i < 6 ? 24 : 2), new Vector2(17, 17)); }
            _petBar = MakeBar("PetBar", _mechRoot, new Vector2(0, 0), new Vector2(310, 14), new Vector2(120, 14), new Color(0.45f, 0.8f, 0.35f), 11);
            _mechRoot.gameObject.SetActive(false);
        }

        TooltipContent MechTooltip(int f)
        {
            if (_host == null || _host.Class == null) return null;
            var b = _host.Button(f);
            var el = f == 4 ? _host.Elite : null;
            var c = new TooltipContent { Title = "F" + (f + 1) + " · " + b.Label, Subtitle = el != null ? "Elite-Spezialisierung " + el.DisplayName : _host.Class.DisplayName + " · " + _host.Class.ResourceName };
            c.Add(el != null ? el.MechanicSummary : _host.Class.MechanicSummary, new Color(0.85f, 0.85f, 0.85f));
            if (f < 4 && _host.Elite != null && _host.Elite.Mechanic != EliteMechanic.None) c.Add(_host.Elite.DisplayName + ": " + _host.Elite.MechanicSummary, _host.Elite.Accent);
            if (b.CooldownMax > 0f) c.TopRight = Mathf.RoundToInt(b.CooldownMax) + " s";
            return c;
        }

        void UpdateMechanicHud()
        {
            if (_mechRoot == null) BuildMechanicHud();
            if (Player == null) return;
            if (_host == null) _host = Player.GetComponent<ClassMechanicHost>();
            var kind = _host != null ? _host.Kind : ClassMechanicKind.None;
            var elite = _host != null ? _host.Elite : null;
            if (kind != _builtKind || elite != _builtElite || !_builtOnce)
            {
                _builtKind = kind; _builtElite = elite; _builtOnce = true;
                _mechRoot.gameObject.SetActive(kind != ClassMechanicKind.None || elite != null);
                _mech[4].Root.gameObject.SetActive(elite != null && elite.Mechanic != EliteMechanic.DoppelEinstimmung);
                for (int i = 0; i < 4; i++) _mech[i].Root.gameObject.SetActive(kind != ClassMechanicKind.None);
                foreach (var p in _pips) p.gameObject.SetActive(kind == ClassMechanicKind.Initiative);
                _petBar.Root.SetActive(kind == ClassMechanicKind.Pet);
            }
            if (kind == ClassMechanicKind.None && elite == null) return;
            for (int i = 0; i < 5; i++)
            {
                if (i < 4 && kind == ClassMechanicKind.None) continue;
                if (i == 4 && (elite == null || elite.Mechanic == EliteMechanic.DoppelEinstimmung)) continue;
                var b = _host.Button(i); var s = _mech[i];
                bool vis = b.Available || kind == ClassMechanicKind.Pet || i == 4;
                if (s.Root.gameObject.activeSelf != (vis || kind != ClassMechanicKind.Initiative)) s.Root.gameObject.SetActive(vis || kind != ClassMechanicKind.Initiative);
                if (s.Label.text != b.Label) s.Label.text = b.Label;
                s.Bg.color = b.Color * (b.Active ? 1.1f : 0.55f) + new Color(0, 0, 0, 0.3f);
                s.Frame.color = b.Active ? new Color(1f, 0.9f, 0.45f, 1f) : new Color(0, 0, 0, 0.75f);
                float frac = b.CooldownMax > 0f ? Mathf.Clamp01(b.Cooldown / b.CooldownMax) : 0f;
                s.Cd.rectTransform.anchorMax = new Vector2(1f, frac);
                string cd = b.Cooldown > 0.05f ? Mathf.CeilToInt(b.Cooldown).ToString() : "";
                if (s.CdText.text != cd) s.CdText.text = cd;
            }
            string res = _host.FullResourceText; if (_mechRes.text != res) _mechRes.text = res;
            if (kind == ClassMechanicKind.Initiative)
            {
                int pts = _host.Initiative.Points;
                for (int i = 0; i < _pips.Length; i++) _pips[i].color = i < pts ? new Color(0.8f, 0.6f, 1f) : new Color(0.2f, 0.16f, 0.25f);
            }
            else if (kind == ClassMechanicKind.Pet)
            {
                var pc = _host.PetCombatant;
                SetBar(_petBar, pc != null && !pc.IsDead ? pc.HealthFraction : 0f, pc != null && !pc.IsDead ? PetModel.DisplayName(_host.Pet.Current) : "–");
            }
        }
    }
}
