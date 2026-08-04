# 16 — Rename Upload to Load

**What to build:** Present the existing Upload command and synchronization workflow as Load without changing what it does, while reserving Upload terminology for genuine protocol and file-transfer behavior.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Users can invoke Load through the existing `Ctrl+Shift+>` shortcut and every former user-facing Upload command entry point now uses the Load name.
- [ ] Load preserves the existing target filters, synchronization stages, stage ordering, desktop push, parsing, and reconciliation outcomes.
- [ ] A Load requested during synchronization is represented as QueuedLoad and active remote synchronization is represented by the one global Loading state.
- [ ] No queued-command or synchronization-state surface retains the former Upload or Uploading names.
- [ ] Protocol and file-transfer behavior that genuinely uploads data continues to use Upload terminology where that meaning remains accurate.
