using System;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Grid
{
    /// <summary>local Surface Patch에서 실제 길이 단위로 flat-top Hex의 중심과 꼭짓점을 계산합니다.</summary>
    public readonly struct IntrinsicHexLayout
    {
        /// <summary>sqrt(3)의 단정도 상수입니다.</summary>
        private const float SquareRootThree = 1.7320508075688772f;

        /// <summary>Grid 원점 Hex의 intrinsic 중심을 가져옵니다.</summary>
        public Vector2 Origin { get; }
        /// <summary>Hex 중심에서 꼭짓점까지의 intrinsic 길이를 가져옵니다.</summary>
        public float Radius { get; }

        /// <summary>intrinsic 원점과 양수 반지름으로 flat-top Hex layout을 생성합니다.</summary>
        public IntrinsicHexLayout(in Vector2 origin, float radius)
        {
            if (!IsFinite(origin))
                throw new ArgumentException("Origin must contain only finite values.", nameof(origin));
            if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
                throw new ArgumentOutOfRangeException(nameof(radius));
            Origin = origin;
            Radius = radius;
        }

        /// <summary>Axial 좌표를 flat-top Hex 중심의 intrinsic 2D 위치로 변환합니다.</summary>
        public Vector2 GetCenter(in AxialCoordinates coordinates)
        {
            // flat-top axial basis는 q축 (3R/2, sqrt(3)R/2), r축 (0, sqrt(3)R)입니다.
            // 두 basis의 정수 선형 결합이므로 인접 Hex 중심 간격과 격자 정합성이 유지됩니다.
            float x = Radius * 1.5f * coordinates.Q;
            float y = Radius * SquareRootThree * (coordinates.R + coordinates.Q * 0.5f);
            return Origin + new Vector2(x, y);
        }

        /// <summary>Axial 좌표 Hex의 반시계 방향 꼭짓점 여섯 개를 반환합니다.</summary>
        public Vector2[] GetCorners(in AxialCoordinates coordinates)
        {
            Vector2 center = GetCenter(coordinates);
            Vector2[] corners = new Vector2[6];
            for (int corner = 0; corner < corners.Length; corner++)
            {
                // flat-top Hex는 첫 꼭짓점이 +X 방향이며 이후 60°씩 반시계 회전합니다.
                float angle = Mathf.Deg2Rad * (corner * 60f);
                corners[corner] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Radius;
            }
            return corners;
        }

        /// <summary>intrinsic 위치를 가장 가까운 Axial Hex 좌표로 반올림합니다.</summary>
        public HexCoordinates GetCoordinates(in Vector2 intrinsicPosition)
        {
            if (!IsFinite(intrinsicPosition))
                throw new ArgumentException("Intrinsic position must contain only finite values.", nameof(intrinsicPosition));
            Vector2 local = (intrinsicPosition - Origin) / Radius;
            // 위의 flat-top basis 행렬을 역변환한 식입니다.
            // q = 2x/3, r = -x/3 + y/sqrt(3)를 얻은 뒤 cube invariant를 보존하며 반올림합니다.
            Vector2 fractionalAxial = new(
                2f * local.x / 3f,
                -local.x / 3f + local.y / SquareRootThree);
            return HexCoordinates.Round(fractionalAxial);
        }

        /// <summary>좌표가 NaN/Infinity를 포함하지 않는지 검사합니다.</summary>
        private static bool IsFinite(in Vector2 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y);
    }
}
