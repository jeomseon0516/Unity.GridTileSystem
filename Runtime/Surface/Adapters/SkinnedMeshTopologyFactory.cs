using System;
using System.Collections.Generic;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using Unity.Collections;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem.Surface.Adapters
{
    /// <summary>
    /// <see cref="SkinnedMeshRenderer"/>의 bind pose Mesh를 Surface topology로 변환하고, 변형 추종에
    /// 필요한 bone 가중치와 프레임별 skinning 행렬을 제공합니다.
    /// </summary>
    /// <remarks>
    /// topology와 bone 가중치는 애니메이션과 무관하게 불변이므로 Bake 시점에 한 번만 읽습니다.
    /// 매 프레임 바뀌는 값은 bone Transform에서 유도하는 skinning 행렬뿐이며, 전체 Mesh를 다시
    /// Bake하지 않습니다.
    /// </remarks>
    public static class SkinnedMeshTopologyFactory
    {
        /// <summary>지정한 Surface identity로 bind pose topology snapshot을 구축합니다.</summary>
        public static SurfaceTopology BuildTopology(Mesh sharedMesh, SurfaceHandle handle)
        {
            if (sharedMesh == null) throw new ArgumentNullException(nameof(sharedMesh));
            if (!sharedMesh.isReadable)
            {
                throw new InvalidOperationException(
                    $"Mesh '{sharedMesh.name}' must have Read/Write Enabled to build runtime surface topology.");
            }

            return SurfaceTopologyBuilder.Build(handle, sharedMesh.vertices, sharedMesh.triangles);
        }

        /// <summary>bind pose Mesh asset identity에서 handle을 유도합니다.</summary>
        public static SurfaceTopology BuildTopology(Mesh sharedMesh)
        {
            if (sharedMesh == null) throw new ArgumentNullException(nameof(sharedMesh));
            ulong handleValue = EntityId.ToULong(sharedMesh.GetEntityId());
            if (handleValue == 0UL) handleValue = 1UL;
            return BuildTopology(sharedMesh, new SurfaceHandle(handleValue));
        }

        /// <summary>
        /// 원본 vertex별 bone influence를 읽습니다. 정점당 4개로 제한된 구형 API 대신
        /// <see cref="Mesh.GetAllBoneWeights"/>를 사용해 가변 개수 influence를 그대로 보존합니다.
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<SurfaceBoneInfluence>> ReadInfluences(Mesh sharedMesh)
        {
            if (sharedMesh == null) throw new ArgumentNullException(nameof(sharedMesh));
            NativeArray<byte> bonesPerVertex = sharedMesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> weights = sharedMesh.GetAllBoneWeights();
            if (!bonesPerVertex.IsCreated || bonesPerVertex.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Mesh '{sharedMesh.name}' has no bone weights and cannot follow skinned deformation.");
            }

            var result = new List<IReadOnlyList<SurfaceBoneInfluence>>(bonesPerVertex.Length);
            int cursor = 0;
            for (int vertex = 0; vertex < bonesPerVertex.Length; vertex++)
            {
                int count = bonesPerVertex[vertex];
                var influences = new SurfaceBoneInfluence[count];
                for (int i = 0; i < count; i++)
                {
                    BoneWeight1 weight = weights[cursor + i];
                    influences[i] = new SurfaceBoneInfluence(weight.boneIndex, weight.weight);
                }
                cursor += count;
                result.Add(Array.AsReadOnly(influences));
            }

            return result.AsReadOnly();
        }

        /// <summary>현재 프레임의 bone Transform으로 skinning 행렬을 채웁니다.</summary>
        /// <param name="renderer">bone Transform 목록을 제공하는 Renderer입니다.</param>
        /// <param name="targetFromWorld">world 공간 결과를 대상 local 공간으로 옮기는 행렬입니다.</param>
        /// <param name="destination">bone 수와 길이가 같은 대상 버퍼입니다.</param>
        public static void GetSkinningMatrices(
            SkinnedMeshRenderer renderer,
            in Matrix4x4 targetFromWorld,
            Matrix4x4[] destination)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            Mesh sharedMesh = renderer.sharedMesh;
            if (sharedMesh == null) throw new InvalidOperationException("SkinnedMeshRenderer has no shared Mesh.");

            Transform[] bones = renderer.bones;
            Matrix4x4[] bindPoses = sharedMesh.bindposes;
            if (bones.Length != bindPoses.Length)
            {
                throw new InvalidOperationException(
                    $"Bone count {bones.Length} does not match bind pose count {bindPoses.Length}.");
            }
            if (destination.Length != bones.Length)
                throw new ArgumentException($"Destination must have exactly {bones.Length} elements.", nameof(destination));

            for (int bone = 0; bone < bones.Length; bone++)
            {
                Transform boneTransform = bones[bone];
                if (boneTransform == null)
                {
                    // 누락된 bone은 변형에 기여하지 않도록 bind pose 자세를 유지합니다.
                    destination[bone] = targetFromWorld * renderer.transform.localToWorldMatrix;
                    continue;
                }

                // bindpose는 bind 시점의 mesh-space → bone-space 변환입니다. 여기에 현재 bone의
                // bone-space → world 변환을 곱하면 bind pose 정점을 현재 자세로 옮기는 행렬이 됩니다.
                destination[bone] = targetFromWorld * boneTransform.localToWorldMatrix * bindPoses[bone];
            }
        }

        /// <summary>Renderer가 참조하는 bone 개수를 가져옵니다.</summary>
        public static int GetBoneCount(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            return renderer.bones.Length;
        }
    }
}
