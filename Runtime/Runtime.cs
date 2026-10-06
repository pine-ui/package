using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Pine
{
    /// <summary>Typed reactive configuration for a Pine-owned canvas.</summary>
    public sealed class CanvasOptions
    {
        /// <summary>Whether a Pine-owned canvas survives scene changes.</summary>
        public bool Persistent = true;
        /// <summary>Reactive name for a Pine-owned canvas; defaults to Canvas.</summary>
        public Value<string> Name = "Canvas";
        /// <summary>Reactive positive reference resolution for native canvas scaling; defaults to 1920 by 1080 with a 0.5 width/height match.</summary>
        public Value<Vector2> ReferenceResolution = new Vector2(1920, 1080);
        /// <summary>Reactive native Canvas sorting order; defaults to 100.</summary>
        public Value<int> SortOrder = 100;
        /// <summary>Reactive rendering mode: overlay by default, camera or world-space when explicitly selected.</summary>
        public Value<RenderMode> RenderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
        /// <summary>Reactive native camera reference.</summary>
        public Value<Camera> Camera;
        /// <summary>Reactive positive UI scale multiplier; it adjusts reference scaling for screen canvases and local transform scale for world canvases.</summary>
        public Value<float> Scale = 1f;
        /// <summary>Reactive opt-in safe-area handling for the Pine-owned screen-space surface.</summary>
        public Value<bool> SafeArea = true;
        /// <summary>Reactive world canvas position, applied in WorldSpace mode.</summary>
        public Value<Vector3> WorldPosition = Vector3.zero;
        /// <summary>Reactive world canvas rotation, applied in WorldSpace mode.</summary>
        public Value<Quaternion> WorldRotation = Quaternion.identity;
        /// <summary>Reactive finite non-negative world canvas dimensions before its scale multiplier.</summary>
        public Value<Vector2> WorldSize = new Vector2(800, 600);
    }

    public static partial class P
    {
        private static bool _springSpacesRegistered;
        /// <summary>Configures Pine's code-supplied default TMP font for subsequent text construction.</summary>
        public static TMP_FontAsset DefaultFont { get; set; }
        private static TMP_FontAsset _fallbackFont;
        internal static TMP_FontAsset ResolveFont()
        {
            if (DefaultFont != null) return DefaultFont;
            if (_fallbackFont == null) _fallbackFont = Resources.Load<TMP_FontAsset>("PineGenerated/Fonts/Latin") ?? Resources.Load<TMP_FontAsset>("Pine/Fonts/Latin");
            if (_fallbackFont != null) return _fallbackFont;
            if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
            throw new InvalidOperationException("Pine's bundled Latin font is missing. Reinstall Pine or supply P.DefaultFont in code.");
        }
        /// <summary>An explicit mounted interface lifetime.</summary>
        public static Mount Mount(Func<Component> component, Transform parent = null, CanvasOptions options = null)
            {
            if (component == null) throw new ArgumentNullException(nameof(component));
            return Mount(() => component()?.gameObject ?? throw new InvalidOperationException("A mount must return a live component."), parent, options);
        }
        /// <summary>An explicit mounted interface lifetime.</summary>
        public static Mount Mount(Func<GameObject> component, Transform parent = null, CanvasOptions options = null)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (parent != null && options?.Persistent == true)
                throw new ArgumentException("Persistent mounting requires a Pine-owned canvas. Set CanvasOptions.Persistent to false when using an external parent.", nameof(options));
            var mount = new Mount();
            if (parent == null) options ??= new CanvasOptions();
            mount.Scope = Root(() =>
            {
                mount.Root = component();
                if (mount.Root == null) throw new InvalidOperationException("A mount must return a live GameObject.");
                Cleanup(mount.Root);
                Transform target = parent;
                mount.Canvas = mount.Root.GetComponent<Canvas>();
                if (target == null && mount.Canvas == null)
                {
                    var canvasObject = new GameObject("Pine Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                    Cleanup(canvasObject);
                    Canvas canvas = canvasObject.GetComponent<Canvas>(); mount.Canvas = canvas;
                    var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.matchWidthOrHeight = 0.5f;
                    var surface = Create<RectTransform>(Name("Safe area"), Stretch());
                    surface.SetParent(canvasObject.transform, false); target = surface;
                    Effect(() =>
                    {
                        string name = options.Name.Read(); Vector2 reference = options.ReferenceResolution.Read(); int order = options.SortOrder.Read();
                        RenderMode mode = options.RenderMode.Read(); Camera camera = options.Camera.Read(); float scale = options.Scale.Read();
                        Vector3 position = options.WorldPosition.Read(); Quaternion rotation = options.WorldRotation.Read(); Vector2 size = options.WorldSize.Read(); bool safe = options.SafeArea.Read();
                        ValidateDimension(reference.x); ValidateDimension(reference.y); ValidateDimension(size.x); ValidateDimension(size.y);
                        if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale)) throw new ArgumentOutOfRangeException(nameof(options.Scale));
                        if (mode == UnityEngine.RenderMode.ScreenSpaceCamera && camera == null) throw new ArgumentException("Camera-space mounts require a Camera supplied in code.");
                        Untrack(() =>
                        {
                            canvasObject.name = name; canvas.sortingOrder = order; canvas.renderMode = mode; canvas.worldCamera = camera;
                            scaler.referenceResolution = reference / scale;
                            if (mode != UnityEngine.RenderMode.WorldSpace) canvasObject.transform.localScale = Vector3.one;
                            var rect = (RectTransform)canvasObject.transform;
                            if (mode == UnityEngine.RenderMode.WorldSpace) { rect.position = position; rect.rotation = rotation; rect.sizeDelta = size; rect.localScale = Vector3.one * scale; }
                            var safeArea = GetOrAdd<PineSafeArea>(surface.gameObject); safeArea.enabled = safe && mode != UnityEngine.RenderMode.WorldSpace;
                            if (safeArea.enabled) safeArea.Refresh(); else { surface.anchorMin = Vector2.zero; surface.anchorMax = Vector2.one; surface.offsetMin = surface.offsetMax = Vector2.zero; }
                        });
                    });
                }
                mount.Root.transform.SetParent(target, false);
                mount.Canvas ??= target?.GetComponentInParent<Canvas>();
            });
            if (parent == null && options.Persistent && Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(mount.Canvas.gameObject);
            try
            {
                RuntimeHost.Ensure();
                mount.Root.AddComponent<MountLifetime>().Scope = mount.Scope;
                RuntimeHost.Observe(mount);
                foreach (var lifetime in mount.Root.GetComponentsInChildren<MountLifetime>(true))
                    RuntimeHost.ObserveView(lifetime.gameObject, lifetime.Scope);
                return mount;
            }
            catch { mount.Dispose(); throw; }
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
            SpringSpaces.Registered[typeof(Pose)] = UnitySpringSpaces.Pose; _springSpacesRegistered = true;
        }
    }
    /// <summary>Built-in spring mappings for Unity vectors, colors, rectangles, quaternions and poses.</summary>
    public static class UnitySpringSpaces
    {
        /// <summary>Built-in fixed-lane mapping for Vector2.</summary>
        public static readonly SpringSpace<Vector2> Vector2 = new(v => new double[] { v.x, v.y }, a => new Vector2((float)a[0], (float)a[1]));
        /// <summary>Built-in fixed-lane mapping for Vector3.</summary>
        public static readonly SpringSpace<Vector3> Vector3 = new(v => new double[] { v.x, v.y, v.z }, a => new Vector3((float)a[0], (float)a[1], (float)a[2]));
        /// <summary>Built-in fixed-lane mapping for Vector4.</summary>
        public static readonly SpringSpace<Vector4> Vector4 = new(v => new double[] { v.x, v.y, v.z, v.w }, a => new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3]));
        /// <summary>Built-in fixed-lane mapping for Color.</summary>
        public static readonly SpringSpace<Color> Color = new(v => new double[] { v.r, v.g, v.b, v.a }, a => new Color(Mathf.Clamp01((float)a[0]), Mathf.Clamp01((float)a[1]), Mathf.Clamp01((float)a[2]), Mathf.Clamp01((float)a[3])));
        /// <summary>Built-in fixed-lane mapping for Rect.</summary>
        public static readonly SpringSpace<Rect> Rect = new(v => new double[] { v.xMin, v.yMin, v.xMax, v.yMax }, a => UnityEngine.Rect.MinMaxRect((float)a[0], (float)a[1], (float)a[2], (float)a[3]));
        /// <summary>Built-in fixed-lane mapping for Quaternion.</summary>
        public static readonly SpringSpace<Quaternion> Quaternion = new(v => PackQuaternion(v), a => UnpackQuaternion(a, 0));
        /// <summary>Built-in fixed-lane mapping for Pose.</summary>
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

}
