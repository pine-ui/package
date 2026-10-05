# Counter sample

Import this sample through Package Manager and press Play. App.cs returns the tree directly; Pine starts and mounts it automatically. No startup hook, scene owner or Inspector attachment is required. Keep one App.cs entry per project; compose the sample into an existing app instead of adding another entry.

The canvas persists across scenes by default. Set CanvasOptions.Persistent=false through an optional App.Options property for scene lifetime. Destroying its root releases bindings and handlers. Explicit UI.Mount remains available for early disposal and external parents.

Follow the matching 0.2.0 installation guide for input setup and any Editor restart.
