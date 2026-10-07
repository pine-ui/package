using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

if (args.Length == 4 && args[0] == "--toolkit")
{
    ToolkitApi.Generate(args[1], args[2], Path.GetFullPath(args[3]));
    return;
}

if (args.Length == 2 && args[0] == "--shared")
{
    SharedApi.Generate(Path.GetFullPath(args[1]));
    return;
}

if (args.Length != 3)
    throw new ArgumentException(
        "Usage: NativeApi <Unity Editor/Data> <project Library/ScriptAssemblies> <package root>"
    );
var editor = Path.GetFullPath(args[0]);
var assemblies = Path.GetFullPath(args[1]);
var package = Path.GetFullPath(args[2]);
var paths = Directory
    .GetFiles(Path.Combine(editor, "Managed/UnityEngine"), "*.dll")
    .Concat(
        Directory.GetFiles(Path.Combine(editor, "MonoBleedingEdge/lib/mono/4.7.2-api"), "*.dll")
    )
    .Concat(
        Directory.GetFiles(
            Path.Combine(editor, "MonoBleedingEdge/lib/mono/4.7.2-api/Facades"),
            "*.dll"
        )
    )
    .Concat(
        new[] { "UnityEngine.UI.dll", "Unity.TextMeshPro.dll", "Unity.InputSystem.dll" }.Select(n =>
            Path.Combine(assemblies, n)
        )
    )
    .Where(File.Exists)
    .Distinct()
    .ToArray();
var compilation = CSharpCompilation.Create(
    "NativeInventory",
    references: paths.Select(p => MetadataReference.CreateFromFile(p))
);
var catalog = new (string Factory, string Type, bool Modifier, string? Gate)[]
{
    ("Frame", "UnityEngine.RectTransform", false, null),
    ("Text", "TMPro.TextMeshProUGUI", false, null),
    ("Image", "UnityEngine.UI.Image", false, null),
    ("RawImage", "UnityEngine.UI.RawImage", false, null),
    ("Button", "UnityEngine.UI.Button", false, null),
    ("Selectable", "UnityEngine.UI.Selectable", false, null),
    ("Toggle", "UnityEngine.UI.Toggle", false, null),
    ("Slider", "UnityEngine.UI.Slider", false, null),
    ("Scrollbar", "UnityEngine.UI.Scrollbar", false, null),
    ("InputField", "TMPro.TMP_InputField", false, null),
    ("Dropdown", "TMPro.TMP_Dropdown", false, null),
    ("LegacyText", "UnityEngine.UI.Text", false, null),
    ("LegacyInputField", "UnityEngine.UI.InputField", false, null),
    ("LegacyDropdown", "UnityEngine.UI.Dropdown", false, null),
    ("ScrollRect", "UnityEngine.UI.ScrollRect", false, null),
    ("Vertical", "UnityEngine.UI.VerticalLayoutGroup", false, null),
    ("Horizontal", "UnityEngine.UI.HorizontalLayoutGroup", false, null),
    ("Grid", "UnityEngine.UI.GridLayoutGroup", false, null),
    ("Canvas", "UnityEngine.Canvas", false, null),
    ("CanvasGroup", "UnityEngine.CanvasGroup", true, null),
    ("CanvasScaler", "UnityEngine.UI.CanvasScaler", true, null),
    ("GraphicRaycaster", "UnityEngine.UI.GraphicRaycaster", true, null),
    ("LayoutElement", "UnityEngine.UI.LayoutElement", true, null),
    ("ContentSizeFitter", "UnityEngine.UI.ContentSizeFitter", true, null),
    ("AspectRatioFitter", "UnityEngine.UI.AspectRatioFitter", true, null),
    ("Mask", "UnityEngine.UI.Mask", true, null),
    ("RectMask2D", "UnityEngine.UI.RectMask2D", true, null),
    ("Shadow", "UnityEngine.UI.Shadow", true, null),
    ("Outline", "UnityEngine.UI.Outline", true, null),
    ("PositionAsUV1", "UnityEngine.UI.PositionAsUV1", true, null),
    ("ToggleGroup", "UnityEngine.UI.ToggleGroup", true, null),
    ("CanvasRenderer", "UnityEngine.CanvasRenderer", true, null),
    ("Animator", "UnityEngine.Animator", true, null),
    ("EventTrigger", "UnityEngine.EventSystems.EventTrigger", true, null),
    ("EventSystem", "UnityEngine.EventSystems.EventSystem", false, null),
    ("BaseInput", "UnityEngine.EventSystems.BaseInput", true, null),
    ("PlayerInput", "UnityEngine.InputSystem.PlayerInput", true, "ENABLE_INPUT_SYSTEM"),
    (
        "MultiplayerEventSystem",
        "UnityEngine.InputSystem.UI.MultiplayerEventSystem",
        false,
        "ENABLE_INPUT_SYSTEM"
    ),
    (
        "TrackedDeviceRaycaster",
        "UnityEngine.InputSystem.UI.TrackedDeviceRaycaster",
        true,
        "ENABLE_INPUT_SYSTEM"
    ),
    (
        "VirtualMouseInput",
        "UnityEngine.InputSystem.UI.VirtualMouseInput",
        true,
        "ENABLE_INPUT_SYSTEM"
    ),
    (
        "StandaloneInputModule",
        "UnityEngine.EventSystems.StandaloneInputModule",
        true,
        "ENABLE_LEGACY_INPUT_MANAGER"
    ),
    (
        "InputSystemUIInputModule",
        "UnityEngine.InputSystem.UI.InputSystemUIInputModule",
        true,
        "ENABLE_INPUT_SYSTEM"
    ),
    ("PhysicsRaycaster", "UnityEngine.EventSystems.PhysicsRaycaster", true, null),
    ("Physics2DRaycaster", "UnityEngine.EventSystems.Physics2DRaycaster", true, null),
    ("RaycastReceiver", "UnityEngine.UI.RaycastReceiver", false, "PINE_UGUI_2_5_OR_NEWER"),
    ("SafeArea", "UnityEngine.UI.SafeArea", true, "PINE_UGUI_2_6_OR_NEWER"),
};
var excluded = new HashSet<string>
{
    "parent",
    "hasChanged",
    "hierarchyCapacity",
    "useGUILayout",
    "runInEditMode",
    "destroyCancellationToken",
    "maskType",
    "isUsingLegacyAnimationComponent",
    "isUsingBold",
    "hasPropertiesChanged",
    "isVolumetricText",
    "havePropertiesChanged",
    "fontSizeBase",
    "isUsingLegacyAnimationComponent",
    "isInputParsingRequired",
    "isTextTruncated",
};
var later = new Dictionary<string, string>
{
    ["UnityEngine.UI.LayoutElement.maxWidth"] = "PINE_UGUI_2_6_OR_NEWER",
    ["UnityEngine.UI.LayoutElement.maxHeight"] = "PINE_UGUI_2_6_OR_NEWER",
    ["TMPro.TMP_Text.enableAdvancedText"] = "PINE_UGUI_2_7_OR_NEWER",
    ["UnityEngine.Canvas.useReflectionProbes"] = "UNITY_6000_4_OR_NEWER",
};
var rectNames = new[]
{
    "anchorMin",
    "anchorMax",
    "pivot",
    "anchoredPosition",
    "anchoredPosition3D",
    "sizeDelta",
    "offsetMin",
    "offsetMax",
    "localPosition",
    "localRotation",
    "localEulerAngles",
    "localScale",
};
var rect =
    compilation.GetTypeByMetadataName("UnityEngine.RectTransform")
    ?? throw new InvalidOperationException("RectTransform metadata unavailable.");
var rectProperties = Properties(rect)
    .Where(p => rectNames.Contains(p.Name))
    .ToDictionary(p => p.Name);
var output = new StringBuilder(
    "// <auto-generated/>\nusing System;\nusing UnityEngine;\n#pragma warning disable 0618\nnamespace Pine.uGUI\n{\n    public static partial class P\n    {\n"
);
var inventory = new List<object>();
foreach (var entry in catalog)
{
    var type = compilation.GetTypeByMetadataName(entry.Type);
    if (type == null)
        throw new InvalidOperationException("Missing native type " + entry.Type);
    var props = Properties(type)
        .Where(p => !excluded.Contains(p.Name) && p.Type.SpecialType != SpecialType.System_Void)
        .Select(p => new Prop(
            Alias(entry.Type, p.Name),
            Name(p.Type),
            EventArguments(p.Type),
            Gate(p),
            false,
            NativeMember: p.Name,
            Part: IsPart(p.Type)
        ))
        .ToList();
    props.AddRange(
        Fields(type)
            .Where(f => !excluded.Contains(f.Name) && !props.Any(p => p.Name == f.Name))
            .Select(f => new Prop(
                f.Name,
                Name(f.Type),
                EventArguments(f.Type),
                null,
                false,
                NativeMember: f.Name,
                Part: IsPart(f.Type)
            ))
    );
    props.AddRange(
        Events(type)
            .Select(e => new Prop(
                e.Name,
                Name(e.Type),
                ((INamedTypeSymbol)e.Type)
                    .DelegateInvokeMethod!.Parameters.Select(p => Name(p.Type))
                    .ToArray(),
                null,
                false,
                NativeMember: e.Name,
                NativeEvent: true
            ))
    );
    if (entry.Factory is "StandaloneInputModule" or "InputSystemUIInputModule")
        props.Add(new Prop("sendPointerHoverToParent", "bool", null, null, false));
    if (entry.Type != "UnityEngine.RectTransform")
        props.AddRange(
            rectNames
                .Where(n => !props.Any(p => p.Name == n))
                .Select(n => new Prop(n, Name(rectProperties[n].Type), null, null, true))
        );
    props.Add(new Prop("active", "bool", null, null, false));
    props.Add(new Prop("layer", "int", null, null, false));
    props.Add(new Prop("isStatic", "bool", null, null, false));
    if (entry.Factory == "InputField")
        props.Add(new Prop("regexValue", "string", null, null, false));
    if (entry.Type == "UnityEngine.UI.Button" || entry.Type == "UnityEngine.UI.Toggle")
    {
        props.Insert(0, new Prop("text", "string", null, null, false));
        props.Add(
            new Prop(
                "caption",
                "global::TMPro.TMP_Text",
                null,
                null,
                false,
                Part: true,
                Synthetic: true
            )
        );
    }
    if (entry.Factory == "EventTrigger")
    {
        var triggerType = compilation.GetTypeByMetadataName(
            "UnityEngine.EventSystems.EventTriggerType"
        )!;
        foreach (
            var field in triggerType
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue)
        )
            props.Add(
                new Prop(
                    "on" + field.Name,
                    "global::UnityEngine.EventSystems.BaseEventData",
                    new[] { "global::UnityEngine.EventSystems.BaseEventData" },
                    null,
                    false,
                    NativeMember: field.Name,
                    Synthetic: true,
                    Trigger: true
                )
            );
    }
    if (entry.Factory is "Dropdown" or "LegacyDropdown")
        props.Add(
            new Prop(
                "item",
                "global::UnityEngine.UI.Toggle",
                null,
                null,
                false,
                Part: true,
                Synthetic: true
            )
        );
    if (entry.Factory is "Text" or "LegacyText" or "InputField" or "LegacyInputField")
        props = props.OrderBy(p => p.Name == "text" ? 0 : 1).ToList();
    props = props.OrderBy(p => p.Name == "text" ? -1 : Priority(p.Name)).ToList();
    if (entry.Gate != null)
        output.Append("#if ").Append(entry.Gate).Append('\n');
    EmitFactory(entry.Factory, entry.Type, entry.Modifier, props, false, false);
    if (!entry.Modifier)
        EmitFactory(entry.Factory, entry.Type, entry.Modifier, props, false, true);
    if (entry.Factory is "Text" or "LegacyText" or "Button")
    {
        EmitFactory(entry.Factory, entry.Type, entry.Modifier, props, true, false);
        if (!entry.Modifier)
            EmitFactory(entry.Factory, entry.Type, entry.Modifier, props, true, true);
    }
    if (entry.Gate != null)
        output.Append("#endif\n\n");
    inventory.Add(
        new
        {
            factory = entry.Factory,
            nativeType = entry.Type,
            placement = entry.Modifier ? "component" : "child",
            gate = entry.Gate,
            children = entry.Modifier ? "static" : "static-or-getter",
            properties = props,
        }
    );
}
output.Append("    }\n}\n");
File.WriteAllText(Path.Combine(package, "Runtime/NativeProps.g.cs"), output.ToString());
var format = System.Diagnostics.Process.Start(
    new System.Diagnostics.ProcessStartInfo("dotnet")
    {
        WorkingDirectory = package,
        ArgumentList = { "csharpier", "format", "Runtime/NativeProps.g.cs", "--include-generated" },
    }
)!;
format.WaitForExit();
if (format.ExitCode != 0)
    throw new InvalidOperationException("Restore the pinned formatter with dotnet tool restore.");
File.WriteAllText(
    Path.Combine(package, "Tools~/NativeApi/catalog.json"),
    JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true })
);
Console.WriteLine($"Generated {catalog.Length} native factories from public Unity metadata.");

bool IsPart(ITypeSymbol type)
{
    for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType)
        if (current.ToDisplayString() is "UnityEngine.Component" or "UnityEngine.GameObject")
            return true;
    return false;
}
string Alias(string type, string name) =>
    (type, name) switch
    {
        ("UnityEngine.UI.Slider", "fillRect") => "fill",
        ("UnityEngine.UI.Slider" or "UnityEngine.UI.Scrollbar", "handleRect") => "handle",
        ("TMPro.TMP_InputField", "textViewport") => "viewport",
        _ => name,
    };
IEnumerable<IEventSymbol> Events(INamedTypeSymbol type)
{
    var names = new HashSet<string>();
    for (var current = type; current != null; current = current.BaseType)
        foreach (var item in current.GetMembers().OfType<IEventSymbol>())
            if (
                !item.IsStatic
                && item.DeclaredAccessibility == Accessibility.Public
                && !Obsolete(item)
                && names.Add(item.Name)
            )
                yield return item;
}

IEnumerable<IPropertySymbol> Properties(INamedTypeSymbol type)
{
    var names = new HashSet<string>();
    for (INamedTypeSymbol? current = type; current != null; current = current.BaseType)
        foreach (var prop in current.GetMembers().OfType<IPropertySymbol>())
            if (
                !prop.IsStatic
                && !prop.IsIndexer
                && prop.GetMethod?.DeclaredAccessibility == Accessibility.Public
                && (
                    prop.SetMethod?.DeclaredAccessibility == Accessibility.Public
                    || EventArguments(prop.Type) != null
                )
                && !Obsolete(prop)
                && names.Add(prop.Name)
            )
                yield return prop;
}
IEnumerable<IFieldSymbol> Fields(INamedTypeSymbol type)
{
    for (INamedTypeSymbol? current = type; current != null; current = current.BaseType)
        foreach (var field in current.GetMembers().OfType<IFieldSymbol>())
            if (
                field.DeclaredAccessibility == Accessibility.Public
                && !field.IsStatic
                && !field.IsReadOnly
                && !field.IsConst
                && !Obsolete(field)
            )
                yield return field;
}
bool Obsolete(ISymbol symbol) =>
    symbol
        .GetAttributes()
        .Any(a => a.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute");
string Name(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
string[]? EventArguments(ITypeSymbol type)
{
    for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType)
    {
        if (current.ContainingNamespace.ToDisplayString() != "UnityEngine.Events")
            continue;
        if (current.Name == "UnityEvent")
            return current.TypeArguments.Select(Name).ToArray();
    }
    return null;
}
string? Gate(IPropertySymbol property) =>
    later.GetValueOrDefault(property.ContainingType.ToDisplayString() + "." + property.Name);
int Priority(string name) =>
    name switch
    {
        "contentType" => 0,
        "lineType" => 1,
        "minValue" => 2,
        "maxValue" => 3,
        "wholeNumbers" => 4,
        "options" => 5,
        "actionsAsset" or "runtimeAnimatorController" => 0,
        "value" or "isOn" => 30,
        _ => 10,
    };
void EmitFactory(
    string factory,
    string native,
    bool modifier,
    List<Prop> props,
    bool getter,
    bool reactive
)
{
    var gate = props.FirstOrDefault(p => p.Gate != null)?.Gate;
    if (gate != null)
    {
        output.Append("#if ").Append(gate).Append('\n');
        EmitFactory(
            factory,
            native,
            modifier,
            props.Select(p => p.Gate == gate ? p with { Gate = null } : p).ToList(),
            getter,
            reactive
        );
        output.Append("#else\n");
        EmitFactory(
            factory,
            native,
            modifier,
            props.Where(p => p.Gate != gate).ToList(),
            getter,
            reactive
        );
        output.Append("#endif\n\n");
        return;
    }
    output
        .Append("        /// <summary>")
        .Append(modifier ? "Attaches " : "Creates a ")
        .Append(native)
        .Append(
            modifier
                ? " on the containing object; omitted props keep native defaults."
                : " view; omitted props keep native defaults."
        )
        .Append("</summary>\n        public static View ")
        .Append(factory)
        .Append("(\n");
    if (reactive && !getter)
        output.Append(
            "            Func<global::System.Collections.Generic.IEnumerable<View>> children,\n"
        );
    foreach (var prop in props)
    {
        string type =
            prop.Event != null
                ? "Action"
                    + (prop.Event.Length == 0 ? "" : "<" + string.Join(", ", prop.Event) + ">")
            : prop.Part ? "Part<" + prop.Type + ">?"
            : getter && prop.Name == "text" ? "Func<string>"
            : "Value<" + prop.Type + ">?";
        output
            .Append("            ")
            .Append(type)
            .Append(' ')
            .Append(Escape(prop.Name))
            .Append(getter && prop.Name == "text" ? ",\n" : " = null,\n");
        if (reactive && getter && prop.Name == "text")
            output.Append(
                "            Func<global::System.Collections.Generic.IEnumerable<View>> children,\n"
            );
    }
    if (!reactive)
        output.Append("            View[] children = null,\n");
    output.Append("            View[] components = null,\n");
    output
        .Append("            Action<global::")
        .Append(native)
        .Append("> configure = null,\n            Action<global::")
        .Append(native)
        .Append("> reference = null)\n");
    if (getter)
    {
        output.Append("            => ").Append(factory).Append("(\n");
        foreach (var prop in props)
        {
            output
                .Append("                ")
                .Append(Escape(prop.Name))
                .Append(": ")
                .Append(prop.Name == "text" ? "new Value<string>(text)" : Escape(prop.Name))
                .Append(",\n");
        }
        output.Append(
            "                children: children, components: components, configure: configure, reference: reference);\n\n"
        );
        return;
    }
    output
        .Append("            => DeclareNative<global::")
        .Append(native)
        .Append(">(modifier: ")
        .Append(modifier ? "true" : "false")
        .Append(", active: active, configure: (target, partsMap) =>\n            {\n");
    if (entryIsTextCaption(native))
        output.Append(
            "                if (text.HasValue) ControlCaption(target, text.Value, partsMap, caption);\n"
        );
    foreach (
        var prop in props
            .Where(p => p.Event == null && p.Name != "active" && !p.Part && !p.Synthetic)
            .OrderBy(p => Input(native, p.Name) != null ? 100 : Priority(p.Name))
    )
    {
        if (entryIsTextCaption(native) && prop.Name == "text")
            continue;
        var target = prop.Rect ? "(global::UnityEngine.RectTransform)target.transform" : "target";
        if (prop.Name is "layer" or "isStatic")
            target = "target.gameObject";
        var member = prop.NativeMember ?? prop.Name;
        var input = Input(native, member);
        if (prop.Name == "regexValue")
            output.Append("                Prop(target, regexValue, SetRegex);\n");
        else if (prop.Name == "sendPointerHoverToParent")
            output.Append(
                "                Prop(target, sendPointerHoverToParent, SetPointerHover);\n"
            );
        else if (input != null)
            output
                .Append("                InputProp(target, ")
                .Append(Escape(prop.Name))
                .Append(", t => t.")
                .Append(member)
                .Append(", (t, v) => t.")
                .Append(input.Value.Setter)
                .Append("(v), t => t.")
                .Append(input.Value.Event)
                .Append(");\n");
        else
            output
                .Append("                Prop(")
                .Append(target)
                .Append(", ")
                .Append(Escape(prop.Name))
                .Append(", (t, v) => t.")
                .Append(Escape(member))
                .Append(" = v);\n");
    }
    foreach (var prop in props.Where(p => p.Event != null))
    {
        if (prop.Trigger)
        {
            output
                .Append(
                    "                ListenTrigger(target, global::UnityEngine.EventSystems.EventTriggerType."
                )
                .Append(prop.NativeMember)
                .Append(", ")
                .Append(prop.Name)
                .Append(");\n");
            continue;
        }
        if (prop.NativeEvent)
        {
            var arguments = string.Join(", ", prop.Event!.Select((_, index) => "arg" + index));
            output
                .Append("                if (")
                .Append(Escape(prop.Name))
                .Append(" != null)\n                {\n")
                .Append(
                    "                    var eventScope = RequireScope();\n                    "
                )
                .Append(prop.Type)
                .Append(" handler = (")
                .Append(arguments)
                .Append(
                    ") => { if (!eventScope.IsDisposed) eventScope.Run(() => Batch(() => Untrack(() => "
                )
                .Append(Escape(prop.Name))
                .Append('(')
                .Append(arguments)
                .Append(")))); };\n                    target.")
                .Append(prop.NativeMember)
                .Append(" += handler;\n                    Cleanup(() => target.")
                .Append(prop.NativeMember)
                .Append(" -= handler);\n                }\n");
            continue;
        }
        output
            .Append("                Listen(target.")
            .Append(Escape(prop.Name))
            .Append(", ")
            .Append(Escape(prop.Name))
            .Append(");\n");
    }
    output
        .Append(
            "                configure?.Invoke(target);\n            }, reference: reference,\n"
        )
        .Append(
            reactive
                ? "            children: null, readChildren: children ?? throw new ArgumentNullException(nameof(children)),\n"
                : "            children: children, readChildren: null,\n"
        )
        .Append("            parts: new NativePart[]\n            {\n");
    foreach (var prop in props.Where(p => p.Part))
        output
            .Append("                NativePart<global::")
            .Append(native)
            .Append(", ")
            .Append(prop.Type)
            .Append("> (\"")
            .Append(prop.Name)
            .Append("\", ")
            .Append(Escape(prop.Name))
            .Append(", ")
            .Append(
                prop.Synthetic
                    ? "null"
                    : "(t, v) => t." + Escape(prop.NativeMember ?? prop.Name) + " = v"
            )
            .Append("),\n");
    output.Append("            }, components: components);\n\n");
}
bool entryIsTextCaption(string native) =>
    native is "UnityEngine.UI.Button" or "UnityEngine.UI.Toggle";
(string Setter, string Event)? Input(string native, string name) =>
    (native, name) switch
    {
        ("UnityEngine.UI.Toggle", "isOn") => ("SetIsOnWithoutNotify", "onValueChanged"),
        (
            "UnityEngine.UI.Slider"
                or "UnityEngine.UI.Scrollbar"
                or "TMPro.TMP_Dropdown"
                or "UnityEngine.UI.Dropdown",
            "value"
        ) => ("SetValueWithoutNotify", "onValueChanged"),
        ("TMPro.TMP_InputField" or "UnityEngine.UI.InputField", "text") => (
            "SetTextWithoutNotify",
            "onValueChanged"
        ),
        _ => null,
    };
string Escape(string name) =>
    SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

record Prop(
    string Name,
    string Type,
    string[]? Event,
    string? Gate,
    bool Rect,
    string? NativeMember = null,
    bool Part = false,
    bool NativeEvent = false,
    bool Synthetic = false,
    bool Trigger = false
);
