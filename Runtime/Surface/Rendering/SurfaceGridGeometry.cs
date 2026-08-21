using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Rendering
{
    /// <summary>렌더 파이프라인 타입을 포함하지 않는 Surface Grid의 CPU Geometry snapshot입니다.</summary>
    public sealed class SurfaceGridGeometry
    {
        /// <summary>Surface local 공간의 vertex 위치 배열입니다.</summary>
        private readonly Vector3[] _positions;
        /// <summary>원본 Face winding에서 계산한 Surface local 법선 배열입니다.</summary>
        private readonly Vector3[] _normals;
        /// <summary>각 vertex의 local chart 좌표 배열입니다.</summary>
        private readonly Vector2[] _intrinsicPositions;
        /// <summary>각 vertex가 속한 Logical Tile의 배열 index입니다.</summary>
        private readonly int[] _tileIndices;
        /// <summary>세 개씩 한 Triangle을 구성하는 Geometry index 배열입니다.</summary>
        private readonly int[] _triangleIndices;
        /// <summary>내부 position 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<Vector3> _positionsView;
        /// <summary>내부 normal 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<Vector3> _normalsView;
        /// <summary>내부 intrinsic 좌표 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<Vector2> _intrinsicPositionsView;
        /// <summary>내부 Tile index 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<int> _tileIndicesView;
        /// <summary>내부 Triangle index 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<int> _triangleIndicesView;

        /// <summary>Surface local vertex 위치를 가져옵니다.</summary>
        public IReadOnlyList<Vector3> Positions => _positionsView;
        /// <summary>Surface local vertex 법선을 가져옵니다.</summary>
        public IReadOnlyList<Vector3> Normals => _normalsView;
        /// <summary>local chart의 intrinsic vertex 위치를 가져옵니다.</summary>
        public IReadOnlyList<Vector2> IntrinsicPositions => _intrinsicPositionsView;
        /// <summary>vertex별 Logical Tile index를 가져옵니다.</summary>
        public IReadOnlyList<int> TileIndices => _tileIndicesView;
        /// <summary>Triangle index buffer를 가져옵니다.</summary>
        public IReadOnlyList<int> TriangleIndices => _triangleIndicesView;

        /// <summary>Geometry builder가 소유한 연속 배열로 불변 snapshot을 생성합니다.</summary>
        internal SurfaceGridGeometry(
            Vector3[] positions,
            Vector3[] normals,
            Vector2[] intrinsicPositions,
            int[] tileIndices,
            int[] triangleIndices)
        {
            _positions = positions ?? throw new ArgumentNullException(nameof(positions));
            _normals = normals ?? throw new ArgumentNullException(nameof(normals));
            _intrinsicPositions = intrinsicPositions ?? throw new ArgumentNullException(nameof(intrinsicPositions));
            _tileIndices = tileIndices ?? throw new ArgumentNullException(nameof(tileIndices));
            _triangleIndices = triangleIndices ?? throw new ArgumentNullException(nameof(triangleIndices));
            _positionsView = Array.AsReadOnly(_positions);
            _normalsView = Array.AsReadOnly(_normals);
            _intrinsicPositionsView = Array.AsReadOnly(_intrinsicPositions);
            _tileIndicesView = Array.AsReadOnly(_tileIndices);
            _triangleIndicesView = Array.AsReadOnly(_triangleIndices);
        }
    }
}
