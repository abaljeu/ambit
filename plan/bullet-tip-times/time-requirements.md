# Bullet tip — non-obvious time requirements

Evidence: [[tmp/node-marker-tip-facts.md]]. Nothing locked; ➡️ marks the current recommendation.

## Candidate clocks

| # | Label (UI) | Real source field | Gate (when it applies) | Instant & local? | De-dup / shadowing | Notes |
|---|------------|-------------------|------------------------|------------------|--------------------|-------|
| 1 | **Update Time** | `Node.updateTime` (`Model.fs:91`) | every loaded Node | yes, always | base clock; after persist == server DataDir mtime | the de-dup baseline |
| 2 | **Workspace file time** | `fact.localMtimeUtc` (`WorkspacePathSyncStatus.fs:30-35`) | mapped label + App/desktop session + snapshot ran | yes (ledger) | distinct family (App local disk) | user's "App workspace file time" |
| 3 | **Server file time** | `fact.serverMtimeUtc` | mapped + desktop + snapshot | yes (ledger) | **often == Update Time**; `effectiveServerMtime` prefers `node.updateTime`; hide when equal to #1 | show raw fact value, not effective |
| 4 | **Last sync** | `fact.lastOp` (seed/upload/download) | mapped + desktop + snapshot | yes (ledger) | it is a **word, not a clock** | user's "sync time" has no timestamp |
| — | ~~active file-status~~ | `VM.desktopFileIndicator.sourceModifiedUtc` | active Node only, needs fetch | **no** | single slot, server-or-app ambiguous | **excluded** as a tip source |

## Cross-cutting rules

- **De-dup**: render a stamp only if it differs from stamps already shown (baseline #1). Identical
  clocks collapse to one line. ⇒ open: equality tolerance (exact ticks vs 1-second).
- **Absent → omit**: no label, no `N/A` (parent lock).
- **Timezone**: all stamps render in the browser's local zone. No client-local formatter exists
  today (`JsInterop.fs:408-420` is hardcoded ET/epoch-seconds) — net-new Fable `Intl.DateTimeFormat`.
  ⇒ open: display precision + format.
- **Availability degrade**: ledger facts need a desktop (WebView2) session + mapped label + a prior
  snapshot. Browser-only or unmapped Nodes show **only Update Time**.
- **Privacy**: precise stamps are screenshot/screen-share visible; local-tz reduces UTC-leak only
  marginally. Accepted under the always-on-inspector decision, but noted.

## Open questions to resolve here

- **T-Q1**: De-dup equality tolerance — exact ticks, or same-second?
- **T-Q2**: Timezone render precision and format (date+time to the second? minute? relative?).
- **T-Q3**: Is a real last-sync-operation *timestamp* wanted (new persistence), or is `lastOp` as a
  word sufficient (➡️ word only)?
- **T-Q4**: Line order when several apply (Update Time first, then Workspace, then Server, then Last
  sync?).
