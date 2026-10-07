# Verification fixtures

Run `dotnet run --project Tests~/Core/Pine.Tests.csproj` with .NET 8+, and `dotnet run --project Tests~/Generator/Pine.Generator.Tests.csproj` with .NET 10. Core syntax is C# 9; neither runner needs Unity assemblies.

Create a disposable Unity project with Pine installed. Copy `Unity/*.cs` into `Assets/Editor`. For the native Play check also import the Composition sample. Run these batch entry points without `-quit`; fixtures exit themselves:

- `Pine.Tests.PineNativeChecks.Run`: all authorable factories, defaults, native structs, props, source write-back, attachments, retained children and context.
- `Pine.Tests.PineInteractionChecks.Run`: simulated input through native control/event paths and captured camera/world-space rendering; includes linked scrollbars, TMP/legacy image dropdowns, multiline input, dynamic toggle groups and native Animator transitions. Import the Composition sample first; copy `PineComplexChecks.cs` along with the other fixtures.
- `Pine.Tests.PineNativeChecks.RunPlay`: generated sample startup, behaviour initialization, dropdown opening and destruction cleanup.
- `Pine.Tests.PineSafeAreaChecks.Run`: synthetic screen-safe geometry, parent/canvas/camera changes and supported-transform validation.
- `Pine.Tests.PineSafeAreaChecks.RunPlay`: native enable/disable callbacks, layout restoration and declaration property ordering.
- `Pine.Tests.PineStartupChecks.Run`: generated App startup, hiding, source updates, scene and external-parent lifetime.
- `Pine.Tests.PineStartupChecks.RunWithoutDomainReload`: two Play sessions with domain reload disabled, restoring the original Editor setting afterward.
- `Pine.Tests.PineCompilerChecks.Run`: one valid example and compile-negative prop/read-only cases.
- `PineSetupChecks.Run`: disposable-project only; missing-only TMP/input setup and unrelated dirty asset preservation.
- `PineSetupChecks.AfterRemoval`: after removing Pine and Pine-dependent scripts, project-owned TMP defaults remain independent.

For UI Toolkit, run `Pine.Tests.PineToolkitChecks.Run` from `Assets/Editor`. Copy `PineToolkitRuntimeChecks.cs` into `Assets` and the ToolkitCounter sample into the project for runtime/render and player checks. `Pine.Tests.PineToolkitBuildChecks.Run` builds the Windows IL2CPP verification player with High managed stripping.

[Compatibility](../COMPATIBILITY.md) records the executed versions and distinguishes simulated interaction/render checks from physical input, other platforms and performance. Earlier API fixtures and player measurements are archived in the private maintenance repository and do not validate this API.
