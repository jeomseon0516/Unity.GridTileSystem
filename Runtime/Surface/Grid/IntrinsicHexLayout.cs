using System;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>local Surface Patch에서 실제 길이 단위로 flat-top Hex의 중심과 꼭짓점을 계산합니다.</summary>
    public readonly struct IntrinsicHexLayout
    {
        /// <summary>sqrt(3)의 단정도 상수입니다.</summary>
        private const float SquareRootThree = 1.7320508075688772f;

        /// <summary>격자 회전을 적용할 때 재사용하는 회전각의 코사인입니다.</summary>
        private readonly float _cosineRotation;
        /// <summary>격자 회전을 적용할 때 재사용하는 회전각의 사인입니다.</summary>
        private readonly float _sineRotation;

        /// <summary>Grid 원점 Hex의 intrinsic 중심을 가져옵니다.</summary>
        public Vector2 Origin { get; }
        /// <summary>Hex 중심에서 꼭짓점까지의 intrinsic 길이를 가져옵니다.</summary>
        public float Radius { get; }
        /// <summary>
        /// 격자 전체를 <see cref="Origin"/> 기준으로 반시계 회전시키는 각도(라디안)를 가져옵니다.
        /// 0이면 첫 꼭짓점이 chart의 +X를 향하는 기존 배치와 완전히 동일합니다.
        /// </summary>
        public float Rotation { get; }

        /// <summary>intrinsic 원점과 양수 반지름으로 회전 없는 flat-top Hex layout을 생성합니다.</summary>
        public IntrinsicHexLayout(in Vector2 origin, float radius) : this(origin, radius, 0f) { }

        /// <summary>intrinsic 원점·양수 반지름·격자 회전각으로 flat-top Hex layout을 생성합니다.</summary>
        public IntrinsicHexLayout(in Vector2 origin, float radius, float rotation)
        {
            if (!IsFinite(origin))
                throw new ArgumentException("Origin must contain only finite values.", nameof(origin));
            if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (float.IsNaN(rotation) || float.IsInfinity(rotation))
                throw new ArgumentOutOfRangeException(nameof(rotation));
            Origin = origin;
            Radius = radius;
            Rotation = rotation;
            // 회전 0이면 cos=1, sin=0이 정확히 나오므로 이하 모든 식이 회전 도입 전과 비트 단위로 같습니다.
            _cosineRotation = Mathf.Cos(rotation);
            _sineRotation = Mathf.Sin(rotation);
        }

        /// <summary>
        /// chart 상의 방향 벡터를 첫 Hex 꼭짓점 방향으로 삼는 layout을 생성합니다. 사용자가 지정하는
        /// 초기 방향(월드 방향을 seed의 tangent 평면에 투영한 chart 방향)을 격자 회전으로 옮기는 진입점입니다.
        /// </summary>
        public static IntrinsicHexLayout FromDirection(
            in Vector2 origin,
            float radius,
            in Vector2 intrinsicDirection)
        {
            if (!IsFinite(intrinsicDirection))
                throw new ArgumentException("Direction must contain only finite values.", nameof(intrinsicDirection));
            if (Mathf.Approximately(intrinsicDirection.sqrMagnitude, 0f))
                throw new ArgumentException("Direction must not be a zero vector.", nameof(intrinsicDirection));
            return new IntrinsicHexLayout(origin, radius, Mathf.Atan2(intrinsicDirection.y, intrinsicDirection.x));
        }

        /// <summary>Axial 좌표를 flat-top Hex 중심의 intrinsic 2D 위치로 변환합니다.</summary>
        public Vector2 GetCenter(in AxialCoordinates coordinates) => Origin + Rotate(GetLatticeOffset(coordinates));

        /// <summary>Axial 좌표 Hex의 반시계 방향 꼭짓점 여섯 개를 반환합니다.</summary>
        public Vector2[] GetCorners(in AxialCoordinates coordinates)
        {
            Vector2 center = GetCenter(coordinates);
            Vector2[] corners = new Vector2[6];
            for (int corner = 0; corner < corners.Length; corner++)
            {
                // flat-top Hex는 회전 0에서 첫 꼭짓점이 +X 방향이며 이후 60°씩 반시계 회전합니다.
                // 격자 회전은 중심 배치와 꼭짓점 각도에 같은 각을 더하므로 Hex끼리의 정합이 유지됩니다.
                float angle = Rotation + Mathf.Deg2Rad * (corner * 60f);
                corners[corner] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Radius;
            }
            return corners;
        }

        /// <summary>intrinsic 위치를 가장 가까운 Axial Hex 좌표로 반올림합니다.</summary>
        public HexCoordinates GetCoordinates(in Vector2 intrinsicPosition)
        {
            if (!IsFinite(intrinsicPosition))
                throw new ArgumentException("Intrinsic position must contain only finite values.", nameof(intrinsicPosition));
            Vector2 local = InverseRotate(intrinsicPosition - Origin) / Radius;
            // 아래 flat-top basis 행렬을 역변환한 식입니다.
            // q = 2x/3, r = -x/3 + y/sqrt(3)를 얻은 뒤 cube invariant를 보존하며 반올림합니다.
            Vector2 fractionalAxial = new(
                2f * local.x / 3f,
                -local.x / 3f + local.y / SquareRootThree);
            return HexCoordinates.Round(fractionalAxial);
        }

        /// <summary>
        /// intrinsic 경계를 덮는 데 필요한 flat-top Hex 열(q) 구간을 계산합니다. 격자가 회전해 있으면
        /// 경계를 격자 좌표계로 역회전한 AABB를 기준으로 삼으므로 결과는 항상 필요한 구간을 포함합니다.
        /// </summary>
        public void GetColumnRange(in Rect intrinsicBounds, out int minimumQ, out int maximumQ)
        {
            GetLatticeBounds(intrinsicBounds, out Rect latticeBounds);
            // x = 1.5R·q 를 q에 대해 푼 값입니다. Hex는 중심에서 R만큼 뻗으므로 양쪽에 한 칸씩 여유를 둡니다.
            float columnSpacing = Radius * 1.5f;
            minimumQ = Mathf.FloorToInt(latticeBounds.xMin / columnSpacing) - 1;
            maximumQ = Mathf.CeilToInt(latticeBounds.xMax / columnSpacing) + 1;
        }

        /// <summary>지정한 q 열에서 intrinsic 경계를 덮는 데 필요한 행(r) 구간을 계산합니다.</summary>
        public void GetRowRange(in Rect intrinsicBounds, int q, out int minimumR, out int maximumR)
        {
            GetLatticeBounds(intrinsicBounds, out Rect latticeBounds);
            // y = sqrt(3)R·(r + q/2) 를 r에 대해 푼 값입니다.
            float rowSpacing = Radius * SquareRootThree;
            float columnOffset = q * 0.5f;
            minimumR = Mathf.FloorToInt(latticeBounds.yMin / rowSpacing - columnOffset) - 1;
            maximumR = Mathf.CeilToInt(latticeBounds.yMax / rowSpacing - columnOffset) + 1;
        }

        /// <summary>회전을 적용하기 전 격자 좌표계에서의 Hex 중심 offset을 계산합니다.</summary>
        private Vector2 GetLatticeOffset(in AxialCoordinates coordinates)
        {
            // flat-top axial basis는 q축 (3R/2, sqrt(3)R/2), r축 (0, sqrt(3)R)입니다.
            // 두 basis의 정수 선형 결합이므로 인접 Hex 중심 간격과 격자 정합성이 유지됩니다.
            return new Vector2(
                Radius * 1.5f * coordinates.Q,
                Radius * SquareRootThree * (coordinates.R + coordinates.Q * 0.5f));
        }

        /// <summary>
        /// intrinsic 경계를 원점·회전이 제거된 격자 좌표계 AABB로 옮깁니다. 회전이 있으면 네 꼭짓점을
        /// 역회전한 뒤 그 AABB를 취하므로 원본 경계를 포함하는 보수적인 구간이 됩니다.
        /// </summary>
        private void GetLatticeBounds(in Rect intrinsicBounds, out Rect latticeBounds)
        {
            if (!IsFinite(intrinsicBounds.min) || !IsFinite(intrinsicBounds.max))
                throw new ArgumentException("Bounds must contain only finite values.", nameof(intrinsicBounds));

            Vector2 lowerLeft = InverseRotate(intrinsicBounds.min - Origin);
            Vector2 lowerRight = InverseRotate(new Vector2(intrinsicBounds.xMax, intrinsicBounds.yMin) - Origin);
            Vector2 upperRight = InverseRotate(intrinsicBounds.max - Origin);
            Vector2 upperLeft = InverseRotate(new Vector2(intrinsicBounds.xMin, intrinsicBounds.yMax) - Origin);

            Vector2 minimum = Vector2.Min(Vector2.Min(lowerLeft, lowerRight), Vector2.Min(upperRight, upperLeft));
            Vector2 maximum = Vector2.Max(Vector2.Max(lowerLeft, lowerRight), Vector2.Max(upperRight, upperLeft));
            latticeBounds = Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        /// <summary>격자 좌표계 벡터를 intrinsic chart 방향으로 회전시킵니다.</summary>
        private Vector2 Rotate(in Vector2 value) => new(
            value.x * _cosineRotation - value.y * _sineRotation,
            value.x * _sineRotation + value.y * _cosineRotation);

        /// <summary>intrinsic chart 벡터에서 격자 회전을 제거합니다.</summary>
        private Vector2 InverseRotate(in Vector2 value) => new(
            value.x * _cosineRotation + value.y * _sineRotation,
            -value.x * _sineRotation + value.y * _cosineRotation);

        /// <summary>좌표가 NaN/Infinity를 포함하지 않는지 검사합니다.</summary>
        private static bool IsFinite(in Vector2 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y);
    }
}
