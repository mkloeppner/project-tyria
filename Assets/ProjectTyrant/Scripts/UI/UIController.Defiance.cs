using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 4: Trotzleiste im Zielfenster (türkis; gebrochen = leer mit „Gebrochen“).</summary>
    public partial class UIController
    {
        Bar _defiance;

        partial void UpdateDefiance(Combatant t)
        {
            var d = DefianceBar.Of(t);
            if (_defiance == null)
            {
                var tf = _targetHp.Root.transform.parent as RectTransform;
                _defiance = MakeBar("TargetDefiance", tf, new Vector2(0.5f, 1f), new Vector2(0, -71), new Vector2(440, 8), new Color(0.35f, 0.85f, 1f), 0);
            }
            bool show = d != null;
            if (_defiance.Root.activeSelf != show) _defiance.Root.SetActive(show);
            if (!show) return;
            var m = d.Model;
            SetBar(_defiance, m.Broken ? 0f : m.Fraction);
            _defiance.FillImage.color = m.Broken ? new Color(1f, 0.85f, 0.3f) : m.Refilling ? new Color(0.3f, 0.5f, 0.6f) : new Color(0.35f, 0.85f, 1f);
        }
    }
}
