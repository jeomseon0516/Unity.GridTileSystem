namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>공유 Edge 강체 펼침으로 현재 호환 Patch를 만드는 Parameterizer입니다.</summary>
    public sealed class TriangleUnfoldingSurfaceParameterizer : ISurfaceParameterizer
    {
        /// <inheritdoc />
        public SurfacePatchSet Parameterize(
            ISurfaceProvider surfaces,
            in SurfacePoint seed,
            in SurfacePatchBuildSettings settings,
            ISurfaceConnectivity connectivity)
        {
            SurfacePatch patch = TriangleUnfoldingParameterizer.Build(surfaces, seed, settings, connectivity);
            if (!settings.SplitWhenLimitReached || !patch.WasTruncated)
                return new SurfacePatchSet(seed, new[] { patch });

            System.Collections.Generic.List<SurfacePatch> patches = new() { patch };
            System.Collections.Generic.HashSet<(SurfaceHandle Surface, int TriangleIndex)> assigned = new();
            AddAssigned(patch, assigned);
            System.Collections.Generic.Queue<SurfacePatchTriangle> pending = new();

            // 첫 Patch를 같은 제외 집합으로 다시 만들지 않고 경계만 얻기 위해 한 번 분할 빌드합니다.
            // 결과는 결정론적으로 동일하며 이후 Patch는 assigned Face를 건너뛰므로 중복 소유가 없습니다.
            SurfacePatch primary = TriangleUnfoldingParameterizer.BuildPartition(
                surfaces, patch.Triangles[0], seed.Barycentric, settings, connectivity, null, out var frontier);
            patches[0] = primary;
            assigned.Clear();
            AddAssigned(primary, assigned);
            EnqueueUnassigned(frontier, assigned, pending);

            while (pending.Count > 0)
            {
                SurfacePatchTriangle patchSeed = pending.Dequeue();
                var key = (patchSeed.Surface, patchSeed.TriangleIndex);
                if (assigned.Contains(key)) continue;

                SurfacePatch next = TriangleUnfoldingParameterizer.BuildPartition(
                    surfaces,
                    patchSeed,
                    new UnityEngine.Vector3(1f / 3f, 1f / 3f, 1f / 3f),
                    settings,
                    connectivity,
                    assigned,
                    out frontier);
                patches.Add(next);
                AddAssigned(next, assigned);
                EnqueueUnassigned(frontier, assigned, pending);
            }

            return new SurfacePatchSet(seed, patches.ToArray());
        }

        private static void AddAssigned(
            SurfacePatch patch,
            System.Collections.Generic.ISet<(SurfaceHandle Surface, int TriangleIndex)> assigned)
        {
            foreach (SurfacePatchTriangle triangle in patch.Triangles)
                assigned.Add((triangle.Surface, triangle.TriangleIndex));
        }

        private static void EnqueueUnassigned(
            System.Collections.Generic.IEnumerable<SurfacePatchTriangle> frontier,
            System.Collections.Generic.ISet<(SurfaceHandle Surface, int TriangleIndex)> assigned,
            System.Collections.Generic.Queue<SurfacePatchTriangle> pending)
        {
            foreach (SurfacePatchTriangle point in frontier)
            {
                if (!assigned.Contains((point.Surface, point.TriangleIndex))) pending.Enqueue(point);
            }
        }
    }
}
