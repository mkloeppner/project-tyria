using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M22: Umriss um das aktuelle Ziel in Rangfarbe (Trash weiß, Veteran silber, Champion gold, Verbündete grün) –
    /// hängt ein Umriss-Material an die Renderer des Ziels und entfernt es beim Zielwechsel.
    /// </summary>
    public class TargetOutline : MonoBehaviour
    {
        public Material OutlineMaterial;
        TargetingSystem _t;
        Combatant _current;
        readonly List<(Renderer r, Material[] original)> _applied = new List<(Renderer, Material[])>();
        readonly Dictionary<Color, Material> _byColor = new Dictionary<Color, Material>();
        public Combatant Outlined => _current;

        public static Color RankColor(Combatant c)
        {
            if (c == null) return Color.white;
            if (c.Team == Team.Ally || c.Team == Team.Player) return new Color(0.45f, 1f, 0.5f);
            switch (c.Tier)
            {
                case EnemyTier.Champion: return new Color(1f, 0.78f, 0.25f);
                case EnemyTier.Veteran: return new Color(0.8f, 0.85f, 0.95f);
                default: return new Color(1f, 1f, 1f, 1f);
            }
        }

        GameObject _ring; Renderer _ringR; MaterialPropertyBlock _ringMpb;

        void Awake() { _t = GetComponent<TargetingSystem>(); }

        /// <summary>Zielkreis unter dem Ziel in Rangfarbe (GW2-Lesbarkeit), folgt dem Ziel.</summary>
        void UpdateRing(Combatant c)
        {
            var lib = VfxLibrary.Current;
            if (c == null || lib == null || lib.GroundMarker == null) { if (_ring != null) _ring.SetActive(false); return; }
            if (_ring == null)
            {
                _ring = GameObject.CreatePrimitive(PrimitiveType.Quad); _ring.name = "TargetRing";
                Destroy(_ring.GetComponent<Collider>());
                _ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _ringR = _ring.GetComponent<Renderer>(); _ringR.sharedMaterial = lib.GroundMarker; _ringMpb = new MaterialPropertyBlock();
                _ringR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _ring.SetActive(true);
            var col = c.GetComponent<CapsuleCollider>();
            float r = Mathf.Max(0.6f, (col != null ? col.radius : 0.5f) * 1.6f) * c.transform.lossyScale.x;
            _ring.transform.position = c.transform.position + Vector3.up * 0.06f;
            _ring.transform.localScale = new Vector3(r * 2f, r * 2f, 1f);
            var rc = RankColor(c); rc.a = 0.75f;
            _ringR.GetPropertyBlock(_ringMpb); _ringMpb.SetColor("_Color", rc); _ringMpb.SetFloat("_Fill", 0f); _ringMpb.SetFloat("_Pulse", 0.5f); _ringR.SetPropertyBlock(_ringMpb);
        }

        void OnDestroy() { if (_ring != null) Destroy(_ring); }

        void LateUpdate()
        {
            var c = _t != null ? _t.Current : null;
            if (c != null && c.IsDead) c = null;
            UpdateRing(c);
            if (c == _current) return;
            Clear();
            _current = c;
            if (c == null || OutlineMaterial == null) return;
            var col = RankColor(c);
            if (!_byColor.TryGetValue(col, out var m)) { m = new Material(OutlineMaterial); m.SetColor("_OutlineColor", col); _byColor[col] = m; }
            foreach (var r in c.GetComponentsInChildren<Renderer>())
            {
                if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)) continue;
                if (r.GetComponent<TMPro.TMP_Text>() != null) continue;
                var orig = r.sharedMaterials;
                var mats = new Material[orig.Length + 1];
                for (int i = 0; i < orig.Length; i++) mats[i] = orig[i];
                mats[orig.Length] = m;
                r.sharedMaterials = mats;
                _applied.Add((r, orig));
            }
        }

        void Clear()
        {
            foreach (var (r, orig) in _applied) if (r != null) r.sharedMaterials = orig;
            _applied.Clear();
        }

        void OnDisable() { Clear(); _current = null; if (_ring != null) _ring.SetActive(false); }
    }
}
