using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Grid;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Rendering
{
    /// <summary>Surface Grid Region을 파이프라인 비종속 연속 vertex/index 배열로 변환합니다.</summary>
        public static class SurfaceGridGeometryBuilder
    {
        /// <summary>역행렬 계산에서 0으로 취급할 determinant 절댓값 한계입니다.</summary>
        private const float SingularDeterminantTolerance = 0.000000000001f;
        /// <summary>모든 Tile Region을 하나의 CPU Geometry snapshot으로 병합합니다.</summary>
        public static SurfaceGridGeometry Build(SurfaceTopology topology, SurfaceGrid grid)
            => Build(topology, grid, Matrix4x4.identity);

        /// <summary>Surface local Geometry를 지정한 대상 local 공간으로 변환하여 snapshot을 만듭니다.</summary>
        public static SurfaceGridGeometry Build(
            SurfaceTopology topology,
            SurfaceGrid grid,
            in Matrix4x4 surfaceToTarget)
            => Build(topology, grid, surfaceToTarget, 0f);

        /// <summary>대상 공간 변환과 법선 방향 offset을 적용하여 Geometry snapshot을 만듭니다.</summary>
        public static SurfaceGridGeometry Build(
            SurfaceTopology topology,
            SurfaceGrid grid,
            in Matrix4x4 surfaceToTarget,
            float surfaceOffset)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (grid.Patch.Surface != topology.Handle)
                throw new ArgumentException("Grid belongs to another surface topology.", nameof(grid));
            if (surfaceOffset < 0f || float.IsNaN(surfaceOffset) || float.IsInfinity(surfaceOffset))
                throw new ArgumentOutOfRangeException(nameof(surfaceOffset));
            if (!IsFinite(surfaceToTarget))
                throw new ArgumentException("Surface transform must contain only finite values.", nameof(surfaceToTarget));
            if (Mathf.Abs(surfaceToTarget.determinant) <= SingularDeterminantTolerance)
            {
                throw new ArgumentException(
                    "Surface transform must be invertible so normals can use its inverse transpose.",
                    nameof(surfaceToTarget));
            }

            List<Vector3> positions = new();
            List<Vector3> normals = new();
            List<Vector2> intrinsicPositions = new();
            List<int> tileIndices = new();
            List<int> triangleIndices = new();
            // 행렬 역산은 vertex마다 반복하기 비싸므로 snapshot 구축당 한 번만 계산합니다.
            Matrix4x4 normalMatrix = surfaceToTarget.inverse.transpose;

            for (int tileIndex = 0; tileIndex < grid.Tiles.Count; tileIndex++)
            {
                SurfaceRegion region = grid.Tiles[tileIndex].Region;
                int vertexOffset = positions.Count;
                foreach (SurfaceRegionVertex vertex in region.Vertices)
                {
                    Vector3 sourceNormal = CalculateFaceNormal(topology, vertex.SurfacePoint.TriangleIndex);
                    // 비균일 Scale에서도 법선이 접평면에 수직으로 남으려면 위치 행렬이 아니라
                    // inverse-transpose 행렬로 변환해야 합니다. 방향이므로 translation은 적용하지 않습니다.
                    Vector3 targetNormal = normalMatrix.MultiplyVector(sourceNormal);
                    targetNormal = targetNormal.sqrMagnitude > 0f ? targetNormal.normalized : Vector3.zero;
                    Vector3 targetPosition = surfaceToTarget.MultiplyPoint3x4(topology.Evaluate(vertex.SurfacePoint));
                    positions.Add(targetPosition + targetNormal * surfaceOffset);
                    normals.Add(targetNormal);
                    intrinsicPositions.Add(vertex.IntrinsicPosition);
                    tileIndices.Add(tileIndex);
                }

                foreach (int sourceIndex in region.TriangleIndices)
                {
                    triangleIndices.Add(vertexOffset + sourceIndex);
                }
            }

            return new SurfaceGridGeometry(
                positions.ToArray(),
                normals.ToArray(),
                intrinsicPositions.ToArray(),
                tileIndices.ToArray(),
                triangleIndices.ToArray());
        }

        /// <summary>원본 Triangle winding의 외적을 정규화해 Face 법선을 계산합니다.</summary>
        private static Vector3 CalculateFaceNormal(SurfaceTopology topology, int triangleIndex)
        {
            SurfaceTriangle triangle = topology.Triangles[triangleIndex];
            Vector3 a = topology.Positions[triangle.A];
            Vector3 b = topology.Positions[triangle.B];
            Vector3 c = topology.Positions[triangle.C];
            // (B-A)×(C-A)는 winding의 오른손 법칙 방향이며 길이는 면적의 두 배입니다.
            // Topology 진단을 통과한 Face라면 normalize할 수 있고, 결함 입력은 zero vector로 남깁니다.
            Vector3 cross = Vector3.Cross(b - a, c - a);
            return cross.sqrMagnitude > 0f ? cross.normalized : Vector3.zero;
        }

        /// <summary>행렬의 모든 원소가 NaN/Infinity 없이 역행렬 계산에 사용할 수 있는지 검사합니다.</summary>
        private static bool IsFinite(in Matrix4x4 matrix)
        {
            for (int element = 0; element < 16; element++)
            {
                float value = matrix[element];
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            }
            return true;
        }
    }
}
