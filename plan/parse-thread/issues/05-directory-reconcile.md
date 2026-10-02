# 05 — Directory reconcile

**Type:** coding
**Status:** coded
Actual: 1.5h
**Blocked by:** None — can start immediately

## Context

A person Loads a Directory Node. Core also pushes a Workspace Node after files land. The Body can lack a disk member. Disk can be newer than a File Node. The parse thread already turns a File Node into Graph. This ticket adds the directory scan that updates the Body.

The open story paths are on [Parse thread architecture](plan/parse-thread/arch.md): **Same as a Workspace**, **Spot missing disk members**, **Directory File walk**, **Nodes below the immediate children**, and **Create missing File Nodes**. Story path **Structure-match is ready to code later** names this coding. Shared segment **Directory into Graph** uses this scan. Test seam **Directory reconcile** is the door the tests cross.

[core-refinement architecture](plan/core-refinement/arch.md) §5 item 10 **Directory reconcile** is the deferred pointer. Pull then Parse stays the handoff in [02 — Git Load: Unparsed then Parse stack](plan/core-refinement/issues/02-git-load-unparsed-then-parse-stack.md). Poll and Browser want stay off this ticket. [04 — Browser want priority versus directory reconcile](04-browser-want-priority-versus-directory-reconcile.md) does not block this work.

## What to build

Directory reconcile scans the directory and updates the Body. The Body is the nodes. The scan does not read the Directory File. A read of that `.amb` file is Directory parse. Directory parse is not this ticket. A new node appends in alphabetical order under the Directory Node. A Workspace Node uses the same Directory reconcile as a Directory Node. There is no second Body for a Workspace Node. When Directory reconcile is done, the parse thread adds InMsg ParseFinished through the private function. The parse thread does not edit the graph axes. Tests cross the Directory reconcile seam. Tests do not start the mailbox, the workspace lock, or the persist thread.

State, Interface, and Uses stay on [Parse thread architecture](plan/parse-thread/arch.md) §2 Module map, item 1 **Directory reconcile**. The parse thread calls Directory reconcile when the popped node is a Directory Node.

### 1. Directory reconcile

Directory reconcile takes the disk directory, the graph, and the directory id. The directory id plus the graph is the Directory Node. Directory reconcile reads the Body from that graph. The disk directory is required. It walks that Body, including nodes below the immediate children. It creates a File Node when the Graph lacks a disk member.

1. [x] Inputs — Directory reconcile takes the disk directory, the graph, and the directory id. The directory id plus the graph is the Directory Node. Directory reconcile reads the Body from that graph. The disk directory is required.
2. [x] Whole tie — The walk covers the Body. That Body is every node tied to the Directory File, including nodes below the immediate children.
3. [x] No extra info — Structure-match uses the Body and the Graph. It does not take a member hint list.
4. [x] Missing File Node — After Directory reconcile, a disk member that the Graph lacked has a File Node.
5. [x] Create — Directory reconcile returns ops that create each missing File Node. A new node appends in alphabetical order under the Directory Node.

### 2. Workspace Node

A Workspace Node uses this same scan. Pull then Parse stays the existing handoff. This ticket does not design a second Body for a Workspace Node.

1. [x] Workspace — A Workspace Node uses the same Directory reconcile as a Directory Node. There is no second Body for a Workspace Node.

### 3. ParseFinished

When Directory reconcile is done, the parse thread adds InMsg ParseFinished for that Directory Node through the private function. The parse thread does not edit the graph axes. The core loop is [core-refinement architecture](plan/core-refinement/arch.md) §10 Core loop.

1. [x] ParseFinished — The parse thread adds InMsg ParseFinished for that Directory Node through the private function. The parse thread does not edit the graph axes.

### 4. Tests

Tests prove Directory reconcile at its seam.

1. [x] Directory reconcile seam — Tests cross the Directory reconcile seam. Tests do not start the mailbox, the workspace lock, or the persist thread.

## See also

[Parse thread spec](plan/parse-thread/spec.md)

[03 — Workspace Load after incoming files](03-workspace-load-after-incoming-files.md)

## Comments

- 2026-10-01 — Alan. The Load command pulls files to the server, then runs Parse. Parse for a Directory and Parse for a Workspace are identical: Directory reconcile. Implement as such.

## Time

- 2026-10-01 1.5h — Directory reconcile scan, parse-thread call, and seam tests (from chat)
