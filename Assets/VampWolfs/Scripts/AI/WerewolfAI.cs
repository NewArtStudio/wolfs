using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Simple invader brain for the werewolf. Uses exactly the same CombatUnit API as the player
    /// (MoveInput / AimPoint / TryCast), so it plays by the same rules: same cooldowns,
    /// same cast times, same telegraphs. Difficulty: MatchSettings.AiReactionTime / AiDodgeChance.
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

            CombatUnit target = match.Player;
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

        void Decide(CombatUnit target, float distance)
        {
            float health = unit.Health.Normalized;
            float energy = unit.Energy.Current;

            // Defensive: tank the incoming burst.
            if (health < 0.5f && target.IsCasting && distance < 12f && unit.TryCast(AbilitySlot.E, false)) return;

            // Ultimate when full energy and in range.
            if (energy >= Energy.Max && distance < 4f && unit.TryCast(AbilitySlot.Ultimate, false)) return;

            if (distance <= 2.6f)
            {
                bool saveForUltimate = energy >= 75f;
                if (!saveForUltimate && energy >= 25f && Random.value < 0.6f && unit.TryCast(AbilitySlot.R, false)) return;
                if (Random.value < 0.25f && unit.TryCast(AbilitySlot.Q, false)) return;
                unit.TryCast(AbilitySlot.Primary, false);
                return;
            }

            if (distance <= 7.5f)
            {
                bool useEx = energy >= 50f && energy < 75f;
                if (unit.TryCast(AbilitySlot.Secondary, useEx)) return;
                if (distance <= 4.2f && Random.value < 0.3f && unit.TryCast(AbilitySlot.Q, false)) return;
                return;
            }

            if (distance > 10f && Random.value < 0.35f)
            {
                unit.TryCast(AbilitySlot.Mobility, false);
            }
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

            Ability mobility = unit.GetAbility(AbilitySlot.Mobility);
            if (mobility == null || !mobility.IsReady)
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
                unit.TryCast(AbilitySlot.Mobility, false);
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

            if (distance < 1.6f)
            {
                // Too close: circle around a little instead of pushing into the target.
                unit.MoveInput = Vector3.Cross(Vector3.up, direction) * strafeSign * 0.5f;
                return;
            }

            unit.MoveInput = Steer(direction);
        }

        void SeekLootOrWander(MatchManager match)
        {
            LootBag bag = LootBag.Active.Count > 0 ? LootBag.Active[0] : null;

            if (bag != null)
            {
                Vector3 toBag = bag.transform.position - unit.Position;
                toBag.y = 0f;
                unit.MoveInput = toBag.magnitude > 0.3f ? Steer(toBag.normalized) : Vector3.zero;
                return;
            }

            if (Time.time > nextWanderTime)
            {
                nextWanderTime = Time.time + 4f;
                float h = match.Settings.ArenaHalfSize - 4f;
                wanderTarget = new Vector3(Random.Range(-h, h), 0f, Random.Range(-h, h));
            }

            Vector3 toWander = wanderTarget - unit.Position;
            toWander.y = 0f;
            unit.MoveInput = toWander.magnitude > 1f ? Steer(toWander.normalized) * 0.5f : Vector3.zero;
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
