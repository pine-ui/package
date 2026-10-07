using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pine.UIToolkit
{
    /// <summary>Settings for a Pine-owned native runtime document.</summary>
    public sealed class PanelOptions
    {
        /// <summary>A supplied native settings asset, retained after disposal.</summary>
        public PanelSettings Settings;

        /// <summary>Named native panel settings, applied in the mount's scope.</summary>
        public Action<PanelSettings> Panel;

        /// <summary>Named native document settings, applied in the mount's scope.</summary>
        public Action<UIDocument> Document;

        /// <summary>Retains an owned document across scene changes.</summary>
        public bool Persistent;

        /// <summary>The owned document's GameObject name.</summary>
        public string Name = "Pine UI Toolkit";
    }

    /// <summary>An explicit UI Toolkit interface lifetime.</summary>
    public sealed class Mount : IDisposable
    {
        private static readonly HashSet<Mount> Mounts = new();
        private Scope _scope;
        private Scope _tree;
        private GameObject _host;
        private Func<View> _component;
        private VisualElement _parent;

        /// <summary>The current native root created by the component.</summary>
        public VisualElement Root { get; private set; }

        /// <summary>The document for an automatically created runtime panel.</summary>
        public UIDocument Document { get; internal set; }

        /// <summary>The interface's owning reactive scope.</summary>
        public Scope Scope => _scope;

        /// <summary>Reports whether the mount has been disposed.</summary>
        public bool IsDisposed => _scope == null || _scope.IsDisposed;

        internal void Initialize(Func<View> component, VisualElement parent, PanelOptions options)
        {
            _component = component ?? throw new ArgumentNullException(nameof(component));
            _scope = Core.Root(() =>
            {
                _scope = Core.RequireScope();
                Mounts.Add(this);
                Core.Cleanup(() => Mounts.Remove(this));
                if (parent != null)
                {
                    if (options != null)
                        throw new ArgumentException(
                            "Panel options require an owned runtime document."
                        );
                    Rebuild(parent);
                    return;
                }
                if (!Application.isPlaying)
                    throw new InvalidOperationException(
                        "Editor UI mounts require an explicit VisualElement root."
                    );
                RuntimeHost.EnsureClock();
                options ??= new PanelOptions();
                _host = new GameObject(options.Name);
                _host.SetActive(false);
                if (options.Persistent)
                    UnityEngine.Object.DontDestroyOnLoad(_host);
                Core.Cleanup(() => Destroy(_host));
                Document = _host.AddComponent<UIDocument>();
                var settings = options.Settings;
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<PanelSettings>();
                    settings.name = "Pine Panel";
                    var owned = settings;
                    Core.Cleanup(() => Destroy(owned));
                    settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(
                        "Pine/Toolkit/Theme"
                    );
                    if (settings.themeStyleSheet == null)
                        throw new InvalidOperationException(
                            "Pine's bundled UI Toolkit theme was not imported."
                        );
                    settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                    settings.referenceResolution = new Vector2Int(1920, 1080);
                }
                Document.panelSettings = settings;
                options.Panel?.Invoke(settings);
                options.Document?.Invoke(Document);
                var lifetime = _host.AddComponent<ToolkitMountLifetime>();
                lifetime.Mount = this;
                _host.SetActive(true);
                RefreshDocument();
            });
        }

        internal void RefreshDocument()
        {
            if (IsDisposed || Document == null)
                return;
            VisualElement parent = Document.isActiveAndEnabled ? Document.rootVisualElement : null;
            if (!ReferenceEquals(parent, _parent))
                Core.Untrack(() => _scope.Run(() => Rebuild(parent)));
        }

        internal void Suspend()
        {
            _tree?.Dispose();
            _tree = null;
            Root = null;
            _parent = null;
        }

        private void Rebuild(VisualElement parent)
        {
            Suspend();
            _parent = parent;
            if (parent == null)
                return;
            _tree = Core.OwnedRoot(() =>
                P.ComponentHost.Provide(
                    _host != null ? _host.transform : null,
                    () =>
                        View.Construct(() =>
                        {
                            var declaration =
                                _component()
                                ?? throw new InvalidOperationException(
                                    "A mount must return a View."
                                );
                            var built = declaration.Build();
                            Root = built.Element;
                            var root = Root;
                            Core.Cleanup(() => root.RemoveFromHierarchy());
                        })
                )
            );
            parent.Add(Root);
        }

        /// <summary>Removes the owned tree and releases its callbacks and bindings.</summary>
        public void Dispose()
        {
            var scope = _scope;
            if (scope == null)
                return;
            _scope = null;
            try
            {
                scope.Dispose();
            }
            finally
            {
                _tree = null;
                Root = null;
                _parent = null;
            }
        }

        internal static void Destroy(UnityEngine.Object value)
        {
            if (value == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(value);
            else
                UnityEngine.Object.DestroyImmediate(value);
        }

        internal static void DisposeAll()
        {
            foreach (var mount in new List<Mount>(Mounts))
                mount.Dispose();
        }
    }

    internal sealed class ToolkitMountLifetime : MonoBehaviour
    {
        internal Mount Mount;

        private void Update() => Mount?.RefreshDocument();

        private void OnEnable() => Mount?.RefreshDocument();

        private void OnDisable() => Mount?.Suspend();

        private void OnDestroy() => Mount?.Dispose();
    }

    public static partial class P
    {
        internal static readonly Context<Transform> ComponentHost = Core.Context<Transform>();

        /// <summary>Mounts a declaration into an existing root or a code-created runtime panel.</summary>
        public static Mount Mount(
            Func<View> component,
            VisualElement parent = null,
            PanelOptions options = null
        )
        {
            var mount = new Mount();
            try
            {
                mount.Initialize(component, parent, options);
                return mount;
            }
            catch
            {
                mount.Dispose();
                throw;
            }
        }

        /// <summary>Mounts an explicitly created native tree with an owned reactive setup scope.</summary>
        public static Mount Mount(
            Func<VisualElement> component,
            VisualElement parent = null,
            PanelOptions options = null
        ) => Mount(() => BuildView(component, null), parent, options);

        /// <summary>Runs a native component factory with an owned setup scope.</summary>
        public static TNative Component<TNative>(Func<TNative> render)
            where TNative : VisualElement
        {
            TNative result = null;
            Scope scope = Core.OwnedRoot(() => result = render());
            if (result != null)
                return result;
            scope.Dispose();
            throw new InvalidOperationException("A component must return a VisualElement.");
        }

        /// <summary>Runs a native component factory backed by an owned MonoBehaviour.</summary>
        public static TNative Component<TBehaviour, TNative>(Func<TBehaviour, TNative> render)
            where TBehaviour : MonoBehaviour
            where TNative : VisualElement =>
            Component(() =>
            {
                var host = new GameObject(typeof(TBehaviour).Name);
                host.SetActive(false);
                host.transform.SetParent(ComponentHost.Value, false);
                Core.Cleanup(() => global::Pine.UIToolkit.Mount.Destroy(host));
                var result = render(host.AddComponent<TBehaviour>());
                View.Activate(() => host.SetActive(true));
                return result;
            });

        /// <summary>Creates a component backed by an independently owned MonoBehaviour.</summary>
        public static View Component<T>(Func<T, View> render)
            where T : MonoBehaviour =>
            Component(() =>
            {
                var host = new GameObject(typeof(T).Name);
                host.SetActive(false);
                host.transform.SetParent(ComponentHost.Value, false);
                Core.Cleanup(() => global::Pine.UIToolkit.Mount.Destroy(host));
                var behaviour = host.AddComponent<T>();
                View declaration = render(behaviour);
                Core.Cleanup(() =>
                {
                    if (host != null)
                        host.SetActive(false);
                });
                return BuildView(
                    () => new VisualElement(),
                    null,
                    children: new[] { declaration },
                    configure: _ => View.Activate(() => host.SetActive(true))
                );
            });
    }
}
