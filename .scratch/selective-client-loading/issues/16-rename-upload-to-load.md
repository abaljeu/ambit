# 16 — Rename Upload to Load

**What to build:** Present the existing Upload command as Load without changing what it does. Load is the umbrella command with three parts: Upload files to server, parse server files to graph, and fetch graph nodes to the client. Reserve Upload terminology for the file-transfer part.

**Blocked by:** None — can start immediately.

**Status:** agent-done

- [x] Users can invoke Load through the existing `Ctrl+Shift+>` shortcut and every former user-facing Upload command entry point now uses the Load name.
- [x] Load preserves the existing target filters, synchronization stages, stage ordering, desktop push, parsing, and reconciliation outcomes.
- [x] A Load requested during synchronization is represented as QueuedLoad (web parse/reconcile / create-workspace). Desktop push keeps QueuedWorkspacePush so its scope is preserved.
- [x] Active file-to-server push remains Uploading (status and sync-state); do not rename that phase to Loading.
- [x] Protocol and file-transfer behavior that genuinely uploads data continues to use Upload terminology where that meaning remains accurate.
