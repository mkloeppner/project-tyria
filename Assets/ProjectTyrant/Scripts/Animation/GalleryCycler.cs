using UnityEngine;

namespace ProjectTyrant.Anim
{
    /// <summary>QA-Szene PT_AnimGallery: spielt eine Liste von Clips nacheinander ab (Fortbewegung mit simulierter Geschwindigkeit), Beschriftung zeigt den Clip.</summary>
    public class GalleryCycler : MonoBehaviour
    {
        public CharacterAnimator Anim;
        public string[] Clips = new string[0];
        public float Hold = 2.5f;
        public TMPro.TextMeshPro Label;
        int _i = -1; float _t;
        public string Current => _i >= 0 && _i < Clips.Length ? Clips[_i] : null;

        void Update()
        {
            if (Anim == null || Clips.Length == 0) return;
            _t -= Time.deltaTime;
            if (_t <= 0f) Next();
            var c = Current;
            Vector3 v = Vector3.zero;
            if (c != null)
            {
                string n = c.Contains("_") ? c.Substring(c.LastIndexOf('_') + 1) : c;
                int li = System.Array.IndexOf(LocomotionBlend.Names, n);
                if (n == "MRun") v = transform.forward * Anim.RunSpeed;
                else if (li > 0) v = Quaternion.LookRotation(transform.forward) * (li == LocomotionBlend.StrafeL ? Vector3.left : li == LocomotionBlend.StrafeR ? Vector3.right : li == LocomotionBlend.Back ? Vector3.back : Vector3.forward)
                        * (li == LocomotionBlend.Walk ? Anim.WalkSpeed : li == LocomotionBlend.Run ? Anim.RunSpeed : li == LocomotionBlend.Sprint ? Anim.SprintSpeed : LocomotionBlend.NominalSpeed[li]);
            }
            Anim.SetMotion(v, true);
        }

        void Next()
        {
            _i = (_i + 1) % Clips.Length;
            var c = Clips[_i];
            var e = Anim.Find(c);
            _t = e != null && !e.Loop ? Mathf.Max(1.2f, e.Length + 0.6f) : Hold;
            Anim.StopAll(0.15f);
            if (e != null && e.Layer != ClipLayer.Locomotion) Anim.PlayAuto(c, 0.15f);
            if (Label != null) Label.text = name + "\n<size=70%>" + c + "</size>";
        }
    }
}
