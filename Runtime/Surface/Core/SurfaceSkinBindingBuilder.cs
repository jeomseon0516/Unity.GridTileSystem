using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>Grid vertex의 barycentric 위치에서 원본 vertex의 bone 가중치를 보간해 binding을 만듭니다.</summary>
    public static class SurfaceSkinBindingBuilder
    {
        /// <summary>합산 후 이 값 이하인 가중치는 버립니다. 4-influence 표준 대비 충분히 작은 값입니다.</summary>
        private const float WeightEpsilon = 0.00001f;

        /// <summary>
        /// 각 Surface binding 지점에 대해 소속 Triangle 세 정점의 bone 가중치를 barycentric으로 보간하고,
        /// 같은 bone index끼리 합산한 뒤 전체 합이 1이 되도록 정규화합니다. Triangle이 서로 다른 bone에
        /// 묶여 있어도 경계에서 가중치가 연속적으로 변하므로 Tile이 표면을 따라 자연스럽게 변형됩니다.
        /// </summary>
        /// <param name="topology">bind pose 위치와 Triangle 구성을 제공하는 원본 topology입니다.</param>
        /// <param name="surfacePoints">Geometry vertex 순서와 같은 Surface binding 목록입니다.</param>
        /// <param name="vertexInfluences">원본 Mesh vertex별 bone influence입니다.</param>
        /// <param name="boneCount">skinning 행렬 배열의 크기이자 유효 bone index의 상한입니다.</param>
        public static SurfaceSkinBinding Build(
            SurfaceTopology topology,
            IReadOnlyList<SurfacePoint> surfacePoints,
            IReadOnlyList<IReadOnlyList<SurfaceBoneInfluence>> vertexInfluences,
            int boneCount)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (surfacePoints == null) throw new ArgumentNullException(nameof(surfacePoints));
            if (vertexInfluences == null) throw new ArgumentNullException(nameof(vertexInfluences));
            if (boneCount <= 0) throw new ArgumentOutOfRangeException(nameof(boneCount));
            if (vertexInfluences.Count != topology.Positions.Count)
            {
                throw new ArgumentException(
                    "Influence list must cover every source vertex.", nameof(vertexInfluences));
            }

            int vertexCount = surfacePoints.Count;
            int[] offsets = new int[vertexCount + 1];
            List<SurfaceBoneInfluence> influences = new();
            Vector3[] bindPositions = new Vector3[vertexCount];
            Vector3[] bindNormals = new Vector3[vertexCount];
            Dictionary<int, float> accumulated = new();

            for (int vertex = 0; vertex < vertexCount; vertex++)
            {
                offsets[vertex] = influences.Count;
                SurfacePoint point = surfacePoints[vertex];
                if (!point.IsValid || point.Surface != topology.Handle)
                    throw new ArgumentException($"Surface point {vertex} does not belong to the supplied topology.", nameof(surfacePoints));

                SurfaceTriangle triangle = topology.Triangles[point.TriangleIndex];
                bindPositions[vertex] = topology.Evaluate(point);
                bindNormals[vertex] = CalculateFaceNormal(topology, triangle);

                accumulated.Clear();
                Accumulate(accumulated, vertexInfluences[triangle.A], point.Barycentric.x, boneCount);
                Accumulate(accumulated, vertexInfluences[triangle.B], point.Barycentric.y, boneCount);
                Accumulate(accumulated, vertexInfluences[triangle.C], point.Barycentric.z, boneCount);

                float total = 0f;
                foreach (KeyValuePair<int, float> pair in accumulated) total += pair.Value;
                if (total <= WeightEpsilon)
                {
                    // 세 정점 모두 유효한 가중치가 없으면 변형을 정의할 수 없습니다. bind pose를 유지하도록
                    // influence를 비워 두면 Evaluate가 zero 위치를 내므로 명시적 오류로 처리합니다.
                    throw new InvalidOperationException(
                        $"Surface point {vertex} has no bone influence on triangle {point.TriangleIndex}.");
                }

                foreach (KeyValuePair<int, float> pair in accumulated)
                {
                    float weight = pair.Value / total;
                    if (weight <= WeightEpsilon) continue;
                    influences.Add(new SurfaceBoneInfluence(pair.Key, weight));
                }
            }

            offsets[vertexCount] = influences.Count;
            return new SurfaceSkinBinding(offsets, influences.ToArray(), bindPositions, bindNormals, boneCount);
        }

        /// <summary>한 원본 정점의 influence를 barycentric 가중치로 스케일해 누적합니다.</summary>
        private static void Accumulate(
            Dictionary<int, float> accumulated,
            IReadOnlyList<SurfaceBoneInfluence> source,
            float barycentricWeight,
            int boneCount)
        {
            if (source == null || barycentricWeight <= 0f) return;
            foreach (SurfaceBoneInfluence influence in source)
            {
                if (influence.Weight <= 0f) continue;
                if ((uint)influence.BoneIndex >= (uint)boneCount)
                    throw new ArgumentOutOfRangeException(nameof(source), $"Bone index {influence.BoneIndex} is out of range.");

                float weight = influence.Weight * barycentricWeight;
                accumulated[influence.BoneIndex] = accumulated.TryGetValue(influence.BoneIndex, out float existing)
                    ? existing + weight
                    : weight;
            }
        }

        /// <summary>bind pose Triangle의 winding 법선을 계산합니다.</summary>
        private static Vector3 CalculateFaceNormal(SurfaceTopology topology, in SurfaceTriangle triangle)
        {
            Vector3 a = topology.Positions[triangle.A];
            Vector3 b = topology.Positions[triangle.B];
            Vector3 c = topology.Positions[triangle.C];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            return cross.sqrMagnitude > 0f ? cross.normalized : Vector3.zero;
        }
    }
}
