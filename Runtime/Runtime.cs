using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace Pine
{
    public sealed class CanvasOptions
    {
        public string Name = "Canvas";
        public Vector2 ReferenceResolution = new(1920, 1080);
        public int SortOrder = 100;
    }

    public static partial class Pine
    {
        private static bool _springSpacesRegistered;
        public static MountHandle Mount(Func<Component> component, Transform parent = null, CanvasOptions options = null)
            => Mount(() => component().gameObject, parent, options);
        public static MountHandle Mount(Func<GameObject> component, Transform parent = null, CanvasOptions options = null)
        {
            RuntimeHost.Ensure();
            MountHandle mount = new();
            mount.Scope = Root(() =>
            {
                Transform target = parent;
                if (target == null)
                {
                    options ??= new CanvasOptions();
                    GameObject canvasObject = new(options.Name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                    Cleanup(canvasObject);
                    Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = options.SortOrder;
                    UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = options.ReferenceResolution; scaler.matchWidthOrHeight = 0.5f;
                    mount.Canvas = canvas; target = canvasObject.transform;
                }
                mount.Root = component();
                Cleanup(mount.Root); mount.Root.transform.SetParent(target, false);
                mount.Canvas ??= target.GetComponentInParent<Canvas>();
            });
            mount.Root.AddComponent<MountLifetime>().Scope = mount.Scope;
            return mount;
        }

        static partial void ConfigureSpringSpaces()
        {
            if (_springSpacesRegistered) return;
            SpringSpaces.Registered[typeof(Vector2)] = UnitySpringSpaces.Vector2;
            SpringSpaces.Registered[typeof(Vector3)] = UnitySpringSpaces.Vector3;
            SpringSpaces.Registered[typeof(Vector4)] = UnitySpringSpaces.Vector4;
            SpringSpaces.Registered[typeof(Color)] = UnitySpringSpaces.Color;
            SpringSpaces.Registered[typeof(Rect)] = UnitySpringSpaces.Rect;
            SpringSpaces.Registered[typeof(Quaternion)] = UnitySpringSpaces.Quaternion;
            SpringSpaces.Registered[typeof(Pose)] = UnitySpringSpaces.Pose;
            _springSpacesRegistered = true;
        }
    }
    public static class UnitySpringSpaces
    {
        public static readonly SpringSpace<Vector2> Vector2 = new(v => new double[] { v.x, v.y }, a => new Vector2((float)a[0], (float)a[1]));
        public static readonly SpringSpace<Vector3> Vector3 = new(v => new double[] { v.x, v.y, v.z }, a => new Vector3((float)a[0], (float)a[1], (float)a[2]));
        public static readonly SpringSpace<Vector4> Vector4 = new(v => new double[] { v.x, v.y, v.z, v.w }, a => new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3]));
        public static readonly SpringSpace<Color> Color = new(v => new double[] { v.r, v.g, v.b, v.a }, a => new Color(Mathf.Clamp01((float)a[0]), Mathf.Clamp01((float)a[1]), Mathf.Clamp01((float)a[2]), Mathf.Clamp01((float)a[3])));
        public static readonly SpringSpace<Rect> Rect = new(v => new double[] { v.xMin, v.yMin, v.xMax, v.yMax }, a => UnityEngine.Rect.MinMaxRect((float)a[0], (float)a[1], (float)a[2], (float)a[3]));
        public static readonly SpringSpace<Quaternion> Quaternion = new(v => PackQuaternion(v), a => UnpackQuaternion(a, 0));
        public static readonly SpringSpace<Pose> Pose = new(v =>
        {
            double[] q = PackQuaternion(v.rotation); return new double[] { v.position.x, v.position.y, v.position.z, q[0], q[1], q[2], q[3] };
        }, a => new Pose(new Vector3((float)a[0], (float)a[1], (float)a[2]), UnpackQuaternion(a, 3)));
        private static double[] PackQuaternion(Quaternion value)
        {
            value = value.normalized; double sign = value.w < 0 ? -1 : 1;
            return new[] { value.x * sign, value.y * sign, value.z * sign, value.w * sign };
        }
        private static Quaternion UnpackQuaternion(double[] a, int offset)
        {
            Quaternion value = new((float)a[offset], (float)a[offset + 1], (float)a[offset + 2], (float)a[offset + 3]);
            double length = value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
            return length < 1e-12 ? UnityEngine.Quaternion.identity : value.normalized;
        }
    }

    internal sealed class MountLifetime : MonoBehaviour
    {
        internal Scope Scope;
        private void OnDestroy() => Scope?.Dispose();
    }

    public sealed class RuntimeHost : MonoBehaviour
    {
        private static RuntimeHost _instance;
        private GameObject _input;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Clock.Reset(); Clock.EnsureHost = Ensure; _instance = null;
            Pine.Strict = true; Pine.Defaults = true; Pine.DeferNestedProperties = true;
        }
        internal static void Ensure()
        {
            Clock.EnsureHost = Ensure;
            if (_instance == null)
            {
                GameObject host = new("Pine Runtime"); _instance = host.AddComponent<RuntimeHost>();
                if (Application.isPlaying) DontDestroyOnLoad(host);
            }
            if (FindAnyObjectByType<EventSystem>() == null && _instance._input == null)
            {
                _instance._input = new GameObject("Pine Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                _instance._input.transform.SetParent(_instance.transform, false);
            }
        }
        private void Update() => Clock.Step(Time.unscaledDeltaTime, false);
        private void OnDestroy()
        {
            if (_instance != this) return;
            _instance = null; Clock.Reset();
        }
    }
}

