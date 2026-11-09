using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Segen-/Zustandsleiste: Icon, Stapelzahl, Restdauer-Ring. Gepoolt, ohne Allokationen pro Frame.</summary>
    public class EffectBar
    {
        class Slot
        {
            public RectTransform Root; public Image Icon, Ring, Frame; public TextMeshProUGUI Stacks; public EffectType Type; public int LastStacks = -1;
        }

        readonly RectTransform _root;
        readonly List<Slot> _slots = new List<Slot>();
        readonly float _size;
        readonly bool _rightToLeft;
        readonly bool _boons;
        readonly List<EffectType> _boonList = new List<EffectType>(), _condList = new List<EffectType>();
        Combatant _target;
        bool _dirty = true;
        static readonly string[] Numbers = BuildNumbers();

        static string[] BuildNumbers() { var a = new string[26]; for (int i = 0; i < a.Length; i++) a[i] = i <= 1 ? "" : i.ToString(); return a; }

        public EffectBar(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, float size, bool rightToLeft, bool boons)
        {
            _size = size; _rightToLeft = rightToLeft; _boons = boons;
            _root = UiFactory.Rect(name, parent).Place(anchor, anchor, pivot, pos, new Vector2(size * 12, size));
        }

        public void Bind(Combatant c)
        {
            if (_target == c) return;
            if (_target != null) _target.Effects.Changed -= MarkDirty;
            _target = c;
            if (_target != null) _target.Effects.Changed += MarkDirty;
            _dirty = true;
        }

        void MarkDirty() => _dirty = true;

        Slot MakeSlot()
        {
            var s = new Slot();
            var frame = UiFactory.Image("Effect", _root, new Color(0, 0, 0, 0.7f), UiFactory.Sprite("ui_round"), true);
            s.Root = frame.rectTransform; s.Frame = frame;
            s.Root.sizeDelta = new Vector2(_size - 2, _size - 2);
            s.Root.anchorMin = s.Root.anchorMax = new Vector2(_rightToLeft ? 1 : 0, 0.5f);
            s.Root.pivot = new Vector2(_rightToLeft ? 1 : 0, 0.5f);
            s.Icon = UiFactory.Image("Icon", s.Root, Color.white);
            s.Icon.rectTransform.Stretch(2, 2, 2, 2);
            s.Ring = UiFactory.Image("Ring", s.Root, new Color(1, 1, 1, 0.85f), UiFactory.Sprite("ui_ring"));
            s.Ring.rectTransform.Stretch(-1, -1, -1, -1);
            s.Ring.type = Image.Type.Filled; s.Ring.fillMethod = Image.FillMethod.Radial360; s.Ring.fillOrigin = 2; s.Ring.fillClockwise = false;
            s.Stacks = UiFactory.Text("Stacks", s.Root, "", _size * 0.42f, TextAlignmentOptions.BottomRight);
            s.Stacks.rectTransform.Stretch(0, 1, 0, 0);
            var trig = frame.gameObject.AddComponent<UiTooltipTrigger>();
            trig.Provider = () => _target == null ? null : TooltipBuilder.Effect(_target, s.Type);
            _slots.Add(s);
            return s;
        }

        public void Update()
        {
            if (_target == null || _target.IsDead)
            {
                for (int i = 0; i < _slots.Count; i++) _slots[i].Root.gameObject.SetActive(false);
                return;
            }
            var list = _boons ? _boonList : _condList;
            if (_dirty)
            {
                _dirty = false;
                _target.Effects.FillActiveTypes(_boonList, _condList);
                while (_slots.Count < list.Count) MakeSlot();
                for (int i = 0; i < _slots.Count; i++)
                {
                    var s = _slots[i];
                    bool on = i < list.Count;
                    s.Root.gameObject.SetActive(on);
                    if (!on) continue;
                    s.Type = list[i];
                    var sprite = UiFactory.Sprite(IconLibrary.EffectId(s.Type));
                    s.Icon.sprite = sprite;
                    s.Icon.color = sprite != null ? Color.white : EffectLibrary.Color(s.Type);
                    s.Frame.color = EffectLibrary.IsBoon(s.Type) ? new Color(0.15f, 0.25f, 0.4f, 0.9f) : new Color(0.4f, 0.1f, 0.1f, 0.9f);
                    s.LastStacks = -1;
                    float x = i * _size * (_rightToLeft ? -1 : 1);
                    s.Root.anchoredPosition = new Vector2(x, 0);
                }
            }
            for (int i = 0; i < list.Count && i < _slots.Count; i++)
            {
                var s = _slots[i];
                s.Ring.fillAmount = _target.Effects.GetFraction(s.Type);
                int n = _target.Effects.GetStacks(s.Type);
                if (n != s.LastStacks) { s.LastStacks = n; s.Stacks.text = Numbers[Mathf.Clamp(n, 0, 25)]; }
            }
        }
    }
}
