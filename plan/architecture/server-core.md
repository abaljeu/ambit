# Server Core

Updated: 2026-09-30

This page is the compact description of the Server Core target Alan locked on 2026-09-28. It states what Core deals with and what stays outside Core. Sole authority for the Core seam (expand-contract, axes detail, stacks, path control, mailbox git handoff, locking model) is [core-refinement architecture](plan/core-refinement/arch.md). That master and this page share one meaning; if they ever drift, the master wins.

## 1. Dated note

1. **2026-09-28 inbound** — Alan locked this Server description. This lock does not chart coding tickets.
2. **2026-09-28 correction** — Git use is Core. Git Load, git Save, pull, push, and commit as the Server uses them are Core responsibilities, not outside work.
3. **2026-09-30 roles** — Compact description stays on this page. Core seam authority stays on [core-refinement architecture](plan/core-refinement/arch.md).

## 2. Core deals with

1. **Database backend** — The Graph modifies the database backend. The database stores Events.
2. **Graph** — The Graph stores on the database. The mailbox modifies the Graph.
3. **Events** — Events store on the database. The mailbox creates Events. The Event sequence is the event source. An Event notes Graph ops, Actor events, file events, and git actions.
4. **File system** — File-system objects correspond to Graph objects. Before a file update and after a file update, the Graph must signal which elements that work uses. File work may take time, but the time is not unlimited. An extended process delays file work until the work is certain. Then it sends a message to the mailbox. Only Core changes files, and only Core changes the graph.
5. **Git** — Git use is Core. Core performs and controls git Load, git Save, pull, push, and commit as the Server uses them. An Actor may request git work by posting to the mailbox. Core performs that work. This is the same pattern as the file system. A Server git Actor is an external function that posts the request. It does not perform git mutations itself.
6. **Actors** — Actors are functions defined outside Core. An Actor receives a Graph. An Actor runs on a thread. An Actor sends messages to the mailbox.
7. **Mailbox** — The mailbox controls access to the Graph, Events, files, git, and Actors. It receives messages. It converts each message to Events by actioning that message. Message processing is fast. Graph modifications are simple and synchronous. Other work, including git work, runs in the background and may have an end Event. The mailbox sends an immediate response.


## 3. Drift axes, parse thread, and locks

Compact markers that match the master. Detail and catch-up sequence stay on [core-refinement architecture](plan/core-refinement/arch.md) §1, §3, and §6.

1. **Axes** — Special nodes (Workspace Node, Directory Node, File Node) each carry Parse Status (Parsed|Unparsed) and PersistenceStatus (Persisted|Unpersisted). Unparsed means information on disk has not been pulled into the graph.  Unpersisted means information in graph has not been written to disk.  Both can be true at once.  These are informational, not locks
2. **Parse and persist threads** — Runs when a node is Unparsed, and sets Parsed on completion. Persist runs when a node is Unpersisted and Parsed, and sets Persisted on completion. Other operations set Unparsed and Unpersisted.
3. **Locks** - Locks are used to prevent race conditions between these threads and also other operations that access files.  Locks are held briefly; only long enough to transfer data.  No one holds more than one lock, but aggregate locks exist.  The Workspace lock for example: announces that member files may change. While pending, new persist locks for those member files cannot be taken. When in-flight member file writes and parse reads have drained, pull proceeds, arrived files are marked Unparsed, and the lock releases. Per-member persist locks are additional protocol beside the axes. Today's exclusive `withWorkTreeGate` is not the lasting work-tree protocol; stand the workspace lock, then contract that gate (master §3 **§6 locks catch-up**).

## 4. Outside Core

1. **Define Actors** — Actor function bodies stay outside Core. A Server git Actor may request git Load or git Save by posting to the mailbox. Core performs and controls that git use.
2. **Outside I/O** — Send and receive HTTP and other outside traffic that is not git use. Git Load, git Save, pull, push, and commit stay Core.

## 5. Related

1. **Map** — [[map.md]]
2. **Core seam master** — [core-refinement architecture](plan/core-refinement/arch.md) — sole authority; expand-contract (§3), axes (§1), locking model (§6).
3. **Actor as client** — [[plan/actor-as-client/project.md]] — concept-only; this description confirms privilege-less Actors that only post to the mailbox.
4. **Core creation** — [[plan/core-creation/project.md]] — existing Core baseline.
5. **Mailbox clear-fast** — [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]
6. **Step 1 implement** — [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md). Further Core revision: [[plan/core-refinement/project.md]]. Git use stays Core on this page.
