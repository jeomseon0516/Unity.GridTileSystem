using System;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>월드 공간 위치와 무관하게 하나의 논리적 Surface를 식별합니다.</summary>
    public readonly struct SurfaceHandle : IEquatable<SurfaceHandle>
    {
        /// <summary>어떤 Surface도 식별하지 않는 sentinel handle을 가져옵니다.</summary>
        public static SurfaceHandle Invalid => default;

        /// <summary>Adapter가 부여한 식별자를 가져옵니다. 0은 <see cref="Invalid"/>용으로 예약됩니다.</summary>
        public ulong Value { get; }
        /// <summary>이 handle이 유효한 Surface를 식별하는지 가져옵니다.</summary>
        public bool IsValid => Value != 0UL;

        /// <summary>Adapter가 부여한 0이 아닌 식별자로 Surface handle을 생성합니다.</summary>
        public SurfaceHandle(int value)
        {
            if (value == 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = unchecked((ulong)(long)value);
        }

        /// <summary>Adapter가 부여한 64-bit 식별자로 Surface handle을 생성합니다.</summary>
        public SurfaceHandle(ulong value)
        {
            if (value == 0UL) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        /// <inheritdoc />
        public bool Equals(SurfaceHandle other) => Value == other.Value;
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is SurfaceHandle other && Equals(other);
        /// <inheritdoc />
        public override int GetHashCode() => Value.GetHashCode();
        /// <inheritdoc />
        public override string ToString() => IsValid ? $"Surface({Value})" : "Surface(Invalid)";
        /// <summary>부여된 식별자로 두 handle이 같은지 비교합니다.</summary>
        public static bool operator ==(SurfaceHandle left, SurfaceHandle right) => left.Equals(right);
        /// <summary>부여된 식별자로 두 handle이 다른지 비교합니다.</summary>
        public static bool operator !=(SurfaceHandle left, SurfaceHandle right) => !left.Equals(right);
    }
}
