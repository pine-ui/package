using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pine
{
    public static partial class UI
    {
        /// <summary>Creates an owned Unity behaviour and renders its native UI in a child reactive scope. Generated Components factories call this helper automatically. The behaviour is a layout-ignored child of the returned UI, so disabling or destroying that UI also affects the behaviour. Render initializes the instance before Unity invokes Awake and OnEnable. The renderer executes once; bindings update retained native objects.</summary>
        /// <typeparam name="TBehaviour">A concrete, non-generic MonoBehaviour used by this component.</typeparam>
        /// <typeparam name="TView">The native component returned by the renderer.</typeparam>
        /// <param name="render">Typed instance renderer, called once inside its owned scope.</param>
        /// <returns>The renderer's native UI root, ready to compose with other components.</returns>
        /// <remarks>Usually call the generated Components factory instead. This helper requires a live construction scope. It cleans up both the behaviour and all rendered bindings when the view or enclosing scope ends.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// // A generated factory has this shape; ordinary application code calls Components.Counter().
        /// var view = UI.Component<Counter, UnityEngine.RectTransform>(render: counter =>
        ///     counter.Create()
        /// );
        /// ]]></code>
        /// </example>
        public static TView Component<TBehaviour, TView>(Func<TBehaviour, TView> render)
            where TBehaviour : MonoBehaviour
            where TView : Component
        {
            if (render == null) throw new ArgumentNullException(nameof(render));
            RequireScope();
            TView view = null;
            var scope = OwnedRoot(() =>
            {
                var owner = new GameObject(typeof(TBehaviour).Name + " behaviour", typeof(RectTransform));
                owner.SetActive(false);
                Cleanup(owner);
                var behaviour = owner.AddComponent<TBehaviour>();
                view = render(behaviour);
                if (view == null) throw new InvalidOperationException("A component must return a live native UI component.");
                if (view.gameObject == owner)
                    throw new InvalidOperationException("A component must return its declared UI, not its behaviour owner.");
                owner.AddComponent<LayoutElement>().ignoreLayout = true;
                owner.transform.SetParent(view.transform, false);
                ((RectTransform)owner.transform).sizeDelta = Vector2.zero;
                owner.SetActive(true);
            });
            try
            {
                view.gameObject.AddComponent<MountLifetime>().Scope = scope;
                RuntimeHost.Ensure();
                RuntimeHost.Observe(new Mount { Scope = scope, Root = view.gameObject });
                return view;
            }
            catch { scope.Dispose(); throw; }
        }
    }
}
