using System;
using UnityEngine.UIElements;

namespace Pine.UIToolkit
{
    public static partial class P
    {
        /// <summary>Draws with the native mesh API and requests repaint when tracked sources change.</summary>
        public static Event Draw(Action<MeshGenerationContext> draw) =>
            new(
                (node, element) =>
                {
                    node.Claim("generateVisualContent");
                    var observer = new DrawingObserver(element, draw);
                    element.generateVisualContent += observer.Paint;
                    Core.Cleanup(() => element.generateVisualContent -= observer.Paint);
                    element.MarkDirtyRepaint();
                }
            );
    }

    internal sealed class DrawingObserver : Observer
    {
        private readonly VisualElement _element;
        private readonly Action<MeshGenerationContext> _draw;

        internal DrawingObserver(VisualElement element, Action<MeshGenerationContext> draw)
        {
            _element = element;
            _draw = draw ?? throw new ArgumentNullException(nameof(draw));
        }

        internal override void Invalidate()
        {
            if (!IsDisposed)
                _element.MarkDirtyRepaint();
        }

        internal void Paint(MeshGenerationContext context)
        {
            if (IsDisposed)
                return;
            ClearDependencies();
            Observer previous = ReactiveRuntime.Observer;
            ReactiveRuntime.Observer = this;
            try
            {
                Owner.Run(() => _draw(context));
            }
            finally
            {
                ReactiveRuntime.Observer = previous;
            }
        }
    }
}
