# Changelog

## 0.2.0 — local unpublished candidate

- Rename the static facade to `UI` and the mounted lifetime to `Mount`; use one `using Pine;` import.
- Mount the complete retained tree once at startup, with optional early disposal and automatic scene/root cleanup. No owner base class is required.
- Restrict declarations to compatible component types; add explicit `Children` and target-typed `Group<T>` composition.
- Add `Column(gap, children...)` and `Row(gap, children...)` shorthand, a five-file single-mount composition sample and native component lifetime checks.
- Make Size exact in native rects and layout; add explicit Fill/Auto axes and reactive uniform grid sizing with conflict diagnostics.
- Supply complete toggle, slider, scrollbar, text field, dropdown, scroll view, raw image and progress controls alongside existing factories.
- Expose immutable observable dynamic result lists and read-only row metadata.
- Add overlay/camera/world configuration, native navigation/focus, safe areas, scaling and reduced motion.
- Bundle licensed accented-Latin text resources; preserve project defaults, dirty assets and external UI/input ownership. Generated defaults reference project-owned resource copies for safe removal.
- Include compiler rejection cases, native lifecycle/control checks, a stripped macOS player workload, source synchronization and reproducible artifacts.
- Document every public API declaration with XML summaries/examples and a complete current web reference.


## 0.1.0 — 2026-10-05

- Reactive sources, eager derived values, effects, batching, contexts and owned cleanup.
- Native component creation, cloning, scoped properties/events and code-created mounts.
- Frame, Column, Row, Label, Image and Button builders with typed reactive inputs.
- Conditional branches and keyed rows with retained exit transitions.
- Reactive springs with Unity value types and manual stepping.
- Independent scheduler/clock error handling, cleanup recovery and current derived reads inside batches.
- Counter sample, executable core checks and versioned Docusaurus documentation.
