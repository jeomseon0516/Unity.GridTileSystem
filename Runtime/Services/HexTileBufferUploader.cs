using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Jeomseon.Unity.Projector;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexTileBufferUploader : IHexTileBufferUploader
    {
        private static readonly int BufferEnabled = Shader.PropertyToID("_HexGridTileBufferEnabled");
        private static readonly int HexTiles = Shader.PropertyToID("_HexGridTiles");

        private ComputeBuffer _tileBuffer;
        private MeshProjector _projector;

        public void Upload(IReadOnlyList<HexTile> tiles, MeshProjector projector)
        {
            if (_projector != null && _projector != projector)
            {
                _projector.SetFloat(BufferEnabled, 0);
                _projector.SetBuffer(HexTiles, (ComputeBuffer)null);
                ReleaseBuffer();
            }

            _projector = projector;

            HexTileRenderData[] renderData = tiles
                .Select(tile => tile.GetRenderData())
                .ToArray();

            if (renderData.Length == 0)
            {
                ReleaseBuffer();
                projector.SetFloat(BufferEnabled, 0);
                projector.SetBuffer(HexTiles, (ComputeBuffer)null);
                return;
            }

            int stride = Marshal.SizeOf<HexTileRenderData>();
            if (_tileBuffer == null || _tileBuffer.count != renderData.Length || _tileBuffer.stride != stride)
            {
                ReleaseBuffer();
                _tileBuffer = new ComputeBuffer(renderData.Length, stride);
            }

            _tileBuffer.SetData(renderData);
            projector.SetBuffer(HexTiles, _tileBuffer);
            projector.SetFloat(BufferEnabled, 1);
        }

        public void Release()
        {
            if (_projector != null)
            {
                _projector.SetFloat(BufferEnabled, 0);
                _projector.SetBuffer(HexTiles, (ComputeBuffer)null);
            }

            ReleaseBuffer();
            _projector = null;
        }

        private void ReleaseBuffer()
        {
            _tileBuffer?.Release();
            _tileBuffer = null;
        }
    }
}
