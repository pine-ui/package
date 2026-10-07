using System;
using System.Reflection;
using Pine;
using Pine.uGUI;
using UnityEditor;
using UnityEngine;

namespace Pine.Tests
{
    [InitializeOnLoad]
    public static class PineSafeAreaChecks
    {
        private const string Request = "Pine.SafeAreaChecks.Play";
        private static readonly Type Helper = typeof(P).Assembly.GetType(
            "Pine.uGUI.PineSafeArea",
            true
        );
        private static readonly MethodInfo Refresh = Helper.GetMethod(
            "Refresh",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(Rect) },
            null
        );
        private static readonly MethodInfo Update = Helper.GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        private static readonly MethodInfo Disable = Helper.GetMethod(
            "OnDisable",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        private static readonly MethodInfo Enable = Helper.GetMethod(
            "OnEnable",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        private static int _checks;

        static PineSafeAreaChecks() => EditorApplication.playModeStateChanged += State;

        public static void RunPlay()
        {
            SessionState.SetBool(Request, true);
            SessionState.SetInt(Request + ".Exit", 1);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }

        private static void State(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Request, false))
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
                EditorApplication.delayCall += VerifyLifecycle;
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Request, false);
                EditorApplication.Exit(SessionState.GetInt(Request + ".Exit", 1));
            }
        }

        private static void VerifyLifecycle()
        {
            try
            {
                Check(Application.isPlaying, "Lifecycle checks execute in Play mode.");
                VerifyPropertyOrder();
                var content = P.Ref<RectTransform>();
                var parent = P.Ref<RectTransform>();
                var minimum = new Vector2(.2f, .3f);
                var maximum = new Vector2(.8f, .9f);
                var offsetMin = new Vector2(2, 3);
                var offsetMax = new Vector2(-4, -5);
                using var mount = P.Mount(
                    P.Canvas(
                        renderMode: RenderMode.ScreenSpaceOverlay,
                        children: new[]
                        {
                            P.Frame(
                                anchorMin: new Vector2(.5f, 0),
                                anchorMax: Vector2.one,
                                offsetMin: Vector2.zero,
                                offsetMax: Vector2.zero,
                                reference: parent,
                                children: new[]
                                {
                                    P.Frame(
                                        anchorMin: minimum,
                                        anchorMax: maximum,
                                        offsetMin: offsetMin,
                                        offsetMax: offsetMax,
                                        reference: content,
                                        configure: rect => P.Apply(rect, P.SafeAreaProperty(false))
                                    ),
                                }
                            ),
                        }
                    )
                );
                Canvas.ForceUpdateCanvases();
                Check(
                    mount.Canvas.renderMode == RenderMode.ScreenSpaceOverlay,
                    "Lifecycle fixture declares a screen-space overlay Canvas."
                );
                var helper = (Behaviour)content.Value.GetComponent(Helper);
                Check(
                    !helper.enabled && content.Value.anchorMin == minimum,
                    "Initially disabled helper preserves supplied layout."
                );
                helper.enabled = true;
                CheckRect(
                    content.Value,
                    parent.Value,
                    null,
                    Screen.safeArea,
                    "Unity OnEnable fits native geometry"
                );
                helper.enabled = false;
                CheckRestored();
                parent.Value.sizeDelta += new Vector2(-Screen.width * .1f, 0);
                helper.enabled = true;
                CheckRect(
                    content.Value,
                    parent.Value,
                    null,
                    Screen.safeArea,
                    "Unity re-enable fits changed parent geometry"
                );
                content.Value.gameObject.SetActive(false);
                CheckRestored();
                content.Value.gameObject.SetActive(true);
                CheckRect(
                    content.Value,
                    parent.Value,
                    null,
                    Screen.safeArea,
                    "Unity GameObject reactivation refits native geometry"
                );
                Debug.Log("PINE_SAFE_AREA_PLAY_PASSED " + _checks);
                SessionState.SetInt(Request + ".Exit", 0);

                void CheckRestored() =>
                    Check(
                        Vector2.Distance(content.Value.anchorMin, minimum) < .00001f
                            && Vector2.Distance(content.Value.anchorMax, maximum) < .00001f
                            && Vector2.Distance(content.Value.offsetMin, offsetMin) < .01f
                            && Vector2.Distance(content.Value.offsetMax, offsetMax) < .01f,
                        "Unity OnDisable restores supplied layout."
                    );
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            EditorApplication.isPlaying = false;
        }

        private static void VerifyPropertyOrder()
        {
            var reference = P.Ref<RectTransform>();
            var minimum = new Vector2(.2f, .3f);
            var maximum = new Vector2(.8f, .9f);
            var offsetMin = new Vector2(2, 3);
            var offsetMax = new Vector2(-4, -5);
            using var mount = P.Mount(
                P.Declare<RectTransform>(
                    reference: reference,
                    properties: new IProperty<RectTransform>[]
                    {
                        P.SafeAreaProperty(true),
                        P.Anchors(minimum, maximum),
                        P.Set<RectTransform, Vector2>(
                            "offsetMin",
                            (rect, value) => rect.offsetMin = value,
                            offsetMin
                        ),
                        P.Set<RectTransform, Vector2>(
                            "offsetMax",
                            (rect, value) => rect.offsetMax = value,
                            offsetMax
                        ),
                    },
                    configure: rect =>
                    {
                        var follower = rect.GetComponent(Helper);
                        Check(
                            !rect.gameObject.activeInHierarchy,
                            "Declare configuration runs before activation."
                        );
                        Check(
                            !(bool)
                                Helper
                                    .GetField(
                                        "_driving",
                                        BindingFlags.Instance | BindingFlags.NonPublic
                                    )
                                    .GetValue(follower),
                            "Inactive SafeArea does not capture layout before later properties."
                        );
                    }
                )
            );
            Canvas.ForceUpdateCanvases();
            var helper = (Behaviour)reference.Value.GetComponent(Helper);
            CheckRect(
                reference.Value,
                (RectTransform)reference.Value.parent,
                null,
                Screen.safeArea,
                "SafeArea-first declaration fits after activation"
            );
            helper.enabled = false;
            Check(
                Vector2.Distance(reference.Value.anchorMin, minimum) < .00001f
                    && Vector2.Distance(reference.Value.anchorMax, maximum) < .00001f
                    && Vector2.Distance(reference.Value.offsetMin, offsetMin) < .01f
                    && Vector2.Distance(reference.Value.offsetMax, offsetMax) < .01f,
                "SafeArea-first declaration restores later supplied layout on disable."
            );
        }

        public static void Run()
        {
            try
            {
                CheckMode(RenderMode.ScreenSpaceOverlay);
                CheckMode(RenderMode.ScreenSpaceCamera);
                Debug.Log("PINE_SAFE_AREA_CHECKS_PASSED " + _checks);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        private static void CheckMode(RenderMode mode)
        {
            Camera camera = null;
            using var cameraLifetime = P.Root(() =>
            {
                if (mode != RenderMode.ScreenSpaceCamera)
                    return;
                var owner = new GameObject("Pine safe-area camera");
                P.Cleanup(owner);
                camera = owner.AddComponent<Camera>();
                camera.orthographic = true;
                camera.transform.position = new Vector3(0, 0, -10);
            });
            var first = P.Ref<RectTransform>();
            var second = P.Ref<RectTransform>();
            var content = P.Ref<RectTransform>();
            var canvas = P.Ref<Canvas>();
            var originalMin = new Vector2(.2f, .3f);
            var originalMax = new Vector2(.8f, .9f);
            var originalOffsetMin = new Vector2(2, 3);
            var originalOffsetMax = new Vector2(-4, -5);
            using var mount = P.Mount(
                P.Canvas(
                    renderMode: mode,
                    worldCamera: camera,
                    reference: canvas,
                    children: new[]
                    {
                        P.Frame(
                            anchorMin: new Vector2(.5f, 0),
                            anchorMax: Vector2.one,
                            offsetMin: Vector2.zero,
                            offsetMax: Vector2.zero,
                            reference: first,
                            children: new[]
                            {
                                P.Frame(
                                    anchorMin: originalMin,
                                    anchorMax: originalMax,
                                    offsetMin: originalOffsetMin,
                                    offsetMax: originalOffsetMax,
                                    reference: content,
                                    configure: rect => P.Apply(rect, P.SafeAreaProperty(true))
                                ),
                            }
                        ),
                        P.Frame(
                            anchorMin: Vector2.zero,
                            anchorMax: Vector2.one,
                            offsetMin: Vector2.zero,
                            offsetMax: Vector2.zero,
                            reference: second
                        ),
                    }
                )
            );
            Canvas.ForceUpdateCanvases();
            var helper = (Behaviour)content.Value.GetComponent(Helper);
            Check(helper != null, "Public SafeAreaProperty creates its helper.");
            var safe = new Rect(
                Screen.width * .1f,
                Screen.height * .1f,
                Screen.width * .8f,
                Screen.height * .8f
            );
            Apply(helper, safe);
            Check(
                Vector2.Distance(Saved(helper, "_anchorMin"), originalMin) < .00001f
                    && Vector2.Distance(Saved(helper, "_anchorMax"), originalMax) < .00001f
                    && Vector2.Distance(Saved(helper, "_offsetMin"), originalOffsetMin) < .01f
                    && Vector2.Distance(Saved(helper, "_offsetMax"), originalOffsetMax) < .01f,
                "The follower captures supplied layout after declarative frame properties."
            );
            CheckRect(content.Value, first.Value, camera, safe, "Partial-screen parent");

            first.Value.localScale = new Vector3(.75f, .6f, 1);
            first.Value.anchoredPosition += new Vector2(Screen.width * .02f, Screen.height * .02f);
            Apply(helper, safe);
            CheckRect(content.Value, first.Value, camera, safe, "Translated and scaled parent");

            first.Value.sizeDelta += new Vector2(-Screen.width * .03f, -Screen.height * .03f);
            Apply(helper, safe);
            CheckRect(content.Value, first.Value, camera, safe, "Resized parent");

            if (camera != null)
            {
                camera.rect = new Rect(.1f, .1f, .8f, .8f);
                camera.orthographicSize *= 1.25f;
                Canvas.ForceUpdateCanvases();
                Apply(helper, safe);
                CheckRect(
                    content.Value,
                    first.Value,
                    camera,
                    safe,
                    "Changed camera viewport/projection"
                );
                canvas.Value.renderMode = RenderMode.ScreenSpaceOverlay;
                Canvas.ForceUpdateCanvases();
                Apply(helper, safe);
                CheckRect(content.Value, first.Value, null, safe, "Canvas changes to overlay");
                canvas.Value.renderMode = mode;
                camera.rect = new Rect(0, 0, 1, 1);
                Canvas.ForceUpdateCanvases();
                Apply(helper, safe);
                CheckRect(content.Value, first.Value, camera, safe, "Canvas returns to its camera");
            }

            content.Value.SetParent(second.Value, false);
            Apply(helper, safe);
            CheckRect(content.Value, second.Value, camera, safe, "Reparented safe area");

            var full = new Rect(0, 0, Screen.width, Screen.height);
            Apply(helper, full);
            CheckRect(content.Value, second.Value, camera, full, "No-cutout Player window");

            second.Value.anchorMin = new Vector2(.75f, 0);
            second.Value.anchorMax = new Vector2(1.25f, 1);
            second.Value.offsetMin = second.Value.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            Apply(helper, Screen.safeArea);
            second.Value.anchoredPosition += new Vector2(Screen.width * .1f, 0);
            Update.Invoke(helper, null);
            CheckRect(
                content.Value,
                second.Value,
                camera,
                Screen.safeArea,
                "Update detects parent movement"
            );

            var minBeforeResize = content.Value.anchorMin;
            var maxBeforeResize = content.Value.anchorMax;
            second.Value.sizeDelta += new Vector2(Screen.width * .1f, 0);
            Update.Invoke(helper, null);
            CheckRect(
                content.Value,
                second.Value,
                camera,
                Screen.safeArea,
                "Update detects parent dimensions"
            );
            Check(
                content.Value.anchorMin != minBeforeResize
                    || content.Value.anchorMax != maxBeforeResize,
                "Parent dimensions change the fitted anchors."
            );

            helper.enabled = false;
            Disable.Invoke(helper, null);
            Check(
                Vector2.Distance(content.Value.anchorMin, originalMin) < .00001f
                    && Vector2.Distance(content.Value.anchorMax, originalMax) < .00001f
                    && Vector2.Distance(content.Value.offsetMin, originalOffsetMin) < .01f
                    && Vector2.Distance(content.Value.offsetMax, originalOffsetMax) < .01f,
                "The disable callback restores the supplied rect layout."
            );
            helper.enabled = true;
            Enable.Invoke(helper, null);
            CheckRect(
                content.Value,
                second.Value,
                camera,
                Screen.safeArea,
                "The enable callback refreshes current geometry"
            );

            second.Value.localRotation = Quaternion.Euler(0, 0, 15);
            ExpectRejected(helper, safe, "Rotated parent is rejected explicitly.");
            second.Value.localRotation = Quaternion.identity;
            canvas.Value.renderMode = RenderMode.WorldSpace;
            ExpectRejected(helper, safe, "World-space Canvas is rejected explicitly.");
            canvas.Value.renderMode = mode;
        }

        private static void Apply(Behaviour helper, Rect safe) =>
            Refresh.Invoke(helper, new object[] { safe });

        private static Vector2 Saved(Behaviour helper, string name) =>
            (Vector2)
                Helper
                    .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(helper);

        private static void ExpectRejected(Behaviour helper, Rect safe, string message)
        {
            bool rejected = false;
            try
            {
                Apply(helper, safe);
            }
            catch (TargetInvocationException error)
                when (error.InnerException is InvalidOperationException)
            {
                rejected = true;
            }
            Check(rejected, message);
        }

        private static void CheckRect(
            RectTransform rect,
            RectTransform parent,
            Camera camera,
            Rect safe,
            string message
        )
        {
            var parentBottom = RectTransformUtility.WorldToScreenPoint(
                camera,
                parent.TransformPoint(parent.rect.min)
            );
            var parentTop = RectTransformUtility.WorldToScreenPoint(
                camera,
                parent.TransformPoint(parent.rect.max)
            );
            var actualBottom = RectTransformUtility.WorldToScreenPoint(
                camera,
                rect.TransformPoint(rect.rect.min)
            );
            var actualTop = RectTransformUtility.WorldToScreenPoint(
                camera,
                rect.TransformPoint(rect.rect.max)
            );
            var expectedBottom = new Vector2(
                Mathf.Clamp(safe.xMin, parentBottom.x, parentTop.x),
                Mathf.Clamp(safe.yMin, parentBottom.y, parentTop.y)
            );
            var expectedTop = new Vector2(
                Mathf.Clamp(safe.xMax, parentBottom.x, parentTop.x),
                Mathf.Clamp(safe.yMax, parentBottom.y, parentTop.y)
            );
            Check(
                Vector2.Distance(actualBottom, expectedBottom) < .1f
                    && Vector2.Distance(actualTop, expectedTop) < .1f,
                message + " maps/clamps synthetic pixels into native parent geometry."
            );
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
            _checks++;
        }
    }
}
