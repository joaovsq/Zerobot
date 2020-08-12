using Stride.Core.Mathematics;
using Stride.Input;

namespace Zerobot.Core
{
    public static class InputManagerExtensions
    {
        public static bool IsGamePadButtonDown(this InputManager input, GamePadButton button, int index)
        {
            if (input.GamePadCount < index)
                return false;

            return (input.GetGamePadByIndex(index).State.Buttons & button) == button;
        }

        public static Vector2 GetLeftThumb(this InputManager input, int index)
        {
            return input.GamePadCount >= index ? input.GetGamePadByIndex(index).State.LeftThumb : Vector2.Zero;
        }

        public static Vector2 GetRightThumb(this InputManager input, int index)
        {
            return input.GamePadCount >= index ? input.GetGamePadByIndex(index).State.RightThumb : Vector2.Zero;
        }

        public static float GetLeftTrigger(this InputManager input, int index)
        {
            return input.GamePadCount >= index ? input.GetGamePadByIndex(index).State.LeftTrigger : 0.0f;
        }

        public static float GetRightTrigger(this InputManager input, int index)
        {
            return input.GamePadCount >= index ? input.GetGamePadByIndex(index).State.RightTrigger : 0.0f;
        }
    }
}
