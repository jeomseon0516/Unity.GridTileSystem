using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Jeomseon.Unity.GridTileSystem.Surface.Rendering
{
    /// <summary>Burst 커널에 전달할 원본 Triangle index와 barycentric 가중치입니다.</summary>
    public readonly struct SurfaceBindingJobData
    {
        /// <summary>원본 Triangle 배열 index를 가져옵니다.</summary>
        public int TriangleIndex { get; }
        /// <summary>A/B/C vertex 가중치를 가져옵니다.</summary>
        public float3 Barycentric { get; }

        /// <summary>Triangle index와 barycentric 가중치를 생성합니다.</summary>
        public SurfaceBindingJobData(int triangleIndex, in float3 barycentric)
        {
            TriangleIndex = triangleIndex;
            Barycentric = barycentric;
        }
    }

    /// <summary>
    /// 준비된 topology 배열에서 Surface binding 위치·법선을 병렬 평가합니다. Unity Object와 virtual
    /// topology 조회는 Job 밖에서 끝내므로 worker thread는 blittable snapshot만 읽습니다.
    /// </summary>
    [BurstCompile]
    public struct SurfaceBindingEvaluationJob : IJobParallelFor
    {
        /// <summary>Surface local vertex 위치 입력입니다.</summary>
        [ReadOnly] public NativeArray<float3> Positions;
        /// <summary>원본 Triangle vertex index 입력입니다.</summary>
        [ReadOnly] public NativeArray<int3> Triangles;
        /// <summary>출력 vertex별 binding 입력입니다.</summary>
        [ReadOnly] public NativeArray<SurfaceBindingJobData> Bindings;
        /// <summary>평가된 Surface local 위치 출력입니다.</summary>
        [WriteOnly] public NativeArray<float3> OutputPositions;
        /// <summary>평가된 Surface local Face 법선 출력입니다.</summary>
        [WriteOnly] public NativeArray<float3> OutputNormals;

        /// <inheritdoc />
        public void Execute(int index)
        {
            SurfaceBindingJobData binding = Bindings[index];
            int3 triangle = Triangles[binding.TriangleIndex];
            float3 a = Positions[triangle.x];
            float3 b = Positions[triangle.y];
            float3 c = Positions[triangle.z];
            OutputPositions[index] = a * binding.Barycentric.x +
                                     b * binding.Barycentric.y +
                                     c * binding.Barycentric.z;
            OutputNormals[index] = math.normalizesafe(math.cross(b - a, c - a));
        }

        /// <summary>배열 길이를 검증하고 병렬 평가를 예약합니다. 배열 수명은 완료까지 호출자가 유지합니다.</summary>
        public JobHandle Schedule(int innerloopBatchCount, JobHandle dependency = default)
        {
            if (Bindings.Length != OutputPositions.Length || Bindings.Length != OutputNormals.Length)
                throw new System.ArgumentException("Binding and output array lengths must match.");
            if (innerloopBatchCount <= 0) throw new System.ArgumentOutOfRangeException(nameof(innerloopBatchCount));
            return IJobParallelForExtensions.Schedule(this, Bindings.Length, innerloopBatchCount, dependency);
        }
    }
}
