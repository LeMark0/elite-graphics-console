# 1.9.5 — Correct status-bar version (prerelease)

The status bar previously displayed a hardcoded 1.9.0 even when a newer executable
was installed. It now reads the running app assembly's version automatically.

Validation: 68 core tests and 57 WPF UI checks, including a status-bar version check.
No preset or game-setting changes. The direct Apply workflow from 1.9.4 is retained.
