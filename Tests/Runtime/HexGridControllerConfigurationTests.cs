using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using Jeomseon.Unity.GridTileSystem;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class HexGridControllerConfigurationTests
    {
        [Test]
        public void BakeTiles_WithoutSettings_ReportsConfigurationErrorInsteadOfThrowing()
        {
            GameObject gameObject = new(nameof(HexGridControllerConfigurationTests));
            HexGridController manager = gameObject.AddComponent<HexGridController>();

            LogAssert.Expect(LogType.Error, "HexGridController requires a HexGridSettings asset.");
            Assert.DoesNotThrow(manager.BakeTiles);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void BakeTiles_WithCompleteStaticMeshConfiguration_CreatesTilesAndOutputMesh()
        {
            Mesh surfaceMesh = CreateLargePlaneMesh();
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = 0.5f;
            settings.GridRadius = 1;

            GameObject sourceObject = new("Surface Source");
            MeshFilter sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = surfaceMesh;
            MeshCollider collider = sourceObject.AddComponent<MeshCollider>();
            collider.sharedMesh = surfaceMesh;

            GameObject outputObject = new("Grid Output");
            MeshFilter outputFilter = outputObject.AddComponent<MeshFilter>();
            MeshRenderer outputRenderer = outputObject.AddComponent<MeshRenderer>();

            GameObject controllerObject = new("Grid Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetField(controller, "sourceMeshFilter", sourceFilter);
            SetField(controller, "surfaceCollider", collider);
            SetField(controller, "outputMeshFilter", outputFilter);
            SetField(controller, "outputMeshRenderer", outputRenderer);
            SetField(controller, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));

            controller.BakeTiles();

            Assert.That(controller.TileCount, Is.EqualTo(7));
            Assert.That(controller.SurfaceGrid, Is.Not.Null);
            Assert.That(outputFilter.sharedMesh, Is.Not.Null);
            Assert.That(outputFilter.sharedMesh.vertexCount, Is.GreaterThan(0));

            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(outputObject);
            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(surfaceMesh);
        }

        [Test]
        public void BakeTiles_WithoutRenderingBackend_CreatesLogicalTilesAndSupportsStateChanges()
        {
            Mesh surfaceMesh = CreateLargePlaneMesh();
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = 0.5f;
            settings.GridRadius = 1;
            GameObject sourceObject = new("Logical Surface");
            MeshFilter sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = surfaceMesh;
            MeshCollider collider = sourceObject.AddComponent<MeshCollider>();
            collider.sharedMesh = surfaceMesh;
            GameObject controllerObject = new("Logical Grid Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetField(controller, "sourceMeshFilter", sourceFilter);
            SetField(controller, "surfaceCollider", collider);
            SetField(controller, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));

            Assert.DoesNotThrow(controller.BakeTiles);
            Assert.That(controller.TileCount, Is.EqualTo(7));
            Assert.That(controller.SurfaceGrid, Is.Not.Null);
            Assert.DoesNotThrow(() => controller.SetTileActive(0, 0, false));
            Assert.That(controller.Tiles[0].Data, Is.Not.Null);

            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(surfaceMesh);
        }

        [Test]
        public void ClearTiles_WithRenderingBackend_ClearsGeometryWithoutApplyingEmptyVisualsToOldMesh()
        {
            Mesh surfaceMesh = CreateLargePlaneMesh();
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = 0.5f;
            settings.GridRadius = 1;
            GameObject sourceObject = new("Clear Surface");
            MeshFilter sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = surfaceMesh;
            MeshCollider collider = sourceObject.AddComponent<MeshCollider>();
            collider.sharedMesh = surfaceMesh;
            GameObject outputObject = new("Clear Output");
            MeshFilter outputFilter = outputObject.AddComponent<MeshFilter>();
            MeshRenderer outputRenderer = outputObject.AddComponent<MeshRenderer>();
            GameObject controllerObject = new("Clear Grid Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetField(controller, "sourceMeshFilter", sourceFilter);
            SetField(controller, "surfaceCollider", collider);
            SetField(controller, "outputMeshFilter", outputFilter);
            SetField(controller, "outputMeshRenderer", outputRenderer);
            SetField(controller, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));
            controller.BakeTiles();

            Assert.That(controller.TileCount, Is.EqualTo(7));
            Assert.That(outputFilter.sharedMesh, Is.Not.Null);
            Assert.DoesNotThrow(controller.ClearTiles);
            Assert.That(controller.TileCount, Is.Zero);
            Assert.That(controller.SurfaceGrid, Is.Null);
            Assert.That(outputFilter.sharedMesh, Is.Null);

            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(outputObject);
            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(surfaceMesh);
        }

        [Test]
        public void ProcessPointer_WithCameraAndMeshCollider_EmitsEnterDownUpAndExitInOrder()
        {
            Mesh surfaceMesh = CreateLargePlaneMesh();
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = 0.5f;
            settings.GridRadius = 1;
            settings.InteractionLayerMask = 1 << 8;
            GameObject sourceObject = new("Pointer Surface") { layer = 8 };
            MeshFilter sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = surfaceMesh;
            MeshCollider collider = sourceObject.AddComponent<MeshCollider>();
            collider.sharedMesh = surfaceMesh;
            GameObject cameraObject = new("Pointer Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            RenderTexture target = new(64, 64, 0);
            camera.targetTexture = target;
            GameObject controllerObject = new("Pointer Grid Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetField(controller, "sourceMeshFilter", sourceFilter);
            SetField(controller, "surfaceCollider", collider);
            SetField(controller, "mainCamera", camera);
            SetField(controller, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));
            controller.BakeTiles();
            var events = new System.Collections.Generic.List<string>();
            controller.OnEnterTile += _ => events.Add("enter");
            controller.OnMouseDownTile += _ => events.Add("down");
            controller.OnMouseUpTile += _ => events.Add("up");
            controller.OnExitTile += _ => events.Add("exit");
            Physics.SyncTransforms();

            InvokeProcessPointer(controller, new Vector2(32f, 32f), true, false);
            InvokeProcessPointer(controller, new Vector2(32f, 32f), false, true);
            InvokeProcessPointer(controller, new Vector2(-100f, -100f), false, false);

            Assert.That(events, Is.EqualTo(new[] { "enter", "down", "up", "exit" }));

            camera.targetTexture = null;
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(surfaceMesh);
        }

        private static Mesh CreateLargePlaneMesh()
        {
            Mesh mesh = new() { name = "Controller Test Surface" };
            mesh.vertices = new[]
            {
                new Vector3(-10f, -10f, 0f), new Vector3(10f, -10f, 0f),
                new Vector3(-10f, 10f, 0f), new Vector3(10f, 10f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void SetField<T>(HexGridController controller, string fieldName, T value)
        {
            FieldInfo field = typeof(HexGridController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized field: {fieldName}");
            field.SetValue(controller, value);
        }

        /// <summary>실제 Update의 장치 polling을 제외한 pointer 처리 단계를 통합 테스트에서 호출합니다.</summary>
        private static void InvokeProcessPointer(
            HexGridController controller,
            Vector2 screenPosition,
            bool pressed,
            bool released)
        {
            MethodInfo method = typeof(HexGridController).GetMethod(
                "ProcessPointer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, new object[] { screenPosition, pressed, released });
        }
    }
}
