# Component composition sample

Import the four files and press Play. App.cs returns the application tree; Pine starts it automatically. Keep exactly one App.cs entry in your project.

- App.cs assembles the interface and supplies shared state.
- Counter.cs declares MonoBehaviour.Create; Pine generates Components.Counter with the same typed, named props. Each call has independent state unless a count source is supplied.
- Card.cs is a plain container function accepting caller-provided children.
- Actions.cs accepts a reactive Value<bool> and a Save callback.

Unity callbacks run on generated behaviour instances. Hiding their returned UI retains state and stops Unity updates; destroying that UI releases the behaviour and reactive scope. The default canvas persists across scenes; CanvasOptions.Persistent=false selects scene lifetime.

See https://pine-ui.com/docs/tutorials/components/ for plain functions, callbacks, conditional components and stable keyed lists.
