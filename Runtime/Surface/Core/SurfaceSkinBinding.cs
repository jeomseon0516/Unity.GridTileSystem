using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// Grid vertex를 원본 Surface의 bone 집합에 묶은 불변 snapshot입니다. Triangle index와 barycentric
    /// 좌표가 변형에 불변이므로 binding은 Bake 시점에 한 번만 계산하고, 이후에는 bone 행렬만 바뀝니다.
    /// influence는 vertex마다 개수가 다르므로 연속 배열 하나와 구간 오프셋으로 저장합니다.
    /// </summary>
    public sealed class SurfaceSkinBinding
    {
        /// <summary>vertex별 influence 구간의 시작 오프셋이며 길이는 vertex 수 + 1입니다.</summary>
        private readonly int[] _influenceOffsets;
        /// <summary>모든 vertex의 influence를 구간 순서대로 이어 붙인 배열입니다.</summary>
        private readonly SurfaceBoneInfluence[] _influences;
        /// <summary>bind pose 공간에서의 vertex 위치입니다.</summary>
        private readonly Vector3[] _bindPositions;
        /// <summary>bind pose 공간에서의 vertex 법선입니다.</summary>
        private readonly Vector3[] _bindNormals;

        /// <summary>binding이 덮는 vertex 개수를 가져옵니다.</summary>
        public int VertexCount => _bindPositions.Length;
        /// <summary>이 binding이 참조하는 bone 개수를 가져옵니다.</summary>
        public int BoneCount { get; }

        /// <summary>Builder가 소유한 배열로 binding snapshot을 생성합니다.</summary>
        internal SurfaceSkinBinding(
            int[] influenceOffsets,
            SurfaceBoneInfluence[] influences,
            Vector3[] bindPositions,
            Vector3[] bindNormals,
            int boneCount)
        {
            _influenceOffsets = influenceOffsets ?? throw new ArgumentNullException(nameof(influenceOffsets));
            _influences = influences ?? throw new ArgumentNullException(nameof(influences));
            _bindPositions = bindPositions ?? throw new ArgumentNullException(nameof(bindPositions));
            _bindNormals = bindNormals ?? throw new ArgumentNullException(nameof(bindNormals));
            BoneCount = boneCount;
        }

        /// <summary>지정한 vertex의 bone influence 목록을 열거합니다.</summary>
        public IEnumerable<SurfaceBoneInfluence> GetInfluences(int vertexIndex)
        {
            if ((uint)vertexIndex >= (uint)VertexCount) throw new ArgumentOutOfRangeException(nameof(vertexIndex));
            int start = _influenceOffsets[vertexIndex];
            int end = _influenceOffsets[vertexIndex + 1];
            for (int i = start; i < end; i++) yield return _influences[i];
        }

        /// <summary>
        /// 현재 프레임의 skinning 행렬로 변형된 위치와 법선을 계산해 대상 버퍼에 채웁니다.
        /// <paramref name="skinningMatrices"/>는 bone별 <c>boneTransform.localToWorldMatrix * bindpose</c>이며
        /// 호출자가 원하는 대상 공간으로 이미 변환된 상태여야 합니다.
        /// </summary>
        public void Evaluate(
            IReadOnlyList<Matrix4x4> skinningMatrices,
            Vector3[] positions,
            Vector3[] normals)
        {
            if (skinningMatrices == null) throw new ArgumentNullException(nameof(skinningMatrices));
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (normals == null) throw new ArgumentNullException(nameof(normals));
            if (positions.Length != VertexCount || normals.Length != VertexCount)
                throw new ArgumentException($"Buffers must have exactly {VertexCount} elements.", nameof(positions));
            if (skinningMatrices.Count < BoneCount)
                throw new ArgumentException($"At least {BoneCount} skinning matrices are required.", nameof(skinningMatrices));

            for (int vertex = 0; vertex < VertexCount; vertex++)
            {
                Vector3 bindPosition = _bindPositions[vertex];
                Vector3 bindNormal = _bindNormals[vertex];
                Vector3 position = Vector3.zero;
                Vector3 normal = Vector3.zero;
                int start = _influenceOffsets[vertex];
                int end = _influenceOffsets[vertex + 1];

                // 선형 blend skinning입니다. 위치는 아핀 변환이라 translation을 포함하고,
                // 법선은 방향이므로 translation을 제외한 회전·스케일 부분만 적용합니다.
                for (int i = start; i < end; i++)
                {
                    SurfaceBoneInfluence influence = _influences[i];
                    Matrix4x4 matrix = skinningMatrices[influence.BoneIndex];
                    position += matrix.MultiplyPoint3x4(bindPosition) * influence.Weight;
                    normal += matrix.MultiplyVector(bindNormal) * influence.Weight;
                }

                positions[vertex] = position;
                // 가중 평균된 법선은 단위 길이가 아니며, 서로 반대인 bone이 상쇄되면 0이 될 수 있습니다.
                normals[vertex] = normal.sqrMagnitude > 0f ? normal.normalized : Vector3.zero;
            }
        }
    }
}
