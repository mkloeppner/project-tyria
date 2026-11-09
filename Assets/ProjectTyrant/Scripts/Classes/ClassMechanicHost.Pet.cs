using UnityEngine;

namespace ProjectTyrant
{
    public partial class ClassMechanicHost
    {
        NpcBrain _pet; float _petCheck;
        public NpcBrain PetBrain => _pet;
        public Combatant PetCombatant => _pet != null ? _pet.Self : null;

        void SummonPet()
        {
            DespawnPet();
            if (Kind != ClassMechanicKind.Pet || _self == null || _merged) return;
            var arch = PetArchetypes[(int)Pet.Current]; if (arch == null) return;
            var pos = transform.position - transform.forward * 2f + transform.right * 1.2f;
            _pet = NpcBrain.Spawn(arch, pos, transform.rotation);
            if (_pet == null) return;
            _pet.UseLeash = false; _pet.DestroyOnDeath = false; _pet.WanderRadius = 1.5f; _pet.Passive = Pet.Passive;
            _pet.LevelOverride = _self.Level; _pet.ApplyArchetype();
            var c = _pet.Self; c.Team = Team.Ally; c.Level = _self.Level; c.DisplayName = PetModel.DisplayName(Pet.Current) + " (Gefährte)";
            c.Stats.MarkDirty();
            c.SetMaxHealth(Mathf.Max(1000f, _self.MaxHealth * 0.8f * PetModel.HealthFactor(Pet.Current)), true);
            c.Died += OnPetDied;
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Pillar, VfxPalette.Natur, VfxSize.S), _pet.transform.position, Quaternion.identity, null, 0.75f);
            GameAudio.Play("Mount/Call", _pet.transform.position, -2f, Pet.Current == PetFamily.Bear ? 0.7f : Pet.Current == PetFamily.Falcon ? 1.5f : 1f);
        }

        void DespawnPet()
        {
            if (_pet != null) { if (_pet.Self != null) _pet.Self.Died -= OnPetDied; Destroy(_pet.gameObject); }
            _pet = null;
        }

        void OnPetDied(Combatant c)
        {
            Pet.Died();
            GameEventBus.Notify(PetModel.DisplayName(Pet.Current) + " ist besiegt – kehrt in 20 s zurück (F4 wechselt sofort)", new Color(1f, 0.7f, 0.5f));
            if (_pet != null) Destroy(_pet.gameObject, 2.5f);
            _pet = null;
        }

        bool PetCommand(int f)
        {
            var target = _targeting != null ? _targeting.Current : null;
            switch (f)
            {
                case 0:   // Angriff
                    if (_pet == null) return false;
                    if (target == null || !_self.IsHostileTo(target)) { GameEventBus.Notify("Kein Ziel", new Color(1f, 0.85f, 0.4f)); return false; }
                    Pet.Passive = false; _pet.Passive = false; _pet.ForceAggro(target);
                    return true;
                case 1:   // Familien-Skill
                {
                    if (_pet == null) return false;
                    var s = PetSkills[(int)Pet.Current]; if (s == null) return false;
                    if (_cds.TryGetValue(s.Id, out var cd) && cd > 0f) { GameEventBus.Notify(s.DisplayName + " lädt noch", new Color(1f, 0.85f, 0.4f)); return false; }
                    _cds[s.Id] = s.Cooldown;
                    var ctx = new SkillContext { Caster = _pet.Self, Skill = s, Target = target != null && _self.IsHostileTo(target) ? target : _pet.Target, GroundPoint = _pet.transform.position, Forward = _pet.transform.forward, Runner = _pet };
                    SkillExecutor.Execute(ctx);
                    SkillFx.Vfx(VfxIds.Compose(VfxShape.Shockwave, VfxPalette.Natur, VfxSize.M), _pet.transform.position, Quaternion.identity, null, 0.75f);
                    if (Pet.Current == PetFamily.Wolf) _self.Effects.Apply(new EffectApplication(EffectType.Fury, 1, 6f), _self);
                    if (Pet.Current == PetFamily.Falcon) _self.Effects.Apply(new EffectApplication(EffectType.Swiftness, 1, 6f), _self);
                    if (Pet.Current == PetFamily.Bear)   // Spott: Gegner in der Nähe greifen den Bären an
                        foreach (var b in Object.FindObjectsByType<NpcBrain>())
                            if (b != null && b != _pet && b.Self != null && !b.Self.IsDead && b.Self.Team == Team.Enemy && (b.transform.position - _pet.transform.position).sqrMagnitude < 36f) b.ForceAggro(_pet.Self);
                    return true;
                }
                case 2:   // Zurückrufen / Passiv umschalten
                    Pet.Passive = !Pet.Passive;
                    if (_pet != null) { _pet.Passive = Pet.Passive; if (Pet.Passive) _pet.ClearTarget(); }
                    GameEventBus.Notify(Pet.Passive ? "Gefährte: zurück (passiv)" : "Gefährte: verteidigt dich", new Color(0.7f, 1f, 0.7f));
                    return true;
                default:  // Wechsel
                    if (!Pet.CanSwap(InCombat, out string why)) { GameEventBus.Notify(why, new Color(1f, 0.85f, 0.4f)); GameAudio.Ui("Error"); return false; }
                    Pet.Swap(InCombat);
                    _self.Modifiers.Trigger(new TriggerContext { Type = TriggerType.PetSwap, Self = _self, Position = transform.position });
                    return true;
            }
        }

        void TickPet(float dt)
        {
            if (_pet == null || _pet.Self == null || _pet.Self.IsDead) return;
            _petCheck -= dt; if (_petCheck > 0f) return; _petCheck = 0.25f;
            var c = _pet.Self; if (c.Level != _self.Level) { c.Level = _self.Level; c.Stats.MarkDirty(); }
            float d = Vector3.Distance(_pet.transform.position, transform.position);
            if (d > 45f) { _pet.transform.position = ZoneAnchors.GroundAt(transform.position - transform.forward * 2f); _pet.ClearTarget(); }
            _pet.HomePosition = transform.position;
            bool idle = _pet.State == NpcState.Idle || _pet.State == NpcState.Reset;
            if (idle) _pet.Objective = d > 3.5f ? transform.position - transform.forward * 1.5f + transform.right * 1.2f : (Vector3?)null;
            else _pet.Objective = null;
            // Unterstützen: greift das Ziel des Spielers an, sobald der Spieler kämpft
            var target = _targeting != null ? _targeting.Current : null;
            if (!Pet.Passive && idle && InCombat && target != null && !target.IsDead && _self.IsHostileTo(target)) _pet.ForceAggro(target);
            if (Pet.Passive && !idle) _pet.ClearTarget();
        }

        void OnDestroy() { DespawnPet(); OnDestroyElite(); }
    }
}
