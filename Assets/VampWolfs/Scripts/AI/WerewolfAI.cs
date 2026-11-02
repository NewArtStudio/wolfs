using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Simple invader brain (any class since nov/2026: the name stayed from the werewolf-only days).
    /// Uses exactly the same CombatUnit API as the player (MoveInput / AimPoint / TryCast), so it plays by the
    /// same rules: same cooldowns, cast times and telegraphs. It reads each ability's range (Ability.AiRange)
    /// to decide, so melee kits close in and ranged kits keep their distance.
    /// Difficulty: MatchSettings.AiReactionTime / AiDodgeChance.
    /// </summary>
    [RequireComponent(typeof(CombatUnit))]
    public class WerewolfAI : MonoBehaviour
    {
        CombatUnit unit;
        MatchSettings settings;

        float nextDecisionTime;
        float strafeSign = 1f;
        float nextStrafeFlip;
        Vector3 wanderTarget;
        float nextWanderTime;

        Vector3 avoidDirection;
        float avoidUntil;

        // path on the procedural map (WalkGrid A*): x,z pairs
        readonly NavAgent nav = new NavAgent();
        System.Random wanderRng;

        float nextJudgedCleanup;
        readonly HashSet<Projectile> judgedProjectiles = new HashSet<Projectile>();

        static readonly float[] SteerAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f, 150f, -150f };

        public void Init(MatchSettings matchSettings)
        {
            settings = matchSettings;
            unit = GetComponent<CombatUnit>();
            nextDecisionTime = Time.time + 0.8f; // small grace period after arriving
        }

        void Update()
        {
            if (unit == null || unit.IsDead || settings == null)
            {
                return;
            }

            MatchManager match = MatchManager.Instance;
            if (match == null || match.State == MatchState.Extracted)
            {
                unit.MoveInput = Vector3.zero;
                return;
            }

            CleanupJudgedProjectiles();

            CombatUnit target = ChooseTarget(match);
            if (target == null || target.IsDead)
            {
                SeekLootOrWander(match);
                return;
            }

            Vector3 toTarget = target.Position - unit.Position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            unit.AimPoint = PredictPosition(target, distance);

            TryDodge();
            MoveToward(toTarget, distance);

            if (Time.time < nextDecisionTime || !unit.CanAct)
            {
                return;
            }

            nextDecisionTime = Time.time + settings.AiReactionTime * Random.Range(0.7f, 1.3f);
            Decide(target, distance);
        }

        // ---------------------------------------------------------------- combat decisions

        /// <summary>Slots the AI tries for offense, in priority order (class skills first, then the weapon, then gear).</summary>
        static readonly AbilitySlot[] OffenseOrder =
        {
            AbilitySlot.Class1, AbilitySlot.Class2, AbilitySlot.Secondary, AbilitySlot.Q, AbilitySlot.E,
            AbilitySlot.R, AbilitySlot.Boots, AbilitySlot.Mobility,
        };

        void Decide(CombatUnit target, float distance)
        {
            float health = unit.Health.Normalized;

            // Defensive: shield / counter / damage reduction against an incoming burst.
            if (health < 0.5f && target.IsCasting && distance < 12f)
            {
                foreach (AbilitySlot slot in OffenseOrder)
                {
                    Ability a = unit.GetAbility(slot);
                    if (a != null && a.IsDefensive && a.IsReady && unit.TryCast(slot, false)) return;
                }
            }

            // Ultimate when there is energy and the target is in its range.
            Ability ult = unit.GetAbility(AbilitySlot.Ultimate);
            if (ult != null && !target.IsMinor && unit.Energy.Current >= ult.EnergyCost && distance <= Mathf.Max(4f, ult.AiRange) && unit.TryCast(AbilitySlot.Ultimate, false)) return;

            // Any offensive skill in range (a little randomness so it isn't a fixed rotation).
            int start = Random.Range(0, OffenseOrder.Length);
            for (int k = 0; k < OffenseOrder.Length; k++)
            {
                AbilitySlot slot = OffenseOrder[(start + k) % OffenseOrder.Length];
                Ability a = unit.GetAbility(slot);
                if (a == null || !a.IsReady || a.IsDefensive) continue;
                // self buffs (damage/haste) have no range: use them when the fight is close
                float range = a.AiRange > 0f ? a.AiRange : (a is BuffAbility ? 6f : 0f);
                if (range <= 0f || distance > range) continue;
                if (a is DashAbility && distance < 3f) continue;   // gap closers are for closing gaps
                if (Random.value < 0.75f && unit.TryCast(slot, false)) return;
            }

            Ability basic = unit.GetAbility(AbilitySlot.Primary);
            if (basic != null && distance <= basic.AiRange) unit.TryCast(AbilitySlot.Primary, false);
        }

        /// <summary>Distance the AI likes to fight at: just inside its basic attack (melee hugs, ranged keeps away).</summary>
        float PreferredDistance()
        {
            Ability basic = unit.GetAbility(AbilitySlot.Primary);
            float r = basic != null ? basic.AiRange : 2.2f;
            return r > 4f ? r * 0.7f : 1.6f;
        }

        Vector3 PredictPosition(CombatUnit target, float distance)
        {
            float lead = Mathf.Clamp(distance / 20f, 0f, 0.35f);
            return target.Position + target.Velocity * lead;
        }

        /// <summary>Dash sideways when a skillshot is about to hit (sometimes — see AiDodgeChance).</summary>
        void TryDodge()
        {
            if (!unit.CanAct)
            {
                return;
            }

            // any ready dash (chest, boots, weapon or class) can dodge
            AbilitySlot dodgeSlot = AbilitySlot.Mobility;
            Ability mobility = null;
            foreach (AbilitySlot slot in OffenseOrder)
            {
                Ability a = unit.GetAbility(slot);
                if (a is DashAbility && a.IsReady) { mobility = a; dodgeSlot = slot; break; }
            }

            if (mobility == null)
            {
                return;
            }

            for (int i = 0; i < Projectile.Active.Count; i++)
            {
                Projectile projectile = Projectile.Active[i];
                if (projectile == null || projectile.Owner == null || projectile.Owner.Team == unit.Team)
                {
                    continue;
                }

                Vector3 toMe = unit.Position - projectile.transform.position;
                toMe.y = 0f;

                float along = Vector3.Dot(toMe, projectile.Direction);
                if (along <= 0f)
                {
                    continue; // already past us
                }

                float lateral = (toMe - projectile.Direction * along).magnitude;
                float timeToHit = along / Mathf.Max(0.1f, projectile.Speed);
                if (lateral > 1.3f || timeToHit > 0.45f || judgedProjectiles.Contains(projectile))
                {
                    continue;
                }

                judgedProjectiles.Add(projectile);
                if (Random.value > settings.AiDodgeChance)
                {
                    continue;
                }

                Vector3 side = Vector3.Cross(Vector3.up, projectile.Direction) * (Random.value < 0.5f ? 1f : -1f);
                unit.AimPoint = unit.Position + side * 5f;
                unit.TryCast(dodgeSlot, false);
                return;
            }
        }

        // ---------------------------------------------------------------- movement

        void MoveToward(Vector3 toTarget, float distance)
        {
            if (Time.time > nextStrafeFlip)
            {
                strafeSign = -strafeSign;
                nextStrafeFlip = Time.time + Random.Range(1f, 2.5f);
            }

            Vector3 direction = distance > 0.01f ? toTarget / distance : Vector3.zero;

            float preferred = PreferredDistance();
            if (distance < preferred)
            {
                // Close enough: circle around (ranged kits also back off a little).
                Vector3 back = preferred > 2f ? -direction * 0.5f : Vector3.zero;
                unit.MoveInput = Steer((Vector3.Cross(Vector3.up, direction) * strafeSign * 0.7f + back).normalized) * (preferred > 2f ? 1f : 0.5f);
                return;
            }

            unit.MoveInput = Steer(NavDirection(unit.Position + toTarget, direction));
        }

        void SeekLootOrWander(MatchManager match)
        {
            LootBag bag = LootBag.Active.Count > 0 ? LootBag.Active[0] : null;

            if (bag != null)
            {
                Vector3 toBag = bag.transform.position - unit.Position;
                toBag.y = 0f;
                unit.MoveInput = toBag.magnitude > 0.3f ? Steer(NavDirection(bag.transform.position, toBag.normalized)) : Vector3.zero;
                return;
            }

            if (Time.time > nextWanderTime)
            {
                nextWanderTime = Time.time + 4f;
                float h = match.Settings.ArenaHalfSize - 4f;
                wanderTarget = new Vector3(Random.Range(-h, h), 0f, Random.Range(-h, h));
                float wx, wz;
                if (wanderRng == null) wanderRng = new System.Random(GetInstanceID());
                if (match.Grid != null && match.Grid.RandomPointAround(wanderRng, unit.Position.x, unit.Position.z, 6f, 18f, 3f, 0.5f, out wx, out wz))
                {
                    wanderTarget = new Vector3(wx, 0f, wz);
                }
            }

            Vector3 toWander = wanderTarget - unit.Position;
            toWander.y = 0f;
            unit.MoveInput = toWander.magnitude > 1f ? Steer(NavDirection(wanderTarget, toWander.normalized)) * 0.5f : Vector3.zero;
        }

        /// <summary>Direction toward "goal" on the procedural map (see NavAgent: straight or A* path).</summary>
        Vector3 NavDirection(Vector3 goal, Vector3 straight)
        {
            return nav.Direction(unit.Position, goal, straight);
        }

        /// <summary>
        /// Normally the player. A peasant that is hitting the invader from up close (and the player is far)
        /// gets punished first — the player can lure the invader into the peasants.
        /// </summary>
        CombatUnit ChooseTarget(MatchManager match)
        {
            CombatUnit player = match.Player;
            float playerDistance = player != null && !player.IsDead ? Vector3.Distance(player.Position, unit.Position) : float.MaxValue;
            if (playerDistance < 10f) return player;

            CombatUnit best = null;
            float bestDistance = 11f;   // covers the shotgun range (10 m)
            for (int i = 0; i < Peasant.All.Count; i++)
            {
                Peasant p = Peasant.All[i];
                if (p == null || p.Unit == null || p.Unit.IsDead || p.Target != unit) continue;
                float d = Vector3.Distance(p.Unit.Position, unit.Position);
                if (d < bestDistance) { bestDistance = d; best = p.Unit; }
            }
            return best != null ? best : player;
        }

        /// <summary>Very small obstacle avoidance: if the way is blocked, try rotated directions.</summary>
        Vector3 Steer(Vector3 desired)
        {
            if (desired.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            if (Time.time < avoidUntil && IsBlocked(desired) && !IsBlocked(avoidDirection))
            {
                // Keep the previous detour briefly (if still clear) to avoid jittering at corners.
                return avoidDirection;
            }

            for (int i = 0; i < SteerAngles.Length; i++)
            {
                Vector3 candidate = Quaternion.Euler(0f, SteerAngles[i], 0f) * desired;
                if (!IsBlocked(candidate))
                {
                    if (i > 0)
                    {
                        avoidDirection = candidate;
                        avoidUntil = Time.time + 0.4f;
                    }

                    return candidate;
                }
            }

            return desired;
        }

        bool IsBlocked(Vector3 direction)
        {
            RaycastHit[] hits = Physics.SphereCastAll(unit.Position + Vector3.up, unit.Controller.radius * 0.9f, direction, 1.8f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.GetComponent<Obstacle>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        void CleanupJudgedProjectiles()
        {
            if (Time.time < nextJudgedCleanup)
            {
                return;
            }

            nextJudgedCleanup = Time.time + 2f;
            judgedProjectiles.RemoveWhere(p => p == null);
        }
    }
}
