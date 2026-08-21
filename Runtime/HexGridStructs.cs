using UnityEngine;
using Jeomseon.Unity.Attributes;

namespace Jeomseon.Unity.GridTileSystem
{
    [System.Serializable]
    /// <summary>육각 Grid의 두 독립 축(q, r)만 저장하는 Axial 정수 좌표입니다.</summary>
    public struct AxialCoordinates : System.IEquatable<AxialCoordinates>
    {
        /// <summary>Axial q축 정수 좌표를 가져옵니다.</summary>
        [field: SerializeField, ReadOnly] public int Q { get; private set; }
        /// <summary>Axial r축 정수 좌표를 가져옵니다.</summary>
        [field: SerializeField, ReadOnly] public int R { get; private set; }

        /// <summary>q와 r을 각각 x와 y로 옮긴 실수 벡터를 반환합니다.</summary>
        public static implicit operator Vector2(in AxialCoordinates axialCoordinates) => new(axialCoordinates.Q, axialCoordinates.R);
        /// <summary>q와 r을 각각 x와 y로 옮긴 정수 벡터를 반환합니다.</summary>
        public static implicit operator Vector2Int(in AxialCoordinates axialCoordinates) => new(axialCoordinates.Q, axialCoordinates.R);
        /// <summary>벡터 성분을 정수로 절삭하여 Axial 좌표로 변환합니다.</summary>
        public static implicit operator AxialCoordinates(in Vector2 vec) => new(vec);
        /// <summary>정수 벡터를 Axial 좌표로 변환합니다.</summary>
        public static implicit operator AxialCoordinates(in Vector2Int vec) => new(vec);
        /// <summary>암시적 세 번째 축 s=-q-r을 계산하여 Cube 좌표로 변환합니다.</summary>
        public static implicit operator HexCoordinates(in AxialCoordinates axialCoordinates) => new(axialCoordinates);

        /// <summary>q와 r 정수로 Axial 좌표를 생성합니다.</summary>
        public AxialCoordinates(int q, int r)
        {
            Q = q;
            R = r;
        }

        /// <summary>Cube 좌표에서 독립 축 q와 r만 복사합니다.</summary>
        public AxialCoordinates(in HexCoordinates hexCoordinates) : this(hexCoordinates.Q, hexCoordinates.R) { }
        /// <summary>정수 벡터의 x, y로 좌표를 생성합니다.</summary>
        public AxialCoordinates(in Vector2Int vec) : this(vec.x, vec.y) { }
        /// <summary>실수 벡터의 x, y를 0 방향으로 절삭하여 좌표를 생성합니다.</summary>
        public AxialCoordinates(in Vector2 vec) : this((int)vec.x, (int)vec.y) { }

        /// <summary>두 Axial 좌표의 q와 r이 모두 같은지 확인합니다.</summary>
        /// <inheritdoc />
        public bool Equals(AxialCoordinates other) => Q == other.Q && R == other.R;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is AxialCoordinates other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return System.HashCode.Combine(Q, R);
        }

        /// <inheritdoc />
        public override string ToString()
            => $"Q : {Q} R : {R}";
    }

    [System.Serializable]
    /// <summary>q+r+s=0 불변식을 유지하는 육각 Grid Cube 정수 좌표입니다.</summary>
    public struct HexCoordinates : System.IEquatable<HexCoordinates>
    {
        /// <summary>Cube q축 정수 좌표를 가져옵니다.</summary>
        [field: SerializeField, ReadOnly] public int Q { get; private set; }
        /// <summary>Cube r축 정수 좌표를 가져옵니다.</summary>
        [field: SerializeField, ReadOnly] public int R { get; private set; }
        /// <summary>Q+R+S=0을 만족하는 Cube s축 정수 좌표를 가져옵니다.</summary>
        [field: SerializeField, ReadOnly] public int S { get; private set; }

        /// <summary>독립 축 q와 r을 Vector2로 변환합니다.</summary>
        public static implicit operator Vector2(in HexCoordinates hexCoordinates) => new(hexCoordinates.Q, hexCoordinates.R);
        /// <summary>독립 축 q와 r을 Vector2Int로 변환합니다.</summary>
        public static implicit operator Vector2Int(in HexCoordinates hexCoordinates) => new(hexCoordinates.Q, hexCoordinates.R);
        /// <summary>q, r, s 세 축을 Vector3로 변환합니다.</summary>
        public static implicit operator Vector3(in HexCoordinates hexCoordinates) => new(hexCoordinates.Q, hexCoordinates.R, hexCoordinates.S);
        /// <summary>q, r, s 세 축을 Vector3Int로 변환합니다.</summary>
        public static implicit operator Vector3Int(in HexCoordinates hexCoordinates) => new(hexCoordinates.Q, hexCoordinates.R, hexCoordinates.S);
        /// <summary>벡터 성분을 정수로 절삭하고 s=-q-r을 계산합니다.</summary>
        public static implicit operator HexCoordinates(in Vector2 vec) => new(vec);
        /// <summary>정수 벡터의 x, y를 q, r로 사용하고 s=-q-r을 계산합니다.</summary>
        public static implicit operator HexCoordinates(in Vector2Int vec) => new(vec);
        /// <summary>독립 축 q와 r만 보존하여 Axial 좌표로 변환합니다.</summary>
        public static implicit operator AxialCoordinates(in HexCoordinates hexCoordinates) => new(hexCoordinates);

        /// <summary>q와 r로 Cube invariant를 만족하는 Hex 좌표를 생성합니다.</summary>
        public HexCoordinates(int q, int r)
        {
            Q = q;
            R = r;
            S = CheckedNegatedSum(q, r);
        }

        /// <summary>Axial 좌표에서 q와 r을 복사하고 s를 계산합니다.</summary>
        public HexCoordinates(in AxialCoordinates axialCoordinates) : this(axialCoordinates.Q, axialCoordinates.R) { }
        /// <summary>정수 벡터의 x, y에서 Cube 좌표를 생성합니다.</summary>
        public HexCoordinates(in Vector2Int vec) : this(vec.x, vec.y) { }
        /// <summary>실수 벡터의 x, y를 0 방향으로 절삭하여 Cube 좌표를 생성합니다.</summary>
        public HexCoordinates(in Vector2 vec) : this((int)vec.x, (int)vec.y) { }

        /// <summary>
        /// 소수 Axial (Q,R)을 가장 가까운 Hex로 반올림합니다. Q/R/S를 각각 반올림한 뒤 오차가 가장 큰
        /// 축을 나머지 두 축에서 다시 계산하여 Q+R+S=0 불변식을 유지합니다.
        /// </summary>
        public static HexCoordinates Round(in Vector2 fractionalAxial)
        {
            float q = fractionalAxial.x;
            float r = fractionalAxial.y;
            float s = -q - r;
            if (!IsFinite(q) || !IsFinite(r) || !IsFinite(s))
                throw new System.ArgumentException("Fractional axial coordinates must be finite.", nameof(fractionalAxial));

            int roundedQ = Mathf.RoundToInt(q);
            int roundedR = Mathf.RoundToInt(r);
            int roundedS = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(roundedQ - q);
            float rDiff = Mathf.Abs(roundedR - r);
            float sDiff = Mathf.Abs(roundedS - s);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                roundedQ = CheckedNegatedSum(roundedR, roundedS);
            }
            else if (rDiff > sDiff)
            {
                roundedR = CheckedNegatedSum(roundedQ, roundedS);
            }

            return new HexCoordinates(roundedQ, roundedR);
        }

        /// <summary>두 좌표의 합을 long에서 부정해 int 범위를 벗어나는 Cube 좌표를 거부합니다.</summary>
        private static int CheckedNegatedSum(int first, int second)
        {
            long result = -(long)first - second;
            if (result < int.MinValue || result > int.MaxValue)
                throw new System.OverflowException("Hex cube coordinate exceeds the Int32 range.");
            return (int)result;
        }

        /// <summary>단정도 값이 NaN 또는 Infinity가 아닌지 검사합니다.</summary>
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <inheritdoc />
        public bool Equals(HexCoordinates other) => Q == other.Q && R == other.R && S == other.S;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is HexCoordinates other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return System.HashCode.Combine(Q, R, S);
        }

        /// <inheritdoc />
        public override string ToString()
            => $"Q : {Q} R : {R} S : {S}";
    }
}
