# Pine 0.2.0 compatibility

Minimum declared Unity version: 6000.3. The manifest declares uGUI 2.0.0 and Input System 1.20.1; Unity resolves the Editor-compatible uGUI core package.

| Unity Editor | uGUI | Input System | Verified scope |
| --- | --- | --- | --- |
| 6000.3.25f1 | 2.0.0 | 1.20.1 | macOS Editor native controls, typed declarations, composition, setup and cleanup |
| 6000.6.4f1 | 2.6.0 | 1.20.1 | macOS Editor native controls, composition, setup and cleanup |

A stripped macOS Mono player passed the controlled Pine workload check on Apple M4 Pro: 300 visible controls, 100 binding changes/frame and 50 springs; zero measured idle Pine allocations and Pine update p95 0.7435 ms. This is not an overall frame-time or universal device guarantee.

Windows, Android, iOS, WebGL/browser, IL2CPP, physical input and native keyboard/IME execution have not been certified by these checks. Validate your target build and hardware before shipping.

Use Pine from Unity's main thread. Additional glyph coverage requires code-configured fonts. Existing input/UI ownership is preserved; platform/backend incompatibilities produce diagnostics.

Pine 0.2.0 is an early release with pre-1.0 API changes.
