using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Pine.Generator;

const string stubs = """
namespace UnityEngine {
 public class Component { }
 public class MonoBehaviour : Component { }
 public class RectTransform : Component { }
 public class GameObject { }
 public enum RuntimeInitializeLoadType { BeforeSceneLoad }
 public class RuntimeInitializeOnLoadMethodAttribute : System.Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
}
namespace UnityEngine.Scripting { public class AlwaysLinkAssemblyAttribute : System.Attribute { } }
namespace Pine {
 public class Mount { }
 public class CanvasOptions { }
 public struct Value<T> { }
 public class View { }
 public static class P {
  public static UnityEngine.RectTransform Column(float gap, params UnityEngine.Component[] children) => new();
  public static Mount Mount(System.Func<UnityEngine.Component> component, CanvasOptions options = null) => new();
  public static Mount Mount(System.Func<UnityEngine.GameObject> component, CanvasOptions options = null) => new();
  public static Mount Mount(System.Func<View> component, CanvasOptions options = null) => new();
  public static View Component<T>(System.Func<T,View> render) where T:UnityEngine.MonoBehaviour => new();
  public static TView Component<T,TView>(System.Func<T,TView> render) where T:UnityEngine.MonoBehaviour where TView:UnityEngine.Component => null;
 }
}
namespace Pine.CompilerServices { public static class AppStartup { public static void Register(string assembly, System.Action start){} } }
""";
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
    .Split(Path.PathSeparator)
    .Select(p => MetadataReference.CreateFromFile(p))
    .ToArray();
var parse = new CSharpParseOptions(LanguageVersion.CSharp9);
int checks = 0;
void Check(bool condition, string description)
{
    if (!condition)
        throw new Exception(description);
    checks++;
}
(Compilation Output, GeneratorDriverRunResult Result) Generate(
    params (string Path, string Code)[] files
)
{
    var trees = files
        .Select(f => CSharpSyntaxTree.ParseText(f.Code, parse, path: f.Path))
        .Prepend(CSharpSyntaxTree.ParseText(stubs, parse, path: "PineStubs.cs"));
    var compilation = CSharpCompilation.Create(
        "Game",
        trees,
        references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );
    GeneratorDriver driver = CSharpGeneratorDriver
        .Create(new PineGenerator())
        .WithUpdatedParseOptions(parse);
    driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
    return (output, driver.GetRunResult());
}
void Compiles(Compilation output)
{
    var errors = output
        .GetDiagnostics()
        .Where(d => d.Severity == DiagnosticSeverity.Error)
        .ToArray();
    Check(errors.Length == 0, string.Join("\n", errors.Select(e => e.ToString())));
}
var good = Generate(
    (
        "Assets/UI/App.cs",
        "using UnityEngine; public static class App { public static Component Mount() => Pine.P.Column(gap: 12, Components.Counter(title: default, initialValue: 10)); }"
    ),
    (
        "Assets/UI/Counter.cs",
        "using Pine; using UnityEngine; public sealed class Counter : MonoBehaviour { public RectTransform Create(Value<string> title, int initialValue = 0) => new RectTransform(); }"
    )
);
Compiles(good.Output);
Check(good.Result.GeneratedTrees.Length == 2, "Generate application and component factories");
Check(
    good.Result.GeneratedTrees.Any(t => t.ToString().Contains("P.Mount(component:")),
    "Native app is mounted once by generated startup"
);
Check(
    good.Result.GeneratedTrees.Any(t => t.ToString().Contains("@initialValue = 0")),
    "Factory preserves optional named props"
);
Check(
    good.Result.GeneratedTrees.Any(t => t.ToString().Contains("AlwaysLinkAssembly")),
    "Application assembly is linked for stripped builds"
);
var views = Generate(
    (
        "Assets/App.cs",
        "public static class App { public static Pine.View Mount() => Components.Counter(); }"
    ),
    (
        "Assets/Counter.cs",
        "using Pine; public sealed class Counter:UnityEngine.MonoBehaviour { public Pine.View Create()=>new Pine.View(); }"
    )
);
Compiles(views.Output);
Check(
    views.Result.GeneratedTrees.Any(t => t.ToString().Contains("P.Component<global::Counter>")),
    "View factories defer behaviour construction"
);
Check(
    views.Result.GeneratedTrees.Any(t => t.ToString().Contains("P.Mount(component:")),
    "View apps are mounted automatically"
);
var advanced = Generate(
    (
        "Assets/App.cs",
        "public static class App { public static Pine.Mount Mount() => Pine.P.Mount(component: () => new UnityEngine.RectTransform()); }"
    )
);
Compiles(advanced.Output);
Check(
    !advanced.Result.GeneratedTrees.Single().ToString().Contains("P.Mount(component:"),
    "Explicit Mount app is not double mounted"
);
var options = Generate(
    (
        "Assets/App.cs",
        "public static class App { public static Pine.CanvasOptions Options => new Pine.CanvasOptions(); public static UnityEngine.Component Mount()=>new UnityEngine.RectTransform(); }"
    )
);
Compiles(options.Output);
Check(
    options.Result.GeneratedTrees.Single().ToString().Contains("options: global::App.Options"),
    "Canvas options are forwarded"
);
var wrongFile = Generate(
    (
        "Assets/Widget.cs",
        "public static class App { public static UnityEngine.Component Mount()=>new UnityEngine.RectTransform(); }"
    )
);
Check(wrongFile.Result.GeneratedTrees.Length == 0, "Ordinary files never start automatically");
var invalid = Generate(
    (
        "Assets/App.cs",
        "public class App { public UnityEngine.Component Mount()=>new UnityEngine.RectTransform(); }"
    )
);
Check(
    invalid.Result.Diagnostics.Any(d => d.Id == "PINE001"),
    "Invalid App gets a compiler diagnostic"
);
var duplicate = Generate(
    (
        "Assets/A/App.cs",
        "namespace A { public static class App { public static UnityEngine.Component Mount()=>new UnityEngine.RectTransform(); } }"
    ),
    (
        "Assets/B/App.cs",
        "namespace B { public static class App { public static UnityEngine.Component Mount()=>new UnityEngine.RectTransform(); } }"
    )
);
Check(
    duplicate.Result.Diagnostics.Count(d => d.Id == "PINE002") == 2,
    "Duplicate app declarations are rejected before startup"
);
var namespaced = Generate(
    (
        "Assets/App.cs",
        "using Pine; namespace Game.UI { public static class App { public static UnityEngine.Component Mount()=> Components.Clock(); } public sealed class Clock:UnityEngine.MonoBehaviour { public UnityEngine.RectTransform Create(int mode=1, string label=\"Time\")=>new UnityEngine.RectTransform(); } }"
    )
);
Compiles(namespaced.Output);
Check(
    namespaced.Result.GeneratedTrees.Any(t => t.ToString().Contains("namespace Game.UI")),
    "Factories share the component namespace"
);
var ignored = Generate(
    (
        "Assets/Editor/App.cs",
        "public static class App { public static UnityEngine.Component Mount()=>new UnityEngine.RectTransform(); }"
    )
);
Check(ignored.Result.GeneratedTrees.Length == 0, "Editor entry files are not runtime applications");
var badProp = Generate(
    (
        "Assets/Counter.cs",
        "using Pine; public sealed class Counter:UnityEngine.MonoBehaviour { public UnityEngine.RectTransform Create(ref int value)=>new UnityEngine.RectTransform(); }"
    )
);
Check(badProp.Result.Diagnostics.Any(d => d.Id == "PINE001"), "Ref props get a clear diagnostic");
var special = Generate(
    (
        "Assets/Gauge.cs",
        "using Pine; public sealed class Gauge:UnityEngine.MonoBehaviour { public UnityEngine.RectTransform Create(float low=float.NaN, double high=double.PositiveInfinity)=>new UnityEngine.RectTransform(); }"
    )
);
Compiles(special.Output);
var missing = Generate(("Assets/App.cs", "public static class Other { }"));
Check(
    missing.Result.Diagnostics.Any(d => d.Id == "PINE001"),
    "App.cs without the convention receives a diagnostic"
);
var partial = Generate(
    ("Assets/Config.cs", "public static partial class App { }"),
    (
        "Assets/App.cs",
        "public static partial class App { public static UnityEngine.Component Mount()=>new UnityEngine.RectTransform(); }"
    )
);
Compiles(partial.Output);
Check(
    partial.Result.GeneratedTrees.Length == 1,
    "Partial app entry is discovered regardless of declaration order"
);
var legacy = Generate(
    (
        "Assets/Legacy.cs",
        "public static class Components {} public sealed class Legacy:UnityEngine.MonoBehaviour { public UnityEngine.RectTransform Create()=>new UnityEngine.RectTransform(); }"
    )
);
Compiles(legacy.Output);
Check(
    legacy.Result.GeneratedTrees.Length == 0,
    "Unrelated existing Create methods and Components classes are untouched"
);
var partialComponent = Generate(
    ("Assets/ClockFields.cs", "public sealed partial class Clock:UnityEngine.MonoBehaviour {}"),
    (
        "Assets/Clock.cs",
        "using Pine; public sealed partial class Clock { public UnityEngine.RectTransform Create()=>new UnityEngine.RectTransform(); }"
    )
);
Compiles(partialComponent.Output);
Check(
    partialComponent.Result.GeneratedTrees.Length == 1,
    "Component opt-in works across partial declarations"
);
Console.WriteLine($"Pine generator: {checks} checks passed.");
