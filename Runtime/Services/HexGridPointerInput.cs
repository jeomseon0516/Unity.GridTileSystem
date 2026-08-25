using UnityEngine;
using UnityEngine.InputSystem;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexGridPointerInput : IHexGridPointerInput
    {
        private bool _wasLeftButtonPressed;

        public bool TryGetPointer(out Vector2 screenPosition, out bool pressedThisFrame, out bool releasedThisFrame)
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
            {
                screenPosition = default;
                pressedThisFrame = false;
                releasedThisFrame = false;
                _wasLeftButtonPressed = false;
                return false;
            }

            screenPosition = mouse.position.ReadValue();
            bool isLeftButtonPressed = mouse.leftButton.isPressed;
            pressedThisFrame = isLeftButtonPressed && !_wasLeftButtonPressed;
            releasedThisFrame = !isLeftButtonPressed && _wasLeftButtonPressed;
            _wasLeftButtonPressed = isLeftButtonPressed;
            return true;
        }
    }
}
