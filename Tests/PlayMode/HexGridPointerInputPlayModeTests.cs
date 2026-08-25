using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Jeomseon.Unity.GridTileSystem.Tests
{
    public sealed class HexGridPointerInputPlayModeTests
    {
        private Mouse[] _disabledNativeMice = System.Array.Empty<Mouse>();
        private Mouse _testMouse;

        [TearDown]
        public void RestoreMouseDevices()
        {
            if (_testMouse != null && _testMouse.added) InputSystem.RemoveDevice(_testMouse);
            _testMouse = null;
            foreach (Mouse nativeMouse in _disabledNativeMice)
            {
                if (nativeMouse != null && nativeMouse.added) InputSystem.EnableDevice(nativeMouse);
            }
            _disabledNativeMice = System.Array.Empty<Mouse>();
        }

        [UnityTest]
        public IEnumerator MouseFrames_EmitEnterDownAndUpEvents()
        {
            // Player에는 native Mouse가 이미 등록돼 있을 수 있습니다. 가상 Mouse state를 처리하는 중
            // native 장치가 다시 current가 되면 Controller가 다른 장치의 button을 읽으므로 격리합니다.
            _disabledNativeMice = InputSystem.devices.OfType<Mouse>().ToArray();
            foreach (Mouse nativeMouse in _disabledNativeMice) InputSystem.DisableDevice(nativeMouse);

            Mesh surfaceMesh = CreatePlaneMesh();
            GameObject surfaceObject = new("Pointer Surface") { layer = 8 };
            surfaceObject.AddComponent<MeshFilter>().sharedMesh = surfaceMesh;
            surfaceObject.AddComponent<MeshCollider>().sharedMesh = surfaceMesh;

            HexGridSettings settings = ScriptableObject.CreateInstance<HexGridSettings>();
            settings.TileRadius = 4f;
            settings.InteractionLayerMask = 1 << 8;

            GameObject cameraObject = new("Pointer Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(-2f, -2f, 10f);
            camera.transform.forward = Vector3.back;
            RenderTexture target = new(64, 64, 0);
            camera.targetTexture = target;

            GameObject controllerObject = new("Pointer Controller");
            HexGridController controller = controllerObject.AddComponent<HexGridController>();
            SetField(controller, "settings", settings);
            SetField(controller, "mainCamera", camera);
            SetField(controller, "seedOffset", new Vector3(-2f, -2f, 0f));
            InvokeNonPublic(controller, "OnValidate");

            Physics.SyncTransforms();
            controller.BakeTiles();
            List<string> events = new();
            controller.OnEnterTile += _ => events.Add("enter");
            controller.OnMouseDownTile += _ => events.Add("down");
            controller.OnMouseUpTile += _ => events.Add("up");

            Mouse mouse = _testMouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(32f, 32f) });
            InputSystem.Update();
            mouse.MakeCurrent();
            InvokeNonPublic(controller, "Update");
            InputSystem.QueueStateEvent(mouse,
                new MouseState { position = new Vector2(32f, 32f) }.WithButton(MouseButton.Left));
            InputSystem.Update();
            mouse.MakeCurrent();
            InvokeNonPublic(controller, "Update");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(32f, 32f) });
            InputSystem.Update();
            mouse.MakeCurrent();
            InvokeNonPublic(controller, "Update");

            Assert.That(events, Is.EqualTo(new[] { "enter", "down", "up" }));

            InputSystem.RemoveDevice(mouse);
            _testMouse = null;
            foreach (Mouse nativeMouse in _disabledNativeMice) InputSystem.EnableDevice(nativeMouse);
            _disabledNativeMice = System.Array.Empty<Mouse>();
            camera.targetTexture = null;
            Object.Destroy(controllerObject);
            Object.Destroy(cameraObject);
            Object.Destroy(surfaceObject);
            Object.Destroy(target);
            Object.Destroy(settings);
            Object.Destroy(surfaceMesh);
            yield return null;
        }

        private static Mesh CreatePlaneMesh()
        {
            Mesh mesh = new() { name = "Pointer Test Surface" };
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
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            field.SetValue(controller, value);
        }

        private static void InvokeNonPublic(HexGridController controller, string methodName)
        {
            MethodInfo method = typeof(HexGridController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
            method.Invoke(controller, null);
        }
    }
}
