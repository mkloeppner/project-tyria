using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M23: Pflicht-Effekte außerhalb von Skills (hängt am GameManager-Objekt): Segen-/Zustands-Anwendung (Element-Spritzer),
    /// Zustands-Dauereffekte am Körper, Stufenaufstieg (goldene Säule), Entdeckung, Wegmarken-Reise, Portal, Beute-Glitzern, Truhe,
    /// Reittier-Aufruf, Beherrschung, Ereignis-Start/-Ende, Trotzleiste zerbrochen, Weltboss-Phase, Ausweich-Nachbild. Combo-Felder/-Enden über ComboTriggered.
    /// </summary>
    public class WorldFx : MonoBehaviour
    {
        public static readonly Dictionary<EffectType, VfxPalette> EffectPalette = new Dictionary<EffectType, VfxPalette>
        {
            { EffectType.Burning, VfxPalette.Feuer }, { EffectType.Poison, VfxPalette.Natur }, { EffectType.Bleeding, VfxPalette.Blut }, { EffectType.Chill, VfxPalette.Frost },
            { EffectType.Confusion, VfxPalette.Arkan }, { EffectType.Fear, VfxPalette.Schatten }, { EffectType.Torment, VfxPalette.Schatten }, { EffectType.Vulnerability, VfxPalette.Blut },
            { EffectType.Might, VfxPalette.Feuer }, { EffectType.Fury, VfxPalette.Glut }, { EffectType.Protection, VfxPalette.Licht }, { EffectType.Regeneration, VfxPalette.Natur },
            { EffectType.Swiftness, VfxPalette.Luft }, { EffectType.Quickness, VfxPalette.Luft }, { EffectType.Stability, VfxPalette.Erde }, { EffectType.Aegis, VfxPalette.Licht },
            { EffectType.Vigor, VfxPalette.Natur }, { EffectType.Resistance, VfxPalette.Arkan }, { EffectType.Resolution, VfxPalette.Licht }, { EffectType.Alacrity, VfxPalette.Arkan },
            { EffectType.Stun, VfxPalette.Licht }, { EffectType.Immobilize, VfxPalette.Erde }, { EffectType.Cripple, VfxPalette.Erde }, { EffectType.Weakness, VfxPalette.Schatten },
            { EffectType.Blind, VfxPalette.Schatten }, { EffectType.Slow, VfxPalette.Arkan }, { EffectType.Taunt, VfxPalette.Feuer },
        };
        /// <summary>Am Körper sichtbare Dauereffekte (Zustände) bzw. sparsame Segens-Auren nur am Spieler (wie GW2: Stabilität, Aegis, Schnelligkeit).</summary>
        public static readonly EffectType[] BodyConditions = { EffectType.Burning, EffectType.Poison, EffectType.Bleeding, EffectType.Chill, EffectType.Confusion, EffectType.Fear };
        public static readonly EffectType[] PlayerBoonAuras = { EffectType.Stability, EffectType.Aegis, EffectType.Quickness };

        public static int Played { get; private set; }
        readonly Dictionary<(Combatant, EffectType), float> _lastBody = new Dictionary<(Combatant, EffectType), float>();
        readonly HashSet<Combatant> _tracked = new HashSet<Combatant>();
        float _scan;
        Combatant Player => GameManager.Instance != null ? GameManager.Instance.Player : null;

        void OnEnable()
        {
            EffectModel.AppliedGlobal += OnEffect;
            GameEventBus.LeveledUp += OnLevel; GameEventBus.AreaEntered += OnArea; GameEventBus.Banner += OnBanner;
            GameEventBus.ItemAcquired += OnItem; GameEventBus.Notification += OnNotify; GameEventBus.ComboTriggered += OnCombo;
            GameEventBus.DodgePerformed += OnDodge; DefianceBar.Broken += OnDefiance; MountController.MountChanged += OnMount;
            EventDirector.ZoneBound += OnZone;
        }

        void OnDisable()
        {
            EffectModel.AppliedGlobal -= OnEffect;
            GameEventBus.LeveledUp -= OnLevel; GameEventBus.AreaEntered -= OnArea; GameEventBus.Banner -= OnBanner;
            GameEventBus.ItemAcquired -= OnItem; GameEventBus.Notification -= OnNotify; GameEventBus.ComboTriggered -= OnCombo;
            GameEventBus.DodgePerformed -= OnDodge; DefianceBar.Broken -= OnDefiance; MountController.MountChanged -= OnMount;
            EventDirector.ZoneBound -= OnZone;
        }

        static void Fx(VfxShape shape, VfxPalette pal, VfxSize size, Vector3 pos, Transform follow = null, float intensity = 1f)
        {
            if (VfxPool.Instance == null) return;
            if (VfxPool.Instance.Play(VfxIds.Compose(shape, pal, size), pos, Quaternion.identity, follow, intensity) != null) Played++;
        }

        bool Near(Vector3 p) { var cam = Camera.main; return cam != null && (cam.transform.position - p).sqrMagnitude < 45f * 45f; }

        void OnEffect(EffectModel m, EffectType t)
        {
            var c = m.Owner; if (c == null || c.IsDead || !Near(c.transform.position)) return;
            if (!EffectPalette.TryGetValue(t, out var pal)) return;
            // Anwendung: kurzer Element-Spritzer am Ziel
            Fx(VfxShape.Impact, pal, VfxSize.S, c.AimPoint, null, c.Team == Team.Ally ? 0.6f : 0.8f);
            _tracked.Add(c);
        }

        void Update()
        {
            _scan -= Time.deltaTime; if (_scan > 0f) return; _scan = 0.5f;
            var dead = new List<Combatant>();
            foreach (var c in _tracked)
            {
                if (c == null || c.IsDead) { dead.Add(c); continue; }
                if (!Near(c.transform.position)) continue;
                bool any = false;
                foreach (var t in BodyConditions) if (c.Effects.Has(t)) { any = true; Body(c, t, VfxSize.S); }
                if (c == Player) foreach (var t in PlayerBoonAuras) if (c.Effects.Has(t)) { any = true; Body(c, t, VfxSize.S); }
                if (!any) dead.Add(c);
            }
            foreach (var d in dead) _tracked.Remove(d);
            var p = Player; if (p != null && !_tracked.Contains(p)) foreach (var t in PlayerBoonAuras) if (p.Effects.Has(t)) { _tracked.Add(p); break; }
        }

        void Body(Combatant c, EffectType t, VfxSize size)
        {
            float now = Time.time;
            if (_lastBody.TryGetValue((c, t), out var last) && now - last < 2.6f) return;
            _lastBody[(c, t)] = now;
            Fx(VfxShape.Aura, EffectPalette[t], size, c.transform.position + Vector3.up * 0.1f, c.transform, c == Player ? 0.5f : 0.7f);
        }

        void OnLevel(Combatant c, int lvl) { if (c != null && c == Player) Fx(VfxShape.Pillar, VfxPalette.Licht, VfxSize.Elite, c.transform.position); }
        void OnArea(string name, string sub) { var p = Player; if (p != null) Fx(VfxShape.Sigil, VfxPalette.Licht, VfxSize.M, p.transform.position, p.transform, 0.6f); }
        void OnZone(ZoneRoot z) { var p = Player; if (p != null) Fx(VfxShape.Shockwave, VfxPalette.Arkan, VfxSize.L, p.transform.position); }
        void OnMount(MountKind k) { var p = Player; if (p != null) Fx(VfxShape.Veil, VfxPalette.Arkan, VfxSize.M, p.transform.position); }
        void OnDefiance(Combatant c) { if (c != null) Fx(VfxShape.Shockwave, VfxPalette.Luft, VfxSize.L, c.transform.position); }
        void OnCombo(ComboEntry e, Vector3 pos) { Fx(VfxShape.Shockwave, VfxPalette.Arkan, VfxSize.M, pos, null, 0.8f); }

        void OnItem(string id, int n)
        {
            var p = Player; if (p == null) return;
            var db = PlayerProfile.Instance != null ? PlayerProfile.Instance.Items : null; var def = db != null ? db.Get(id) : null;
            var r = def != null ? def.Rarity : ItemRarity.Basic;
            // Glitzern nach Seltenheit (Farbe wie GW2: Fein blau, Meisterwerk grün, Selten gelb, Exotisch+ orange)
            var pal = r >= ItemRarity.Exotic ? VfxPalette.Glut : r == ItemRarity.Rare ? VfxPalette.Licht : r == ItemRarity.Masterwork ? VfxPalette.Natur : r == ItemRarity.Fine ? VfxPalette.Wasser : VfxPalette.Frost;
            Fx(VfxShape.Impact, pal, r >= ItemRarity.Rare ? VfxSize.M : VfxSize.S, p.AimPoint + Vector3.up * 0.4f, null, r >= ItemRarity.Fine ? 0.8f : 0.4f);
        }

        void OnNotify(string msg, Color c)
        {
            var p = Player; if (p == null || string.IsNullOrEmpty(msg)) return;
            if (msg.StartsWith("Gereist")) Fx(VfxShape.Pillar, VfxPalette.Arkan, VfxSize.L, p.transform.position);
        }

        void OnBanner(string title, string sub, Color col)
        {
            var p = Player; if (p == null || string.IsNullOrEmpty(title)) return;
            if (title.Contains("Beherrschung")) Fx(VfxShape.Pillar, VfxPalette.Natur, VfxSize.L, p.transform.position);
            else if (title.Contains("Truhe")) Fx(VfxShape.Release, VfxPalette.Licht, VfxSize.L, p.transform.position + Vector3.up);
            else if (title.Contains("Phase") || title.Contains("Anführer")) Fx(VfxShape.Shockwave, VfxPalette.Glut, VfxSize.Elite, p.transform.position);
            else if (title.Contains("Ereignis") || title.Contains("Event")) Fx(VfxShape.Sigil, VfxPalette.Licht, VfxSize.L, p.transform.position, null, 0.7f);
            else if (title.Contains("Entdeckt") || title.Contains("Aussicht") || title.Contains("Wegmarke")) Fx(VfxShape.Sigil, VfxPalette.Luft, VfxSize.M, p.transform.position, p.transform, 0.7f);
        }

        // ---------------- Ausweich-Nachbild ----------------
        readonly Queue<(GameObject go, Mesh mesh, float t)> _ghosts = new Queue<(GameObject, Mesh, float)>();
        Material _ghostMat;
        public Material GhostMaterial;

        void OnDodge(Combatant c)
        {
            if (c == null || c != Player) return;
            var smr = c.GetComponentInChildren<SkinnedMeshRenderer>(); if (smr == null) return;
            var mat = GhostMaterial; if (mat == null) return;
            var mesh = new Mesh(); smr.BakeMesh(mesh, true);
            var go = new GameObject("DodgeGhost"); go.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); var mats = new Material[mesh.subMeshCount]; for (int i = 0; i < mats.Length; i++) mats[i] = mat; r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            StartCoroutine(Fade(go, mesh, r));
        }

        System.Collections.IEnumerator Fade(GameObject go, Mesh mesh, Renderer r)
        {
            var mpb = new MaterialPropertyBlock(); float t = 0f;
            while (t < 0.35f) { t += Time.deltaTime; r.GetPropertyBlock(mpb); mpb.SetFloat("_Dissolve", t / 0.35f); r.SetPropertyBlock(mpb); yield return null; }
            Destroy(go); Destroy(mesh);
        }
    }
}
