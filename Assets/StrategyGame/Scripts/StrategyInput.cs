using UnityEngine;
using UnityEngine.InputSystem;

namespace StrategyGame
{
    public static class StrategyInput
    {
        public static bool LeftClickThisFrame =>
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        public static bool RightClickThisFrame =>
            Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

        public static Vector2 PointerScreenPosition =>
            Mouse.current?.position.ReadValue() ?? Vector2.zero;
    }
}
