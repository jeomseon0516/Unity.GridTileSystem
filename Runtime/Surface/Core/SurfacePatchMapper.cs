using System;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>SurfacePoint와 local Surface Patch의 intrinsic 좌표 사이를 변환합니다.</summary>
    public static class SurfacePatchMapper
    {
        /// <summary>SurfacePoint가 Patch에 포함되어 있으면 barycentric 보간으로 intrinsic 좌표를 계산합니다.</summary>
        public static bool TryGetIntrinsicPosition(
            SurfacePatch patch,
            in SurfacePoint point,
            out Vector2 intrinsicPosition)
        {
            if (patch == null) throw new ArgumentNullException(nameof(patch));
            if (!point.IsValid || point.Surface != patch.Surface)
            {
                intrinsicPosition = default;
                return false;
            }

            foreach (SurfacePatchTriangle triangle in patch.Triangles)
            {
                if (!triangle.Matches(point.Surface, point.TriangleIndex)) continue;
                // 펼쳐진 corner에도 원본 Triangle과 같은 barycentric 가중치를 적용할 수 있습니다.
                // Triangle Unfolding은 affine 좌표 관계를 보존하므로 3D와 2D에서 가중치가 동일합니다.
                intrinsicPosition = triangle.A * point.Barycentric.x +
                                    triangle.B * point.Barycentric.y +
                                    triangle.C * point.Barycentric.z;
                return true;
            }

            intrinsicPosition = default;
            return false;
        }
    }
}
