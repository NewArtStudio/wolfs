using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Peasant brain. Uses the same CombatUnit API as everyone (MoveInput / AimPoint / TryCast), so peasants
    /// follow the same rules (cast times, telegraphs, cooldowns).
    /// - Calm: guards shuffle around their chest; roamers walk from place to place.
    /// - Sees an enemy (player or invader) within AggroRange, or gets hit: attacks and calls its group.
    /// - Too far from home (LeashRange): gives up, walks back and heals to full.
    /// Numbers in MatchSettings ("Peasants") [PROTÓTIPO].
    /// </summary>
    [RequireComponent(typeof(CombatUnit))]
    public class PeasantAI : MonoBehaviour
    {
        Peasant peasant;
        CombatUnit unit;
        MatchSettings settings;
        readonly NavAgent nav = new NavAgent(30000);   // small search budget: peasants fight near home
        System.Random rng;

        bool returning;
        float returnStarted;
        Vector3 idleTarget;
        float nextIdleTime;
        float nextDecisionTime;
        float nextScanTime;

        public void Init(Peasant owner, MatchSettings matchSettings)
        {
            peasant = owner;
            unit = owner.Unit;
            settings = matchSettings;
            rng = new System.Random(GetInstanceID());
            idleTarget = owner.Home;
            nextDecisionTime = Time.time + 0.5f;
        }

        float AggroRange { get { return peasant.IsGuard ? settings.PeasantAggroRange : settings.PeasantAggroRange * 0.8f; } }

        void Update()
        {
            if (unit == null || unit.IsDead || settings == null)
            {
                return;
            }

            MatchManager match = MatchManager.Instance;
            if (match == null || match.IsOver)
            {
                unit.MoveInput = Vector3.zero;
                return;
            }

            // give up when dragged too far from home (or the enemy died)
            float fromHome = Flat(unit.Position - peasant.Home).magnitude;
            if (peasant.Target != null && (peasant.Target.IsDead || fromHome > settings.PeasantLeashRange))
            {
                // give up: walk home and heal (also when the enemy died next to home)
                peasant.Target = null;
                returning = true;
                returnStarted = Time.time;
            }

            if (returning)
            {
                ReturnHome(fromHome);
                return;
            }

            if (peasant.Target == null && Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + 0.25f;
                CombatUnit seen = NearestEnemy(AggroRange);
                if (seen != null) peasant.Alert(seen);
            }

            if (peasant.Target != null) Fight(peasant.Target);
            else Idle();
        }

        // ---------------------------------------------------------------- states

        void Fight(CombatUnit target)
        {
            Vector3 to = Flat(target.Position - unit.Position);
            float distance = to.magnitude;
            Ability basic = unit.GetAbility(AbilitySlot.Primary);
            float range = basic != null ? basic.AiRange : 2f;

            // shotgun: lead the target a little
            unit.AimPoint = peasant.Weapon == PeasantWeapon.Shotgun
                ? target.Position + target.Velocity * Mathf.Clamp(distance / 28f, 0f, 0.3f)
                : target.Position;

            Vector3 dir = distance > 0.01f ? to / distance : unit.transform.forward;
            if (peasant.Weapon == PeasantWeapon.Shotgun)
            {
                // keep a shooting distance: back off if too close, come closer if too far
                // back off around obstacles, but never past the leash
                float fromHome = Flat(unit.Position - peasant.Home).magnitude;
                if (distance < 4f && fromHome < settings.PeasantLeashRange - 3f) unit.MoveInput = nav.Direction(unit.Position, unit.Position - dir * 4f, -dir);
                else if (distance < 4f) unit.MoveInput = Vector3.Cross(Vector3.up, dir);
                else if (distance > range * 0.85f) unit.MoveInput = nav.Direction(unit.Position, target.Position, dir);
                else unit.MoveInput = Vector3.Cross(Vector3.up, dir) * 0.4f;
            }
            else
            {
                unit.MoveInput = distance > range * 0.8f ? nav.Direction(unit.Position, target.Position, dir) : Vector3.zero;
            }

            if (Time.time < nextDecisionTime || !unit.CanAct) return;
            nextDecisionTime = Time.time + settings.AiReactionTime * (0.8f + (float)rng.NextDouble() * 0.6f);
            if (basic != null && basic.IsReady && distance <= range) unit.TryCast(AbilitySlot.Primary, false);
        }

        void ReturnHome(float fromHome)
        {
            // like Albion mobs "evading": they heal fast on the way back (no free kills by kiting past the leash)
            unit.Health.Heal(unit.Health.MaxHealth * 0.3f * Time.deltaTime);

            // could not get home (blocked spot)? stay here: this is home now
            if (Time.time - returnStarted > 10f)
            {
                peasant.Home = unit.Position;
                fromHome = 0f;
            }

            if (fromHome <= 1.5f)
            {
                returning = false;
                unit.MoveInput = Vector3.zero;
                unit.Health.Heal(unit.Health.MaxHealth);   // reset, like Albion mobs
                return;
            }
            Vector3 to = Flat(peasant.Home - unit.Position);
            unit.MoveInput = nav.Direction(unit.Position, peasant.Home, to.normalized);
            unit.AimPoint = peasant.Home;
        }

        void Idle()
        {
            if (Time.time >= nextIdleTime)
            {
                nextIdleTime = Time.time + 3f + (float)rng.NextDouble() * 4f;
                idleTarget = PickIdleSpot();
            }
            Vector3 to = Flat(idleTarget - unit.Position);
            if (to.magnitude < 0.6f)
            {
                if (!peasant.IsGuard) peasant.Home = idleTarget;   // roamers live wherever they arrived
                unit.MoveInput = Vector3.zero;
                return;
            }
            // calm walk = 40% speed
            unit.MoveInput = nav.Direction(unit.Position, idleTarget, to.normalized) * 0.4f;
            unit.AimPoint = idleTarget;
        }

        Vector3 PickIdleSpot()
        {
            MatchManager match = MatchManager.Instance;
            WalkGrid grid = match != null ? match.Grid : null;
            float min = peasant.IsGuard ? 0.5f : 8f, max = peasant.IsGuard ? 3.5f : 20f;
            float x, z;
            Vector3 from = peasant.IsGuard ? peasant.Home : unit.Position;
            if (grid != null && grid.RandomPointAround(rng, from.x, from.z, min, max, 3f, 0.3f, out x, out z))
            {
                Vector3 spot = new Vector3(x, 0f, z);
                return spot;
            }
            return peasant.Home;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Nearest living player or invader within range (peasants ignore sheep and each other).</summary>
        CombatUnit NearestEnemy(float range)
        {
            CombatUnit best = null;
            float bestDistance = range;
            for (int i = 0; i < CombatUnit.All.Count; i++)
            {
                CombatUnit u = CombatUnit.All[i];
                if (u == null || u.IsDead || u.Team == Team.Peasants || u.Team == Team.Neutral) continue;
                float d = Flat(u.Position - unit.Position).magnitude;
                if (d < bestDistance) { bestDistance = d; best = u; }
            }
            return best;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
