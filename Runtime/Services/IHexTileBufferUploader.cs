using System.Collections.Generic;
using Jeomseon.Unity.Projector;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public interface IHexTileBufferUploader
    {
        void Upload(IReadOnlyList<HexTile> tiles, MeshProjector projector);
        void Release();
    }
}
