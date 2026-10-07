using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace Pine.UIToolkit
{
    /// <summary>A typed native collection template with owned recycled item lifetimes.</summary>
    public sealed class Rows
    {
        internal readonly Action<Node, BaseVerticalCollectionView> Apply;

        internal Rows(Action<Node, BaseVerticalCollectionView> apply) => Apply = apply;
    }

    /// <summary>A typed column cell declaration resolved against its table's current data.</summary>
    public sealed class Cell
    {
        internal readonly Type DataType;
        internal readonly Func<object, int, View> Render;

        internal Cell(Type type, Func<object, int, View> render)
        {
            DataType = type;
            Render = render;
        }
    }

    public static partial class P
    {
        private static readonly ConditionalWeakTable<Column, Cell> CellTemplates = new();

        internal static TValue ReadInitial<TValue>(Value<TValue>? value) =>
            value.HasValue ? Core.Untrack(value.Value.Read) : default;

        internal static void Columns(Node node, Columns native, Column[] columns)
        {
            if (columns == null)
                return;
            node.Claim("columns");
            foreach (var column in columns)
                native.Add(column);
            Core.Cleanup(() =>
            {
                foreach (var column in columns)
                    if (native.Contains(column))
                        native.Remove(column);
            });
        }

        internal static void SortColumns(
            Node node,
            SortColumnDescriptions native,
            SortColumnDescription[] columns
        )
        {
            if (columns == null)
                return;
            node.Claim("sortColumnDescriptions");
            foreach (var column in columns)
                native.Add(column);
            Core.Cleanup(() =>
            {
                foreach (var column in columns)
                    native.Remove(column);
            });
        }

        internal static void Cells(Node node, Column column, Cell cell)
        {
            if (cell == null)
                return;
            node.Claim("makeCell");
            node.Claim("bindCell");
            node.Claim("unbindCell");
            node.Claim("destroyCell");
            if (
                column.makeCell != null
                || column.bindCell != null
                || column.unbindCell != null
                || column.destroyCell != null
            )
                throw new InvalidOperationException(
                    "Declare a cell template or native cell callbacks, once."
                );
            CellTemplates.Add(column, cell);
        }

        /// <summary>Declares a column's typed cell template.</summary>
        public static Cell Cell<T>(Func<T, int, View> render) =>
            new(typeof(T), (data, index) => render((T)data, index));

        /// <summary>Declares a column's typed cell template.</summary>
        public static Cell Cell<T>(Func<T, View> render) => Cell<T>((data, _) => render(data));

        /// <summary>Supplies reactive typed list data and an optional recycled row template.</summary>
        public static Rows Rows<T>(Func<IReadOnlyList<T>> items, Func<T, int, View> row = null) =>
            new(
                (node, native) =>
                {
                    if (!(native is BaseListView list))
                        throw new InvalidOperationException(
                            "List rows require a native list view."
                        );
                    node.ClaimValue("itemsSource");
                    IReadOnlyList<T> current = Array.Empty<T>();
                    InstallTemplates(
                        node,
                        native,
                        index => current[index],
                        typeof(T),
                        row == null ? null : (data, index) => row((T)data, index)
                    );
                    Core.Effect(() =>
                    {
                        var values = items() ?? Array.Empty<T>();
                        var snapshot = new List<T>(values.Count);
                        for (int i = 0; i < values.Count; i++)
                            snapshot.Add(values[i]);
                        current = snapshot;
                        Core.Untrack(() =>
                        {
                            list.itemsSource = snapshot;
                            list.RefreshItems();
                        });
                    });
                }
            );

        /// <summary>Supplies typed list data and an optional recycled row template.</summary>
        public static Rows Rows<T>(IReadOnlyList<T> items, Func<T, int, View> row = null) =>
            Rows(() => items, row);

        /// <summary>Supplies reactive typed list data and a recycled row template.</summary>
        public static Rows Rows<T>(Source<IReadOnlyList<T>> items, Func<T, int, View> row = null) =>
            Rows(() => items.Value, row);

        /// <summary>Supplies reactive native tree data while preserving native IDs, selection and expansion.</summary>
        public static Rows TreeRows<T>(
            Func<IReadOnlyList<TreeViewItemData<T>>> roots,
            Func<T, int, View> row = null
        ) =>
            new(
                (node, native) =>
                {
                    if (!(native is BaseTreeView tree))
                        throw new InvalidOperationException(
                            "Tree rows require a native tree view."
                        );
                    node.Claim("rootItems");
                    InstallTemplates(
                        node,
                        tree,
                        index => tree.GetItemDataForIndex<T>(index),
                        typeof(T),
                        row == null ? null : (data, index) => row((T)data, index)
                    );
                    Core.Effect(() =>
                    {
                        var values = roots() ?? Array.Empty<TreeViewItemData<T>>();
                        var snapshot = new List<TreeViewItemData<T>>(values.Count);
                        for (int i = 0; i < values.Count; i++)
                            snapshot.Add(values[i]);
                        Core.Untrack(() =>
                        {
                            tree.SetRootItems(snapshot);
                            tree.RefreshItems();
                        });
                    });
                }
            );

        /// <summary>Supplies native tree data and an optional recycled row template.</summary>
        public static Rows TreeRows<T>(
            IReadOnlyList<TreeViewItemData<T>> roots,
            Func<T, int, View> row = null
        ) => TreeRows(() => roots, row);

        private static void InstallTemplates(
            Node node,
            BaseVerticalCollectionView native,
            Func<int, object> data,
            Type type,
            Func<object, int, View> row
        )
        {
            Columns columns =
                native is MultiColumnListView list ? list.columns
                : native is MultiColumnTreeView tree ? tree.columns
                : null;
            if (columns != null)
            {
                if (row != null)
                    throw new InvalidOperationException(
                        "Multi-column views use column cell templates."
                    );
                foreach (var column in columns)
                {
                    if (!CellTemplates.TryGetValue(column, out var cell))
                        continue;
                    if (cell.DataType != type)
                        throw new InvalidOperationException(
                            "Cell and collection data types must match."
                        );
                    var registry = new RecycledViews(
                        node,
                        index => cell.Render(data(index), index)
                    );
                    column.makeCell = registry.Make;
                    column.bindCell = registry.Bind;
                    column.unbindCell = registry.Unbind;
                    column.destroyCell = registry.Destroy;
                    Core.Cleanup(() =>
                    {
                        if (column.makeCell == registry.Make)
                            column.makeCell = null;
                        if (column.bindCell == registry.Bind)
                            column.bindCell = null;
                        if (column.unbindCell == registry.Unbind)
                            column.unbindCell = null;
                        if (column.destroyCell == registry.Destroy)
                            column.destroyCell = null;
                    });
                }
                return;
            }
            if (row == null)
                return;
            node.Claim("makeItem");
            node.Claim("bindItem");
            node.Claim("unbindItem");
            node.Claim("destroyItem");
            var rows = new RecycledViews(node, index => row(data(index), index));
            if (native is ListView view)
            {
                if (view.makeItem != null || view.bindItem != null)
                    throw new InvalidOperationException("Native row callbacks already exist.");
                view.makeItem = rows.Make;
                view.bindItem = rows.Bind;
                view.unbindItem = rows.Unbind;
                view.destroyItem = rows.Destroy;
                Core.Cleanup(() =>
                {
                    if (view.makeItem == rows.Make)
                        view.makeItem = null;
                    if (view.bindItem == rows.Bind)
                        view.bindItem = null;
                    if (view.unbindItem == rows.Unbind)
                        view.unbindItem = null;
                    if (view.destroyItem == rows.Destroy)
                        view.destroyItem = null;
                });
            }
            else if (native is TreeView treeView)
            {
                if (treeView.makeItem != null || treeView.bindItem != null)
                    throw new InvalidOperationException("Native row callbacks already exist.");
                treeView.makeItem = rows.Make;
                treeView.bindItem = rows.Bind;
                treeView.unbindItem = rows.Unbind;
                treeView.destroyItem = rows.Destroy;
                Core.Cleanup(() =>
                {
                    if (treeView.makeItem == rows.Make)
                        treeView.makeItem = null;
                    if (treeView.bindItem == rows.Bind)
                        treeView.bindItem = null;
                    if (treeView.unbindItem == rows.Unbind)
                        treeView.unbindItem = null;
                    if (treeView.destroyItem == rows.Destroy)
                        treeView.destroyItem = null;
                });
            }
        }
    }

    internal sealed class RecycledViews : IDisposable
    {
        private readonly Node _owner;
        private readonly Func<int, View> _render;
        private readonly Dictionary<VisualElement, Scope> _items = new();

        internal RecycledViews(Node owner, Func<int, View> render)
        {
            _owner = owner;
            _render = render;
            owner.Scope.Own(this);
        }

        internal VisualElement Make() => new();

        internal void Bind(VisualElement shell, int index)
        {
            Destroy(shell);
            P.Scoped(
                _owner,
                () =>
                {
                    Scope scope = Core.OwnedRoot(() =>
                        View.Construct(() =>
                        {
                            var view =
                                _render(index)
                                ?? throw new InvalidOperationException(
                                    "A cell or row must return a View."
                                );
                            var built = view.Build();
                            var element = built.Element;
                            shell.Add(element);
                            Core.Cleanup(() => element.RemoveFromHierarchy());
                        })
                    );
                    _items.Add(shell, scope);
                }
            );
        }

        internal void Unbind(VisualElement shell, int index) => Destroy(shell);

        internal void Destroy(VisualElement shell)
        {
            if (!_items.TryGetValue(shell, out var scope))
                return;
            _items.Remove(shell);
            scope.Dispose();
        }

        public void Dispose()
        {
            foreach (var scope in _items.Values)
                scope.Dispose();
            _items.Clear();
        }
    }
}
