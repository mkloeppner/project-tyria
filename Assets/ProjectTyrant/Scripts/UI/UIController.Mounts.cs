using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Lauf 4: Reittier-HUD (HP, Aufsitzen, Springer-Ladung) und Radialmenü (X halten).</summary>
    public partial class UIController
    {
        Bar _mountHp, _mountChannel, _mountCharge;
        RectTransform _radial;
        readonly System.Collections.Generic.List<(MountKind kind, Image img, Vector2 dir)> _radialItems = new System.Collections.Generic.List<(MountKind, Image, Vector2)>();
        MountKind _radialHover;
        TextMeshProUGUI _radialLabel;

        void EnsureMountHud()
        {
            if (_mountHp != null) return;
            _mountHp = MakeBar("MountHp", HudLayer, new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(300, 12), new Color(0.95f, 0.75f, 0.35f), 11);
            _mountChannel = MakeBar("MountChannel", HudLayer, new Vector2(0.5f, 0f), new Vector2(0, 240), new Vector2(360, 18), new Color(1f, 0.85f, 0.45f), 14);
            _mountCharge = MakeBar("MountCharge", HudLayer, new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(240, 12), new Color(0.6f, 1f, 0.6f), 11);
            _mountHp.Root.SetActive(false); _mountChannel.Root.SetActive(false); _mountCharge.Root.SetActive(false);
        }

        public void UpdateMountHud(MountController mc)
        {
            if (mc == null) return;
            EnsureMountHud();
            var m = mc.Motor;
            bool hp = m.IsMounted && !HudHidden, ch = m.IsChanneling, cg = m.Charging;
            if (_mountHp.Root.activeSelf != hp) _mountHp.Root.SetActive(hp);
            if (_mountChannel.Root.activeSelf != ch) _mountChannel.Root.SetActive(ch);
            if (_mountCharge.Root.activeSelf != cg) _mountCharge.Root.SetActive(cg);
            if (hp) SetBar(_mountHp, m.HealthFraction, mc.Def(m.Mounted) != null ? mc.Def(m.Mounted).DisplayName + "  " + Mathf.CeilToInt(m.Health) : "");
            if (ch) SetBar(_mountChannel, m.ChannelProgress, "Aufsitzen: " + (mc.Def(m.Channeling) != null ? mc.Def(m.Channeling).DisplayName : m.Channeling.ToString()) + " …");
            if (cg) SetBar(_mountCharge, m.Charge, "Sprungkraft " + Mathf.RoundToInt(Mathf.Lerp(m.Stats.MinJump, m.Stats.MaxJump, m.Charge)) + " m");
            if (_radial != null && _radial.gameObject.activeSelf) UpdateRadial();
        }

        /// <summary>Öffnet (true) oder schließt (false) das Radialmenü; beim Schließen das gewählte Reittier.</summary>
        public MountKind ShowMountRadial(bool open)
        {
            var prof = PlayerProfile.Instance;
            if (open)
            {
                if (_radial == null)
                {
                    _radial = UiFactory.Rect("MountRadial", HudLayer);
                    _radial.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 420));
                    var bg = UiFactory.Image("Bg", _radial, new Color(0f, 0f, 0f, 0.45f), UiFactory.Sprite("ui_round"));
                    bg.rectTransform.Stretch();
                    _radialLabel = UiFactory.Text("Label", _radial, "", 20, TextAlignmentOptions.Center, Color.white);
                    _radialLabel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 60));
                }
                UiFactory.Clear(_radial.Find("Items") != null ? _radial.Find("Items") : UiFactory.Rect("Items", _radial).Stretch());
                _radialItems.Clear();
                var items = _radial.Find("Items");
                int n = 0; foreach (var m in prof.MountDefinitions) if (m != null && prof.OwnsMount(m.Kind)) n++;
                int i = 0;
                foreach (var m in prof.MountDefinitions)
                {
                    if (m == null || !prof.OwnsMount(m.Kind)) continue;
                    float a = Mathf.PI * 0.5f - i * Mathf.PI * 2f / Mathf.Max(1, n);
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var img = UiFactory.Image("Mount_" + m.Kind, items, Color.white, UiFactory.Sprite(m.Icon));
                    img.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dir * 140f, new Vector2(96, 96));
                    _radialItems.Add((m.Kind, img, dir));
                    i++;
                }
                _radialHover = prof.State.SelectedMount;
                _radial.gameObject.SetActive(true);
                ThirdPersonCameraRig.UiWantsCursor = true;
                UpdateRadial();
                return MountKind.None;
            }
            if (_radial == null || !_radial.gameObject.activeSelf) return MountKind.None;
            _radial.gameObject.SetActive(false);
            ThirdPersonCameraRig.UiWantsCursor = false;
            return _radialHover;
        }

        void UpdateRadial()
        {
            var input = InputManager.Instance;
            if (input != null)
            {
                var d = input.PointerPosition - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                if (d.magnitude > 40f)
                {
                    float best = -2f;
                    foreach (var it in _radialItems) { float dot = Vector2.Dot(d.normalized, it.dir); if (dot > best) { best = dot; _radialHover = it.kind; } }
                }
            }
            foreach (var it in _radialItems)
            {
                bool hov = it.kind == _radialHover;
                it.img.rectTransform.localScale = Vector3.one * (hov ? 1.25f : 1f);
                it.img.color = hov ? Color.white : new Color(0.75f, 0.75f, 0.8f, 0.85f);
            }
            var def = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mount(_radialHover) : null;
            _radialLabel.text = def != null ? "<b>" + def.DisplayName + "</b>\n<size=70%>loslassen zum Aufsitzen</size>" : "";
        }
    }
}
