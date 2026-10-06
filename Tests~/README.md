# Verification fixtures

Run `dotnet run --project Tests~/Core/Pine.Tests.csproj` with .NET 8+, and `dotnet run --project Tests~/Generator/Pine.Generator.Tests.csproj` with .NET 10. Core syntax is C# 9; neither runner needs Unity assemblies.

Create a disposable Unity project with Pine installed. Copy `Unity/*.cs` into `Assets/Editor`. For the native Play check also import the Composition sample. Run these batch entry points without `-quit`; fixtures exit themselves:

- `Pine.Tests.PineNativeChecks.Run`: all authorable factories, defaults, native structs, props, source write-back, attachments, retained children and context.
- `Pine.Tests.PineNativeChecks.RunPlay`: generated sample startup, behaviour initialization, dropdown opening and destruction cleanup.
- `Pine.Tests.PineStartupChecks.Run`: generated App startup, hiding, source updates, scene and external-parent lifetime.
- `Pine.Tests.PineCompilerChecks.Run`: one valid example and compile-negative prop/read-only cases.
- `PineSetupChecks.Run`: disposable-project only; missing-only TMP/input setup and unrelated dirty asset preservation.
- `PineSetupChecks.AfterRemoval`: after removing Pine and Pine-dependent scripts, project-owned TMP defaults remain independent.

[Compatibility](../COMPATIBILITY.md) records the executed versions. Batch Editor checks do not certify physical input, rendering, performance, minimum-version runtime or IL2CPP. Earlier API fixtures and player measurements are archived in the private maintenance repository and do not validate this API.
