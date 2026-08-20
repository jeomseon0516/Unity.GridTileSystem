using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexGridPointerInput
    {
        bool TryGetPointer(out Vector2 screenPosition, out bool pressedThisFrame, out bool releasedThisFrame);
    }
}
