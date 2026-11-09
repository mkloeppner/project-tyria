using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Schaltet das sichtbare Waffenmodell passend zum aktiven Waffenset; Lauf 5 / M25: Klassen-Farben (Primär/Sekundär) über Vertex-Farben.</summary>
    public class PlayerVisuals : MonoBehaviour
    {
        public GameObject GreatswordModel;
        public GameObject StaffModel;
        [Tooltip("Lauf 5: Modelle je WeaponVisual (Index = enum); Zweithand-Teile als zweites Objekt")] public GameObject[] Models = new GameObject[0];
        public GameObject[] OffhandModels = new GameObject[0];
        public Color OrigCloth = new Color(0.3f, 0.45f, 0.75f), OrigTrim = new Color(0.85f, 0.7f, 0.3f);
        public WeaponVisual Current { get; private set; }
        Mesh _origMesh; SkinnedMeshRenderer _smr;

        public void ShowWeapon(WeaponVisual v)
        {
            Current = v;
            if (Models != null && Models.Length > 0)
            {
                for (int i = 0; i < Models.Length; i++) if (Models[i] != null) Models[i].SetActive(i == (int)v);
                for (int i = 0; i < OffhandModels.Length; i++) if (OffhandModels[i] != null) OffhandModels[i].SetActive(i == (int)v);
                return;
            }
            if (GreatswordModel != null) GreatswordModel.SetActive(v == WeaponVisual.Greatsword);
            if (StaffModel != null) StaffModel.SetActive(v == WeaponVisual.Staff);
        }

        /// <summary>Färbt Kleidung (Primär) und Besatz (Sekundär) um: die Figur ist vertex-gefärbt, daher eine Mesh-Kopie mit getauschten Farben.</summary>
        public void ApplyLook(ClassDefinition c, string primaryHex, string secondaryHex)
        {
            if (_smr == null) _smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (_smr == null || _smr.sharedMesh == null) return;
            if (_origMesh == null) _origMesh = _smr.sharedMesh;
            Color p = c != null ? c.Color : OrigCloth, s = c != null ? c.SecondaryColor : OrigTrim;
            if (!string.IsNullOrEmpty(primaryHex)) ColorUtility.TryParseHtmlString(primaryHex, out p);
            if (!string.IsNullOrEmpty(secondaryHex)) ColorUtility.TryParseHtmlString(secondaryHex, out s);
            var m = Instantiate(_origMesh); m.name = _origMesh.name + "_Look";
            var cols = m.colors;
            if (cols == null || cols.Length == 0) return;
            for (int i = 0; i < cols.Length; i++)
            {
                if (Near(cols[i], OrigCloth)) cols[i] = new Color(p.r, p.g, p.b, cols[i].a) * Shade(cols[i], OrigCloth);
                else if (Near(cols[i], OrigTrim)) cols[i] = new Color(s.r, s.g, s.b, cols[i].a) * Shade(cols[i], OrigTrim);
            }
            m.colors = cols;
            if (_smr.sharedMesh != _origMesh) Destroy(_smr.sharedMesh);
            _smr.sharedMesh = m;
        }

        // Lauf 5 / M26: Farbakzent der Elite-Spezialisierung am Toon-Material (Rand-Licht), eigene Material-Instanz nur für den Spieler.
        Material _accentMat; Color _rimOrig; float _rimStrengthOrig; bool _accentInit;
        public Color? Accent { get; private set; }
        public void SetAccent(Color? accent)
        {
            if (_smr == null) _smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (_smr == null) return;
            if (!_accentInit)
            {
                _accentMat = _smr.material;   // Instanz
                _rimOrig = _accentMat.HasProperty("_RimColor") ? _accentMat.GetColor("_RimColor") : Color.white;
                _rimStrengthOrig = _accentMat.HasProperty("_RimStrength") ? _accentMat.GetFloat("_RimStrength") : 0.6f;
                _accentInit = true;
            }
            Accent = accent;
            if (_accentMat == null) return;
            if (_accentMat.HasProperty("_RimColor")) _accentMat.SetColor("_RimColor", accent.HasValue ? accent.Value * 1.4f : _rimOrig);
            if (_accentMat.HasProperty("_RimStrength")) _accentMat.SetFloat("_RimStrength", accent.HasValue ? 1.2f : _rimStrengthOrig);
        }

        static bool Near(Color a, Color b) { float h1, s1, v1, h2, s2, v2; Color.RGBToHSV(a, out h1, out s1, out v1); Color.RGBToHSV(b, out h2, out s2, out v2); float dh = Mathf.Abs(h1 - h2); dh = Mathf.Min(dh, 1f - dh); return dh < 0.04f && Mathf.Abs(s1 - s2) < 0.25f && s1 > 0.15f; }
        static float Shade(Color a, Color b) { float v1 = a.maxColorComponent, v2 = Mathf.Max(0.01f, b.maxColorComponent); float k = v1 / v2; return Mathf.Clamp(k, 0.5f, 1.5f); }
    }
}
