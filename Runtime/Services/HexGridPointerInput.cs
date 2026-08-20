using UnityEngine;
using UnityEngine.InputSystem;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexGridPointerInput : IHexGridPointerInput
    {
        public bool TryGetPointer(out Vector2 screenPosition, out bool pressedThisFrame, out bool releasedThisFrame)
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
            {
                screenPosition = default;
                pressedThisFrame = false;
                releasedThisFrame = false;
                return false;
            }

            screenPosition = mouse.position.ReadValue();
            pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
            releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
            return true;
        }
    }
}
