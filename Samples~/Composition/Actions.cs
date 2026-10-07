using System;
using Pine;
using Pine.uGUI;

namespace PineComposition.Examples
{
    public static class Actions
    {
        public static View Save(Value<bool> canSave, Action save) =>
            P.Button(
                "Save",
                onClick: save,
                interactable: canSave,
                children: new[] { P.LayoutElement(preferredHeight: 40) }
            );
    }
}
