using System;
using Pine;
using UnityEngine.UI;

namespace PineComposition.Examples
{
    public static class Actions
    {
        public static Button Save(Value<bool> canSave, Action save)
        {
            return UI.Button(
                text: "Save",
                click: save,
                UI.Enabled(enabled: canSave),
                UI.Size(width: 420, height: 40)
            );
        }
    }
}
