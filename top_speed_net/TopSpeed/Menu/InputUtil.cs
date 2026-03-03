using System;
using SharpDX.DirectInput;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal static class MenuInputUtil
    {
        private const int JoystickThreshold = 50;

        public static bool TryGetPressedLetter(InputManager input, out char letter)
        {
            letter = '\0';
            for (var c = 'A'; c <= 'Z'; c++)
            {
                if (!input.WasPressed(ToLetterKey(c)))
                    continue;

                letter = c;
                return true;
            }

            return false;
        }

        public static bool ItemStartsWithLetter(MenuItem item, char letter)
        {
            var text = item.GetDisplayText();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (!char.IsLetterOrDigit(ch))
                    continue;

                return char.ToUpperInvariant(ch) == letter;
            }

            return false;
        }

        public static bool IsNearCenter(JoystickStateSnapshot state, bool useAxes)
        {
            if (!useAxes)
                return true;
            return Math.Abs(state.X) <= JoystickThreshold && Math.Abs(state.Y) <= JoystickThreshold;
        }

        public static bool WasJoystickUpPressed(JoystickStateSnapshot current, JoystickStateSnapshot previous, bool useAxes)
        {
            var currentUp = (useAxes && current.Y < -JoystickThreshold) || current.Pov1;
            var previousUp = (useAxes && previous.Y < -JoystickThreshold) || previous.Pov1;
            return currentUp && !previousUp;
        }

        public static bool WasJoystickDownPressed(JoystickStateSnapshot current, JoystickStateSnapshot previous, bool useAxes)
        {
            var currentDown = (useAxes && current.Y > JoystickThreshold) || current.Pov3;
            var previousDown = (useAxes && previous.Y > JoystickThreshold) || previous.Pov3;
            return currentDown && !previousDown;
        }

        public static bool WasJoystickActivatePressed(JoystickStateSnapshot current, JoystickStateSnapshot previous, bool useAxes)
        {
            var currentRight = (useAxes && current.X > JoystickThreshold) || current.Pov2;
            var previousRight = (useAxes && previous.X > JoystickThreshold) || previous.Pov2;
            if (currentRight && !previousRight)
                return true;
            return current.B1 && !previous.B1;
        }

        public static bool WasJoystickBackPressed(JoystickStateSnapshot current, JoystickStateSnapshot previous, bool useAxes)
        {
            var currentLeft = (useAxes && current.X < -JoystickThreshold) || current.Pov4;
            var previousLeft = (useAxes && previous.X < -JoystickThreshold) || previous.Pov4;
            return currentLeft && !previousLeft;
        }

        private static Key ToLetterKey(char letter)
        {
            return letter switch
            {
                'A' => Key.A,
                'B' => Key.B,
                'C' => Key.C,
                'D' => Key.D,
                'E' => Key.E,
                'F' => Key.F,
                'G' => Key.G,
                'H' => Key.H,
                'I' => Key.I,
                'J' => Key.J,
                'K' => Key.K,
                'L' => Key.L,
                'M' => Key.M,
                'N' => Key.N,
                'O' => Key.O,
                'P' => Key.P,
                'Q' => Key.Q,
                'R' => Key.R,
                'S' => Key.S,
                'T' => Key.T,
                'U' => Key.U,
                'V' => Key.V,
                'W' => Key.W,
                'X' => Key.X,
                'Y' => Key.Y,
                'Z' => Key.Z,
                _ => Key.Unknown
            };
        }
    }
}
