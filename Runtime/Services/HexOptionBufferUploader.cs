using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Services
{
    public sealed class HexOptionBufferUploader : IHexOptionBufferUploader
    {
        private static readonly int _bufferOn = Shader.PropertyToID("_BufferOn");
        private static readonly int _hexOptions = Shader.PropertyToID("_HexOptions");

        private ComputeBuffer _hexOptionBuffer;

        public void Upload(IReadOnlyList<HexGrid> tiles, Material material)
        {
            HexOption[] hexOptions = tiles
                .Select(hex => hex.GetShaderOption())
                .ToArray();

            int stride = Marshal.SizeOf<HexOption>();
            bool isZero = hexOptions.Length > 0;

            material.SetInt(_bufferOn, isZero ? 1 : 0);

            if (isZero)
            {
                _hexOptionBuffer?.Release();
                _hexOptionBuffer = new(hexOptions.Length, stride);
                _hexOptionBuffer.SetData(hexOptions);
                material.SetBuffer(_hexOptions, _hexOptionBuffer);
            }
        }

        public void Release()
        {
            _hexOptionBuffer?.Release();
            _hexOptionBuffer = null;
        }
    }
}
