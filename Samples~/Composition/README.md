# Component composition sample

Import **Component composition** through Package Manager and enter Play Mode. Root.cs mounts App.Create once through Unity's startup attribute, with no Inspector setup. The remaining four files declare ordinary components; they never mount themselves.

- App.cs assembles the interface and supplies shared state.
- Counter.cs creates fresh local state unless a source is supplied explicitly.
- Card.cs accepts caller-provided children and adds its own heading.
- Actions.cs accepts a read-only Value<bool> input and a callback for the Save button.

Increment either independent counter: only that instance changes. Increment either shared counter: both update, and Save becomes available. Save resets their shared count.

The root belongs to its scene. Scene unload or mounted-root destruction disposes the full interface. Use Pine conditional and keyed-list declarations when a nested component needs to be removed independently. See the component tutorial at https://pine-ui.com/docs/tutorials/components/ for hiding, reconstruction, persistent state and stable keyed lists.
