using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Beherrschungs-Fähigkeiten am Spieler: Gleiter (Leertaste in der Luft, ab „Gleiten 1“), Aufwinde (ab „Gleiten 2“).
    /// Umwelt-Objekte (Giftnebel, Sprungpilze, verborgene Truhen, Lava) fragen die Ränge selbst ab.
    /// </summary>
    public class MasteryAbilities : MonoBehaviour
    {
        PlayerController _pc;
        MountController _mc;
        GameObject _wings;
        public bool Gliding { get; private set; }
        public static MasteryAbilities Local { get; private set; }

        void Awake() { _pc = GetComponent<PlayerController>(); _mc = GetComponent<MountController>(); Local = this; }
        void OnDestroy() { if (Local == this) Local = null; }

        static MasteryModel M => PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;

        void Update()
        {
            var input = InputManager.Instance; var m = M;
            if (m == null || input == null) return;
            bool mounted = _mc != null && (_mc.Motor.IsMounted || _mc.Motor.IsChanneling);
            var self = _pc.Combatant;
            if (Gliding && ((!_pc.Airborne && _glideTime > 0.15f) || mounted || self.IsDead || self.IsDowned || input.JumpPressed && _glideTime > 0.2f)) StopGlide();
            else if (!Gliding && input.JumpPressed && _pc.Airborne && !mounted && !self.IsDead && !self.IsDowned && _pc.VerticalVelocity < 3f)
            {
                if (MasteryEffects.CanGlide(m)) StartGlide(m);
                else if (_hintCd <= 0f) { _hintCd = 5f; GameEventBus.Notify("Gleiten erfordert die Beherrschung „Gleiten 1“", new Color(0.6f, 1f, 0.8f)); }
            }
            _hintCd -= Time.deltaTime;
            if (Gliding)
            {
                _glideTime += Time.deltaTime;
                _pc.SpeedOverride = MasteryEffects.GlideSpeed(m);
                _pc.MaxFallSpeed = MasteryEffects.GlideSink(m);
                _pc.ResetFallTracking();
            }
        }

        float _glideTime, _hintCd;

        void StartGlide(MasteryModel m)
        {
            Gliding = true; _glideTime = 0f;
            _pc.GravityScale = 0.35f;
            _pc.MaxFallSpeed = MasteryEffects.GlideSink(m);
            if (_pc.VerticalVelocity < -MasteryEffects.GlideSink(m)) _pc.SetVerticalVelocity(-MasteryEffects.GlideSink(m));
            if (_wings == null) _wings = BuildWings();
            _wings.SetActive(true);
        }

        public void StopGlide()
        {
            if (!Gliding) return;
            Gliding = false;
            _pc.GravityScale = 1f; _pc.MaxFallSpeed = 0f;
            if (_mc == null || !_mc.Motor.IsMounted) _pc.SpeedOverride = 0f;
            _pc.ResetFallTracking();
            if (_wings != null) _wings.SetActive(false);
        }

        /// <summary>Aufwind: hebt einen gleitenden Spieler an (ab „Gleiten 2“).</summary>
        public bool ApplyUpdraft(float strength)
        {
            var m = M;
            if (!Gliding || m == null || !MasteryEffects.UsesUpdrafts(m)) return false;
            _pc.SetVerticalVelocity(strength);
            return true;
        }

        GameObject BuildWings()
        {
            var root = new GameObject("Glider");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0, 2.4f, -0.1f);
            var col = new Color(0.95f, 0.85f, 0.55f);
            for (int i = -1; i <= 1; i += 2)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(w.GetComponent<Collider>());
                w.transform.SetParent(root.transform, false);
                w.transform.localPosition = new Vector3(i * 1.1f, 0.1f, 0);
                w.transform.localRotation = Quaternion.Euler(0, 0, -i * 12f);
                w.transform.localScale = new Vector3(2.2f, 0.05f, 1.1f);
                var r = w.GetComponent<Renderer>();
                var lib = VfxLibrary.Current;
                if (lib != null && lib.FlashSolid != null) { r.sharedMaterial = lib.FlashSolid; var mpb = new MaterialPropertyBlock(); mpb.SetColor("_BaseColor", col); r.SetPropertyBlock(mpb); }
            }
            return root;
        }
    }
}
