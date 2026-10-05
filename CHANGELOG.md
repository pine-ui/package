# Changelog

## 0.2.0 — 2026-10-06

- Rename the static facade to `UI` and the mounted lifetime to `Mount`; use one `using Pine;` import.
- Automatically start App.cs / App.Mount with the bundled source generator; default persistent canvas and optional scene lifetime through CanvasOptions.
- Restrict declarations to compatible component types; add explicit `Children` and target-typed `Group<T>` composition.
- Add `Column(gap, children...)` and `Row(gap, children...)` shorthand, a four-file automatic-start composition sample and native component lifetime checks.
- Generate typed Components factories from MonoBehaviour.Create props; initialize before Awake/OnEnable and release behaviours with their returned UI roots. Plain component functions remain supported.
- Make Size exact in native rects and layout; add explicit Fill/Auto axes and reactive uniform grid sizing with conflict diagnostics.
- Supply complete toggle, slider, scrollbar, text field, dropdown, scroll view, raw image and progress controls alongside existing factories.
- Expose immutable observable dynamic result lists and read-only row metadata.
- Add overlay/camera/world configuration, native navigation/focus, safe areas, scaling and reduced motion.
- Bundle licensed accented-Latin text resources; preserve project defaults, dirty assets and external UI/input ownership. Generated defaults reference project-owned resource copies for safe removal.
- Include compiler rejection cases, native lifecycle/control checks, a stripped macOS player workload, source synchronization and reproducible artifacts.
- Document every public API declaration with XML summaries/examples and a complete current web reference.

This early release changes the pre-1.0 API. See [compatibility](COMPATIBILITY.md) for the exact verified scope.
