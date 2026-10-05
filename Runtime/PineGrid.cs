using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pine
{
    internal sealed class PineGrid : MonoBehaviour
    {
        internal void Validate(Vector2 size)
        {
            foreach (Transform child in transform)
            {
                var sizing = child.GetComponent<PineSize>(); if (sizing == null) continue;
                Check(sizing.ExactWidth, size.x); Check(sizing.ExactHeight, size.y);
            }
        }
        internal void ValidateChild(Transform child)
        { var sizing = child.GetComponent<PineSize>(); if (sizing == null) return; Vector2 size = GetComponent<GridLayoutGroup>().cellSize; Check(sizing.ExactWidth, size.x); Check(sizing.ExactHeight, size.y); }
        internal void ValidateChild(Transform child, int axis, float exact) => Check(exact, axis == 0 ? GetComponent<GridLayoutGroup>().cellSize.x : GetComponent<GridLayoutGroup>().cellSize.y);
        private static void Check(float exact, float cell)
        { if (!float.IsNaN(exact) && !Mathf.Approximately(exact, cell)) throw new InvalidOperationException("A grid owns its uniform CellSize; a child's exact Size conflicts with it."); }
    }
}
