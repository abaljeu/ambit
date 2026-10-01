# Parse / Upload for Current Files (Warm Reconcile)

**Type:** grilling
**Status:** defined
Blocked by: None

## Question

Reconcile [Parse / Upload for Current Files (Warm Reconcile)](doc/roadmap/parse-file-reconcile-current.md) with existing plans and with [[doc/current/]]. Decide which claims are current behavior, which stay planned, and which home owns the leftover. Leave the file at that path. This filing does not merge the claims.

## Contradictions

- The doc Status says server-apply ParseFile landed (Shared, Client, and Desktop verified), with a Server.Tests rebuild note.
- [[doc/index.md]] has no feature page for Parse / Upload or warm reconcile. [[doc/current/]] has no page that states this command.
- [Parse thread](plan/parse-thread/project.md) is the Parse product home. [Core refinement architecture](plan/core-refinement/arch.md) is the sole authority for the Core seam. This doc still describes the command path (`POST /ambit/file/parse`, client posts `fileId` plus optional text). Whether leftover product text moves to Parse thread is open. Chart it here first.

## Comments

- 2026-10-01 — Filed from Alan. Map: [[../map.md]]. Source stays [[doc/roadmap/parse-file-reconcile-current.md]].
