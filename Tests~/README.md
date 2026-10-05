# Verification fixtures

Run `dotnet run --project Tests~/Core/Pine.Tests.csproj` with .NET 8 (or `DOTNET_ROLL_FORWARD=Major` on a newer runtime). The runner is C# 9 and requires no Unity assemblies.

For Unity tests, create a disposable project with Pine installed, copy `Unity/*.cs` into `Assets/Editor`, and use the exact tuples in `matrix.json`. Run batch entry points without `-quit`; the fixtures exit themselves:

- `Pine.Verification.PineChecks.Run`: native adapter operations in Edit Mode.
- `Pine.Verification.PineChecks.RunPlayMode`: automatic clock, glyph geometry, raycasts and teardown.
- `Pine.Tests.PineProductionChecks.Run`: full controls, exact/fill/content sizing, grid rejection, camera/world configuration, reduced motion, root/scene/input lifetime and an informational Editor workload.
- `Pine.Tests.PineCompositionChecks.Run`: one mounted canvas, nested/caller-provided children, independent and shared state, retained hiding, conditional cleanup/reconstruction and stable keyed reordering.
- `Pine.Tests.PineCompilerChecks.Run`: valid typed composition plus compile-negative property/value/group/extension/read-only cases. Expected compiler errors are logged, then the fixture asserts their diagnostics.
- `PineSetupChecks.Run`: disposable-project only; exercises owned backend configuration, unrelated dirty asset preservation and independent generated text defaults.
- `PineSetupChecks.AfterRemoval`: run after removing Pine and Pine-dependent scripts; ordinary TMP should still render with retained project defaults.


For the controlled macOS workload, copy `Player/PinePlayerProbe.cs` into Assets and `Player/PinePlayerBuild.cs` into Assets/Editor. Set `PINE_PROBE_OUTPUT` to an absolute `.app` path and invoke `PinePlayerBuild.Run`. It builds a non-development Mono player with High stripping. Run the app with `PINE_PROBE_RESULT` pointing to a JSON output file and window dimensions 1280×720. The probe includes synchronous bindings and the actual runtime Update; automatic Update is disabled only to avoid double stepping during this controlled test. It warms 120 frames, checks 300 idle updates and records 600 active frames. Render counters and full-frame pacing are recorded separately. Its exit status gates idle allocation and Pine CPU budget only, not the complete production/device contract.

Physical input, native keyboard/IME, target platforms, IL2CPP and browser compatibility need their own recorded execution evidence. CI core checks do not pass those gates.
