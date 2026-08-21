using System;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>
    /// Surface, 원본 Triangle, barycentric 좌표로 점을 intrinsic하게 식별합니다.
    /// 월드 위치와 달리 겹친 Surface layer를 구분하며 강체 Transform 이후에도 identity를 유지합니다.
    /// </summary>
    public readonly struct SurfacePoint : IEquatable<SurfacePoint>
    {
        /// <summary>barycentric 합과 Triangle 경계 검사에 사용하는 수치 허용 오차입니다.</summary>
        private const float BarycentricTolerance = 0.0001f;

        /// <summary>이 점을 포함하는 논리적 Surface를 가져옵니다.</summary>
        public SurfaceHandle Surface { get; }
        /// <summary>이 점을 포함하는 원본 Triangle index를 가져옵니다.</summary>
        public int TriangleIndex { get; }
        /// <summary>
        /// Triangle vertex (A,B,C)의 가중치 (u,v,w)를 가져옵니다. u+v+w=1이고 각 값은 0 이상입니다.
        /// </summary>
        public Vector3 Barycentric { get; }

        /// <summary>Surface, Triangle index와 barycentric 불변식이 국소적으로 유효한지 가져옵니다.</summary>
        public bool IsValid => Surface.IsValid && TriangleIndex >= 0 &&
                               Barycentric.x >= -BarycentricTolerance &&
                               Barycentric.y >= -BarycentricTolerance &&
                               Barycentric.z >= -BarycentricTolerance &&
                               Mathf.Abs(Barycentric.x + Barycentric.y + Barycentric.z - 1f) <=
                               BarycentricTolerance;

        /// <summary>월드 공간으로 평가하지 않고 intrinsic point를 생성합니다.</summary>
        public SurfacePoint(SurfaceHandle surface, int triangleIndex, in Vector3 barycentric)
        {
            Surface = surface;
            TriangleIndex = triangleIndex;
            Barycentric = barycentric;
        }

        /// <inheritdoc />
        public bool Equals(SurfacePoint other) =>
            Surface.Equals(other.Surface) && TriangleIndex == other.TriangleIndex &&
            Barycentric.Equals(other.Barycentric);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is SurfacePoint other && Equals(other);
        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Surface, TriangleIndex, Barycentric);
    }
}
