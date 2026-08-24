using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// Surface local 3D 방향을 펼쳐진 chart의 2D 방향으로 옮깁니다. 사용자가 지정하는 Grid 초기
    /// 방향은 월드 방향이지만 격자는 chart 위에 놓이므로, 그 사이를 잇는 유일한 변환입니다.
    /// </summary>
    public static class SurfaceChartDirection
    {
        /// <summary>Triangle basis가 면적을 갖지 않는다고 판정하는 Gram 행렬식 하한입니다.</summary>
        private const float DegenerateBasisEpsilon = 1e-12f;
        /// <summary>chart 방향이 방향으로서 의미를 잃었다고 판정하는 제곱 길이 하한입니다.</summary>
        private const float DegenerateDirectionEpsilon = 1e-12f;

        /// <summary>
        /// Surface local 방향을 seed Face의 chart 좌표계 방향으로 변환합니다. Triangle 평면에 수직인
        /// 성분은 chart에 대응하는 방향이 없으므로 자연스럽게 사라지며, 방향 전체가 법선과 나란하면
        /// 격자 방향을 정의할 수 없으므로 <see langword="false"/>를 반환합니다.
        /// </summary>
        public static bool TryGetChartDirection(
            SurfaceTopology topology,
            in SurfacePatchTriangle patchTriangle,
            in Vector3 surfaceDirection,
            out Vector2 chartDirection)
        {
            chartDirection = Vector2.zero;
            if (topology == null) return false;
            if ((uint)patchTriangle.TriangleIndex >= (uint)topology.Triangles.Count) return false;

            SurfaceTriangle triangle = topology.Triangles[patchTriangle.TriangleIndex];
            Vector3 firstEdge = topology.Positions[triangle.B] - topology.Positions[triangle.A];
            Vector3 secondEdge = topology.Positions[triangle.C] - topology.Positions[triangle.A];

            // 펼침은 Triangle 하나 위에서 등거리 변환이므로, 3D edge 두 개를 basis로 쓴 계수를 그대로
            // 대응하는 2D edge에 적용하면 방향이 보존됩니다. 계수는 Gram 행렬을 풀어 얻습니다.
            float firstDotFirst = Vector3.Dot(firstEdge, firstEdge);
            float firstDotSecond = Vector3.Dot(firstEdge, secondEdge);
            float secondDotSecond = Vector3.Dot(secondEdge, secondEdge);
            float determinant = firstDotFirst * secondDotSecond - firstDotSecond * firstDotSecond;
            if (determinant <= DegenerateBasisEpsilon) return false;

            float firstProjection = Vector3.Dot(surfaceDirection, firstEdge);
            float secondProjection = Vector3.Dot(surfaceDirection, secondEdge);
            float firstCoefficient = (firstProjection * secondDotSecond - secondProjection * firstDotSecond) / determinant;
            float secondCoefficient = (secondProjection * firstDotFirst - firstProjection * firstDotSecond) / determinant;

            chartDirection = firstCoefficient * (patchTriangle.B - patchTriangle.A) +
                             secondCoefficient * (patchTriangle.C - patchTriangle.A);
            if (chartDirection.sqrMagnitude <= DegenerateDirectionEpsilon)
            {
                // 방향이 통째로 표면 법선과 나란한 경우입니다. 조용히 기본 방향으로 되돌리지 않고
                // 호출자가 진단할 수 있도록 실패로 알립니다.
                chartDirection = Vector2.zero;
                return false;
            }
            return true;
        }
    }
}
