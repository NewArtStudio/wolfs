using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Turns keyboard/mouse into orders for a CombatUnit:
    /// WASD move, mouse aims, C = cancel cast. Skills (nov/2026, Albion-style): weapon LMB/RMB/Q/E + F (ultimate),
    /// helmet R, chest Space, boots Shift, class skills 1 and 2. (The old Shift = EX modifier is gone.)
    /// Presses made during a cast are buffered briefly so combos feel responsive.
    /// </summary>
    [RequireComponent(typeof(CombatUnit))]
    public class PlayerController : MonoBehaviour
    {
        const float BufferTime = 0.3f;

        CombatUnit unit;
        Camera cam;

        bool hasBuffer;
        AbilitySlot bufferedSlot;
        bool bufferedEx;
        float bufferedUntil;

        static readonly Plane Ground = new Plane(Vector3.up, Vector3.zero);

        public void Init(Camera camera)
        {
            cam = camera;
            unit = GetComponent<CombatUnit>();
            unit.IsPlayer = true;
        }

        void Update()
        {
            if (unit == null || unit.IsDead)
            {
                return;
            }

            MatchManager match = MatchManager.Instance;
            if (match != null && match.IsOver)
            {
                unit.MoveInput = Vector3.zero;
                return;
            }

            Vector2 move = InputReader.Move();
            unit.MoveInput = new Vector3(move.x, 0f, move.y);

            Vector3 aim;
            if (TryGetMouseGroundPoint(out aim))
            {
                unit.AimPoint = aim;
            }

            if (InputReader.CancelPressed())
            {
                unit.CancelCast(true);
                hasBuffer = false;
            }

            if (InputReader.SecondaryPressed()) Buffer(AbilitySlot.Secondary, false);
            if (InputReader.MobilityPressed()) Buffer(AbilitySlot.Mobility, false);
            if (InputReader.QPressed()) Buffer(AbilitySlot.Q, false);
            if (InputReader.EPressed()) Buffer(AbilitySlot.E, false);
            if (InputReader.RPressed()) Buffer(AbilitySlot.R, false);
            if (InputReader.UltimatePressed()) Buffer(AbilitySlot.Ultimate, false);
            if (InputReader.BootsPressed()) Buffer(AbilitySlot.Boots, false);
            if (InputReader.Class1Pressed()) Buffer(AbilitySlot.Class1, false);
            if (InputReader.Class2Pressed()) Buffer(AbilitySlot.Class2, false);

            if (hasBuffer)
            {
                if (Time.time > bufferedUntil)
                {
                    hasBuffer = false;
                }
                else if (unit.TryCast(bufferedSlot, bufferedEx))
                {
                    hasBuffer = false;
                }
            }

            // Holding LMB keeps attacking, like in Battlerite.
            if (!hasBuffer && InputReader.PrimaryHeld())
            {
                unit.TryCast(AbilitySlot.Primary, false);
            }
        }

        void Buffer(AbilitySlot slot, bool ex)
        {
            Ability ability = unit.GetAbility(slot);
            if (ability == null)
            {
                return;
            }

            Ability toCast = ex && ability.Ex != null ? ability.Ex : ability;
            if (toCast.EnergyCost > 0f && unit.Energy.Current < toCast.EnergyCost)
            {
                FloatingText.Spawn(unit.Position + Vector3.up * 2.8f, "Energia insuficiente", new Color(0.7f, 0.6f, 1f));
                return;
            }

            if (!ability.IsReady && ability.CooldownRemaining > BufferTime)
            {
                return;
            }

            hasBuffer = true;
            bufferedSlot = slot;
            bufferedEx = ex;
            bufferedUntil = Time.time + BufferTime;
        }

        bool TryGetMouseGroundPoint(out Vector3 point)
        {
            point = Vector3.zero;
            if (cam == null)
            {
                return false;
            }

            Ray ray = cam.ScreenPointToRay(InputReader.MousePosition());
            float enter;
            if (Ground.Raycast(ray, out enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }

            return false;
        }
    }
}
