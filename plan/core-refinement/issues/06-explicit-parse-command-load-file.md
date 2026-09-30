# 06 — Explicit parse command on a File (Load)

**Type:** coding
**Status:** defined
**Blocked by:** None — can start immediately

## Context

A person issues an explicit parse command on a file (today’s `ParseFile` / `postParseFile`). Core must accept that as mailbox **Load** whose subject is a File node, and run parse through the new Parse actor stack beside today’s hop. Expand only: new mailbox Load, new stack, new loop; the old parse body stays. Contract of old paths is later.

## What to build

Expand on [[../arch.md]] §3 step 2 Parse actor and stack for this first use case. Load arrives on the mailbox queue. Subject is a File node. When executed, it asks the parse function to parse the file. That request is handled by pushing onto a stack. A loop pulls from that stack and runs the old parse function (`DocumentPersistWrite.planParseFile` — the body `postParseFile` already uses in [[src/Server/Api.fs]] / [[src/Server/DocumentPersistWrite.fs]]). That background loop cannot work except on a separate thread, so the loop is an Actor function ([[plan/architecture/server-core.md]] §2 Actors — function outside Core, runs on a thread, posts to the mailbox). This is why the Parse actor owns the loop. Persist is not an Actor. Existing old paths do not need to be removed yet.

### 1. Mailbox Load of a File node

- [ ] Load on mailbox queue — Load arrives on the mailbox queue.
- [ ] File node subject — Subject is a File node.
- [ ] Ask parse — When executed, it asks the parse function to parse the file.

### 2. Parse stack and loop

- [ ] Push onto stack — That request is handled by pushing onto a stack.
- [ ] Loop on Actor thread — The background loop that pulls the stack and runs the old parse function cannot work except on a separate thread; that implies the loop is an Actor function.
- [ ] Loop runs old parse — A loop pulls from that stack and runs the old parse function (`DocumentPersistWrite.planParseFile`).
- [ ] Keep old paths — Existing old paths do not need to be removed yet.

## Out of scope

1. **Contract old parse paths** — Do not remove today’s Load → Parse / graph-push hop or other old parse callers.
2. **Later sequence steps** — Persist collectors, DataDir / path control, and git requests stay on later [[../arch.md]] §3 steps.

## See also

[[../arch.md]] §3 step 2 Parse actor and stack; [03 — One Parse actor stack](03-one-parse-actor-stack.md); [05 — Selection-scoped Parse after whole-tree git Load](05-selection-scoped-parse-after-whole-tree-git-load.md)

## Comments

- 2026-09-29: Specced from Alan’s first use case (explicit parse command on a file). Expand only; Status `defined`.
- 2026-09-29: Alan — background parse loop needs a separate thread, so the loop is an Actor function.
