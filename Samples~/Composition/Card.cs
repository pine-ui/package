using Pine;

namespace PineComposition.Examples
{
    public static class Card
    {
        public static View Create(string title, params View[] children) =>
            P.Vertical(
                    spacing: 8,
                    childControlWidth: true,
                    childControlHeight: true,
                    childForceExpandHeight: false
                )
                .With(
                    P.Text(title, fontSize: 24).With(P.LayoutElement(preferredHeight: 36)),
                    P.Vertical(
                            spacing: 8,
                            childControlWidth: true,
                            childControlHeight: true,
                            childForceExpandHeight: false
                        )
                        .With(children)
                );
    }
}
