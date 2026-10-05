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
                "Save",
                save,
                UI.Enabled(canSave),
                UI.Size(420, 40)
            );
        }
    }
}
