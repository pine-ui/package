using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Pine.uGUI
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
        /// <summary>Configures Pine's code-supplied default TMP font for subsequent text construction.</summary>
        public static TMP_FontAsset DefaultFont { get; set; }
        private static TMP_FontAsset _fallbackFont;

        internal static TMP_FontAsset ResolveFont()
        {
            if (DefaultFont != null)
                return DefaultFont;
            if (_fallbackFont == null)
                _fallbackFont =
                    Resources.Load<TMP_FontAsset>("PineGenerated/Fonts/Latin")
                    ?? Resources.Load<TMP_FontAsset>("Pine/Fonts/Latin");
            if (_fallbackFont != null)
                return _fallbackFont;
            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;
            throw new InvalidOperationException(
                "Pine's bundled Latin font is missing. Reinstall Pine or supply P.DefaultFont in code."
            );
        }

        /// <summary>An explicit mounted interface lifetime.</summary>
        public static Mount Mount(
            Func<Component> component,
            Transform parent = null,
            CanvasOptions options = null
        )
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            return Mount(
                () =>
                    component()?.gameObject
                    ?? throw new InvalidOperationException("A mount must return a live component."),
                parent,
                options
            );
        }

        /// <summary>An explicit mounted interface lifetime.</summary>
        public static Mount Mount(
            Func<GameObject> component,
            Transform parent = null,
            CanvasOptions options = null
        )
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            if (parent != null && options?.Persistent == true)
                throw new ArgumentException(
                    "Persistent mounting requires a Pine-owned canvas. Set CanvasOptions.Persistent to false when using an external parent.",
                    nameof(options)
                );
            var mount = new Mount();
            if (parent == null)
                options ??= new CanvasOptions();
            mount.Scope = Root(() =>
            {
                mount.Root = component();
                if (mount.Root == null)
                    throw new InvalidOperationException("A mount must return a live GameObject.");
                Cleanup(mount.Root);
                Transform target = parent;
                mount.Canvas = mount.Root.GetComponent<Canvas>();
                if (target == null && mount.Canvas == null)
                {
                    var canvasObject = new GameObject(
                        "Pine Canvas",
                        typeof(RectTransform),
                        typeof(Canvas),
                        typeof(UnityEngine.UI.CanvasScaler),
                        typeof(UnityEngine.UI.GraphicRaycaster)
                    );
                    Cleanup(canvasObject);
                    Canvas canvas = canvasObject.GetComponent<Canvas>();
                    mount.Canvas = canvas;
                    var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.matchWidthOrHeight = 0.5f;
                    var surface = Create<RectTransform>(Name("Safe area"), Stretch());
                    surface.SetParent(canvasObject.transform, false);
                    target = surface;
                    Effect(() =>
                    {
                        string name = options.Name.Read();
                        Vector2 reference = options.ReferenceResolution.Read();
                        int order = options.SortOrder.Read();
                        RenderMode mode = options.RenderMode.Read();
                        Camera camera = options.Camera.Read();
                        float scale = options.Scale.Read();
                        Vector3 position = options.WorldPosition.Read();
                        Quaternion rotation = options.WorldRotation.Read();
                        Vector2 size = options.WorldSize.Read();
                        bool safe = options.SafeArea.Read();
                        ValidateDimension(reference.x);
                        ValidateDimension(reference.y);
                        ValidateDimension(size.x);
                        ValidateDimension(size.y);
                        if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale))
                            throw new ArgumentOutOfRangeException(nameof(options.Scale));
                        if (mode == UnityEngine.RenderMode.ScreenSpaceCamera && camera == null)
                            throw new ArgumentException(
                                "Camera-space mounts require a Camera supplied in code."
                            );
                        Untrack(() =>
                        {
                            canvasObject.name = name;
                            canvas.sortingOrder = order;
                            canvas.renderMode = mode;
                            canvas.worldCamera = camera;
                            scaler.referenceResolution = reference / scale;
                            if (mode != UnityEngine.RenderMode.WorldSpace)
                                canvasObject.transform.localScale = Vector3.one;
                            var rect = (RectTransform)canvasObject.transform;
                            if (mode == UnityEngine.RenderMode.WorldSpace)
                            {
                                rect.position = position;
                                rect.rotation = rotation;
                                rect.sizeDelta = size;
                                rect.localScale = Vector3.one * scale;
                            }
                            var safeArea = GetOrAdd<PineSafeArea>(surface.gameObject);
                            safeArea.enabled = safe && mode != UnityEngine.RenderMode.WorldSpace;
                            if (safeArea.enabled)
                                safeArea.Refresh();
                            else
                            {
                                surface.anchorMin = Vector2.zero;
                                surface.anchorMax = Vector2.one;
                                surface.offsetMin = surface.offsetMax = Vector2.zero;
                            }
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
            catch
            {
                mount.Dispose();
                throw;
            }
        }
    }
}
