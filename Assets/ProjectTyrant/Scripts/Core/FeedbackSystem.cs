using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Hit-Partikel und Sounds für Kampf-, Combo- und Event-Feedback (hört auf den GameEventBus).</summary>
    public class FeedbackSystem : MonoBehaviour
    {
        ParticleSystem _sparks;
        AudioSource _audio;
        bool _wasDodging;

        VfxLibrary Lib => VfxLibrary.Current;
        Combatant Player => GameManager.Instance != null ? GameManager.Instance.Player : null;

        void Start()
        {
            var go = new GameObject("HitSparks");
            go.transform.SetParent(transform, false);
            _sparks = go.AddComponent<ParticleSystem>();
            _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _sparks.main;
            main.playOnAwake = false; main.loop = false; main.startLifetime = 0.35f; main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f); main.gravityModifier = 0.8f; main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = _sparks.emission; em.enabled = false;
            var sh = _sparks.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (Lib != null) r.sharedMaterial = Lib.ParticleGlow != null ? Lib.ParticleGlow : Lib.FlashSolid;
        }

        void OnEnable()
        {
            GameEventBus.DamageDealt += OnDamage;
            GameEventBus.SkillCast += OnCast;
            GameEventBus.ComboTriggered += OnCombo;
            GameEventBus.Notification += OnNotify;
            GameEventBus.EntityKilled += OnKilled;
            GameEventBus.Banner += OnBanner;
            GameEventBus.ItemAcquired += OnItem;
            GameEventBus.LeveledUp += OnLevel;
        }

        void OnDisable()
        {
            GameEventBus.DamageDealt -= OnDamage;
            GameEventBus.SkillCast -= OnCast;
            GameEventBus.ComboTriggered -= OnCombo;
            GameEventBus.Notification -= OnNotify;
            GameEventBus.EntityKilled -= OnKilled;
            GameEventBus.Banner -= OnBanner;
            GameEventBus.ItemAcquired -= OnItem;
            GameEventBus.LeveledUp -= OnLevel;
        }

        float _lootCooldown;
        // Lauf 5 / M24: Klänge über den AudioPool (Mixer, Stimmen-Budget); Banner/Benachrichtigungen spielt der MusicDirector (mit Stinger).
        void OnItem(string id, int n)
        {
            if (Time.unscaledTime < _lootCooldown) return; _lootCooldown = Time.unscaledTime + 0.25f;
            var prof = PlayerProfile.Instance; var def = prof != null && prof.Items != null ? prof.Items.Get(id) : null;
            var p = Player; GameAudio.Play(def != null ? "Loot/" + def.Rarity : "Loot/Basic", p != null ? p.transform.position : transform.position);
        }
        void OnLevel(Combatant c, int lvl) { }
        void OnBanner(string title, string sub, Color col) { }

        void Play(AudioClip c, float vol = 1f, float pitch = 1f)
        {
            if (c == null || _audio == null) return;
            _audio.pitch = pitch;
            _audio.PlayOneShot(c, vol);
        }

        void OnDamage(DamageInfo d)
        {
            var p = Player;
            if (p == null || d.IsCondition || d.Evaded) return;
            if (d.Blocked && (d.Source == p || d.Target == p)) { GameAudio.Play("Hit/Block", d.Point, 0f, 1f, VoicePriority.Hit); return; }
            if (d.Amount <= 0f) return;
            if (d.Source != p && d.Target != p) return;
            if (_sparks != null)
            {
                var ep = new ParticleSystem.EmitParams { position = d.Point, applyShapeToPosition = true, startColor = d.Target == p ? new Color(1f, 0.3f, 0.25f) : new Color(1f, 0.85f, 0.45f) };
                _sparks.Emit(ep, d.Amount > 1500f ? 18 : 8);
            }
            var brain = d.Target != null ? d.Target.GetComponent<NpcBrain>() : null;
            var anim = d.Target != null ? d.Target.GetComponent<ProjectTyrant.Anim.AnimDriver>() : null;
            bool creature = anim != null && !string.IsNullOrEmpty(anim.Prefix);
            string mat = d.Blocked ? "Hit/Block" : GameSoundMap.HitMaterial(brain != null && brain.Archetype != null ? brain.Archetype.Palette : (VfxPalette?)null, creature, d.Target == p);
            float db = d.Target == p ? 0f : -2f;
            GameAudio.Play(mat, d.Point, db, Random.Range(0.94f, 1.06f), VoicePriority.Hit);
            if (d.Crit && d.Source == p) GameAudio.Play("Hit/Crit", d.Point, -2f, 1f, VoicePriority.Hit);
        }

        void OnCast(Combatant c, SkillDefinition s) { }   // Aufladung spielt die SFX-Spur der Zeitachse (SkillFxDirector)
        void OnCombo(ComboEntry e, Vector3 p) { GameAudio.Play("Combat/ComboFinish", p); }
        void OnNotify(string msg, Color c) { }

        void OnKilled(Combatant victim, Combatant killer)
        {
            var p = Player;
            if (p == null || victim == null || victim.Team != Team.Enemy) return;
            if (killer != p && !victim.Attackers.Contains(p)) return;
            var brain = victim.GetComponent<NpcBrain>();
            // XP für Kills vergibt jetzt PlayerProfile (Level/Rang-abhängig).
        }

        void Update()
        {
            var p = Player;
            if (p == null || Lib == null) return;
            var pc = p.GetComponent<PlayerController>();
            if (pc == null) return;
            bool dodging = pc.Dodge.IsDodging;
            if (dodging && !_wasDodging) GameAudio.Play("Move/Dodge", p.transform.position, 0f, 1f, VoicePriority.PlayerSkill);
            _wasDodging = dodging;
        }
    }
}
