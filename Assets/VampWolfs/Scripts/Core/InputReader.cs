using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VampWolfs
{
    /// <summary>
    /// Reads keyboard and mouse. Works with the new Input System package (default in Unity 6 URP
    /// templates) and with the old Input Manager, so the project compiles in either setup.
    /// All bindings live here: change keys in this one file.
    /// </summary>
    public static class InputReader
    {
#if ENABLE_INPUT_SYSTEM
        static bool Held(Key key)
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb[key].isPressed;
        }

        static bool Pressed(Key key)
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb[key].wasPressedThisFrame;
        }

        public static Vector2 Move()
        {
            float x = (Held(Key.D) || Held(Key.RightArrow) ? 1f : 0f) - (Held(Key.A) || Held(Key.LeftArrow) ? 1f : 0f);
            float y = (Held(Key.W) || Held(Key.UpArrow) ? 1f : 0f) - (Held(Key.S) || Held(Key.DownArrow) ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        public static Vector2 MousePosition()
        {
            Mouse m = Mouse.current;
            return m != null ? m.position.ReadValue() : Vector2.zero;
        }

        public static bool PrimaryHeld()
        {
            Mouse m = Mouse.current;
            return m != null && m.leftButton.isPressed;
        }

        public static bool SecondaryPressed()
        {
            Mouse m = Mouse.current;
            return m != null && m.rightButton.wasPressedThisFrame;
        }

        public static bool ExHeld() { return Held(Key.LeftShift) || Held(Key.RightShift); }
        public static bool MobilityPressed() { return Pressed(Key.Space); }
        public static bool QPressed() { return Pressed(Key.Q); }
        public static bool EPressed() { return Pressed(Key.E); }
        public static bool RPressed() { return Pressed(Key.R); }
        public static bool UltimatePressed() { return Pressed(Key.F); }
        public static bool ExSecondaryPressed() { return Pressed(Key.Digit1); }
        public static bool ExMobilityPressed() { return Pressed(Key.Digit2); }
        public static bool CancelPressed() { return Pressed(Key.C); }
        public static bool HelpPressed() { return Pressed(Key.H); }
        public static bool RestartPressed() { return Pressed(Key.Enter); }
#else
        public static Vector2 Move()
        {
            float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        public static Vector2 MousePosition() { return Input.mousePosition; }
        public static bool PrimaryHeld() { return Input.GetMouseButton(0); }
        public static bool SecondaryPressed() { return Input.GetMouseButtonDown(1); }
        public static bool ExHeld() { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
        public static bool MobilityPressed() { return Input.GetKeyDown(KeyCode.Space); }
        public static bool QPressed() { return Input.GetKeyDown(KeyCode.Q); }
        public static bool EPressed() { return Input.GetKeyDown(KeyCode.E); }
        public static bool RPressed() { return Input.GetKeyDown(KeyCode.R); }
        public static bool UltimatePressed() { return Input.GetKeyDown(KeyCode.F); }
        public static bool ExSecondaryPressed() { return Input.GetKeyDown(KeyCode.Alpha1); }
        public static bool ExMobilityPressed() { return Input.GetKeyDown(KeyCode.Alpha2); }
        public static bool CancelPressed() { return Input.GetKeyDown(KeyCode.C); }
        public static bool HelpPressed() { return Input.GetKeyDown(KeyCode.H); }
        public static bool RestartPressed() { return Input.GetKeyDown(KeyCode.Return); }
#endif
    }
}
