#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Pine.UIToolkit
{
    public static partial class P
    {
        /// <summary>Installs native SerializedObject bindings on this declaration's subtree.</summary>
        public static Binding SerializedBinding(SerializedObject serialized) =>
            new(
                (node, element) =>
                {
                    if (serialized == null)
                        throw new ArgumentNullException(nameof(serialized));
                    ClaimSerialized(element, node.Scope);
                    element.Bind(serialized);
                    Core.Cleanup(element.Unbind);
                }
            );

        private static void ClaimSerialized(VisualElement element, Scope scope)
        {
            if (element is IBindable bindable && !string.IsNullOrEmpty(bindable.bindingPath))
            {
                if (bindable.binding != null)
                    throw new InvalidOperationException(
                        "A serialized field already has a native binding."
                    );
                var target = new Node(element, scope);
                target.ClaimValue(element is TextElement ? "text" : "value");
            }
            foreach (var child in element.hierarchy.Children())
                ClaimSerialized(child, scope);
        }
    }
}
#endif
