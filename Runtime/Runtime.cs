using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Pine
{
    /// <summary>Typed reactive configuration for a Pine-owned canvas. Fields accept literals, sources, derived values, springs and Value-wrapped getters. Options only configure canvases created by Pine; an explicit parent retains ownership of its existing canvas. Camera and world-space rendering are selected in code.</summary>
    /// <example>
    /// <code><![CDATA[
    /// UI.Mount(
    ///     () => UI.Label(text: "Overlay"),
    ///     options: new CanvasOptions
    ///     {
    ///         ReferenceResolution = new UnityEngine.Vector2(x: 1280, y: 720),
    ///         SafeArea = true,
    ///     }
    /// );
    /// ]]></code>
    /// </example>
    public sealed class CanvasOptions
    {
        /// <summary>Whether a Pine-owned canvas survives scene changes. Defaults to true and is read once when mounting. Set false for scene-lived UI. Persistence never takes ownership of an external parent or canvas; explicitly requesting it with an external parent is rejected.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions { Persistent = false };
        /// ]]></code>
        /// </example>
        public bool Persistent = true;
        /// <summary>Reactive name for a Pine-owned canvas; defaults to Canvas.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions { Name = "HUD" };
        /// ]]></code>
        /// </example>
        public Value<string> Name = "Canvas";
        /// <summary>Reactive positive reference resolution for native canvas scaling; defaults to 1920 by 1080 with a 0.5 width/height match.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions
        /// {
        ///     ReferenceResolution = new UnityEngine.Vector2(x: 1280, y: 720),
        /// };
        /// ]]></code>
        /// </example>
        public Value<Vector2> ReferenceResolution = new Vector2(1920, 1080);
        /// <summary>Reactive native Canvas sorting order; defaults to 100.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions { SortOrder = 200 };
        /// ]]></code>
        /// </example>
        public Value<int> SortOrder = 100;
        /// <summary>Reactive rendering mode: overlay by default, camera or world-space when explicitly selected.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions
        /// {
        ///     RenderMode = UnityEngine.RenderMode.ScreenSpaceOverlay,
        /// };
        /// ]]></code>
        /// </example>
        public Value<RenderMode> RenderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
        /// <summary>Reactive native camera reference. Camera-space mounts require an explicit live camera; world-space input can also use this reference.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions { Camera = camera };
        /// ]]></code>
        /// </example>
        public Value<Camera> Camera;
        /// <summary>Reactive positive UI scale multiplier; it adjusts reference scaling for screen canvases and local transform scale for world canvases. Defaults to one.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions { Scale = 1.25f };
        /// ]]></code>
        /// </example>
        public Value<float> Scale = 1f;
        /// <summary>Reactive opt-in safe-area handling for the Pine-owned screen-space surface. Defaults to true; world-space canvases bypass screen safe areas.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions { SafeArea = true };
        /// ]]></code>
        /// </example>
        public Value<bool> SafeArea = true;
        /// <summary>Reactive world canvas position, applied in WorldSpace mode. Defaults to world zero.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions
        /// {
        ///     WorldPosition = new UnityEngine.Vector3(x: 0, y: 1, z: 2),
        /// };
        /// ]]></code>
        /// </example>
        public Value<Vector3> WorldPosition = Vector3.zero;
        /// <summary>Reactive world canvas rotation, applied in WorldSpace mode. Defaults to identity.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions
        /// {
        ///     WorldRotation = UnityEngine.Quaternion.identity,
        /// };
        /// ]]></code>
        /// </example>
        public Value<Quaternion> WorldRotation = Quaternion.identity;
        /// <summary>Reactive finite non-negative world canvas dimensions before its scale multiplier. Defaults to 800 by 600.</summary>
        /// <example>
        /// <code><![CDATA[
        /// var options = new CanvasOptions
        /// {
        ///     WorldSize = new UnityEngine.Vector2(x: 200, y: 100),
        /// };
        /// ]]></code>
        /// </example>
        public Value<Vector2> WorldSize = new Vector2(800, 600);
    }

    public static partial class UI
    {
        private static bool _springSpacesRegistered;
        /// <summary>Configures Pine&#x27;s code-supplied default TMP font for subsequent text construction. Null reuses the project-owned copy of the bundled Latin fallback, then the package fallback, then the project TMP default if both are unavailable. Existing created labels retain their font unless a reactive Font property is bound.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.DefaultFont = localizedFont;
        /// ]]></code>
        /// </example>
        public static TMP_FontAsset DefaultFont { get; set; }
        private static TMP_FontAsset _fallbackFont;
        internal static TMP_FontAsset ResolveFont()
        {
            if (DefaultFont != null) return DefaultFont;
            if (_fallbackFont == null) _fallbackFont = Resources.Load<TMP_FontAsset>("PineGenerated/Fonts/Latin") ?? Resources.Load<TMP_FontAsset>("Pine/Fonts/Latin");
            if (_fallbackFont != null) return _fallbackFont;
            if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
            throw new InvalidOperationException("Pine's bundled Latin font is missing. Reinstall Pine or supply UI.DefaultFont in code.");
        }
        /// <summary>An explicit mounted interface lifetime. Scope owns bindings and created native objects; Root identifies the returned interface and Canvas identifies its containing canvas. Dispose removes the interface; destroying Root also disposes its scope. App.Mount returns the whole tree once at startup; generated startup calls this method automatically. Pine-owned canvases persist across scenes by default. CanvasOptions.Persistent=false opts into scene lifetime. Destroying the root or disposing the result ends its scope; disabling a caller does not rebuild UI.</summary>
        /// <param name="component">Builder returning the live native root in the mount scope.</param>
        /// <param name="parent">Optional external native parent; null creates a Pine-owned canvas.</param>
        /// <param name="options">Code-configured typed reactive canvas options, used only for Pine-owned canvases.</param>
        /// <returns>The mounted tree’s scope, native root and containing canvas. Keep this optional result only for explicit early disposal. Persistence is configured with CanvasOptions.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// Mount mount = UI.Mount(component: () => UI.Label(text: "Hello"));
        /// mount.Dispose();
        /// ]]></code>
        /// </example>
        public static Mount Mount(Func<Component> component, Transform parent = null, CanvasOptions options = null)
            {
            if (component == null) throw new ArgumentNullException(nameof(component));
            return Mount(() => component()?.gameObject ?? throw new InvalidOperationException("A mount must return a live component."), parent, options);
        }
        /// <summary>An explicit mounted interface lifetime. Scope owns bindings and created native objects; Root identifies the returned interface and Canvas identifies its containing canvas. Dispose removes the interface; destroying Root also disposes its scope. App.Mount returns the whole tree once at startup; generated startup calls this method automatically. Pine-owned canvases persist across scenes by default. CanvasOptions.Persistent=false opts into scene lifetime. Destroying the root or disposing the result ends its scope; disabling a caller does not rebuild UI.</summary>
        /// <param name="component">Builder returning the live native root in the mount scope.</param>
        /// <param name="parent">Optional external native parent; null creates a Pine-owned canvas.</param>
        /// <param name="options">Code-configured typed reactive canvas options, used only for Pine-owned canvases.</param>
        /// <returns>The mounted tree’s scope, native root and containing canvas. Keep this optional result only for explicit early disposal. Persistence is configured with CanvasOptions.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// Mount mount = UI.Mount(component: () => UI.Label(text: "Hello"));
        /// mount.Dispose();
        /// ]]></code>
        /// </example>
        public static Mount Mount(Func<GameObject> component, Transform parent = null, CanvasOptions options = null)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (parent != null && options?.Persistent == true)
                throw new ArgumentException("Persistent mounting requires a Pine-owned canvas. Set CanvasOptions.Persistent to false when using an external parent.", nameof(options));
            RuntimeHost.Ensure(); var mount = new Mount();
            mount.Scope = Root(() =>
            {
                Transform target = parent;
                if (target == null)
                {
                    options ??= new CanvasOptions();
                    var canvasObject = new GameObject("Pine Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                    Cleanup(canvasObject);
                    Canvas canvas = canvasObject.GetComponent<Canvas>(); mount.Canvas = canvas;
                    var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.matchWidthOrHeight = 0.5f;
                    var surface = Frame(Name("Safe area"), Stretch());
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
                mount.Root = component();
                if (mount.Root == null) throw new InvalidOperationException("A mount must return a live GameObject.");
                Cleanup(mount.Root); mount.Root.transform.SetParent(target, false);
                mount.Canvas ??= target.GetComponentInParent<Canvas>();
            });
            if (parent == null && options.Persistent && Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(mount.Canvas.gameObject);
            mount.Root.AddComponent<MountLifetime>().Scope = mount.Scope; RuntimeHost.Observe(mount); return mount;
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
    /// <summary>Built-in spring mappings for Unity vectors, colors, rectangles, quaternions and poses. Colors are clamped on unpack; quaternions are normalized and packed into a consistent hemisphere. UI.Spring automatically selects these mappings for their supported types.</summary>
    /// <example>
    /// <code><![CDATA[
    /// UI.Spring(() => UnityEngine.Vector3.one, space: UnitySpringSpaces.Vector3);
    /// ]]></code>
    /// </example>
    public static class UnitySpringSpaces
    {
        /// <summary>Built-in fixed-lane mapping for Vector2. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: UnitySpringSpaces.Vector2);
        /// ]]></code>
        /// </example>
        public static readonly SpringSpace<Vector2> Vector2 = new(v => new double[] { v.x, v.y }, a => new Vector2((float)a[0], (float)a[1]));
        /// <summary>Built-in fixed-lane mapping for Vector3. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: UnitySpringSpaces.Vector3);
        /// ]]></code>
        /// </example>
        public static readonly SpringSpace<Vector3> Vector3 = new(v => new double[] { v.x, v.y, v.z }, a => new Vector3((float)a[0], (float)a[1], (float)a[2]));
        /// <summary>Built-in fixed-lane mapping for Vector4. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: UnitySpringSpaces.Vector4);
        /// ]]></code>
        /// </example>
        public static readonly SpringSpace<Vector4> Vector4 = new(v => new double[] { v.x, v.y, v.z, v.w }, a => new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3]));
        /// <summary>Built-in fixed-lane mapping for Color. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: UnitySpringSpaces.Color);
        /// ]]></code>
        /// </example>
        public static readonly SpringSpace<Color> Color = new(v => new double[] { v.r, v.g, v.b, v.a }, a => new Color(Mathf.Clamp01((float)a[0]), Mathf.Clamp01((float)a[1]), Mathf.Clamp01((float)a[2]), Mathf.Clamp01((float)a[3])));
        /// <summary>Built-in fixed-lane mapping for Rect. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: UnitySpringSpaces.Rect);
        /// ]]></code>
        /// </example>
        public static readonly SpringSpace<Rect> Rect = new(v => new double[] { v.xMin, v.yMin, v.xMax, v.yMax }, a => UnityEngine.Rect.MinMaxRect((float)a[0], (float)a[1], (float)a[2], (float)a[3]));
        /// <summary>Built-in fixed-lane mapping for Quaternion. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: UnitySpringSpaces.Quaternion);
        /// ]]></code>
        /// </example>
        public static readonly SpringSpace<Quaternion> Quaternion = new(v => PackQuaternion(v), a => UnpackQuaternion(a, 0));
        /// <summary>Built-in fixed-lane mapping for Pose. Use the matching space when declaring a spring; Unity mappings normalize rotations and clamp color output as documented.</summary>
        /// <example>
        /// <code><![CDATA[
        /// UI.Spring(() => target.Value, space: UnitySpringSpaces.Pose);
        /// ]]></code>
        /// </example>
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
