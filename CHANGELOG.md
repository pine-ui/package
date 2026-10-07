# Changelog

## 1.1.0 — 2026-10-07

- Replace View.With with factory `children` arrays/getters; use `components` for static attachments beside reactive children. This changes the 1.0.0 composition API.

- Declare named control parts and resolve typed forward/cyclic native references before activation; clear references when targets leave their lifetime.

- Preserve supplied external hierarchies, graphics and fonts; create only omitted default parts.

- Permit native component multiplicity where Unity permits it.

- Add multiplayer EventSystems, tracked-device raycasters and virtual-mouse input from the existing Input System dependency, with independently owned default input actions.

- Declare Animator controllers and PlayerInput UI-module relationships with native typed settings.

- Expose TMP overlay, mask offset, auto-size container, owned pre-render events and declarative EventTrigger callbacks.

- Initialize missing TMP resources before entering Play mode and import shader includes before shaders.

- Update generators, samples, current docs and downloadable examples; retain the 1.0.0 documentation snapshot.

## 1.0.0 — 2026-10-06

- Use `P.Text`, `P.Vertical` and `P.Horizontal`; factories return deferred `View` declarations.

- Build once; literals, sources and tracked getters update retained native components.

- Compose nested children and same-object modifiers with `.With(...)`; `P.Self(...)` explicitly attaches visual components.

- Cover 41 authorable native uGUI/TMP factories and Inspector settings as typed named props. Later native members are version-gated.

- Preserve native defaults and grouped structs; wire required graphics, captions, input fields, dropdown templates and scroll references.

- Support editable source write-back, scoped native events/context, retained children and generated MonoBehaviour startup/lifetimes.

- Update all samples, downloadable examples, complete web reference and concise XML summaries.

[Compatibility](COMPATIBILITY.md) records the exact verification boundary.
