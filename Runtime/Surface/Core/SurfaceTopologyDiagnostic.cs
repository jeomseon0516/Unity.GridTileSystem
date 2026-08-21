namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>intrinsic traversal을 모호하거나 수치적으로 불안정하게 만드는 topology 결함을 분류합니다.</summary>
    public enum SurfaceTopologyDiagnosticKind
    {
        /// <summary>Triangle이 index를 반복하거나 수치적으로 면적 0입니다.</summary>
        DegenerateTriangle,
        /// <summary>인접 Triangle 두 개가 공유 Edge를 같은 방향으로 순회합니다.</summary>
        InconsistentWinding,
        /// <summary>두 개를 초과한 Triangle이 하나의 무방향 Edge를 공유합니다.</summary>
        NonManifoldEdge
    }

    /// <summary>Unity Object 참조를 보존하지 않고 topology 결함 하나를 설명합니다.</summary>
    public readonly struct SurfaceTopologyDiagnostic
    {
        /// <summary>탐지한 결함의 종류를 가져옵니다.</summary>
        public SurfaceTopologyDiagnosticKind Kind { get; }
        /// <summary>결함이 탐지된 Triangle index를 가져옵니다.</summary>
        public int TriangleIndex { get; }
        /// <summary>결함에 관련된 다른 Triangle index 또는 -1을 가져옵니다.</summary>
        public int RelatedTriangleIndex { get; }
        /// <summary>관련 Edge의 작은 원본 vertex index 또는 -1을 가져옵니다.</summary>
        public int VertexA { get; }
        /// <summary>관련 Edge의 큰 원본 vertex index 또는 -1을 가져옵니다.</summary>
        public int VertexB { get; }

        /// <summary>Triangle과 선택적 관련 Edge/Triangle에 대한 진단 레코드를 생성합니다.</summary>
        public SurfaceTopologyDiagnostic(
            SurfaceTopologyDiagnosticKind kind,
            int triangleIndex,
            int relatedTriangleIndex = -1,
            int vertexA = -1,
            int vertexB = -1)
        {
            Kind = kind;
            TriangleIndex = triangleIndex;
            RelatedTriangleIndex = relatedTriangleIndex;
            VertexA = vertexA;
            VertexB = vertexB;
        }
    }
}
