using System;
using UnityEngine;

namespace Pine
{
    internal sealed class PineSafeArea : MonoBehaviour
    {
        private Rect _area;
        private RectTransform _parent;
        private Canvas _canvas;
        private Camera _camera;
        private Rect _parentRect,
            _cameraRect;
        private Matrix4x4 _parentMatrix,
            _canvasMatrix,
            _cameraMatrix,
            _projection;
        private RenderMode _mode;
        private int _width,
            _height;
        private Vector2 _anchorMin,
            _anchorMax,
            _offsetMin,
            _offsetMax;
        private Vector2 _fittedMin,
            _fittedMax;
        private bool _hasState,
            _driving;

        private void OnEnable() => Refresh();

        private void Update() => Refresh();

        private void OnDisable()
        {
            _hasState = false;
            if (!_driving)
                return;
            var rect = (RectTransform)transform;
            rect.anchorMin = _anchorMin;
            rect.anchorMax = _anchorMax;
            rect.offsetMin = _offsetMin;
            rect.offsetMax = _offsetMax;
            _driving = false;
        }

        internal void Refresh() => Refresh(Screen.safeArea);

        internal void Refresh(Rect safeArea)
        {
            if (
                !enabled
                || !gameObject.activeInHierarchy
                || Screen.width <= 0
                || Screen.height <= 0
            )
                return;
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            var canvas = GetComponentInParent<Canvas>();
            if (parent == null || canvas == null)
                return;
            canvas = canvas.rootCanvas;
            if (canvas.renderMode == RenderMode.WorldSpace)
                throw new InvalidOperationException(
                    "SafeAreaProperty requires a screen-space Canvas."
                );
            var camera =
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && camera == null)
            {
                if (!gameObject.activeInHierarchy)
                    return;
                throw new InvalidOperationException(
                    "SafeAreaProperty requires the Canvas worldCamera."
                );
            }
            var bounds = parent.rect;
            if (bounds.width <= 0 || bounds.height <= 0)
                return;
            if (
                Quaternion.Angle(rect.localRotation, Quaternion.identity) > .001f
                || rect.localScale != Vector3.one
                || !Mathf.Approximately(rect.localPosition.z, 0)
            )
                throw new InvalidOperationException(
                    "SafeAreaProperty requires an unrotated, unscaled rect in its parent's plane."
                );
            var matrix = parent.localToWorldMatrix;
            var canvasMatrix = canvas.transform.localToWorldMatrix;
            var cameraRect = camera != null ? camera.pixelRect : default;
            var cameraMatrix = camera != null ? camera.worldToCameraMatrix : default;
            var projection = camera != null ? camera.projectionMatrix : default;
            if (
                _hasState
                && _area == safeArea
                && _parent == parent
                && _canvas == canvas
                && _camera == camera
                && _mode == canvas.renderMode
                && _width == Screen.width
                && _height == Screen.height
                && _parentRect == bounds
                && _cameraRect == cameraRect
                && _parentMatrix.Equals(matrix)
                && _canvasMatrix.Equals(canvasMatrix)
                && _cameraMatrix.Equals(cameraMatrix)
                && _projection.Equals(projection)
                && rect.anchorMin == _fittedMin
                && rect.anchorMax == _fittedMax
                && rect.offsetMin == Vector2.zero
                && rect.offsetMax == Vector2.zero
            )
                return;
            var relative = canvas.transform.worldToLocalMatrix * matrix;
            if (
                Mathf.Approximately(relative.m00, 0)
                || Mathf.Approximately(relative.m11, 0)
                || Mathf.Abs(relative.m10) > .00001f * Mathf.Abs(relative.m00)
                || Mathf.Abs(relative.m20) > .00001f * Mathf.Abs(relative.m00)
                || Mathf.Abs(relative.m01) > .00001f * Mathf.Abs(relative.m11)
                || Mathf.Abs(relative.m21) > .00001f * Mathf.Abs(relative.m11)
            )
                throw new InvalidOperationException(
                    "SafeAreaProperty requires an unrotated parent relative to its Canvas."
                );
            if (
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    safeArea.min,
                    camera,
                    out var first
                )
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    safeArea.max,
                    camera,
                    out var second
                )
            )
                return;
            if (!_driving)
            {
                _anchorMin = rect.anchorMin;
                _anchorMax = rect.anchorMax;
                _offsetMin = rect.offsetMin;
                _offsetMax = rect.offsetMax;
                _driving = true;
            }
            var minimum = Vector2.Min(first, second);
            var maximum = Vector2.Max(first, second);
            _fittedMin = new Vector2(
                Mathf.Clamp01((minimum.x - bounds.xMin) / bounds.width),
                Mathf.Clamp01((minimum.y - bounds.yMin) / bounds.height)
            );
            _fittedMax = new Vector2(
                Mathf.Clamp01((maximum.x - bounds.xMin) / bounds.width),
                Mathf.Clamp01((maximum.y - bounds.yMin) / bounds.height)
            );
            rect.anchorMin = _fittedMin;
            rect.anchorMax = _fittedMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _area = safeArea;
            _parent = parent;
            _canvas = canvas;
            _camera = camera;
            _mode = canvas.renderMode;
            _width = Screen.width;
            _height = Screen.height;
            _parentRect = bounds;
            _cameraRect = cameraRect;
            _parentMatrix = matrix;
            _canvasMatrix = canvasMatrix;
            _cameraMatrix = cameraMatrix;
            _projection = projection;
            _hasState = true;
        }
    }
}
