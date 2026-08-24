using System;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Jeomseon.Unity.GridTileSystem.Surface.Query;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>
    /// 월드 좌표 하나로 Grid를 생성하는 진입점입니다. Surface 등록·목록·선택 단계가 없으므로 사용자
    /// workflow는 "어디에, 얼마나 촘촘하게"만 지정하는 것으로 끝납니다.
    /// </summary>
    /// <remarks>
    /// Seed 탐색은 <see cref="ISurfaceQuery"/>, 경계 너머 topology 조회는 <see cref="ISurfaceProvider"/>가
    /// 담당합니다. 기본 구성에서는 <see cref="GeometrySurfaceQuery"/> 하나가 두 역할을 겸합니다.
    /// </remarks>
    public sealed class SurfaceGridSystem : IDisposable
    {
        /// <summary>Seed 위치에서 시작 표면을 찾는 계층입니다.</summary>
        private readonly ISurfaceQuery _query;
        /// <summary>handle로 topology를 되찾는 계층입니다.</summary>
        private readonly ISurfaceProvider _surfaces;
        /// <summary>기본 구성에서 이 시스템이 직접 만들어 수명을 책임지는 query입니다.</summary>
        private readonly GeometrySurfaceQuery _ownedQuery;

        /// <summary>기본 Physics 후보 수집과 기본 Adapter 목록으로 시스템을 만듭니다.</summary>
        public SurfaceGridSystem() : this(new GeometrySurfaceQuery())
        {
        }

        /// <summary>질의와 provider를 겸하는 구현 하나로 시스템을 만들고 그 수명을 넘겨받습니다.</summary>
        public SurfaceGridSystem(GeometrySurfaceQuery query)
        {
            _ownedQuery = query ?? throw new ArgumentNullException(nameof(query));
            _query = query;
            _surfaces = query;
        }

        /// <summary>질의와 provider를 각각 주입합니다. 수명은 호출자가 관리합니다.</summary>
        public SurfaceGridSystem(ISurfaceQuery query, ISurfaceProvider surfaces)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _surfaces = surfaces ?? throw new ArgumentNullException(nameof(surfaces));
            _ownedQuery = null;
        }

        /// <summary>
        /// 요청 하나로 Grid를 생성합니다. 실패해도 예외를 던지지 않고
        /// <see cref="SurfaceGridBuildResult.Status"/>로 원인을 알립니다.
        /// </summary>
        public SurfaceGridBuildResult Build(in SurfaceGridRequest request)
        {
            if (!_query.TryFindSeed(request.SeedPosition, request.QueryOptions, out SurfaceQueryHit hit))
            {
                return SurfaceGridBuildResult.Failure(
                    SurfaceGridBuildStatus.SurfaceNotFound,
                    $"No supported surface was found within {request.QueryOptions.SearchRadius} units of {request.SeedPosition}.");
            }

            Vector3 surfaceDirection = ToSurfaceDirection(hit, request.InitialDirection);
            SurfaceGrid grid;
            try
            {
                grid = SurfaceGridBuilder.Build(
                    _surfaces, hit.Point, request.TileRadius, request.PatchSettings, surfaceDirection);
            }
            catch (ArgumentException exception)
            {
                // 초기 방향이 표면 법선과 나란한 경우만 방향 오류로 구분하고 나머지는 구축 실패로 봅니다.
                SurfaceGridBuildStatus status = exception.ParamName == "initialSurfaceDirection"
                    ? SurfaceGridBuildStatus.InvalidInitialDirection
                    : SurfaceGridBuildStatus.BuildFailed;
                return SurfaceGridBuildResult.Failure(status, exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return SurfaceGridBuildResult.Failure(SurfaceGridBuildStatus.BuildFailed, exception.Message);
            }

            if (grid.Tiles.Count == 0)
            {
                return SurfaceGridBuildResult.Empty(
                    grid,
                    hit.Adapter,
                    hit.Topology,
                    hit.Point,
                    $"The surface has no region large enough for a complete tile of radius {request.TileRadius}.");
            }

            return SurfaceGridBuildResult.Success(grid, hit.Adapter, hit.Topology, hit.Point);
        }

        /// <summary>이 시스템이 직접 만든 query가 캐시한 Adapter와 topology를 해제합니다.</summary>
        public void Dispose() => _ownedQuery?.Clear();

        /// <summary>월드 초기 방향을 seed 표면의 local 방향으로 옮깁니다.</summary>
        private static Vector3 ToSurfaceDirection(in SurfaceQueryHit hit, in Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude <= 0f) return Vector3.zero;
            Transform surfaceTransform = hit.Adapter != null ? hit.Adapter.SurfaceTransform : null;
            // Transform이 없으면 topology가 이미 월드 기준이라는 뜻이므로 방향을 그대로 씁니다.
            return surfaceTransform != null
                ? surfaceTransform.InverseTransformDirection(worldDirection)
                : worldDirection;
        }
    }
}
