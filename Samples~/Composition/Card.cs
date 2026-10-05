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
                gap: 8,
                UI.Label(
                    text: title,
                    UI.FontSize(size: 24),
                    UI.Size(width: 420, height: 36)
                ),
                UI.Column(gap: 8, children)
            );
        }
    }
}
