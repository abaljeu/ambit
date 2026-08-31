# API-version-only CodeOutdated

Date: 2026-08-28
Branch: `w/client-start-time`
Parent: [[free-tier-cold-start-sync.md]]

`CodeOutdated` is `poll.apiVersion <> ApiVersion.current` in [[src/Shared/SyncLogic.fs]] `getPollOutcome`. The Shared marker is [[src/Shared/ApiResponses.fs]] `ApiVersion.current` (1). Server Poll/load/POST overlay send it as JSON `v`. Missing `v` decodes as 0 and is `CodeOutdated`.

Process restart, Azure Free unload, `dotnet` reload, and wwwroot page mtime do **not** set `CodeOutdated`. Same API + new process: Polls continue; later `ready: true` can clear Starting up. Same API + revision ahead: `DataOutdated`. Bump `ApiVersion.current` on an incompatible Poll/state/changes/load wire or semantics change.

Stamps `b`/`p` and HTML `__BUILD_TS__` / `__PAGE_BUILD_TS__` remain. `ClientPollContext` is removed. No separate “please reload for new UI” hint.
