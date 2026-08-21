using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// intrinsic polygon과 Surface Patch가 겹치는 영역을 Triangle 목록으로 표현한 불변 snapshot입니다.
    /// 각 vertex의 <see cref="SurfacePoint"/>를 평가하면 원본 local 3D Geometry를 복원할 수 있습니다.
    /// </summary>
    public sealed class SurfaceRegion
    {
        /// <summary>Triangle index buffer가 참조하는 Region vertex 배열입니다.</summary>
        private readonly SurfaceRegionVertex[] _vertices;
        /// <summary>세 index가 한 Triangle을 이루는 연속 Triangle index 배열입니다.</summary>
        private readonly int[] _triangleIndices;
        /// <summary>내부 vertex 배열의 외부 변경을 차단하는 view입니다.</summary>
        private readonly IReadOnlyList<SurfaceRegionVertex> _verticesView;
        /// <summary>내부 index 배열의 외부 변경을 차단하는 view입니다.</summary>
        private readonly IReadOnlyList<int> _triangleIndicesView;

        /// <summary>Region의 intrinsic 위치와 Surface binding을 가진 vertex를 가져옵니다.</summary>
        public IReadOnlyList<SurfaceRegionVertex> Vertices => _verticesView;
        /// <summary>세 개씩 묶어 읽는 Triangle index를 가져옵니다.</summary>
        public IReadOnlyList<int> TriangleIndices => _triangleIndicesView;
        /// <summary>clipping 결과가 차지하는 intrinsic 2D 면적을 가져옵니다.</summary>
        public float IntrinsicArea { get; }

        /// <summary>Region builder가 소유한 vertex와 index 배열로 완성된 Region을 생성합니다.</summary>
        internal SurfaceRegion(SurfaceRegionVertex[] vertices, int[] triangleIndices)
        {
            _vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            _triangleIndices = triangleIndices ?? throw new ArgumentNullException(nameof(triangleIndices));
            _verticesView = Array.AsReadOnly(_vertices);
            _triangleIndicesView = Array.AsReadOnly(_triangleIndices);
            IntrinsicArea = CalculateIntrinsicArea(_vertices, _triangleIndices);
        }

        /// <summary>Region triangle들의 절댓값 면적을 합산합니다.</summary>
        private static float CalculateIntrinsicArea(
            IReadOnlyList<SurfaceRegionVertex> vertices,
            IReadOnlyList<int> triangleIndices)
        {
            float areaTwice = 0f;
            for (int i = 0; i < triangleIndices.Count; i += 3)
            {
                Vector2 a = vertices[triangleIndices[i]].IntrinsicPosition;
                Vector2 b = vertices[triangleIndices[i + 1]].IntrinsicPosition;
                Vector2 c = vertices[triangleIndices[i + 2]].IntrinsicPosition;
                areaTwice += Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
            }
            return areaTwice * 0.5f;
        }
    }
}
