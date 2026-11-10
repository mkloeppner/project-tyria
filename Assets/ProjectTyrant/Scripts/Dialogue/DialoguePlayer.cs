using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6: spielt Szenen in drei Formen (GW2): Gesprächsszene (zwei Figuren vor gemalter Kulisse), In-World-Cinematic
    /// (Letterbox, Schuss/Gegenschuss, Akteure in der Welt) und Sprechblase (ohne Kamera). Zeilen laufen mit der Sprachdauer + 0,4 s,
    /// Leertaste/Klick springt weiter, Esc fragt „Überspringen?“, Wahl-Stellen zählen Persönlichkeit und setzen Flags.
    /// Am GameManager-Objekt (kein Singleton).
    /// </summary>
    public class DialoguePlayer : MonoBehaviour, IInvulnerabilitySource
    {
        public const float LineGap = 0.4f;
        int _entryIdx;
        public bool Playing { get; private set; }
        public bool IsInvulnerable => Playing && _form != SceneForm.Bubble;
        public DialogueScript CurrentScene { get; private set; }
        public StoryLine CurrentLine { get; private set; }
        public float LineProgress { get; private set; }
        public static float TextSpeed = 1f;            // Option „Textgeschwindigkeit“ (Dauer ohne Sprache)
        public static bool AutoAdvance = true;          // Option „Dialoge automatisch fortsetzen“
        public static bool HeroVoiceB;                  // Stimm-Variante des Helden (Charaktererstellung)

        SceneForm _form;
        AudioSource _voice;
        bool _next, _skipRequested, _skip;
        int _chosen = -1;
        readonly Dictionary<string, StoryActor> _actors = new Dictionary<string, StoryActor>();
        readonly List<GameObject> _temp = new List<GameObject>();
        ConversationStage _stage;
        CinemachineCamera _cam;

        StoryDatabase Db => StoryDatabase.Loaded;
        StoryState State => PlayerProfile.Instance != null ? PlayerProfile.Instance.State.Story : null;

        void Awake()
        {
            _voice = gameObject.AddComponent<AudioSource>(); _voice.playOnAwake = false; _voice.spatialBlend = 0f; _voice.priority = 0;
        }

        public void Play(DialogueScript sc, Action done)
        {
            if (sc == null) { done?.Invoke(); return; }
            if (Playing) { StartCoroutine(Queue(sc, done)); return; }
            StartCoroutine(Run(sc, done));
        }

        IEnumerator Queue(DialogueScript sc, Action done) { while (Playing) yield return null; yield return Run(sc, done); }

        /// <summary>Weiter (Leertaste/Klick) – auch aus der UI.</summary>
        public void Next() => _next = true;
        public void RequestSkip() => _skipRequested = true;
        public void Choose(int i) => _chosen = i;

        IEnumerator Run(DialogueScript sc, Action done)
        {
            Playing = true; CurrentScene = sc; _form = sc.Form; _skip = false; _skipRequested = false;
            var prof = PlayerProfile.Instance; var player = prof != null ? prof.Player : null;
            var pc = player != null ? player.GetComponent<PlayerController>() : null;
            var caster = player != null ? player.GetComponent<SkillCaster>() : null;
            var ui = UIController.Instance;
            bool seen = State != null && State.SeenScenes.Contains(sc.Id);
            var mixer = AudioPool.Current != null ? AudioPool.Current.Mixer : null;
            if (_form != SceneForm.Bubble)
            {
                if (pc != null) pc.InputBlocked = true;
                if (caster != null) caster.InputSuppressed = true;
                if (player != null && !player.InvulnerabilitySources.Contains(this)) player.InvulnerabilitySources.Add(this);
                ThirdPersonCameraRig.CinematicActive = true;
                if (mixer != null) mixer.Dialogue = true;
                HideNameplates(true);
            }
            var entries = StoryModel.Entries(sc, State);
            SetupForm(sc, entries, player);
            ui?.DialogueOpen(sc.Form, sc.Form == SceneForm.Cinematic);
            if (mixer != null && _form != SceneForm.Bubble) Debug.Log("[ProjectTyrant] Mixer während Szene " + sc.Id + ": Stimme " + mixer.GainDb(MixGroup.Voice).ToString("0.0") + " dB · Musik " + mixer.GainDb(MixGroup.Music).ToString("0.0") + " dB");

            for (int i = 0; i < entries.Count && !_skip; i++)
            {
                var e = entries[i]; _entryIdx = i;
                yield return PlayLine(StoryModel.Resolve(Db.Text, e.Line, State), e, seen || sc.SkipAllowed);
                if (_skip) break;
                if (e.ChoiceIndex >= 0 && e.ChoiceIndex < sc.Choices.Count)
                {
                    var ch = sc.Choices[e.ChoiceIndex];
                    _chosen = -1;
                    ui?.DialogueChoices(ch, Db.Text, State);
                    while (_chosen < 0) { _skipRequested = false; yield return null; }   // Wahl-Stellen lassen sich nicht überspringen
                    ui?.DialogueChoices(null, null, null);
                    var opt = ch.Options[Mathf.Clamp(_chosen, 0, ch.Options.Count - 1)];
                    StoryModel.Choose(State, opt); _entryIdx = i + 1;   // Wahl erledigt
                    yield return PlayLine(StoryModel.Resolve(Db.Text, opt.Line, State), new DialogueEntry(), true);
                    if (!string.IsNullOrEmpty(opt.Reply)) yield return PlayLine(StoryModel.Resolve(Db.Text, opt.Reply, State), new DialogueEntry(), true);
                }
            }

            // Übersprungen: offene Wahl-Stellen trotzdem stellen (Bündnis-Wahl, Flags dürfen nicht verloren gehen)
            if (_skip)
            {
                _voice.Stop();
                for (int k = _entryIdx; k < entries.Count; k++)
                {
                    var e = entries[k]; if (e.ChoiceIndex < 0 || e.ChoiceIndex >= sc.Choices.Count) continue;
                    var ch = sc.Choices[e.ChoiceIndex]; bool answered = false;
                    foreach (var o in ch.Options) if (!string.IsNullOrEmpty(o.Flag) && State.HasFlag(o.Flag)) answered = true;
                    if (answered) continue;
                    _chosen = -1; ui?.DialogueChoices(ch, Db.Text, State);
                    while (_chosen < 0) { if (ui == null) _chosen = 0; yield return null; }
                    ui?.DialogueChoices(null, null, null);
                    StoryModel.Choose(State, ch.Options[Mathf.Clamp(_chosen, 0, ch.Options.Count - 1)]);
                }
            }

            _voice.Stop();
            Cleanup();
            ui?.DialogueClose();
            if (_form != SceneForm.Bubble)
            {
                if (pc != null) pc.InputBlocked = false;
                if (caster != null) caster.InputSuppressed = false;
                if (player != null) player.InvulnerabilitySources.Remove(this);
                ThirdPersonCameraRig.CinematicActive = false;
                if (mixer != null) mixer.Dialogue = false;
                HideNameplates(false);
            }
            Playing = false; CurrentScene = null; CurrentLine = null;
            if (_skip) GameEventBus.Notify("Szene übersprungen", new Color(0.8f, 0.8f, 0.85f));
            done?.Invoke();
        }

        IEnumerator PlayLine(StoryLine line, DialogueEntry e, bool skippable)
        {
            if (line == null) yield break;
            CurrentLine = line; _next = false;
            var prof = Db.Cast.Get(line.Speaker);
            var clip = line.Voice;
            if (line.Speaker == "held" && HeroVoiceB && line.VoiceB != null) clip = line.VoiceB;
            float dur = clip != null ? clip.length : Mathf.Max(1.6f, line.Words * 0.42f) / Mathf.Max(0.25f, TextSpeed);
            dur += LineGap + (e != null ? e.Wait : 0f);
            if (clip != null)
            {
                _voice.clip = clip; _voice.pitch = line.Speaker == "held" ? HeroPitch() : 1f;
                var mixer = AudioPool.Current != null ? AudioPool.Current.Mixer : null;
                _voice.volume = mixer != null ? mixer.Gain(MixGroup.Voice) : 1f;
                _voice.Play();
            }
            var actor = Actor(line.Speaker);
            foreach (var kv in _actors) kv.Value.SetSpeaking(kv.Value == actor, line.Emotion);
            if (actor != null) { actor.Speak(line); if (_form == SceneForm.Cinematic) FrameShot(actor, line.Camera); }
            if (_stage != null) _stage.Highlight(line.Speaker);
            string shown = SpeakerName(line.Speaker, prof);
            UIController.Instance?.DialogueLine(shown, prof != null ? prof.Color : Color.white, line.Text, _form, actor != null ? actor.transform : null);
            UIController.Instance?.AddChatLine(shown, line.Text, prof != null ? prof.Color : Color.white);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime; LineProgress = t / dur;
                if (_next && t > 0.15f) break;
                if (_skipRequested) { _skipRequested = false; if (skippable) { bool? ans = null; UIController.Instance?.ConfirmSkip(b => ans = b); if (UIController.Instance == null) ans = true; while (!ans.HasValue) yield return null; if (ans.Value) { _skip = true; yield break; } } }
                if (!AutoAdvance && t >= dur - 0.05f) { while (!_next && !_skip) yield return null; break; }
                yield return null;
            }
            _voice.Stop();
            if (actor != null) actor.StopSpeaking();
        }

        float HeroPitch()
        {
            var c = PlayerProfile.Instance != null ? PlayerProfile.Instance.State.ClassId : "";
            switch (c) { case "waechter": return 0.97f; case "wildhueter": return 1.02f; case "elementarweber": return 1.05f; case "schattenklinge": return 0.99f; }
            return 1f;
        }

        /// <summary>Held trägt den Namen des Charakters statt „Held“.</summary>
        public static string SpeakerName(string speaker, SpeakerProfile prof)
        {
            if (speaker == "held") { var c = PlayerProfile.Instance != null ? PlayerProfile.Instance.ActiveCharacter : null; if (c != null && !string.IsNullOrEmpty(c.Name)) return c.Name; }
            return prof != null ? prof.DisplayName : speaker;
        }

        readonly List<Renderer> _hiddenPlates = new List<Renderer>();
        /// <summary>Welt-Namensschilder (Billboard-Texte) während Gesprächsszene/Cinematic ausblenden.</summary>
        void HideNameplates(bool hide)
        {
            if (hide)
            {
                _hiddenPlates.Clear();
                foreach (var b in FindObjectsByType<Billboard>(FindObjectsInactive.Exclude))
                    if (b.GetComponent<TMPro.TextMeshPro>() != null)
                    foreach (var r in b.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; _hiddenPlates.Add(r); }
            }
            else { foreach (var r in _hiddenPlates) if (r != null) r.enabled = true; _hiddenPlates.Clear(); }
        }

        StoryActor Actor(string speaker) => speaker != null && _actors.TryGetValue(speaker, out var a) ? a : null;

        // ------------------------------------------------------------ Formen
        void SetupForm(DialogueScript sc, List<DialogueEntry> entries, Combatant player)
        {
            var speakers = new List<string>();
            foreach (var e in entries) { var l = Db.Text.Get(e.Line); if (l != null && !speakers.Contains(l.Speaker)) speakers.Add(l.Speaker); }
            foreach (var c in sc.Choices) foreach (var o in c.Options) { var r = Db.Text.Get(o.Reply); if (r != null && !speakers.Contains(r.Speaker)) speakers.Add(r.Speaker); }
            switch (sc.Form)
            {
                case SceneForm.Conversation:
                    _stage = ConversationStage.Open(Db, sc, Backdrop(sc.Backdrop));
                    foreach (var kv in _stage.Actors) _actors[kv.Key] = kv.Value;
                    _cam = MakeCam(_stage.CameraPose.position, _stage.CameraPose.rotation, 40f);
                    break;
                case SceneForm.Cinematic:
                    var cut = sc.Cutscene;
                    Vector3 origin = player != null ? player.transform.position : Vector3.zero;
                    Vector3 fwd = player != null ? player.transform.forward : Vector3.forward;
                    int i = 0;
                    foreach (var sp in speakers)
                    {
                        Vector3 pos;
                        if (sp == "held" && player != null) { _actors[sp] = StoryActor.ForPlayer(player); i++; continue; }
                        var existing = FindWorldActor(sp);
                        if (existing != null) { _actors[sp] = existing; i++; continue; }
                        var m = cut != null ? cut.Actors.Find(a => a.Speaker == sp) : null;
                        if (m != null && cut.HasMarker(m.Marker)) pos = origin + Quaternion.LookRotation(fwd) * (cut.Marker(m.Marker) - cut.Origin);
                        else { float ang = (i % 2 == 0 ? 1 : -1) * (35f + 20f * (i / 2)); pos = origin + Quaternion.Euler(0, ang, 0) * fwd * (3.2f + i * 0.6f); }
                        var a = StoryActor.Spawn(Db, sp, ZoneAnchors.GroundAt(pos + Vector3.up * 3f), origin);
                        if (a != null) { _actors[sp] = a; _temp.Add(a.gameObject); if (m != null && !string.IsNullOrEmpty(m.Clip)) a.PlayClip(m.Clip); }
                        i++;
                    }
                    var look = origin + Vector3.up * 1.5f;
                    _cam = MakeCam(origin - fwd * 6f + Vector3.up * 3f, Quaternion.LookRotation(look - (origin - fwd * 6f + Vector3.up * 3f)), 50f);
                    break;
                case SceneForm.Bubble:
                    foreach (var sp in speakers) { var a = sp == "held" && player != null ? StoryActor.ForPlayer(player) : FindWorldActor(sp); if (a != null) _actors[sp] = a; }
                    break;
            }
        }

        StoryActor FindWorldActor(string speaker)
        {
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            StoryActor best = null; float bd = 40f * 40f;
            foreach (var a in StoryActor.All) if (a != null && a.Speaker == speaker && !a.IsPlayer) { float d = p != null ? (a.transform.position - p.transform.position).sqrMagnitude : 0f; if (d < bd) { bd = d; best = a; } }
            return best;
        }

        CinemachineCamera MakeCam(Vector3 pos, Quaternion rot, float fov)
        {
            var go = new GameObject("PT_DialogueCam"); go.transform.SetPositionAndRotation(pos, rot);
            var cam = go.AddComponent<CinemachineCamera>(); cam.Priority = 1200; cam.Lens.FieldOfView = fov; cam.Lens.NearClipPlane = 0.1f;
            _temp.Add(go); return cam;
        }

        /// <summary>Schuss/Gegenschuss nach Kamera-Hinweis der Zeile (nah, total, links, rechts).</summary>
        void FrameShot(StoryActor a, string cue)
        {
            if (_cam == null || a == null) return;
            var head = a.transform.position + Vector3.up * 1.6f;
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            var other = p != null && !a.IsPlayer ? p.transform.position + Vector3.up * 1.5f : head + a.transform.forward * 3f;
            Vector3 dir = (head - other); dir.y = 0f; if (dir.sqrMagnitude < 0.01f) dir = -a.transform.forward; dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            Vector3 pos;
            switch (cue)
            {
                case "total": pos = head - dir * 7.5f + side * 2f + Vector3.up * 2.5f; break;
                case "links": pos = head - dir * 2.6f - side * 1.2f + Vector3.up * 0.2f; break;
                case "rechts": pos = head - dir * 2.6f + side * 1.2f + Vector3.up * 0.2f; break;
                default: pos = head - (head - other).normalized * 2.4f + side * 0.8f; break;   // nah: über die Schulter des Gegenübers
            }
            pos = Unblocked(head, pos);
            _cam.transform.position = pos; _cam.transform.rotation = Quaternion.LookRotation(head - pos);
        }

        /// <summary>Verdeckung vermeiden: ist die Sicht vom Kopf zur Kamera blockiert (Hauswand, Fels), um den Sprecher herum
        /// schwenken (±25°…±150°); bleibt alles verdeckt, rückt die Kamera vor das Hindernis.</summary>
        public static Vector3 Unblocked(Vector3 head, Vector3 pos)
        {
            const int mask = ~(1 << 2);   // ohne „Ignore Raycast“
            bool Clear(Vector3 p) { var d = p - head; return Block(head, d, mask) < 0f; }
            if (Clear(pos)) return pos;
            var off = pos - head;
            for (int k = 1; k <= 6; k++)
                foreach (int sgn in new[] { 1, -1 })
                {
                    var p = head + Quaternion.Euler(0f, sgn * 25f * k, 0f) * off;
                    if (Clear(p)) return p;
                }
            float dist = Block(head, off, mask);
            return dist >= 0f ? head + off.normalized * Mathf.Max(0.8f, dist - 0.3f) : pos;
        }

        /// <summary>Abstand zum ersten Hindernis (Figuren zählen nicht), −1 = frei.</summary>
        static float Block(Vector3 from, Vector3 d, int mask)
        {
            float best = -1f;
            foreach (var h in Physics.SphereCastAll(from, 0.25f, d.normalized, d.magnitude, mask, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.GetComponentInParent<Combatant>() != null || h.collider.GetComponentInParent<StoryActor>() != null || h.collider is CharacterController) continue;
                if (best < 0f || h.distance < best) best = h.distance;
            }
            return best;
        }

        Texture2D Backdrop(string zone)
        {
            if (Db == null) return null;
            foreach (var t in Db.Backdrops) if (t != null && t.name == "Backdrop_" + zone) return t;
            return Db.Backdrops.Count > 0 ? Db.Backdrops[0] : null;
        }

        void Cleanup()
        {
            foreach (var kv in _actors) if (kv.Value != null) kv.Value.StopSpeaking(true);
            _actors.Clear();
            foreach (var g in _temp) if (g != null) Destroy(g);
            _temp.Clear(); _cam = null;
            if (_stage != null) { _stage.Close(); _stage = null; }
        }
    }
}
