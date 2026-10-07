# Native factory generator

For the shared renderer facades and UI Toolkit declarations:

```text
dotnet run --project Tools~/NativeApi/Pine.NativeApi.csproj -- --shared <PackageCheckout>
dotnet run --project Tools~/NativeApi/Pine.NativeApi.csproj -- --toolkit <6000.3.0f1 Editor/Data> <6000.3.25f1 Editor/Data> <PackageCheckout>
```

`toolkit-types.json` records reviewed native type names. The generator reads actual native metadata for constructors, inherited current members, events, style/conversion adapters and constraints. Editor-only controls and `MonoBehaviour.runInEditMode` are gated separately; verified patch additions use the assembly's Unity version define. Format the generated C# and run native checks after regeneration. Editor engine metadata alone does not prove player compilation.

Using .NET 10 and compiled native assemblies from a disposable Unity project:

```text
dotnet run --project Tools~/NativeApi/Pine.NativeApi.csproj -- <UnityEditorData> <ProjectLibraryScriptAssemblies> <PackageCheckout>
```

Generate `Runtime/NativeProps.g.cs` and `catalog.json` together. The source lists authorable native types and reviewed version gates; it skips obsolete/generated companion types, retains inherited mutable properties and public fields and exposes native events as typed callbacks. Rebuild/check C# 9 with the applicable Unity/Input System/uGUI symbols. `Pine.asmdef` supplies package version defines. Run the native factory fixture after regeneration.
