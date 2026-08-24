using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>Physics hit의 Triangle/barycentric identity를 intrinsic Tile 좌표로 변환합니다.</summary>
    public sealed class SurfaceGridPicker
    {
        /// <summary>Picking이 허용되는 원본 Surface Collider입니다.</summary>
        private readonly Collider _surfaceCollider;
        /// <summary>Raycast Triangle identity가 속하는 topology snapshot입니다.</summary>
        private readonly SurfaceTopology _topology;
        /// <summary>Patch mapper와 Logical Tile lookup을 제공하는 Grid snapshot입니다.</summary>
        private readonly SurfaceGrid _grid;

        /// <summary>원본 Collider, topology와 Grid snapshot으로 picker를 생성합니다.</summary>
        public SurfaceGridPicker(Collider surfaceCollider, SurfaceTopology topology, SurfaceGrid grid)
        {
            _surfaceCollider = surfaceCollider != null
                ? surfaceCollider
                : throw new ArgumentNullException(nameof(surfaceCollider));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            // chart는 연결을 건너 여러 Surface에 걸칠 수 있습니다. picking은 이 Collider가 속한
            // Surface의 Tile만 해석하면 되므로, 그 Surface가 chart에 포함되어 있는지만 확인합니다.
            if (!ContainsSurface(_grid.Patch, _topology.Handle))
                throw new ArgumentException("Grid does not cover the supplied surface topology.", nameof(grid));
        }

        /// <summary>chart에 지정한 Surface의 Face가 하나라도 포함되어 있는지 검사합니다.</summary>
        private static bool ContainsSurface(SurfacePatch patch, SurfaceHandle surface)
        {
            foreach (SurfacePatchTriangle triangle in patch.Triangles)
            {
                if (triangle.Surface == surface) return true;
            }
            return false;
        }

        /// <summary>Ray가 원본 Surface의 활성 Tile과 교차하면 해당 hit와 Tile Region을 반환합니다.</summary>
        public bool TryPick(
            in Ray ray,
            LayerMask layerMask,
            out RaycastHit hit,
            out SurfaceGridTileRegion tile)
        {
            hit = default;
            tile = null;
            int surfaceLayerBit = 1 << _surfaceCollider.gameObject.layer;
            if ((layerMask.value & surfaceLayerBit) == 0 ||
                !_surfaceCollider.Raycast(ray, out hit, Mathf.Infinity))
                return false;

            SurfacePoint point;
            Vector3 localHit = _surfaceCollider.transform.InverseTransformPoint(hit.point);
            if (_topology.TryGetSurfacePoint(localHit, out point))
            {
                // Terrain 등 계산형 topology는 Collider의 내부 Triangle index 계약에 의존하지 않고
                // local hit 위치를 규칙적 topology identity로 직접 복원합니다.
            }
            else if (hit.triangleIndex >= 0)
            {
                // MeshCollider는 원본 Triangle index와 barycentricCoordinate를 직접 제공합니다.
                point = new SurfacePoint(_topology.Handle, hit.triangleIndex, hit.barycentricCoordinate);
            }
            else
            {
                return false;
            }
            if (!SurfacePatchMapper.TryGetIntrinsicPosition(_grid.Patch, point, out Vector2 intrinsic)) return false;
            HexCoordinates coordinates = _grid.Layout.GetCoordinates(intrinsic);
            return _grid.TryGetTile(coordinates, out tile);
        }
    }
}
