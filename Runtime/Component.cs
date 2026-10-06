using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pine
{
    public static partial class P
    {
        /// <summary>Declares an owned Unity behaviour and its deferred UI.</summary>
        public static View Component<TBehaviour>(Func<TBehaviour, View> render) where TBehaviour : MonoBehaviour
        {
            if (render == null) throw new ArgumentNullException(nameof(render));
            return new View(null, null, null, null, false, null, factory: parent =>
            {
                var owner = new GameObject(typeof(TBehaviour).Name + " behaviour", typeof(RectTransform));
                owner.SetActive(false); Cleanup(owner);
                var behaviour = owner.AddComponent<TBehaviour>();
                var declaration = render(behaviour) ?? throw new InvalidOperationException("A component must return a view.");
                var native = declaration.Build(owner.transform, false, out var active);
                native.transform.SetParent(parent, false);
                owner.AddComponent<LayoutElement>().ignoreLayout = true;
                owner.transform.SetParent(native.transform, false);
                ((RectTransform)owner.transform).sizeDelta = Vector2.zero;
                owner.SetActive(true);
                return (native, active);
            });
        }

        /// <summary>Creates an owned Unity behaviour and renders its native UI in a child reactive scope.</summary>
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
