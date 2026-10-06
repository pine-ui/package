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
            "P.Vertical(P.Text(\"Typed\", color: UnityEngine.Color.white), P.Button(\"Click\", onClick: () => {})).With(P.Outline()); P.Button().With(P.Self(P.Image(color: UnityEngine.Color.black)), P.Text(\"Child\").With(P.Outline()));",
            "P.Text(color: true);",
            "P.Button(colors: UnityEngine.Color.white);",
            "P.Button().With(P.Size(80, 20));",
            "P.Text(reference: (UnityEngine.UI.Button b) => {});",
            "P.InputField(text: P.Source(1));",
            "P.Show(() => true, () => 1).Value = new[] { 2 };",
            "P.Show(() => true, () => 1).Value[0] = 2;",
            "P.Values(() => new[] { 1 }, (value, index) => { index.Value = 2; return value; });"
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
                bool expected = _index == 0 ? errors.Length == 0 : errors.Any(error => error.message.Contains("CS1503") || error.message.Contains("CS0411") || error.message.Contains("CS0200") || error.message.Contains("CS1661") || error.message.Contains("CS1678"));
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
