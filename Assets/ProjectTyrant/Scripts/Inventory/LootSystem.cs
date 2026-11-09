using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Würfelt beim Tod eines Gegners Beute (Loot-Tabelle des Archetyps) und legt einen Beutesack ab.</summary>
    public class LootSystem : MonoBehaviour
    {
        public int Seed = 424242;
        public float BagLifetime = 90f;
        System.Random _rng;

        void Awake() => _rng = new System.Random(Seed);
        void OnEnable() => GameEventBus.EntityKilled += OnKilled;
        void OnDisable() => GameEventBus.EntityKilled -= OnKilled;

        void OnKilled(Combatant victim, Combatant killer)
        {
            var prof = PlayerProfile.Instance;
            if (prof == null || victim == null || victim.Team != Team.Enemy || prof.Player == null) return;
            if (killer != prof.Player && !victim.Attackers.Contains(prof.Player)) return;
            var brain = victim.GetComponent<NpcBrain>();
            var table = brain != null && brain.Archetype != null ? brain.Archetype.Loot : null;
            if (table == null) return;
            var loot = LootRoller.Roll(table, prof.Items, _rng, victim.Level);
            if (loot.IsEmpty) return;
            LootBag.Spawn(victim.transform.position, loot, BagLifetime, victim.Tier == EnemyTier.Champion);
        }
    }

    /// <summary>Beutesack: Glitzer, Plakette „Beute“, F hebt alle Säcke im Umkreis von 5 m auf (Bereichs-Beute).</summary>
    public class LootBag : Interactable
    {
        public static readonly List<LootBag> Bags = new List<LootBag>();
        /// <summary>Lauf 4: Kommandant-Beherrschung „Auto-Beute“.</summary>
        public static System.Func<bool> AutoLoot;
        [System.NonSerialized] public LootResult Loot;
        float _life;
        Transform _plate;

        public static LootBag Spawn(Vector3 pos, LootResult loot, float lifetime, bool big)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "LootBag";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = ZoneAnchors.GroundAt(pos + Vector3.up) + Vector3.up * 0.25f;
            go.transform.localScale = new Vector3(0.45f, 0.4f, 0.45f) * (big ? 1.5f : 1f);
            var lib = VfxLibrary.Current;
            var r = go.GetComponent<Renderer>();
            if (lib != null && lib.ProjectileGlow != null)
            {
                r.sharedMaterial = lib.ProjectileGlow;
                var mpb = new MaterialPropertyBlock();
                var col = big ? new Color(1f, 0.6f, 0.1f) : new Color(0.75f, 0.55f, 0.25f);
                mpb.SetColor("_BaseColor", col); mpb.SetColor("_EmissionColor", col * 0.8f);
                r.SetPropertyBlock(mpb);
            }
            var bag = go.AddComponent<LootBag>();
            bag.Loot = loot; bag._life = lifetime; bag.PromptText = "Aufheben"; bag.Range = 2.5f; bag.Priority = 5;
            // Glitzer
            var ps = new GameObject("Glitter").AddComponent<ParticleSystem>();
            ps.transform.SetParent(go.transform, false);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = 0.8f; main.startSpeed = 0.6f; main.startSize = 0.08f; main.maxParticles = 30; main.loop = true;
            main.startColor = new Color(1f, 0.9f, 0.5f);
            var em = ps.emission; em.rateOverTime = 12f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
            var pm = VfxLibrary.Particle(); if (pm != null) ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = pm;
            ps.Play();
            // Plakette
            var plate = new GameObject("Plate");
            plate.transform.SetParent(go.transform, false);
            plate.transform.localPosition = Vector3.up * 2.2f;
            var tmp = plate.AddComponent<TextMeshPro>();
            tmp.text = "Beute"; tmp.fontSize = 3f; tmp.alignment = TextAlignmentOptions.Center; tmp.color = new Color(1f, 0.85f, 0.4f);
            tmp.rectTransform.sizeDelta = new Vector2(4, 1);
            bag._plate = plate.transform;
            return bag;
        }

        protected override void OnEnable() { base.OnEnable(); Bags.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Bags.Remove(this); }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }
            transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
            if (AutoLoot != null && AutoLoot() && GameOptions.Load().AutoLoot)
            {
                var prof = PlayerProfile.Instance;
                if (prof != null && prof.Player != null && !prof.Player.IsDead && (prof.Player.transform.position - transform.position).sqrMagnitude < 36f) { Interact(prof.Player); return; }
            }
            var cam = Camera.main;
            if (_plate != null && cam != null) _plate.rotation = Quaternion.LookRotation(_plate.position - cam.transform.position);
        }

        public override void Interact(Combatant player)
        {
            var prof = PlayerProfile.Instance;
            if (prof == null) return;
            // Bereichs-Beute: alle Säcke im Umkreis von 5 m
            var near = new List<LootBag>();
            foreach (var b in Bags) if (b != null && (b.transform.position - player.transform.position).sqrMagnitude <= 25f) near.Add(b);
            if (!near.Contains(this)) near.Add(this);
            foreach (var b in near)
            {
                var rest = prof.GrantLoot(b.Loot, "Beute");
                if (rest.Items.Count == 0) Destroy(b.gameObject);
                else { b.Loot = rest; }
            }
        }
    }
}
