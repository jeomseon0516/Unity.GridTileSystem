using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
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
        /// <summary>각 vertex의 원본 Surface barycentric binding입니다.</summary>
        private readonly SurfacePoint[] _surfacePoints;
        /// <summary>세 개씩 한 Triangle을 구성하는 Geometry index 배열입니다.</summary>
        private readonly int[] _triangleIndices;
        /// <summary>
        /// 두 개씩 한 Edge를 구성하는 Tile 외곽선 index 배열입니다. Tile마다 내부 fragment 사이의
        /// 공유 Edge(정확히 두 Triangle이 참조)는 제외하고, 정확히 한 Triangle만 참조하는 Edge만
        /// 남긴 결과라 다중 fragment로 clipping된 Tile도 실제 육각형 윤곽만 남습니다.
        /// </summary>
        private readonly int[] _outlineIndices;
        /// <summary>내부 position 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<Vector3> _positionsView;
        /// <summary>내부 normal 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<Vector3> _normalsView;
        /// <summary>내부 intrinsic 좌표 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<Vector2> _intrinsicPositionsView;
        /// <summary>내부 Tile index 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<int> _tileIndicesView;
        /// <summary>내부 Surface binding 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<SurfacePoint> _surfacePointsView;
        /// <summary>내부 Triangle index 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<int> _triangleIndicesView;
        /// <summary>내부 Outline index 배열을 노출하지 않는 read-only view입니다.</summary>
        private readonly IReadOnlyList<int> _outlineIndicesView;

        /// <summary>Surface local vertex 위치를 가져옵니다.</summary>
        public IReadOnlyList<Vector3> Positions => _positionsView;
        /// <summary>Surface local vertex 법선을 가져옵니다.</summary>
        public IReadOnlyList<Vector3> Normals => _normalsView;
        /// <summary>local chart의 intrinsic vertex 위치를 가져옵니다.</summary>
        public IReadOnlyList<Vector2> IntrinsicPositions => _intrinsicPositionsView;
        /// <summary>vertex별 Logical Tile index를 가져옵니다.</summary>
        public IReadOnlyList<int> TileIndices => _tileIndicesView;
        /// <summary>
        /// vertex별 원본 Surface binding을 가져옵니다. Triangle index와 barycentric 좌표는 표면이
        /// 변형돼도 불변이므로, 이 값으로 Geometry를 다시 만들지 않고 위치만 재평가할 수 있습니다.
        /// </summary>
        public IReadOnlyList<SurfacePoint> SurfacePoints => _surfacePointsView;
        /// <summary>Triangle index buffer를 가져옵니다.</summary>
        public IReadOnlyList<int> TriangleIndices => _triangleIndicesView;
        /// <summary>Tile 외곽선 Line index buffer를 가져옵니다.</summary>
        public IReadOnlyList<int> OutlineIndices => _outlineIndicesView;

        /// <summary>Geometry builder가 소유한 연속 배열로 불변 snapshot을 생성합니다.</summary>
        internal SurfaceGridGeometry(
            Vector3[] positions,
            Vector3[] normals,
            Vector2[] intrinsicPositions,
            int[] tileIndices,
            int[] triangleIndices,
            int[] outlineIndices,
            SurfacePoint[] surfacePoints)
        {
            _positions = positions ?? throw new ArgumentNullException(nameof(positions));
            _normals = normals ?? throw new ArgumentNullException(nameof(normals));
            _intrinsicPositions = intrinsicPositions ?? throw new ArgumentNullException(nameof(intrinsicPositions));
            _tileIndices = tileIndices ?? throw new ArgumentNullException(nameof(tileIndices));
            _triangleIndices = triangleIndices ?? throw new ArgumentNullException(nameof(triangleIndices));
            _outlineIndices = outlineIndices ?? throw new ArgumentNullException(nameof(outlineIndices));
            _surfacePoints = surfacePoints ?? throw new ArgumentNullException(nameof(surfacePoints));
            _positionsView = Array.AsReadOnly(_positions);
            _normalsView = Array.AsReadOnly(_normals);
            _intrinsicPositionsView = Array.AsReadOnly(_intrinsicPositions);
            _tileIndicesView = Array.AsReadOnly(_tileIndices);
            _triangleIndicesView = Array.AsReadOnly(_triangleIndices);
            _outlineIndicesView = Array.AsReadOnly(_outlineIndices);
            _surfacePointsView = Array.AsReadOnly(_surfacePoints);
        }
    }
}
