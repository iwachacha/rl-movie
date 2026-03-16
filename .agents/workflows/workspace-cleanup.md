---
description: workflow alias for reporting and cleaning safe cache, temp, and generated artifacts in this repository
---

# Workflow Alias: workspace-cleanup

- Primary tool: `tools/cleanup-workspace.ps1`
- Use when the user wants to reclaim disk space, remove stale caches, or clean generated artifacts without touching source files.
- Start with: `powershell -ExecutionPolicy Bypass -File .\tools\cleanup-workspace.ps1 -Mode report`
- Safe cleanup: `powershell -ExecutionPolicy Bypass -File .\tools\cleanup-workspace.ps1 -Mode safe`
- Deeper cleanup: `powershell -ExecutionPolicy Bypass -File .\tools\cleanup-workspace.ps1 -Mode deep`
- Optional heavy resets: add `-IncludeLibrary`, `-IncludeVenv`, `-IncludeRootLogs`, or `-FullUvCacheClean`
- Do not run cleanup without an explicit user request because it deletes files.
