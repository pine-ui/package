# Counter sample

Import this sample through Package Manager and enter Play Mode. `PineCounter` creates its owner automatically through `RuntimeInitializeOnLoadMethod`; `Start()` mounts its counter once. No manual GameObject attachment is needed. Pine builds the Canvas, input host, counter and increment/reset controls in C#.

Pine supplies missing-only TMP defaults and its bundled accented-Latin text resources while preserving existing project settings. Follow the matching 0.2.0 installation instructions for input-backend setup and any Editor restart.

Alternatively, launch a player with `-pine-example`, or set `PINE_EXAMPLE=1`, to run the separate static example bootstrap. Choose one example startup path when testing.

Disabling or destroying the creating component does not dispose its separate mounted tree. Scene unload or destruction of the mounted root releases it. Keep the returned `Mount` if your own code needs explicit early disposal.
