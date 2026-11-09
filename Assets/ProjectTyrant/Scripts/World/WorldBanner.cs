using UnityEngine;

namespace ProjectTyrant
{
    public enum Faction { Neutral, Ally, Enemy }

    /// <summary>Banner, deren Farbe den Besitzer eines Ortes zeigt (Welt-Reaktion auf Events).</summary>
    public class WorldBanner : MonoBehaviour
    {
        public Renderer Cloth;
        public Faction Owner = Faction.Ally;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock _mpb;
        float _phase;

        void Awake() { _phase = Random.value * 10f; Apply(); }

        public void SetOwner(Faction f) { Owner = f; Apply(); }

        void Apply()
        {
            if (Cloth == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Color c = Owner == Faction.Ally ? Palette.AllyBlue : Owner == Faction.Enemy ? Palette.EnemyRed : new Color(0.8f, 0.8f, 0.75f);
            Cloth.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColor, c);
            Cloth.SetPropertyBlock(_mpb);
        }

        void Update()
        {
            if (Cloth == null) return;
            float t = Time.time * 2.2f + _phase;
            Cloth.transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(t) * 8f, Mathf.Sin(t * 1.7f) * 3f);
        }
    }
}
