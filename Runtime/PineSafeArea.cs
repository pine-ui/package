using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pine
{
    internal sealed class PineSafeArea : MonoBehaviour
    {
        private Rect _area; private int _width, _height;
        private void Update() { if (_area != Screen.safeArea || _width != Screen.width || _height != Screen.height) Refresh(); }
        internal void Refresh()
        {
            _area = Screen.safeArea; _width = Screen.width; _height = Screen.height;
            if (_width <= 0 || _height <= 0) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(_area.xMin / _width, _area.yMin / _height);
            rect.anchorMax = new Vector2(_area.xMax / _width, _area.yMax / _height); rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
