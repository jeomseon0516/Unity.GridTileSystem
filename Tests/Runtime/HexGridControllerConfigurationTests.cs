using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Jeomseon.Unity.GridTileSystem.Surface.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class HexGridControllerConfigurationTests
    {
        /// <summary>Grid를 시작할 월드 위치의 X 성분입니다. 평면 중앙에서 조금 벗어난 지점입니다.</summary>
        private const float SeedWorldX = -2f;
        /// <summary>같은 seed 위치의 Y 성분입니다.</summary>
        private const float SeedWorldY = -2f;

        [Test]
        public void BakeTiles_WithoutSettings_ReportsConfigurationErrorInsteadOfThrowing()
        {
            GameObject gameObject = new(nameof(HexGridControllerConfigurationTests));
            HexGridController controller = gameObject.AddComponent<HexGridController>();

            LogAssert.Expect(LogType.Error, "HexGridController requires a HexGridSettings asset.");
            Assert.DoesNotThrow(controller.BakeTiles);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void BakeTiles_WithoutAnySurfaceNearTheSeed_ReportsDiagnosticInsteadOfFailingSilently()
        {
            HexGridSettings settings = CreateSettings(4f);
            GameObject controllerObject = new("Empty World Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetField(controller, "seedOffset", new Vector3(0f, 5000f, 0f));

            LogAssert.Expect(LogType.Error, new Regex("could not build a grid \\(SurfaceNotFound\\)"));
            Assert.DoesNotThrow(controller.BakeTiles);
            Assert.That(controller.TileCount, Is.Zero);

            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void BakeTiles_FromASeedPositionAlone_FindsTheSurfaceAndCreatesOutputMesh()
        {
            Mesh surfaceMesh = CreateLargePlaneMesh();
            GameObject sourceObject = CreatePlane(surfaceMesh, Vector3.zero);
            GameObject outputObject = new("Grid Output");
            MeshFilter outputFilter = outputObject.AddComponent<MeshFilter>();
            MeshRenderer outputRenderer = outputObject.AddComponent<MeshRenderer>();
            HexGridSettings settings = CreateSettings(4f);
            HexGridController controller = CreateController(settings, outputFilter, outputRenderer);

            Physics.SyncTransforms();
            controller.BakeTiles();

            // 사용자는 Surface를 등록하지도 Triangle 번호를 입력하지도 않았습니다.
            Assert.That(controller.SurfaceGrid, Is.Not.Null);
            Assert.That(controller.Seed.IsValid, Is.True);
            Assert.That(controller.TileCount, Is.GreaterThan(0));
            Assert.That(controller.TileCount, Is.EqualTo(controller.SurfaceGrid.Tiles.Count));
            Assert.That(outputFilter.sharedMesh, Is.Not.Null);
            Assert.That(outputFilter.sharedMesh.vertexCount, Is.GreaterThan(0));

            DestroyAll(controller.gameObject, outputObject, sourceObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(surfaceMesh);
        }

        [Test]
        public void BakeTiles_WithoutRenderingBackend_CreatesLogicalTilesAndSupportsStateChanges()
        {
            Mesh surfaceMesh = CreateLargePlaneMesh();
            GameObject sourceObject = CreatePlane(surfaceMesh, Vector3.zero);
            HexGridSettings settings = CreateSettings(4f);
            HexGridController controller = CreateController(settings, null, null);

            Physics.SyncTransforms();
            Assert.DoesNotThrow(controller.BakeTiles);

            Assert.That(controller.TileCount, Is.GreaterThan(0));
            Assert.DoesNotThrow(() => controller.SetTileActive(0, 0, false));
            Assert.That(controller.Tiles[0].Data, Is.Not.Null);

            DestroyAll(controller.gameObject, sourceObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(surfaceMesh);
        }

        [Test]
        public void ClearTiles_WithRenderingBackend_ClearsGeometryWithoutApplyingEmptyVisualsToOldMesh()
        {
            Mesh surfaceMesh = CreateLargePlaneMesh();
            GameObject sourceObject = CreatePlane(surfaceMesh, Vector3.zero);
            GameObject outputObject = new("Clear Output");
            MeshFilter outputFilter = outputObject.AddComponent<MeshFilter>();
            MeshRenderer outputRenderer = outputObject.AddComponent<MeshRenderer>();
            HexGridSettings settings = CreateSettings(4f);
            HexGridController controller = CreateController(settings, outputFilter, outputRenderer);

            Physics.SyncTransforms();
            controller.BakeTiles();

            Assert.That(controller.TileCount, Is.GreaterThan(0));
            Assert.That(outputFilter.sharedMesh, Is.Not.Null);
            Assert.DoesNotThrow(controller.ClearTiles);
            Assert.That(controller.TileCount, Is.Zero);
            Assert.That(controller.SurfaceGrid, Is.Null);
            Assert.That(outputFilter.sharedMesh, Is.Null);

            DestroyAll(controller.gameObject, outputObject, sourceObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(surfaceMesh);
        }

        [Test]
        public void ProcessPointer_WithCameraAndMeshCollider_EmitsEnterDownUpAndExitInOrder()
        {
            Mesh surfaceMesh = CreateLargePlaneMesh();
            GameObject sourceObject = CreatePlane(surfaceMesh, Vector3.zero);
            HexGridSettings settings = CreateSettings(4f);
            settings.InteractionLayerMask = 1 << 8;
            GameObject cameraObject = new("Pointer Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            // CreateLargePlaneMesh의 winding 법선은 +z이고 Physics.queriesHitBackfaces 기본값은 false이므로
            // 카메라는 앞면인 +z쪽에서 진입해야 하며, 화면 중앙 ray가 seed 위를 지나야 원점 Tile에 닿습니다.
            camera.transform.position = new Vector3(SeedWorldX, SeedWorldY, 10f);
            camera.transform.forward = Vector3.back;
            RenderTexture target = new(64, 64, 0);
            camera.targetTexture = target;
            HexGridController controller = CreateController(settings, null, null);
            SetField(controller, "mainCamera", camera);

            Physics.SyncTransforms();
            controller.BakeTiles();
            List<string> events = new();
            controller.OnEnterTile += _ => events.Add("enter");
            controller.OnMouseDownTile += _ => events.Add("down");
            controller.OnMouseUpTile += _ => events.Add("up");
            controller.OnExitTile += _ => events.Add("exit");

            InvokeProcessPointer(controller, new Vector2(32f, 32f), true, false);
            InvokeProcessPointer(controller, new Vector2(32f, 32f), false, true);
            InvokeProcessPointer(controller, new Vector2(-100f, -100f), false, false);

            Assert.That(events, Is.EqualTo(new[] { "enter", "down", "up", "exit" }));

            camera.targetTexture = null;
            DestroyAll(controller.gameObject, cameraObject, sourceObject);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(surfaceMesh);
        }

        [Test]
        public void BakeTiles_WithTouchingSurfaces_ExtendsOneGridAcrossBoth()
        {
            // 두 표면 중 어느 것도 Controller에 지정하지 않습니다. 맞닿아 있으면 하나의 Grid가 됩니다.
            Mesh firstMesh = CreateLargePlaneMesh();
            Mesh secondMesh = CreateLargePlaneMesh();
            GameObject firstObject = CreatePlane(firstMesh, Vector3.zero);
            GameObject secondObject = CreatePlane(secondMesh, new Vector3(20f, 0f, 0f));
            HexGridSettings settings = CreateSettings(4f);
            HexGridController controller = CreateController(settings, null, null);
            SetField(controller, "seedSearchRadius", 40f);
            SetField(controller, "maximumPatchRadius", 40f);

            Physics.SyncTransforms();
            controller.BakeTiles();

            HashSet<SurfaceHandle> surfaces = new();
            foreach (Surface.Grid.SurfaceGridTileRegion tile in controller.SurfaceGrid.Tiles)
            {
                foreach (SurfaceRegionVertex vertex in tile.Region.Vertices) surfaces.Add(vertex.SurfacePoint.Surface);
            }

            Assert.That(controller.SurfaceGrid.Patch.SpansMultipleSurfaces, Is.True);
            Assert.That(surfaces.Count, Is.EqualTo(2));

            DestroyAll(controller.gameObject, secondObject, firstObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(secondMesh);
            Object.DestroyImmediate(firstMesh);
        }

        [Test]
        public void BakeTiles_OverTerrain_BuildsAndPicksVirtualTopologyGrid()
        {
            TerrainData terrainData = new() { heightmapResolution = 33, size = new Vector3(32f, 4f, 32f) };
            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Terrain Surface";
            terrainObject.layer = 8;
            TerrainCollider terrainCollider = terrainObject.GetComponent<TerrainCollider>();

            HexGridSettings settings = CreateSettings(2f);
            settings.InteractionLayerMask = 1 << 8;
            GameObject controllerObject = new("Terrain Grid Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetField(controller, "seedOffset", new Vector3(16f, 0f, 16f));

            Physics.SyncTransforms();
            controller.BakeTiles();
            Physics.SyncTransforms();

            Assert.That(controller.SurfaceGrid, Is.Not.Null);
            Assert.That(controller.TileCount, Is.GreaterThan(0));
            Ray ray = new(new Vector3(16.333333f, 10f, 16.333333f), Vector3.down);
            Assert.That(controller.TryPickTile(ray, out RaycastHit hit, out _), Is.True);
            Assert.That(hit.collider, Is.SameAs(terrainCollider));

            DestroyAll(controllerObject, terrainObject);
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(terrainData);
        }

        private static HexGridSettings CreateSettings(float tileRadius)
        {
            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = tileRadius;
            settings.InteractionLayerMask = ~0;
            return settings;
        }

        /// <summary>Seed가 표면 위 (-2, -2)를 가리키는 Controller를 만듭니다.</summary>
        private static HexGridController CreateController(
            HexGridSettings settings,
            MeshFilter outputFilter,
            MeshRenderer outputRenderer)
        {
            GameObject controllerObject = new("Grid Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetField(controller, "seedOffset", new Vector3(SeedWorldX, SeedWorldY, 0f));
            if (outputFilter != null) SetField(controller, "outputMeshFilter", outputFilter);
            if (outputRenderer != null) SetField(controller, "outputMeshRenderer", outputRenderer);
            return controller;
        }

        private static GameObject CreatePlane(Mesh mesh, in Vector3 position)
        {
            GameObject surfaceObject = new("Surface Source") { layer = 8 };
            surfaceObject.transform.position = position;
            MeshFilter filter = surfaceObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshCollider collider = surfaceObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            return surfaceObject;
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

        private static void DestroyAll(params GameObject[] objects)
        {
            foreach (GameObject target in objects) Object.DestroyImmediate(target);
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
