using System.Collections.Generic;
using UnityEngine;
using ProjectTyrant.Anim;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6: Figur in Szenen – Emotions-Pose beim Sprechen (Talk_&lt;Emotion&gt;), Kopf und Kiefer-Andeutung folgen der Lautstärke-Hüllkurve
    /// der Sprachdatei (RMS je 50 ms), Blick zum Gesprächspartner. Auf Story-Figuren (Prefab) und zur Laufzeit auf dem Spieler.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class StoryActor : MonoBehaviour
    {
        public static readonly List<StoryActor> All = new List<StoryActor>();
        public string Speaker;
        public bool IsPlayer;
        Transform _head, _jaw;
        CharacterAnimator _anim;
        float[] _env = new float[0]; float _t; bool _speaking; float _level;
        Quaternion _jawRest;

        void Awake() { Bind(); }
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        void Bind()
        {
            _anim = GetComponentInChildren<CharacterAnimator>();
            foreach (var t in GetComponentsInChildren<Transform>(true)) { if (t.name == SkeletonDef.Head) _head = t; if (t.name == "Jaw" || t.name == "Mouth") _jaw = t; }
            if (_jaw != null) _jawRest = _jaw.localRotation;
        }

        public static StoryActor ForPlayer(Combatant player)
        {
            var a = player.GetComponent<StoryActor>(); if (a == null) { a = player.gameObject.AddComponent<StoryActor>(); a.Speaker = "held"; a.IsPlayer = true; }
            return a;
        }

        public static StoryActor Spawn(StoryDatabase db, string speaker, Vector3 pos, Vector3 lookAt)
        {
            var prefab = db != null ? db.Figure(speaker) : null; if (prefab == null) return null;
            var go = Instantiate(prefab, pos, Quaternion.identity);
            var d = lookAt - pos; d.y = 0f; if (d.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(d);
            var a = go.GetComponent<StoryActor>() ?? go.AddComponent<StoryActor>(); a.Speaker = speaker;
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            return a;
        }

        public void Speak(StoryLine line) { _env = line != null && line.Envelope != null ? line.Envelope : new float[0]; _t = 0f; _speaking = true; if (_env.Length == 0 && line != null) _env = SyntheticEnvelope(line.Text); }
        public void StopSpeaking(bool end = false) { _speaking = false; if (end && _anim != null) _anim.StopUpper(0.3f); }

        public void SetSpeaking(bool speaking, Emotion e)
        {
            if (_anim == null) return;
            if (speaking) _anim.PlayUpper("Talk_" + e, 0.25f, 1f, true);
            else if (!IsPlayer) _anim.PlayUpper("Talk_Neutral", 0.4f, 0.5f, true);
        }

        public void PlayClip(string clip) { if (_anim != null && !string.IsNullOrEmpty(clip)) _anim.PlayAuto(clip, 0.2f, 1f, true); }

        /// <summary>Ohne Sprachdatei: Silben-Hüllkurve aus dem Text (Vokale offen, Satzzeichen Pausen).</summary>
        public static float[] SyntheticEnvelope(string text)
        {
            var list = new List<float>();
            foreach (char ch in text ?? "")
            {
                char c = char.ToLowerInvariant(ch);
                if ("aeiouäöüy".IndexOf(c) >= 0) { list.Add(0.9f); list.Add(0.6f); }
                else if (c == ' ') list.Add(0.15f);
                else if (".,!?;:–".IndexOf(c) >= 0) { list.Add(0f); list.Add(0f); list.Add(0f); }
                else list.Add(0.35f);
            }
            return list.ToArray();
        }

        void LateUpdate()
        {
            float target = 0f;
            if (_speaking && _env.Length > 0) { _t += Time.deltaTime; int i = Mathf.Min(_env.Length - 1, Mathf.FloorToInt(_t / 0.05f)); target = _env[i]; }
            _level = Mathf.Lerp(_level, target, Time.deltaTime * 18f);
            if (_head != null && _level > 0.01f) _head.localRotation *= Quaternion.Euler(-4f * _level, 0f, 1.5f * Mathf.Sin(_t * 9f) * _level);
            if (_jaw != null) _jaw.localRotation = _jawRest * Quaternion.Euler(18f * _level, 0, 0);
        }
    }
}
