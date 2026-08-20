using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexOptionBufferUploader
    {
        void Upload(IReadOnlyList<HexGrid> tiles, Material material);
        void Release();
    }
}
