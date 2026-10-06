using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Pine.Generator
{
    [Generator]
    public sealed class PineGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor Invalid = new DiagnosticDescriptor(
            "PINE001", "Invalid Pine declaration", "{0}", "Pine", DiagnosticSeverity.Error, true);
        private static readonly DiagnosticDescriptor Duplicate = new DiagnosticDescriptor(
            "PINE002", "Duplicate Pine application", "Only one App.cs application entry is allowed in an assembly", "Pine", DiagnosticSeverity.Error, true);
        private static string Name(ISymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        private static bool Inherits(ITypeSymbol type, string fullName)
        {
            for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType)
                if (current.ToDisplayString() == fullName) return true;
            return false;
        }
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var ui = context.Compilation.GetTypeByMetadataName("Pine.P") ??
                     context.Compilation.GetTypeByMetadataName("Assets.Scripts.Utils.Pine.P");
            if (ui == null) return;
            string runtime = Name(ui), ns = ui.ContainingNamespace.ToDisplayString();
            var appMethods = new List<IMethodSymbol>();
            var components = new List<IMethodSymbol>();
            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                string path = tree.FilePath.Replace('\\', '/');
                if (path.StartsWith("Packages/", StringComparison.Ordinal) || path.Contains("/Packages/") ||
                    path.Contains("/Editor/") || path.Contains("/Tests/") || path.Contains("/Tests~/")) continue;
                var model = context.Compilation.GetSemanticModel(tree);
                var classes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
                bool entryFile = Path.GetFileName(path) == "App.cs", foundApp = false;
                foreach (var declaration in classes)
                {
                    var type = model.GetDeclaredSymbol(declaration);
                    if (type == null) continue;
                    bool visible = type.DeclaredAccessibility == Accessibility.Public && type.ContainingType == null;
                    if (entryFile && type.Name == "App")
                    {
                        foundApp = true;
                        var methods = type.GetMembers("Mount").OfType<IMethodSymbol>().ToArray();
                        bool valid = visible && type.IsStatic && type.Arity == 0 && methods.Length == 1 &&
                            methods[0].IsStatic && methods[0].DeclaredAccessibility == Accessibility.Public &&
                            methods[0].Arity == 0 && methods[0].Parameters.Length == 0 &&
                            (methods[0].ReturnType.ToDisplayString() == ns + ".View" || Inherits(methods[0].ReturnType, "UnityEngine.Component") ||
                             methods[0].ReturnType.ToDisplayString() == "UnityEngine.GameObject" ||
                             methods[0].ReturnType.ToDisplayString() == ns + ".Mount" || methods[0].ReturnsVoid);
                        if (!valid) Error(context, declaration.GetLocation(), "App.cs must declare public static App.Mount() returning View, a native Component, GameObject or Mount (or void for an explicit P.Mount entry).");
                        else if (!appMethods.Any(m => SymbolEqualityComparer.Default.Equals(m, methods[0]))) appMethods.Add(methods[0]);
                    }
                    if (type.DeclaringSyntaxReferences[0].SyntaxTree != tree || !Inherits(type, "UnityEngine.MonoBehaviour")) continue;
                    bool importsPine = type.DeclaringSyntaxReferences.Any(r => r.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u =>
                    {
                        string imported = u.Name.ToString().Replace("global::", "");
                        return imported == ns || imported.StartsWith(ns + ".", StringComparison.Ordinal);
                    }));
                    bool pineNamespace = type.ContainingNamespace.ToDisplayString() == ns || type.ContainingNamespace.ToDisplayString().StartsWith(ns + ".", StringComparison.Ordinal);
                    bool qualifiedPine = !importsPine && !pineNamespace && type.DeclaringSyntaxReferences.Any(r =>
                        r.GetSyntax().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(access =>
                            access.Expression.ToString().EndsWith(ns + ".P", StringComparison.Ordinal) &&
                            context.Compilation.GetSemanticModel(r.SyntaxTree).GetSymbolInfo(access).Symbol is IMethodSymbol called && SymbolEqualityComparer.Default.Equals(called.ContainingType, ui)));
                    if (!importsPine && !pineNamespace && !qualifiedPine) continue;
                    foreach (var method in type.GetMembers("Create").OfType<IMethodSymbol>())
                    {
                        if (method.IsStatic || method.DeclaredAccessibility != Accessibility.Public ||
                            (!Inherits(method.ReturnType, "UnityEngine.Component") && method.ReturnType.ToDisplayString() != ns + ".View")) continue;
                        if (!visible || type.IsAbstract || type.Arity != 0 || method.Arity != 0 ||
                            method.Parameters.Any(p => p.RefKind != RefKind.None))
                        {
                            Error(context, method.Locations.FirstOrDefault(), "Pine component Create methods need a public, concrete, non-generic top-level MonoBehaviour, View or native Component return type and by-value props.");
                            continue;
                        }
                        components.Add(method);
                    }
                }
                if (entryFile && !foundApp)
                    Error(context, tree.GetRoot().GetLocation(), "App.cs must declare a public static class named App with a Mount factory.");
            }
            if (appMethods.Count > 1)
                foreach (var method in appMethods) context.ReportDiagnostic(Diagnostic.Create(Duplicate, method.Locations.FirstOrDefault()));
            else if (appMethods.Count == 1) GenerateApp(context, appMethods[0], runtime, ns);
            foreach (var group in components.GroupBy(m => m.ContainingNamespace.ToDisplayString()))
                GenerateComponents(context, group.Key, group.ToArray(), runtime);
        }
        private static void Error(GeneratorExecutionContext context, Location location, string text) =>
            context.ReportDiagnostic(Diagnostic.Create(Invalid, location, text));

        private static void GenerateApp(GeneratorExecutionContext context, IMethodSymbol method, string runtime, string ns)
        {
            string call = Name(method.ContainingType) + ".Mount()";
            var options = method.ContainingType.GetMembers("Options");
            string option = "null";
            if (options.Length != 0)
            {
                var property = options.Length == 1 ? options[0] as IPropertySymbol : null;
                if (property == null || !property.IsStatic || property.DeclaredAccessibility != Accessibility.Public ||
                    property.GetMethod == null || property.GetMethod.DeclaredAccessibility != Accessibility.Public ||
                    property.Type.ToDisplayString() != ns + ".CanvasOptions")
                {
                    Error(context, method.Locations.FirstOrDefault(), "Optional App.Options must be a public static readable CanvasOptions property.");
                    return;
                }
                if (method.ReturnType.ToDisplayString() == ns + ".Mount" || method.ReturnsVoid)
                {
                    Error(context, method.Locations.FirstOrDefault(), "App.Options applies to a returned native tree. An explicit Mount entry supplies options to P.Mount instead.");
                    return;
                }
                option = Name(method.ContainingType) + ".Options";
            }
            string body = method.ReturnType.ToDisplayString() == ns + ".Mount" || method.ReturnsVoid
                ? call + ";"
                : runtime + ".Mount(component: () => " + call + ", options: " + option + ");";
            string preserve = context.Compilation.Assembly.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "UnityEngine.Scripting.AlwaysLinkAssemblyAttribute")
                ? "" : "[assembly: global::UnityEngine.Scripting.AlwaysLinkAssembly]\n";
            string source = "// <auto-generated/>\n" + preserve +
                "internal static class PineGeneratedAppEntry\n{\n" +
                "    [global::UnityEngine.RuntimeInitializeOnLoadMethod(global::UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]\n" +
                "    private static void Register() => global::" + ns + ".CompilerServices.AppStartup.Register(" +
                SymbolDisplay.FormatLiteral(context.Compilation.AssemblyName, true) + ", () => { " + body + " });\n}\n";
            context.AddSource("Pine.App.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        private static void GenerateComponents(GeneratorExecutionContext context, string ns, IMethodSymbol[] methods, string runtime)
        {
            var existing = ns == "<global namespace>" ? context.Compilation.GetTypeByMetadataName("Components") :
                context.Compilation.GetTypeByMetadataName(ns + ".Components");
            if (existing != null && (!existing.IsStatic || existing.DeclaringSyntaxReferences.Any(r =>
                r.GetSyntax() is ClassDeclarationSyntax c && !c.Modifiers.Any(SyntaxKind.PartialKeyword))))
            {
                Error(context, methods[0].Locations.FirstOrDefault(), "An existing Components class must be static partial so Pine can add typed factories.");
                return;
            }
            var source = new StringBuilder("// <auto-generated/>\n");
            bool namespaced = ns != "<global namespace>";
            if (namespaced) source.Append("namespace ").Append(ns).Append("\n{\n");
            source.Append("/// <summary>Typed component factories generated from public MonoBehaviour.Create declarations.</summary>\npublic static partial class Components\n{\n");
            foreach (var method in methods)
            {
                string type = Name(method.ContainingType), result = Name(method.ReturnType);
                source.Append("    /// <summary>Creates an owned ").Append(method.ContainingType.Name).Append(" instance and its declared P. Each call has independent state and Unity callbacks.</summary>\n");
                foreach (var parameter in method.Parameters)
                    source.Append("    /// <param name=\"").Append(parameter.Name).Append("\">Typed ").Append(parameter.Name).Append(" prop forwarded to Create.</param>\n");
                source.Append("    /// <returns>The native UI root returned by Create, with an independently owned behaviour scope.</returns>\n");
                source.Append("    /// <remarks>Construct once inside App.Mount or a component factory. Props and UI initialize before Awake and OnEnable. Hiding retains state; destroying the view releases bindings and the behaviour.</remarks>\n");
                source.Append("    /// <example><code><![CDATA[Components.").Append(method.ContainingType.Name).Append("(");
                source.Append(string.Join(", ", method.Parameters.Where(p => !p.IsOptional && !p.IsParams).Select(p => "@" + p.Name + ": @" + p.Name)));
                source.Append(");]]></code></example>\n");
                source.Append("    public static ").Append(result).Append(" @").Append(method.ContainingType.Name).Append("(");
                source.Append(string.Join(", ", method.Parameters.Select(p =>
                    (p.IsParams ? "params " : "") + Name(p.Type) + " @" + p.Name + (p.HasExplicitDefaultValue ? " = " + Default(p) : ""))));
                source.Append(") =>\n        ").Append(runtime).Append(".Component<").Append(type);
                if (!method.ReturnType.ToDisplayString().EndsWith(".View", StringComparison.Ordinal)) source.Append(", ").Append(result);
                source.Append(">(render: instance => instance.Create(");
                source.Append(string.Join(", ", method.Parameters.Select(p => "@" + p.Name + ": @" + p.Name)));
                source.Append("));\n\n");
            }
            source.Append("}\n");
            if (namespaced) source.Append("}\n");
            context.AddSource("Pine.Components." + (namespaced ? ns : "Global") + ".g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }
        private static string Default(IParameterSymbol parameter)
        {
            object value = parameter.ExplicitDefaultValue;
            if (value == null) return "default(" + Name(parameter.Type) + ")";
            if (parameter.Type.TypeKind == TypeKind.Enum) return "(" + Name(parameter.Type) + ")" + Convert.ToString(value, CultureInfo.InvariantCulture);
            if (value is string text) return SymbolDisplay.FormatLiteral(text, true);
            if (value is char character) return SymbolDisplay.FormatLiteral(character, true);
            if (value is bool boolean) return boolean ? "true" : "false";
            if (value is float single && (float.IsNaN(single) || float.IsInfinity(single)))
                return "global::System.Single." + (float.IsNaN(single) ? "NaN" : single > 0 ? "PositiveInfinity" : "NegativeInfinity");
            if (value is double real && (double.IsNaN(real) || double.IsInfinity(real)))
                return "global::System.Double." + (double.IsNaN(real) ? "NaN" : real > 0 ? "PositiveInfinity" : "NegativeInfinity");
            string number = Convert.ToString(value, CultureInfo.InvariantCulture);
            return number + (value is float ? "F" : value is double ? "D" : value is decimal ? "M" : value is ulong ? "UL" : value is long ? "L" : value is uint ? "U" : "");
        }
    }
}
