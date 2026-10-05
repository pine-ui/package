<p align="center"><img src="Documentation~/pine-icon.svg" alt="Pine tree" width="120"></p>

# Pine 0.1.0

A reactive C# library for code-created Unity uGUI.

Build retained interfaces with typed sources, derived values, effects, contexts, scoped native bindings, keyed rows and springs.

## Installation

In Unity's Package Manager, choose **Install package from Git URL** and enter:

```text
https://github.com/pine-ui/package.git#v0.1.0
```

The repository root is the UPM package `com.kbenim.pine`. For a local checkout, choose **Install package from disk** and select `package.json`.

The manifest declares **Unity 6000.7.0b2**, **uGUI 2.7.0** and **Input System 6.7.0**. Enable the Input System backend in PlayerSettings and configure TMP settings/default font before creating text.

## Build UI

```csharp
using Pine;
using UI = Pine.Pine;

var count = UI.Source(0);
MountHandle mount = UI.Mount(() => UI.Column(
    UI.Size(400, 180),
    UI.Label(() => $"Count: {count.Value}"),
    UI.Button("Increment", () => count.Value++)
));

// Dispose when the interface owner ends.
mount.Dispose();
```

Run Pine on Unity's main thread. A parentless mount creates an overlay Canvas, scaler and raycaster. Pine reuses an existing EventSystem or creates one with an Input System UI module. Supply a native Canvas parent to mount inside an existing hierarchy. Destroying the mounted root disposes its scope.

Import the **Counter** sample through Package Manager for retained history and spring animation. Its README covers component and code-driven startup.

## Features

- Explicit typed sources, derived values, effects, batching and contexts.
- Native Frame, Column, Row, Label, Image and Button builders.
- Scoped component creation, cloning, properties, events and two-way native control bindings.
- Reactive children, conditional branches, keyed rows and retained exit transitions.
- Springs for scalar, array and Unity value types, with automatic or manual stepping.
- Owned cleanup for bindings, resources, branches and native objects.

## Documentation and checks


```sh
dotnet run --project Tests~/Core/Pine.Tests.csproj
```


## License

[MIT](LICENSE). Dependency licenses are recorded in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
