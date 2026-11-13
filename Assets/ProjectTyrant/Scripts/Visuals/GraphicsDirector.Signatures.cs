using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G6: Welt-Signaturen – Trotzbalken-Bruch, Auflösen beim Tod, Wegmarken-Reise (Abreise/Ankunft), Reittier-Effekte
    /// (Beschwören, Absitzen, Bewegungsspur, Landung), Rang-Auren und Boss-Signaturen. Rein visuell, alles über den VFX-Baukasten.
    /// </summary>
    public partial class GraphicsDirector
    {
        bool _sigSubscribed; Vector3 _lastPlayerPos; bool _hadPos; float _rankScan, _mountTrail; MountKind _mount = MountKind.None; bool _wasAirborne;
        public static int SignatureEvents { get; private set; }

        /// <summary>Palette eines Reittiers (rein).</summary>
        public static VfxPalette MountPalette(MountKind k)
        {
            switch (k)
            {
                case MountKind.Raptor: return VfxPalette.Glut;
                case MountKind.Springer: return VfxPalette.Natur;
                case MountKind.Skimmer: case MountKind.Skiff: return VfxPalette.Wasser;
                case MountKind.Griffon: return VfxPalette.Himmel;
                case MountKind.Skyscale: return VfxPalette.Feuer;
                case MountKind.DuneRunner: return VfxPalette.Staubgold;
                default: return VfxPalette.Licht;
            }
        }

        /// <summary>Boss-Element je Biom (rein).</summary>
        public static VfxPalette BiomePalette(string biome)
        {
            switch (biome)
            {
                case "glut": return VfxPalette.Lava; case "frost": return VfxPalette.Frost; case "moor": return VfxPalette.Dorngift; case "kueste": return VfxPalette.Wasser;
                case "wueste": return VfxPalette.Staubgold; case "kristall": return VfxPalette.Arkan; case "himmel": return VfxPalette.Himmel; case "jade": return VfxPalette.Jade;
                case "verderbnis": return VfxPalette.Tiefviolett; default: return VfxPalette.Natur;
            }
        }

        /// <summary>Großer Positionssprung = Reise (Wegmarke, Teleport), kein Laufen/Reiten (rein).</summary>
        public static bool IsTravelJump(Vector3 a, Vector3 b, float dt) => (b - a).magnitude > Mathf.Max(40f, dt * 120f);

        void SignaturesTick(float dt)
        {
            if (!_sigSubscribed)
            {
                _sigSubscribed = true;
                DefianceBar.Broken += OnBroken; GameEventBus.EntityKilled += OnKilled; MountController.MountChanged += OnMount;
            }
            var gm = GameManager.Instance; var player = gm != null ? gm.Player : null;
            if (player != null)
            {
                var pos = player.transform.position;
                if (_hadPos && IsTravelJump(_lastPlayerPos, pos, dt))
                {
                    Fx(VfxShape.Pillar, VfxPalette.Licht, VfxSize.L, _lastPlayerPos);   // Abreise
                    Fx(VfxShape.Pillar, VfxPalette.Licht, VfxSize.L, pos); Fx(VfxShape.Shockwave, VfxPalette.Licht, VfxSize.M, pos);   // Ankunft
                    SceneLighting.Shockwaves.Spawn(pos, 6f, 0.4f, 0.8f);
                }
                _lastPlayerPos = pos; _hadPos = true;

                // Reittier: Bewegungsspur und Landung
                if (_mount != MountKind.None)
                {
                    var cc = player.GetComponent<CharacterController>();
                    bool airborne = cc != null && !cc.isGrounded;
                    if (_wasAirborne && !airborne) { Fx(VfxShape.Impact, MountPalette(_mount), VfxSize.M, pos); SceneLighting.Shockwaves.Spawn(pos, 4f, 0.3f, 0.5f); }
                    _wasAirborne = airborne;
                    if ((_mountTrail -= dt) <= 0f && cc != null && cc.velocity.sqrMagnitude > 16f && !airborne) { _mountTrail = 0.35f; Fx(VfxShape.Trail, MountPalette(_mount), VfxSize.S, pos); }
                }
            }

            // Rang-Auren und Boss-Signaturen in Kameranähe
            var cam = Camera.main;
            if ((_rankScan -= dt) <= 0f && cam != null)
            {
                _rankScan = 0.75f;
                foreach (var c in Combatant.All)
                {
                    if (c == null || c.IsDead || c.Team != Team.Enemy) continue;
                    if ((c.transform.position - cam.transform.position).sqrMagnitude > 60f * 60f || c.GetComponent<RankAuraFx>() != null) continue;
                    var b = c.GetComponent<NpcBrain>(); if (b == null || b.Archetype == null) continue;
                    bool boss = IsEventBoss(b);
                    if (!boss && b.Archetype.Tier < EnemyTier.Veteran) continue;
                    var a = c.gameObject.AddComponent<RankAuraFx>();
                    a.Kind = boss ? RankAuraFx.Rank.Boss : b.Archetype.Tier == EnemyTier.Champion ? RankAuraFx.Rank.Champion : RankAuraFx.Rank.Veteran;
                    a.Tint = SkillFx.PaletteColor(BiomePalette(Profile != null ? Profile.Biome : ""));
                }
            }
        }

        static bool IsEventBoss(NpcBrain b) => b.Archetype != null && (b.Archetype.name.Contains("Boss") || b.Archetype.name.Contains("Weltboss"));

        void OnBroken(Combatant c)
        {
            if (c == null) return; SignatureEvents++;
            var p = c.transform.position + Vector3.up * 1.2f;
            Fx(VfxShape.Impact, VfxPalette.Licht, VfxSize.L, p); Fx(VfxShape.Shockwave, VfxPalette.Staubgold, VfxSize.L, c.transform.position);
            SceneLighting.Shockwaves.Spawn(p, 7f, 0.45f, 1f);
            VfxLayerPlayer.CameraKick?.Invoke(Photosensitive.Shake(0.1f), 0.06f);
        }

        void OnKilled(Combatant victim, Combatant killer)
        {
            if (victim == null || victim.Team != Team.Enemy || Camera.main == null) return;
            if ((victim.transform.position - Camera.main.transform.position).sqrMagnitude > 70f * 70f) return;
            SignatureEvents++;
            var rank = victim.GetComponent<RankAuraFx>();
            var pal = rank != null && rank.Kind == RankAuraFx.Rank.Boss ? BiomePalette(Profile != null ? Profile.Biome : "") : rank != null ? VfxPalette.Staubgold : VfxPalette.Zwielicht;
            Fx(VfxShape.Veil, pal, rank != null ? VfxSize.M : VfxSize.S, victim.transform.position);   // Auflösen
            Fx(VfxShape.Release, pal, VfxSize.S, victim.transform.position + Vector3.up);
        }

        void OnMount(MountKind k)
        {
            var gm = GameManager.Instance; if (gm == null || gm.Player == null) return; SignatureEvents++;
            var p = gm.Player.transform.position;
            if (k != MountKind.None) { Fx(VfxShape.Release, MountPalette(k), VfxSize.M, p); Fx(VfxShape.GroundCircle, MountPalette(k), VfxSize.M, p); }   // Beschwören
            else if (_mount != MountKind.None) Fx(VfxShape.Veil, MountPalette(_mount), VfxSize.S, p);   // Absitzen
            _mount = k; _wasAirborne = false;
        }

        static void Fx(VfxShape s, VfxPalette p, VfxSize z, Vector3 pos) => SkillFx.Vfx(VfxIds.Compose(s, p, z), pos, Quaternion.identity, null, 1f);
    }
}
