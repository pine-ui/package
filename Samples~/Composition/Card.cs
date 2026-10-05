using Pine;
using UnityEngine;

namespace PineComposition.Examples
{
    public static class Card
    {
        public static RectTransform Create(
            string title,
            params Component[] children
        )
        {
            return UI.Column(
                8,
                UI.Label(title, UI.FontSize(24), UI.Size(420, 36)),
                UI.Column(8, children)
            );
        }
    }
}
