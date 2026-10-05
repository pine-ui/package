using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Pine.Tests
{
    internal static class PineCompilerChecks
    {
        private static readonly string[] Cases =
        {
            "UI.Frame(UI.Children(UI.Label(\"Typed\", UI.Size(80, 24)), UI.Button(\"Click\", () => {}, UI.Enabled(true))), UI.Size(100, 60)); UI.Apply(UI.Create<TMPro.TextMeshProUGUI>(), UI.Group<TMPro.TMP_Text>(UI.Text(\"Good\"), UI.FontSize(20))); UI.Column(12, UI.Row(8, UI.Label(\"Nested\")), UI.Frame());",
            "UI.Frame(UI.Text(\"Invalid text target\"));",
            "UI.Label(\"Text\", UI.Enabled(true));",
            "UI.Button(\"Click\", () => {}, UI.Sprite(null));",
            "UI.Apply(UI.Frame(), UI.Group<TMPro.TMP_Text>(UI.Text(\"Bad group\")));",
            "UI.Label(\"Text\", UI.Set<UnityEngine.UI.Slider, float>(\"Value\", (target, value) => target.value = value, 1f));",
            "UI.Frame(UI.Size(UI.Source(\"Wrong value type\")));",
            "UI.Frame(UI.CellSize(new UnityEngine.Vector2(10, 10)));",
            "UI.Show(() => true, () => 1).Value = new[] { 2 };",
            "UI.Show(() => true, () => 1).Value[0] = 2;",
            "UI.Values(() => new[] { 1 }, (value, index) => { index.Value = 2; return value; });",
            "UI.Column(12, UI.Text(\"A property is not a child component\"));"
        };
        private static int _index;
        private static readonly string DirectoryPath = "Library/PineCompilerChecks";
        [MenuItem("Tools/Pine/Run Compiler Checks")]
        public static void Run() { _index = 0; Directory.CreateDirectory(DirectoryPath); BuildNext(); }
        private static void BuildNext()
        {
            if (_index == Cases.Length)
            { Debug.Log($"Pine compiler checks passed: one valid declaration and {_index - 1} compile-negative cases."); if (Application.isBatchMode) EditorApplication.Exit(0); return; }
            string source = DirectoryPath + "/Case.cs";
            File.WriteAllText(source, "using Pine; public static class CompileCase { public static void Build() { " + Cases[_index] + " } }");
            var builder = new AssemblyBuilder(DirectoryPath + "/Case.dll", new[] { source });
            builder.flags = AssemblyBuilderFlags.EditorAssembly;
            builder.excludeReferences = builder.defaultReferences.Where(path => Path.GetFileName(path) == "UnityEngine.dll").ToArray();
            builder.additionalReferences = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) && (a.GetName().Name == "Pine" || a.GetName().Name == "Assembly-CSharp" || a.GetName().Name == "Unity.TextMeshPro" || a.GetName().Name == "Unity.ugui" || a.GetName().Name.StartsWith("UnityEngine."))).Select(a => a.Location).ToArray();
            builder.buildFinished += (_, messages) =>
            {
                var errors = messages.Where(message => message.type == CompilerMessageType.Error).ToArray();
                bool expected = _index == 0 ? errors.Length == 0 : errors.Any(error => error.message.Contains("CS1503") || error.message.Contains("CS0411") || error.message.Contains("CS0200"));
                if (!expected)
                {
                    Debug.LogError("Pine compiler case " + _index + " failed its expected outcome: " + string.Join("\n", errors.Select(error => error.message)));
                    if (Application.isBatchMode) EditorApplication.Exit(1); return;
                }
                _index++; EditorApplication.delayCall += BuildNext;
            };
            if (!builder.Build()) { Debug.LogError("Cannot start Pine compiler checks."); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
