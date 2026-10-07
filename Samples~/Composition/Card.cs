using Pine;
using Pine.uGUI;

namespace PineComposition.Examples
{
    public static class Card
    {
        public static View Create(string title, params View[] children) =>
            P.Vertical(
                spacing: 8,
                childControlWidth: true,
                childControlHeight: true,
                childForceExpandHeight: false,
                children: new[]
                {
                    P.Text(
                        title,
                        fontSize: 24,
                        children: new[] { P.LayoutElement(preferredHeight: 36) }
                    ),
                    P.Vertical(
                        spacing: 8,
                        childControlWidth: true,
                        childControlHeight: true,
                        childForceExpandHeight: false,
                        children: children
                    ),
                }
            );
    }
}
