using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jeomseon.Unity.Attributes;
using UnityEngine.Serialization;

namespace Jeomseon.Unity.GridTileSystem
{
    [System.Serializable]
    public struct AxialCoordinates : System.IEquatable<AxialCoordinates>
    {
        [field: SerializeField, ReadOnly] public int Q { get; private set; }
        [field: SerializeField, ReadOnly] public int R { get; private set; }

        public static implicit operator Vector2(in AxialCoordinates axialCoordinates) => new(axialCoordinates.Q, axialCoordinates.R);
        public static implicit operator Vector2Int(in AxialCoordinates axialCoordinates) => new(axialCoordinates.Q, axialCoordinates.R);
        public static implicit operator AxialCoordinates(in Vector2 vec) => new(vec);
        public static implicit operator AxialCoordinates(in Vector2Int vec) => new(vec);
        public static implicit operator HexCoordinates(in AxialCoordinates axialCoordinates) => new(axialCoordinates);

        public AxialCoordinates(int q, int r)
        {
            Q = q;
            R = r;
        }

        public AxialCoordinates(in HexCoordinates hexCoordinates) : this(hexCoordinates.Q, hexCoordinates.R) { }
        public AxialCoordinates(in Vector2Int vec) : this(vec.x, vec.y) { }
        public AxialCoordinates(in Vector2 vec) : this((int)vec.x, (int)vec.y) { }

        public bool Equals(AxialCoordinates other) => Q == other.Q && R == other.R;

        public override bool Equals(object obj)
        {
            return obj is AxialCoordinates other && Equals(other);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(Q, R);
        }

        public override string ToString()
            => $"Q : {Q} R : {R}";
    }

    [System.Serializable]
    public struct HexCoordinates : System.IEquatable<HexCoordinates>
    {
        [field: SerializeField, ReadOnly] public int Q { get; private set; }
        [field: SerializeField, ReadOnly] public int R { get; private set; }
        [field: SerializeField, ReadOnly] public int S { get; private set; }

        public static implicit operator Vector2(in HexCoordinates hexCoordinates) => new(hexCoordinates.Q, hexCoordinates.R);
        public static implicit operator Vector2Int(in HexCoordinates hexCoordinates) => new(hexCoordinates.Q, hexCoordinates.R);
        public static implicit operator Vector3(in HexCoordinates hexCoordinates) => new(hexCoordinates.Q, hexCoordinates.R, hexCoordinates.S);
        public static implicit operator Vector3Int(in HexCoordinates hexCoordinates) => new(hexCoordinates.Q, hexCoordinates.R, hexCoordinates.S);
        public static implicit operator HexCoordinates(in Vector2 vec) => new(vec);
        public static implicit operator HexCoordinates(in Vector2Int vec) => new(vec);
        public static implicit operator AxialCoordinates(in HexCoordinates hexCoordinates) => new(hexCoordinates);

        public HexCoordinates(int q, int r)
        {
            Q = q;
            R = r;
            S = -q - r;
        }

        public HexCoordinates(in AxialCoordinates axialCoordinates) : this(axialCoordinates.Q, axialCoordinates.R) { }
        public HexCoordinates(in Vector2Int vec) : this(vec.x, vec.y) { }
        public HexCoordinates(in Vector2 vec) : this((int)vec.x, (int)vec.y) { }

        /// <summary>
        /// Rounds a fractional axial (Q, R) position to the nearest hex, using the cube-coordinate
        /// rounding algorithm: round Q/R/S independently, then recompute whichever of the three had
        /// the largest rounding error from the other two so the Q + R + S == 0 invariant holds.
        /// </summary>
        public static HexCoordinates Round(in Vector2 fractionalAxial)
        {
            float q = fractionalAxial.x;
            float r = fractionalAxial.y;
            float s = -q - r;

            int roundedQ = Mathf.RoundToInt(q);
            int roundedR = Mathf.RoundToInt(r);
            int roundedS = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(roundedQ - q);
            float rDiff = Mathf.Abs(roundedR - r);
            float sDiff = Mathf.Abs(roundedS - s);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                roundedQ = -roundedR - roundedS;
            }
            else if (rDiff > sDiff)
            {
                roundedR = -roundedQ - roundedS;
            }

            return new HexCoordinates(roundedQ, roundedR);
        }

        public bool Equals(HexCoordinates other) => Q == other.Q && R == other.R && S == other.S;

        public override bool Equals(object obj)
        {
            return obj is HexCoordinates other && Equals(other);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(Q, R, S);
        }

        public override string ToString()
            => $"Q : {Q} R : {R} S : {S}";
    }
}
