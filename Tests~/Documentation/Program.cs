using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var rows = new List<object>();
foreach (string path in Directory.GetFiles(args[0], "*.cs"))
{
    var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
    foreach (var member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
    {
        if (
            !member.GetModifiers().Any(SyntaxKind.PublicKeyword)
            && !member.GetModifiers().Any(SyntaxKind.ProtectedKeyword)
            && !(member.Parent is InterfaceDeclarationSyntax)
        )
            continue;
        var parents = member.Ancestors().OfType<TypeDeclarationSyntax>().ToArray();
        if (parents.Any(p => !p.Modifiers.Any(SyntaxKind.PublicKeyword)))
            continue;
        if (
            member is TypeDeclarationSyntax td
            && td.Identifier.Text == "P"
            && Path.GetFileName(path) != "Pine.cs"
        )
            continue;
        string name = member switch
        {
            TypeDeclarationSyntax t => t.Identifier.Text,
            MethodDeclarationSyntax m => m.Identifier.Text,
            ConstructorDeclarationSyntax c => c.Identifier.Text,
            PropertyDeclarationSyntax p => p.Identifier.Text,
            FieldDeclarationSyntax f => f.Declaration.Variables.First().Identifier.Text,
            ConversionOperatorDeclarationSyntax c => "implicit",
            _ => "",
        };
        if (name == "")
            continue;
        var parameters = member switch
        {
            MethodDeclarationSyntax m => m.ParameterList,
            ConstructorDeclarationSyntax c => c.ParameterList,
            ConversionOperatorDeclarationSyntax c => c.ParameterList,
            _ => null,
        };
        var generics = member switch
        {
            TypeDeclarationSyntax t => t.TypeParameterList,
            MethodDeclarationSyntax m => m.TypeParameterList,
            _ => null,
        };
        var doc = member
            .GetLeadingTrivia()
            .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            .Select(t => t.ToFullString());
        rows.Add(
            new
            {
                path,
                start = member.SpanStart,
                line = member.GetLocation().GetLineSpan().StartLinePosition.Line,
                type = parents.FirstOrDefault()?.Identifier.Text ?? name,
                name,
                kind = member.Kind().ToString(),
                parameters = parameters
                    ?.Parameters.Select(p => new
                    {
                        name = p.Identifier.Text,
                        type = p.Type?.ToString(),
                    })
                    .ToArray(),
                generics = generics?.Parameters.Select(p => p.Identifier.Text).ToArray(),
                signature = (
                    member is MethodDeclarationSyntax method
                        ? method
                            .WithBody(null)
                            .WithExpressionBody(null)
                            .WithSemicolonToken(default)
                            .ToString()
                    : member is ConstructorDeclarationSyntax ctor
                        ? ctor.WithBody(null).WithExpressionBody(null).ToString()
                    : member is TypeDeclarationSyntax typeNode
                        ? typeNode.Identifier.Text + (typeNode.TypeParameterList?.ToString() ?? "")
                    : member is PropertyDeclarationSyntax prop
                        ? prop.Type.ToString() + " " + prop.Identifier.Text
                    : member is FieldDeclarationSyntax field ? field.Declaration.ToString()
                    : member.ToString()
                ).Replace("\r", ""),
                documented = doc.Any(),
            }
        );
    }
}
Console.WriteLine(JsonSerializer.Serialize(rows));

static class SyntaxExtensions
{
    public static SyntaxTokenList GetModifiers(this MemberDeclarationSyntax m) =>
        m switch
        {
            TypeDeclarationSyntax t => t.Modifiers,
            BaseMethodDeclarationSyntax b => b.Modifiers,
            BasePropertyDeclarationSyntax p => p.Modifiers,
            BaseFieldDeclarationSyntax f => f.Modifiers,
            _ => default,
        };
}
