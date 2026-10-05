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
        private readonly List<Mount> _mounts = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Clock.Reset(); Clock.EnsureHost = Ensure; _instance = null;
            UI.Strict = true; UI.Defaults = true; UI.DeferNestedProperties = true; UI.DefaultFont = null; UI.ReducedMotion.Value = false;
        }
        internal static void Ensure()
        {
            Clock.EnsureHost = Ensure;
            if (_instance == null)
            {
                var host = new GameObject("Pine Runtime"); _instance = host.AddComponent<RuntimeHost>();
                if (Application.isPlaying) DontDestroyOnLoad(host);
            }
            var existing = FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var system in existing)
            {
                if (!system.isActiveAndEnabled || system.gameObject == _instance._input) continue;
#if ENABLE_INPUT_SYSTEM
                if (system.GetComponent<InputSystemUIInputModule>() is InputSystemUIInputModule input && input.isActiveAndEnabled) { ReleaseOwnedInput(); return; }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (system.GetComponent<StandaloneInputModule>() is StandaloneInputModule legacy && legacy.isActiveAndEnabled) { ReleaseOwnedInput(); return; }
#endif
                throw new InvalidOperationException("An external EventSystem has no enabled input module compatible with the active backend. Configure it in application code; Pine preserves external ownership.");
            }
            if (_instance._input != null) return;
            _instance._input = new GameObject("Pine Input", typeof(EventSystem)); _instance._input.transform.SetParent(_instance.transform, false);
#if ENABLE_INPUT_SYSTEM
#if UNITY_ANDROID && ENABLE_LEGACY_INPUT_MANAGER
            _instance._input.AddComponent<StandaloneInputModule>();
#else
            var module = _instance._input.AddComponent<InputSystemUIInputModule>(); module.AssignDefaultActions();
#endif
#elif ENABLE_LEGACY_INPUT_MANAGER
            _instance._input.AddComponent<StandaloneInputModule>();
#else
            throw new InvalidOperationException("No supported input backend is enabled. Pine's Editor setup must complete before running the UI.");
#endif
        }
        private static void ReleaseOwnedInput()
        {
            if (_instance._input == null) return;
            _instance._input.SetActive(false); UI.DestroyObject(_instance._input); _instance._input = null;
        }
        internal static void Observe(Mount mount)
        {
            _instance._mounts.Add(mount);
            var host = _instance;
            mount.Scope.Run(() => UI.Cleanup(() => host._mounts.Remove(mount)));
        }
        private void DisposeDestroyedRoots()
        {
            // Unity skips OnDestroy for a component that was never enabled. The shared host
            // observes only mount roots; this check allocates nothing while interfaces are idle.
            for (int i = _mounts.Count - 1; i >= 0; i--)
                if (_mounts[i].Root == null)
                { try { _mounts[i].Dispose(); } catch (Exception error) { Debug.LogException(error); } }
        }
        private void Awake()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += SceneUnloaded;
        }
        private void SceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => Ensure();
        private void SceneUnloaded(UnityEngine.SceneManagement.Scene scene) { DisposeDestroyedRoots(); Ensure(); }
        private void Update()
        {
            using (UpdateMarker.Auto())
            {
                DisposeDestroyedRoots(); Clock.Step(Time.unscaledDeltaTime, false);
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
                try { mount.Dispose(); } catch (Exception error) { Debug.LogException(error); }
            }
            if (_instance != this) return; _instance = null; Clock.Reset();
        }
    }
}
