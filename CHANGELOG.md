# Changelog

## 1.0.0 — 2026-10-06

- Use `P.Text`, `P.Vertical` and `P.Horizontal`; factories return deferred `View` declarations.
- Build once; literals, sources and tracked getters update retained native components.
- Compose nested children and same-object modifiers with `.With(...)`; `P.Self(...)` explicitly attaches visual components.
- Cover 41 authorable native uGUI/TMP factories and Inspector settings as typed named props. Later native members are version-gated.
- Preserve native defaults and grouped structs; wire required graphics, captions, input fields, dropdown templates and scroll references.
- Support editable source write-back, scoped native events/context, retained children and generated MonoBehaviour startup/lifetimes.
- Update all samples, downloadable examples, complete web reference and concise XML summaries.

This replaces the earlier 1.0.0 tag/archive with a breaking API, as requested. Remove/reinstall earlier installations to refresh cached Git revisions. No old API aliases are provided. [Compatibility](COMPATIBILITY.md) records the exact verification boundary.
