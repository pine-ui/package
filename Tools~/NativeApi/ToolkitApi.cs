using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class ToolkitApi
{
    internal static void Generate(string baselineData, string patchData, string package)
    {
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(package, "Tools~/NativeApi/toolkit-types.json"))
        );
        var versions = manifest.RootElement.GetProperty("versions").EnumerateArray().ToArray();
        var generator = new ToolkitGenerator(
            Native(baselineData),
            Native(patchData),
            versions[0],
            versions[1]
        );
        string output = Path.Combine(package, "Runtime/UIToolkit");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "Factories.g.cs"), generator.Factories());
        File.WriteAllText(Path.Combine(output, "Styles.g.cs"), generator.Styles());
        File.WriteAllText(Path.Combine(output, "Schemas.g.cs"), generator.Schemas());
        WriteReference(package, output);
        Console.WriteLine(
            $"UI Toolkit: {generator.FactoryCount} factories, {generator.StyleCount} style properties."
        );
    }

    private static void WriteReference(string package, string output)
    {
        var parse = new CSharpParseOptions(
            LanguageVersion.CSharp9,
            preprocessorSymbols: new[] { "UNITY_EDITOR", "PINE_UNITY_6000_3_25_OR_NEWER" }
        );
        var methods = new[] { "Factories.g.cs", "Schemas.g.cs" }
            .SelectMany(file =>
                CSharpSyntaxTree
                    .ParseText(File.ReadAllText(Path.Combine(output, file)), parse)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
            )
            .Where(method =>
                method.Modifiers.Any(SyntaxKind.PublicKeyword)
                && method.Modifiers.Any(SyntaxKind.StaticKeyword)
            )
            .GroupBy(method => method.Identifier.ValueText);
        var common = new HashSet<string>
        {
            "children",
            "style",
            "classes",
            "styleSheets",
            "events",
            "bindings",
            "targets",
            "manipulators",
            "reference",
            "configure",
            "enabled",
            "create",
        };
        var text = new System.Text.StringBuilder(
            "# UI Toolkit native reference\n\nGenerated from the supported native metadata. Use `using Pine;` and `using Pine.UIToolkit;`. See [UI Toolkit](ui-toolkit.md) for ownership, binding and composition. This reference targets Pine 1.2.0.\n\n## Factory inputs\n\nFactories preserve native types and constructor overloads. The table lists optional named native inputs across their overloads; native defaults are retained when omitted. Editor controls compile only under `UNITY_EDITOR`; patch members are gated from 6000.3.25f1. Exact signatures are available through C# completion.\n\nVisual factories also accept `children`, `style`, `classes`, `styleSheets`, `events`, `bindings`, `targets`, `manipulators`, `reference`, `configure` and `enabled`. Every native method remains available through typed references. Abstract controls require a typed `create` callback.\n\n| Factory | Named native inputs |\n| --- | --- |\n"
        );
        foreach (var group in methods.OrderBy(group => group.Key))
        {
            var inputs = group
                .SelectMany(method => method.ParameterList.Parameters)
                .Where(parameter =>
                    parameter.Default != null && !common.Contains(parameter.Identifier.ValueText)
                )
                .Select(parameter => parameter.Identifier.ValueText)
                .Distinct()
                .OrderBy(name => name);
            text.Append("| `P.")
                .Append(group.Key)
                .Append("` | ")
                .Append(string.Join(", ", inputs.Select(name => "`" + name + "`")))
                .AppendLine(" |");
        }
        text.Append(
            "\n## Native styles\n\n`Style` exposes every current writable native `IStyle` property. Each accepts native literals, sources, typed calculations and reset keywords.\n\n"
        );
        var styles = CSharpSyntaxTree
            .ParseText(File.ReadAllText(Path.Combine(output, "Styles.g.cs")), parse)
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single(type => type.Identifier.ValueText == "Style")
            .Members.OfType<FieldDeclarationSyntax>()
            .SelectMany(field => field.Declaration.Variables)
            .Select(variable => variable.Identifier.ValueText)
            .OrderBy(name => name);
        text.AppendLine(string.Join(", ", styles.Select(name => "`" + name + "`")) + ".");
        text.Append(
            "\n## Composition and native integrations\n\nUse `P.Component` for owned setup; `P.Mount` for an explicit lifetime; `P.Element<T>` for custom controls; `P.Template` and `P.Target` for UXML; `P.On<TEvent>` for native events; `P.Draw` for tracked native painting; `P.Bind` for native runtime bindings; `P.SerializedBinding` for Editor bindings; `P.Rows`, `P.TreeRows` and `P.Cell` for native virtualization. `PanelOptions` selects external settings or owned panel/document configuration.\n"
        );
        File.WriteAllText(
            Path.Combine(package, "Documentation~/ui-toolkit-reference.md"),
            text.ToString()
        );
    }

    private static CSharpCompilation Native(string data)
    {
        var paths = Directory
            .GetFiles(Path.Combine(data, "Managed/UnityEngine"), "*.dll")
            .Concat(Directory.GetFiles(Path.Combine(data, "NetStandard/ref/2.1.0"), "*.dll"));
        return CSharpCompilation.Create(
            "PineToolkitMetadata",
            references: paths.Select(p => MetadataReference.CreateFromFile(p)),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                metadataImportOptions: MetadataImportOptions.All
            )
        );
    }
}
