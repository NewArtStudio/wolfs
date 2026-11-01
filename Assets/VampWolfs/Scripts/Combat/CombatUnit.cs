using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// A playable character (vampire, werewolf, later humans).
    /// It does NOT read input: PlayerController or WerewolfAI write MoveInput / AimPoint and call TryCast.
    /// This keeps the same character usable by a player, an AI or (later) the network.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CombatUnit : MonoBehaviour
    {
        /// <summary>Every living character in the scene (used by AI, sheep and HUD).</summary>
        public static readonly List<CombatUnit> All = new List<CombatUnit>();

        public float MoveSpeed = 6f;
        public bool IsPlayer;

        public string DisplayName { get; private set; }
        public Health Health { get; private set; }
        public Energy Energy { get; private set; }
        public StatusEffects Status { get; private set; }
        public Inventory Inventory { get; private set; }
        public Equipment Equipment { get; private set; }
        public UnitVisual Visual { get; private set; }
        public CharacterController Controller { get; private set; }
        public TelegraphIndicator Telegraph { get; private set; }

        public Team Team { get { return Health.Team; } }
        public bool IsDead { get { return Health == null || Health.IsDead; } }
        public Vector3 Position { get { return transform.position; } }
        public Vector3 Velocity { get; private set; }

        /// <summary>Desired movement on the ground plane (x,z), length 0..1. Written every frame.</summary>
        [NonSerialized] public Vector3 MoveInput;

        /// <summary>World point the character is aiming at (mouse for the player). Written every frame.</summary>
        [NonSerialized] public Vector3 AimPoint;

        /// <summary>unit, killer (killer may be null).</summary>
        public event Action<CombatUnit, CombatUnit> Died;

        readonly Ability[] abilities = new Ability[7];

        Ability currentCast;
        Ability currentCastSlot;   // the base ability whose cooldown was started (EX shares it)
        float castStartTime;
        float castEndTime;
        float castEnergySpent;

        bool forcedActive;
        Vector3 forcedVelocity;
        float forcedUntil;
        Action onForcedEnd;

        static readonly Color EnemyTelegraphColor = new Color(0.95f, 0.15f, 0.1f);
        static readonly Color OwnTelegraphColor = new Color(0.35f, 0.3f, 0.55f);

        public Ability CurrentCast { get { return currentCast; } }
        public bool IsCasting { get { return currentCast != null; } }
        public bool IsInForcedMove { get { return forcedActive; } }

        public float CastProgress
        {
            get
            {
                if (!IsCasting || castEndTime <= castStartTime) return 0f;
                return Mathf.InverseLerp(castStartTime, castEndTime, Time.time);
            }
        }

        /// <summary>True when the unit may start a new ability right now.</summary>
        public bool CanAct
        {
            get { return !IsDead && !Status.IsStunned && !IsCasting && !forcedActive; }
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Health and speed before gear (set by CharacterFactory).</summary>
        public float BaseMaxHealth { get; private set; }
        public float BaseMoveSpeed { get; private set; }

        public void Init(string displayName, float moveSpeed)
        {
            DisplayName = displayName;
            MoveSpeed = moveSpeed;
            BaseMoveSpeed = moveSpeed;

            Health = GetComponent<Health>();
            Energy = GetComponent<Energy>();
            Status = GetComponent<StatusEffects>();
            Inventory = GetComponent<Inventory>();
            Equipment = GetComponent<Equipment>();
            Visual = GetComponent<UnitVisual>();
            Controller = GetComponent<CharacterController>();
            Telegraph = GetComponent<TelegraphIndicator>();

            AimPoint = transform.position + transform.forward;
            BaseMaxHealth = Health.MaxHealth;
            Health.Died += OnHealthDied;
        }

        /// <summary>
        /// Wears gear and applies its stats: max health (tier + VIT), Égide (ward), move speed.
        /// Damage, defense, cast speed and status resistance are read from Equipment.Stats when needed.
        /// </summary>
        public void ApplyGear(GearCatalog catalog, IDictionary<string, ItemInstance> items)
        {
            Equipment.Wear(catalog, items);
            CharacterStats stats = Equipment.Stats;
            float maxHealth = BaseMaxHealth * stats.HpMult;
            Health.SetMaxHealth(maxHealth, maxHealth * stats.WardPct);
            Health.HealScale = BaseMaxHealth > 0f ? maxHealth / BaseMaxHealth : 1f;
            MoveSpeed = BaseMoveSpeed * stats.MoveMult;
        }

        /// <summary>Cast time after DEX: basic attack uses attack speed, other skills use cast speed.</summary>
        float CastTimeFor(AbilitySlot slot, Ability ability)
        {
            CharacterStats stats = Equipment != null ? Equipment.Stats : null;
            if (stats == null) return ability.CastTime;
            return ability.CastTime * (slot == AbilitySlot.Primary ? stats.AspdMult : stats.CastMult);
        }

        public void SetAbilities(Ability[] kit)
        {
            for (int i = 0; i < abilities.Length; i++)
            {
                abilities[i] = i < kit.Length ? kit[i] : null;
            }
        }

        public Ability GetAbility(AbilitySlot slot)
        {
            return abilities[(int)slot];
        }

        // ---------------------------------------------------------------- casting

        /// <summary>
        /// Tries to start an ability. useEx = Shift held: uses the EX version if the ability has one
        /// (costs energy, shares the cooldown with the normal version).
        /// </summary>
        public bool TryCast(AbilitySlot slot, bool useEx)
        {
            if (!CanAct)
            {
                return false;
            }

            Ability slotAbility = GetAbility(slot);
            if (slotAbility == null || !slotAbility.IsReady)
            {
                return false;
            }

            Ability ability = (useEx && slotAbility.Ex != null) ? slotAbility.Ex : slotAbility;

            if (ability.EnergyCost > 0f && !Energy.TrySpend(ability.EnergyCost))
            {
                return false;
            }

            slotAbility.StartCooldown(ability.Cooldown);

            currentCast = ability;
            currentCastSlot = slotAbility;
            castEnergySpent = ability.EnergyCost;
            castStartTime = Time.time;
            castEndTime = Time.time + CastTimeFor(slot, ability);
            FaceTowards(AimPoint);

            if (castEndTime <= castStartTime)
            {
                ReleaseCast();
            }

            return true;
        }

        /// <summary>Stops the current cast. refund = give back cooldown and energy (player pressed cancel).</summary>
        public void CancelCast(bool refund)
        {
            if (!IsCasting)
            {
                return;
            }

            if (refund)
            {
                currentCastSlot.ResetCooldown();
                Energy.Add(castEnergySpent);
            }

            currentCast = null;
            currentCastSlot = null;
            Telegraph.Hide();
        }

        void ReleaseCast()
        {
            Ability ability = currentCast;
            currentCast = null;
            currentCastSlot = null;
            Telegraph.Hide();

            FaceTowards(AimPoint);
            Visual.PlayAttack(ability.Motion);
            ability.Execute(this, AimPoint);
        }

        /// <summary>Moves the unit with a fixed velocity, ignoring input (dashes and leaps).</summary>
        public void StartForcedMove(Vector3 velocity, float duration, Action onEnd)
        {
            forcedVelocity = new Vector3(velocity.x, 0f, velocity.z);
            forcedUntil = Time.time + duration;
            onForcedEnd = onEnd;
            forcedActive = true;
        }

        // ---------------------------------------------------------------- frame update

        void Update()
        {
            if (IsDead)
            {
                return;
            }

            float dt = Time.deltaTime;

            if (IsCasting)
            {
                if (Status.IsStunned)
                {
                    CancelCast(false);
                }
                else
                {
                    UpdateTelegraph();
                    FaceTowards(AimPoint);

                    if (Time.time >= castEndTime)
                    {
                        ReleaseCast();
                    }
                }
            }

            Vector3 velocity = Vector3.zero;

            if (forcedActive)
            {
                velocity = forcedVelocity;

                if (Time.time >= forcedUntil)
                {
                    forcedActive = false;
                    velocity = Vector3.zero;

                    Action callback = onForcedEnd;
                    onForcedEnd = null;
                    if (callback != null)
                    {
                        callback();
                    }
                }
            }
            else if (!Status.IsStunned)
            {
                Vector3 input = Vector3.ClampMagnitude(new Vector3(MoveInput.x, 0f, MoveInput.z), 1f);
                float multiplier = Status.MoveSpeedMultiplier * (IsCasting ? currentCast.MoveMultiplierWhileCasting : 1f);
                velocity = input * MoveSpeed * multiplier;

                if (!IsCasting && input.sqrMagnitude > 0.01f)
                {
                    FaceDirection(input, dt);
                }
            }

            if (Controller.enabled)
            {
                // Small constant push down keeps the unit on the floor collider.
                Controller.Move((velocity + Vector3.down * 4f) * dt);
            }

            Velocity = velocity;
            Visual.SetMotion(velocity, MoveSpeed);
            Visual.SetCasting(IsCasting ? CastProgress : 0f, IsCasting ? currentCast.Motion : AttackMotion.None);
        }

        void UpdateTelegraph()
        {
            // Enemies always show what they are casting (readability = skill-based dodging).
            // For the player only long casts are shown, so spamming M1 does not clutter the screen.
            bool show = !IsPlayer || currentCast.CastTime >= 0.5f;

            if (show)
            {
                Telegraph.Show(currentCast.GetTelegraph(this, AimPoint), IsPlayer ? OwnTelegraphColor : EnemyTelegraphColor);
            }
            else
            {
                Telegraph.Hide();
            }
        }

        public void FaceTowards(Vector3 point)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        void FaceDirection(Vector3 direction, float dt)
        {
            Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 900f * dt);
        }

        void OnHealthDied(CombatUnit killer)
        {
            CancelCast(false);
            forcedActive = false;
            MoveInput = Vector3.zero;
            Velocity = Vector3.zero;
            Status.ClearAll();
            Controller.enabled = false;
            Visual.PlayDeath();

            if (Died != null)
            {
                Died(this, killer);
            }
        }
    }
}
