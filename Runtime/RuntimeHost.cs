using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Pine
{
    internal sealed class RuntimeHost : MonoBehaviour
    {
        private static RuntimeHost _instance;
        private GameObject _input;
        private Scope _inputScope;
        private readonly List<Mount> _mounts = new();
        private readonly HashSet<Scope> _observedScopes = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Clock.Reset();
            Clock.EnsureHost = Ensure;
            _instance = null;
            P.Strict = true;
            P.Defaults = true;
            P.DeferNestedProperties = true;
            P.DefaultFont = null;
            P.ReducedMotion.Value = false;
        }

        internal static void Ensure()
        {
            Clock.EnsureHost = Ensure;
            if (_instance == null)
            {
                var host = new GameObject("Pine Runtime");
                _instance = host.AddComponent<RuntimeHost>();
                if (Application.isPlaying)
                    DontDestroyOnLoad(host);
            }
            if (View.IsConstructing)
                return;
#if UNITY_6000_7_OR_NEWER
            var existing = FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude);
#else
            var existing = FindObjectsByType<EventSystem>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
#endif
            foreach (var system in existing)
            {
                if (!system.isActiveAndEnabled || system.gameObject == _instance._input)
                    continue;
#if ENABLE_INPUT_SYSTEM
                if (
                    system.GetComponent<InputSystemUIInputModule>()
                        is InputSystemUIInputModule input
                    && input.isActiveAndEnabled
                )
                {
                    ReleaseOwnedInput();
                    return;
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (
                    system.GetComponent<StandaloneInputModule>() is StandaloneInputModule legacy
                    && legacy.isActiveAndEnabled
                )
                {
                    ReleaseOwnedInput();
                    return;
                }
#endif
                throw new InvalidOperationException(
                    "An external EventSystem has no enabled input module compatible with the active backend. Configure it in application code; Pine preserves external ownership."
                );
            }
            if (_instance._input != null)
                return;
            _instance._input = new GameObject("Pine Input");
            _instance._input.SetActive(false);
            _instance._input.AddComponent<EventSystem>();
            _instance._input.transform.SetParent(_instance.transform, false);
#if ENABLE_INPUT_SYSTEM
#if UNITY_ANDROID && ENABLE_LEGACY_INPUT_MANAGER
            _instance._input.AddComponent<StandaloneInputModule>();
#else
            var module = _instance._input.AddComponent<InputSystemUIInputModule>();
            _instance._inputScope = P.Root(() => P.OwnInputDefaults(module));
#endif
#elif ENABLE_LEGACY_INPUT_MANAGER
            _instance._input.AddComponent<StandaloneInputModule>();
#else
            throw new InvalidOperationException(
                "No supported input backend is enabled. Pine's Editor setup must complete before running the P."
            );
#endif
            _instance._input.SetActive(true);
        }

        private static void ReleaseOwnedInput()
        {
            if (_instance._input == null)
                return;
            _instance._input.SetActive(false);
            _instance._inputScope?.Dispose();
            _instance._inputScope = null;
            P.DestroyObject(_instance._input);
            _instance._input = null;
        }

        internal static void Observe(Mount mount)
        {
            if (!_instance._observedScopes.Add(mount.Scope))
                return;
            _instance._mounts.Add(mount);
            var host = _instance;
            mount.Scope.Run(() =>
                P.Cleanup(() =>
                {
                    host._mounts.Remove(mount);
                    host._observedScopes.Remove(mount.Scope);
                })
            );
        }

        internal static void ObserveView(GameObject root, Scope scope)
        {
            if (_instance != null)
                Observe(new Mount { Root = root, Scope = scope });
        }

        private void DisposeDestroyedRoots()
        {
            // Unity skips OnDestroy for never-active objects. Disposing a parent can also
            // remove its children's records, so clamp the next index after cleanup.
            for (int i = _mounts.Count - 1; i >= 0; i = Math.Min(i - 1, _mounts.Count - 1))
                if (_mounts[i].Root == null)
                {
                    try
                    {
                        _mounts[i].Dispose();
                    }
                    catch (Exception error)
                    {
                        Debug.LogException(error);
                    }
                }
        }

        private void Awake()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += SceneUnloaded;
        }

        private void SceneLoaded(
            UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode
        ) => Ensure();

        private void SceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            DisposeDestroyedRoots();
            Ensure();
        }

        private void Update()
        {
            using (UpdateMarker.Auto())
            {
                DisposeDestroyedRoots();
                Clock.Step(Time.unscaledDeltaTime, false);
            }
        }

        private static readonly Unity.Profiling.ProfilerMarker UpdateMarker = new("Pine.Update");

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= SceneUnloaded;
            while (_mounts.Count > 0)
            {
                var mount = _mounts[_mounts.Count - 1];
                try
                {
                    mount.Dispose();
                }
                catch (Exception error)
                {
                    Debug.LogException(error);
                }
            }
            if (_instance != this)
                return;
            _inputScope?.Dispose();
            _instance = null;
            Clock.Reset();
        }
    }
}
