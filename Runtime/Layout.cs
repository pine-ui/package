using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pine.uGUI
{
    public static partial class P
    {
        /// <summary>Requests exact non-negative finite width and height in both RectTransform and native LayoutElement.</summary>
        public static IProperty<Component> Size(Value<Vector2> size) =>
            new Property<Component>(
                target =>
                    BindValue(
                        target,
                        size,
                        (item, value) =>
                        {
                            ValidateDimension(value.x);
                            ValidateDimension(value.y);
                            SetAxis(item, 0, value.x, SizingMode.Exact);
                            SetAxis(item, 1, value.y, SizingMode.Exact);
                        }
                    ),
                "Layout.Size"
            );

        /// <summary>Requests exact non-negative finite width and height in both RectTransform and native LayoutElement.</summary>
        public static IProperty<Component> Size(float width, float height) =>
            Size(new Vector2(width, height));

        /// <summary>Requests exact non-negative finite width and height in both RectTransform and native LayoutElement.</summary>
        public static IProperty<Component> Size(Func<Vector2> size) =>
            Size(new Value<Vector2>(size));

        /// <summary>Requests one exact non-negative finite axis in both native rect and layout sizing.</summary>
        public static IProperty<Component> Width(Value<float> width) =>
            new Property<Component>(
                target =>
                    BindValue(
                        target,
                        width,
                        (item, value) =>
                        {
                            ValidateDimension(value);
                            SetAxis(item, 0, value, SizingMode.Exact);
                        }
                    ),
                "Layout.Width"
            );

        /// <summary>Requests one exact non-negative finite axis in both native rect and layout sizing.</summary>
        public static IProperty<Component> Width(Func<float> width) =>
            Width(new Value<float>(width));

        /// <summary>Requests one exact non-negative finite axis in both native rect and layout sizing.</summary>
        public static IProperty<Component> Height(Value<float> height) =>
            new Property<Component>(
                target =>
                    BindValue(
                        target,
                        height,
                        (item, value) =>
                        {
                            ValidateDimension(value);
                            SetAxis(item, 1, value, SizingMode.Exact);
                        }
                    ),
                "Layout.Height"
            );

        /// <summary>Requests one exact non-negative finite axis in both native rect and layout sizing.</summary>
        public static IProperty<Component> Height(Func<float> height) =>
            Height(new Value<float>(height));

        /// <summary>Explicitly fills available space on both axes, using layout flexibility under a layout group and stretching under a plain parent.</summary>
        public static IProperty<Component> Fill() => Group<Component>(FillWidth(), FillHeight());

        /// <summary>Fills available width, using native layout flexibility or parent stretching.</summary>
        public static IProperty<Component> FillWidth() =>
            new Property<Component>(
                target => SetAxis(target, 0, 0, SizingMode.Fill),
                "Layout.Width"
            );

        /// <summary>Fills available height, using native layout flexibility or parent stretching.</summary>
        public static IProperty<Component> FillHeight() =>
            new Property<Component>(
                target => SetAxis(target, 1, 0, SizingMode.Fill),
                "Layout.Height"
            );

        /// <summary>Sizes both axes from native content preference rather than specifying exact dimensions.</summary>
        public static IProperty<Component> Auto() => Group<Component>(AutoWidth(), AutoHeight());

        /// <summary>Sizes width from native content preference.</summary>
        public static IProperty<Component> AutoWidth() =>
            new Property<Component>(
                target => SetAxis(target, 0, -1, SizingMode.Content),
                "Layout.Width"
            );

        /// <summary>Sizes height from native content preference.</summary>
        public static IProperty<Component> AutoHeight() =>
            new Property<Component>(
                target => SetAxis(target, 1, -1, SizingMode.Content),
                "Layout.Height"
            );

        // Kept as a layout-preference operation for native extension code; normal declarations use Size.
        /// <summary>Sets native LayoutElement preferred dimensions without imposing exact minima.</summary>
        public static IProperty<Component> PreferredSize(Value<Vector2> size) =>
            new Property<Component>(
                target =>
                    BindValue(
                        GetOrAdd<LayoutElement>(target.gameObject),
                        size,
                        (item, value) =>
                        {
                            ValidateDimension(value.x);
                            ValidateDimension(value.y);
                            item.preferredWidth = value.x;
                            item.preferredHeight = value.y;
                        }
                    ),
                "Layout.PreferredSize"
            );

        /// <summary>Sets native LayoutElement preferred dimensions without imposing exact minima.</summary>
        public static IProperty<Component> PreferredSize(float width, float height) =>
            PreferredSize(new Vector2(width, height));

        /// <summary>Sets native LayoutElement preferred dimensions without imposing exact minima.</summary>
        public static IProperty<Component> PreferredSize(Func<Vector2> size) =>
            PreferredSize(new Value<Vector2>(size));

        /// <summary>Binds RectTransform.anchoredPosition.</summary>
        public static IProperty<Component> Position(Value<Vector2> position) =>
            new Property<Component>(
                target =>
                    BindValue(
                        Require<RectTransform>(target.gameObject),
                        position,
                        (item, value) => item.anchoredPosition = value
                    ),
                "Layout.Position"
            );

        /// <summary>Binds RectTransform.anchoredPosition.</summary>
        public static IProperty<Component> Position(float x, float y) =>
            Position(new Vector2(x, y));

        /// <summary>Binds RectTransform.anchoredPosition.</summary>
        public static IProperty<Component> Position(Func<Vector2> position) =>
            Position(new Value<Vector2>(position));

        /// <summary>Binds normalized minimum and maximum anchors.</summary>
        public static IProperty<Component> Anchors(
            Value<Vector2> minimum,
            Value<Vector2> maximum
        ) =>
            new Property<Component>(
                target =>
                {
                    var rect = Require<RectTransform>(target.gameObject);
                    BindValue(rect, minimum, (item, value) => item.anchorMin = value);
                    BindValue(rect, maximum, (item, value) => item.anchorMax = value);
                },
                "Layout.Anchors"
            );

        /// <summary>Binds the normalized native RectTransform pivot.</summary>
        public static IProperty<Component> Pivot(Value<Vector2> pivot) =>
            new Property<Component>(
                target =>
                    BindValue(
                        Require<RectTransform>(target.gameObject),
                        pivot,
                        (item, value) => item.pivot = value
                    ),
                "Layout.Pivot"
            );

        /// <summary>Binds the normalized native RectTransform pivot.</summary>
        public static IProperty<Component> Pivot(Func<Vector2> pivot) =>
            Pivot(new Value<Vector2>(pivot));

        /// <summary>Sets anchors to zero/one and offsets to zero once.</summary>
        public static IProperty<Component> Stretch() =>
            new Property<Component>(
                target =>
                {
                    var rect = Require<RectTransform>(target.gameObject);
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                },
                "Layout.Stretch"
            );

        /// <summary>Adds or reuses RectMask2D and binds its enabled state.</summary>
        public static IProperty<Component> Clip(Value<bool> enabled) =>
            new Property<Component>(
                target =>
                    BindValue(
                        GetOrAdd<RectMask2D>(target.gameObject),
                        enabled,
                        (item, value) => item.enabled = value
                    ),
                "Layout.Clip"
            );

        /// <summary>Adds or reuses RectMask2D and binds its enabled state.</summary>
        public static IProperty<Component> Clip(Func<bool> enabled) =>
            Clip(new Value<bool>(enabled));

        /// <summary>Binds native horizontal/vertical layout padding on a frame.</summary>
        public static IProperty<RectTransform> Padding(Value<RectOffset> padding) =>
            new Property<RectTransform>(
                target =>
                    BindValue(
                        (
                            target.GetComponent<HorizontalOrVerticalLayoutGroup>()
                            ?? GetOrAdd<VerticalLayoutGroup>(target.gameObject)
                        ),
                        padding,
                        (item, value) => item.padding = value ?? new RectOffset()
                    ),
                "Layout.Padding"
            );

        /// <summary>Binds native horizontal/vertical layout padding on a frame.</summary>
        public static IProperty<RectTransform> Padding(Func<RectOffset> padding) =>
            Padding(new Value<RectOffset>(padding));

        /// <summary>Adds or reuses a vertical native layout group with controlled child sizing and explicit flexible expansion.</summary>
        public static IProperty<RectTransform> VerticalProperty() => VerticalProperty(8f);

        /// <summary>Adds or reuses a vertical native layout group with controlled child sizing and explicit flexible expansion.</summary>
        public static IProperty<RectTransform> VerticalProperty(Value<float> spacing) =>
            new Property<RectTransform>(
                target =>
                {
                    var group = GetOrAdd<VerticalLayoutGroup>(target.gameObject);
                    ConfigureLayout(group);
                    BindValue(group, spacing, (item, value) => item.spacing = value);
                },
                "Layout.Vertical",
                priority: 0
            );

        /// <summary>Adds or reuses a vertical native layout group with controlled child sizing and explicit flexible expansion.</summary>
        public static IProperty<RectTransform> VerticalProperty(Func<float> spacing) =>
            VerticalProperty(new Value<float>(spacing));

        /// <summary>Adds or reuses a horizontal native layout group with controlled child sizing and explicit flexible expansion.</summary>
        public static IProperty<RectTransform> HorizontalProperty() => HorizontalProperty(8f);

        /// <summary>Adds or reuses a horizontal native layout group with controlled child sizing and explicit flexible expansion.</summary>
        public static IProperty<RectTransform> HorizontalProperty(Value<float> spacing) =>
            new Property<RectTransform>(
                target =>
                {
                    var group = GetOrAdd<HorizontalLayoutGroup>(target.gameObject);
                    ConfigureLayout(group);
                    BindValue(group, spacing, (item, value) => item.spacing = value);
                },
                "Layout.Horizontal",
                priority: 0
            );

        /// <summary>Adds or reuses a horizontal native layout group with controlled child sizing and explicit flexible expansion.</summary>
        public static IProperty<RectTransform> HorizontalProperty(Func<float> spacing) =>
            HorizontalProperty(new Value<float>(spacing));

        private static void ConfigureLayout(HorizontalOrVerticalLayoutGroup group)
        {
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = group.childForceExpandHeight = false;
        }

        /// <summary>Binds uniform grid dimensions shared by every cell.</summary>
        public static IProperty<GridLayoutGroup> CellSize(Value<Vector2> size) =>
            Set<GridLayoutGroup, Vector2>(
                "CellSize",
                (target, value) =>
                {
                    ValidateDimension(value.x);
                    ValidateDimension(value.y);
                    GetOrAdd<PineGrid>(target.gameObject).Validate(value);
                    target.cellSize = value;
                },
                size
            );

        /// <summary>Binds uniform grid dimensions shared by every cell.</summary>
        public static IProperty<GridLayoutGroup> CellSize(Func<Vector2> size) =>
            CellSize(new Value<Vector2>(size));

        /// <summary>Binds horizontal and vertical native grid spacing.</summary>
        public static IProperty<GridLayoutGroup> GridSpacing(Value<Vector2> spacing) =>
            Set<GridLayoutGroup, Vector2>(
                "Spacing",
                (target, value) => target.spacing = value,
                spacing
            );

        /// <summary>Binds horizontal and vertical native grid spacing.</summary>
        public static IProperty<GridLayoutGroup> GridSpacing(Func<Vector2> spacing) =>
            GridSpacing(new Value<Vector2>(spacing));

        /// <summary>Binds native uniform-grid padding.</summary>
        public static IProperty<GridLayoutGroup> GridPadding(Value<RectOffset> padding) =>
            Set<GridLayoutGroup, RectOffset>(
                "Padding",
                (target, value) => target.padding = value ?? new RectOffset(),
                padding
            );

        /// <summary>Adds or reuses an allocation-free native safe-area follower, with reactive enablement.</summary>
        public static IProperty<Component> SafeAreaProperty(Value<bool> enabled) =>
            new Property<Component>(
                target =>
                    BindValue(
                        GetOrAdd<PineSafeArea>(target.gameObject),
                        enabled,
                        (item, value) =>
                        {
                            item.enabled = value;
                            if (value)
                                item.Refresh();
                        }
                    ),
                "Layout.SafeArea"
            );

        /// <summary>Adds or reuses an allocation-free native safe-area follower, with reactive enablement.</summary>
        public static IProperty<Component> SafeAreaProperty(Func<bool> enabled) =>
            SafeAreaProperty(new Value<bool>(enabled));

        internal static void ValidateDimension(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Dimensions must be finite and non-negative."
                );
        }

        private enum SizingMode
        {
            Exact,
            Fill,
            Content,
        }

        private static void SetAxis(Component target, int axis, float dimension, SizingMode mode)
        {
            var rect = Require<RectTransform>(target.gameObject);
            var sizing = GetOrAdd<PineSize>(target.gameObject);
            var layout = GetOrAdd<LayoutElement>(target.gameObject);
            float exact = mode == SizingMode.Exact ? dimension : float.NaN;
            if (rect.parent != null && rect.parent.TryGetComponent<PineGrid>(out var grid))
                grid.ValidateChild(rect, axis, exact);
            if (axis == 0)
            {
                sizing.ExactWidth = exact;
                layout.minWidth =
                    mode == SizingMode.Exact ? dimension
                    : mode == SizingMode.Fill ? 0
                    : -1;
                layout.preferredWidth =
                    mode == SizingMode.Exact ? dimension
                    : mode == SizingMode.Fill ? 0
                    : -1;
                layout.flexibleWidth =
                    mode == SizingMode.Fill ? 1
                    : mode == SizingMode.Content ? -1
                    : 0;
            }
            else
            {
                sizing.ExactHeight = exact;
                layout.minHeight =
                    mode == SizingMode.Exact ? dimension
                    : mode == SizingMode.Fill ? 0
                    : -1;
                layout.preferredHeight =
                    mode == SizingMode.Exact ? dimension
                    : mode == SizingMode.Fill ? 0
                    : -1;
                layout.flexibleHeight =
                    mode == SizingMode.Fill ? 1
                    : mode == SizingMode.Content ? -1
                    : 0;
            }
            var fitter =
                mode == SizingMode.Content
                    ? GetOrAdd<ContentSizeFitter>(target.gameObject)
                    : target.GetComponent<ContentSizeFitter>();
            var fit =
                mode == SizingMode.Content
                    ? UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize
                    : UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            if (fitter != null)
            {
                if (axis == 0)
                    fitter.horizontalFit = fit;
                else
                    fitter.verticalFit = fit;
            }
            if (mode == SizingMode.Exact)
            {
                if (axis == 0)
                    rect.anchorMax = new Vector2(rect.anchorMin.x, rect.anchorMax.y);
                else
                    rect.anchorMax = new Vector2(rect.anchorMax.x, rect.anchorMin.y);
                rect.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, dimension);
            }
            else if (mode == SizingMode.Fill)
            {
                if (axis == 0)
                {
                    rect.anchorMin = new Vector2(0, rect.anchorMin.y);
                    rect.anchorMax = new Vector2(1, rect.anchorMax.y);
                    rect.offsetMin = new Vector2(0, rect.offsetMin.y);
                    rect.offsetMax = new Vector2(0, rect.offsetMax.y);
                }
                else
                {
                    rect.anchorMin = new Vector2(rect.anchorMin.x, 0);
                    rect.anchorMax = new Vector2(rect.anchorMax.x, 1);
                    rect.offsetMin = new Vector2(rect.offsetMin.x, 0);
                    rect.offsetMax = new Vector2(rect.offsetMax.x, 0);
                }
            }
        }

        internal static void ValidateGridChild(Transform child)
        {
            if (child.parent != null && child.parent.TryGetComponent<PineGrid>(out var grid))
                grid.ValidateChild(child);
        }
    }
}
