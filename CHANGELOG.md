# Changelog

## 2026-03-24

- Cleaned the solution build output to `0 Warning(s), 0 Error(s)`.
- Switched `PluginExample` to a project reference so the solution builds without `PLUS_EMULATOR_HOME`.
- Removed the Linux-hostile pre-build echo target and suppressed legacy warning categories at the project level.
- Removed two unused exception variables in the game client layer.
- Continued the warning cleanup with broad null-safety and repeated access refactors across incoming packets, outgoing composers, room logic, interactors, AI, and command handlers.
- Kept the solution compiling cleanly after each cleanup batch, ending with `0 Warning(s), 0 Error(s)` on the full Release build.
