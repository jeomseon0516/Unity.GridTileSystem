using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using Jeomseon.Unity.GridTileSystem;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class HexGridControllerConfigurationTests
    {
        /// <summary>
        /// seed barycentric (0.2,0.4,0.4)을 <see cref="CreateLargePlaneMesh"/>의 Triangle 0
        /// (-10,-10) · (10,-10) · (-10,10)에 적용한 local 위치의 X 성분입니다.
        /// </summary>
        private const float SeedLocalX = -2f;
        /// <summary>같은 seed local 위치의 Y 성분입니다.</summary>
        private const float SeedLocalY = -2f;

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
            settings.TileRadius = 4f;

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
            HexGridReceiver receiver = new(sourceFilter, collider, outputFilter, outputRenderer);
            SetReceiverField(receiver, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));
            SetReceivers(controller, receiver);

            controller.BakeTiles();

            Assert.That(controller.TileCount, Is.GreaterThan(0));
            Assert.That(controller.TileCount, Is.EqualTo(receiver.SurfaceGrid.Tiles.Count));
            Assert.That(receiver.SurfaceGrid, Is.Not.Null);
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
            settings.TileRadius = 4f;
            GameObject sourceObject = new("Logical Surface");
            MeshFilter sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = surfaceMesh;
            MeshCollider collider = sourceObject.AddComponent<MeshCollider>();
            collider.sharedMesh = surfaceMesh;
            GameObject controllerObject = new("Logical Grid Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            HexGridReceiver receiver = new(sourceFilter, collider);
            SetReceiverField(receiver, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));
            SetReceivers(controller, receiver);

            Assert.DoesNotThrow(controller.BakeTiles);
            Assert.That(controller.TileCount, Is.GreaterThan(0));
            Assert.That(controller.TileCount, Is.EqualTo(receiver.SurfaceGrid.Tiles.Count));
            Assert.That(receiver.SurfaceGrid, Is.Not.Null);
            Assert.DoesNotThrow(() => controller.SetTileActive(0, 0, 0, false));
            Assert.That(receiver.Tiles[0].Data, Is.Not.Null);

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
            settings.TileRadius = 4f;
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
            HexGridReceiver receiver = new(sourceFilter, collider, outputFilter, outputRenderer);
            SetReceiverField(receiver, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));
            SetReceivers(controller, receiver);
            controller.BakeTiles();

            Assert.That(controller.TileCount, Is.GreaterThan(0));
            Assert.That(controller.TileCount, Is.EqualTo(receiver.SurfaceGrid.Tiles.Count));
            Assert.That(outputFilter.sharedMesh, Is.Not.Null);
            Assert.DoesNotThrow(controller.ClearTiles);
            Assert.That(controller.TileCount, Is.Zero);
            Assert.That(receiver.SurfaceGrid, Is.Null);
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
            settings.TileRadius = 4f;
            settings.InteractionLayerMask = 1 << 8;
            GameObject sourceObject = new("Pointer Surface") { layer = 8 };
            MeshFilter sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = surfaceMesh;
            MeshCollider collider = sourceObject.AddComponent<MeshCollider>();
            collider.sharedMesh = surfaceMesh;
            GameObject cameraObject = new("Pointer Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            // CreateLargePlaneMesh의 winding 법선은 +z이고 Physics.queriesHitBackfaces 기본값은
            // false이므로, Collider.Raycast가 성립하려면 카메라가 앞면인 +z쪽에서 진입해야 한다.
            // 또한 Grid는 seed barycentric (0.2,0.4,0.4)이 가리키는 (-2,-2,0) 주변에만 생성되므로
            // 화면 중앙 ray가 원점이 아니라 seed 위를 지나야 Grid 원점 Tile에 도달한다.
            camera.transform.position = new Vector3(SeedLocalX, SeedLocalY, 10f);
            camera.transform.forward = Vector3.back;
            RenderTexture target = new(64, 64, 0);
            camera.targetTexture = target;
            GameObject controllerObject = new("Pointer Grid Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            HexGridReceiver receiver = new(sourceFilter, collider);
            SetReceiverField(receiver, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));
            SetReceivers(controller, receiver);
            SetField(controller, "mainCamera", camera);
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

        [Test]
        public void BakeTiles_WithTwoReceivers_BuildsIndependentGridsAndPicksNearestSurface()
        {
            Mesh firstMesh = CreateLargePlaneMesh();
            Mesh secondMesh = CreateLargePlaneMesh();
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = 4f;
            settings.InteractionLayerMask = 1 << 8;

            GameObject firstObject = new("First Surface") { layer = 8 };
            MeshFilter firstFilter = firstObject.AddComponent<MeshFilter>();
            firstFilter.sharedMesh = firstMesh;
            MeshCollider firstCollider = firstObject.AddComponent<MeshCollider>();
            firstCollider.sharedMesh = firstMesh;

            GameObject secondObject = new("Second Surface") { layer = 8 };
            secondObject.transform.position = new Vector3(0f, 0f, -5f);
            MeshFilter secondFilter = secondObject.AddComponent<MeshFilter>();
            secondFilter.sharedMesh = secondMesh;
            MeshCollider secondCollider = secondObject.AddComponent<MeshCollider>();
            secondCollider.sharedMesh = secondMesh;

            HexGridReceiver firstReceiver = new(firstFilter, firstCollider);
            HexGridReceiver secondReceiver = new(secondFilter, secondCollider);
            SetReceiverField(firstReceiver, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));
            SetReceiverField(secondReceiver, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));

            GameObject controllerObject = new("Multi Receiver Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetReceivers(controller, firstReceiver, secondReceiver);
            controller.BakeTiles();
            Physics.SyncTransforms();

            Assert.That(firstReceiver.SurfaceGrid, Is.Not.Null);
            Assert.That(secondReceiver.SurfaceGrid, Is.Not.Null);
            Assert.That(controller.TileCount, Is.EqualTo(firstReceiver.TileCount + secondReceiver.TileCount));
            Ray ray = new(new Vector3(SeedLocalX, SeedLocalY, 10f), Vector3.back);
            Assert.That(controller.TryPickTile(ray, out RaycastHit hit, out _), Is.True);
            Assert.That(hit.collider, Is.SameAs(firstCollider));

            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(secondObject);
            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(secondMesh);
            Object.DestroyImmediate(firstMesh);
        }

        [Test]
        public void BakeTiles_WhenOneReceiverIsInvalid_KeepsOtherReceiverAlive()
        {
            Mesh mesh = CreateLargePlaneMesh();
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = 4f;
            GameObject surfaceObject = new("Valid Surface");
            MeshFilter filter = surfaceObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshCollider collider = surfaceObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            HexGridReceiver validReceiver = new(filter, collider);
            HexGridReceiver invalidReceiver = new();
            SetReceiverField(validReceiver, "seedBarycentric", new Vector3(0.2f, 0.4f, 0.4f));
            GameObject controllerObject = new("Partial Failure Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetReceivers(controller, invalidReceiver, validReceiver);

            LogAssert.Expect(LogType.Error, "Receiver 0 requires a source MeshFilter with a Mesh.");
            controller.BakeTiles();

            Assert.That(invalidReceiver.TileCount, Is.Zero);
            Assert.That(validReceiver.TileCount, Is.GreaterThan(0));
            Assert.That(validReceiver.SurfaceGrid, Is.Not.Null);

            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(surfaceObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void BakeTiles_WithTerrainReceiver_BuildsAndPicksVirtualTopologyGrid()
        {
            TerrainData terrainData = new() { heightmapResolution = 33, size = new Vector3(32f, 4f, 32f) };
            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Terrain Surface";
            terrainObject.layer = 8;
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            TerrainCollider terrainCollider = terrainObject.GetComponent<TerrainCollider>();
            HexGridReceiver receiver = new(terrain, terrainCollider);
            int seedTriangle = (16 * 32 + 16) * 2;
            SetReceiverField(receiver, "seedTriangleIndex", seedTriangle);

            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = 2f;
            settings.InteractionLayerMask = 1 << 8;
            GameObject controllerObject = new("Terrain Grid Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetReceivers(controller, receiver);

            controller.BakeTiles();
            Physics.SyncTransforms();

            Assert.That(receiver.SurfaceGrid, Is.Not.Null);
            Assert.That(receiver.TileCount, Is.GreaterThan(0));
            Ray ray = new(new Vector3(16.333333f, 10f, 16.333333f), Vector3.down);
            Assert.That(controller.TryPickTile(ray, out RaycastHit hit, out _), Is.True);
            Assert.That(hit.collider, Is.SameAs(terrainCollider));

            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(terrainObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(terrainData);
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

        private static void SetReceivers(HexGridController controller, params HexGridReceiver[] receivers) =>
            SetField(controller, "receivers", new System.Collections.Generic.List<HexGridReceiver>(receivers));

        private static void SetReceiverField<T>(HexGridReceiver receiver, string fieldName, T value)
        {
            FieldInfo field = typeof(HexGridReceiver).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing receiver field: {fieldName}");
            field.SetValue(receiver, value);
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
