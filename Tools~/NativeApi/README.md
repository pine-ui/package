# Native factory generator

Using .NET 10 and compiled native assemblies from a disposable Unity project:

```text
dotnet run --project Tools~/NativeApi/Pine.NativeApi.csproj -- <UnityEditorData> <ProjectLibraryScriptAssemblies> <PackageCheckout>
```

Generate `Runtime/NativeProps.g.cs` and `catalog.json` together. The source lists authorable native types and reviewed version gates; it skips obsolete/generated companion types, retains inherited mutable properties and public fields and exposes native events as typed callbacks. Rebuild/check C# 9 with the applicable Unity/Input System/uGUI symbols. `Pine.asmdef` supplies package version defines. Run the native factory fixture after regeneration.
