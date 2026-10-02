# Directory reconcile and Actor

2026-10-01. Ownership rename was already on disk. This note records Alan's five decisions.

## 1. Result

Parse is not listed as an Actor. Denial sentences are gone. New nodes append alphabetically on [Parse thread architecture](../arch.md) §2 Module map, item 1 **Directory reconcile**, Interface item 2 **Create**. Pull then Parse sits on [03 — Workspace Load after incoming files](../issues/03-workspace-load-after-incoming-files.md) §2 Answer. Poll stays open. Browser want stays open. Stage stays arch. No commit.

## 2. Actor: deleted

These sentences only denied the pairing. They are gone. Nothing replaced them.

- [core-refinement architecture](../../core-refinement/arch.md) §3 step 2: "Parse is not an Actor." The "; it is not an Actor" clause on the background loop. §7 Vocabulary item 6 "Say parse thread, not parse actor."
- [core-refinement map](../../core-refinement/map.md): the "not an Actor" clause, and "Parse is not an Actor".
- [03 — One Parse thread stack](../../core-refinement/issues/03-one-parse-thread-stack.md) Answer: "Parse is **not** an Actor".
- [02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md) and [05 — Selection-scoped Parse after whole-tree git Load](../../core-refinement/issues/05-selection-scoped-parse-after-whole-tree-git-load.md): the wording comments that only said Parse is not an Actor.
- [06 — Explicit parse command on a File (Load)](../../core-refinement/issues/06-explicit-parse-command-load-file.md): the "Parse loop is not an Actor" clause.
- [Parse thread architecture](../arch.md) §1 Story paths, story path 6 **No Actor after pull**, the hop **Not an Actor**.
- [Actors](../../../doc/current/actors.md) and [Parse and persist](../../../doc/current/parse-persist.md): "Parse thread: a thread."
- [initial core changes implementation](../../core-creation/initial-core-changes-implementation.md): "Do not make Parse an Actor".

"Nobody starts an Actor after pull" stays. That is the pull rule.

## 3. Actor: corrected

Parse was removed from Actor lists. The rest of each sentence stays.

- [Event-sourced ops architecture](../../event-sourced-ops/architecture.md) **Roles**: the Parse File job is no longer an Actor example. Same cut in [overview](../../event-sourced-ops/overview.md).
- [Actors and jobs](../../event-sourced-ops/details/actors-and-jobs.md): Parse left the Actor list. The Parse File section is no longer "the first Actor". Shell command is no longer "the same kind" as Parse.
- [Soft lock](../../event-sourced-ops/details/soft-lock.md): Parse File left the long-running Actor example.
- [08 — Parse File realignment (tracer bullet)](../../event-sourced-ops/issues/08-parse-file-realignment-tracer.md): dropped "fits the long-running Actor shape".
- [Transport layer overview](../../transport-layer/overview.md) and [transport parse-persist](../../transport-layer/details/parse-persist.md): Parse File is no longer a Server Actor.
- [Actors supported](../../roadmap/epics/chapters/actors-supported.md): Parse File is no longer "the first Actor definition".
- [here-to-there](../../github-transport/here-to-there.md): "Parse actor" is now the parse thread, and the home is [Parse thread](../project.md).
- [Actor as client map](../../actor-as-client/map.md): Parse left the Actor migration list.

## 4. Directory reconcile

1. **Changes from directory reconcile** stays open. No Poll design.
2. **Alphabetical append** is only Interface item 2 **Create** on [Parse thread architecture](../arch.md) §2 Module map, item 1 **Directory reconcile**. [core-refinement architecture](../../core-refinement/arch.md) §5 item 7 **Create special** still says a new special node starts Unparsed and Persisted.
3. **Pull then Parse** is [03 — Workspace Load after incoming files](../issues/03-workspace-load-after-incoming-files.md) §2 Answer. Status is done. A Workspace Node uses Directory reconcile. There is no second workspace body.
4. **Browser want priority versus directory reconcile** stays `defined`. Alan: this is poll. Not addressing.

## 5. Left alone

Reports, including [arch-match-core-and-ops](arch-match-core-and-ops.md). `plan/done`. Stage stays arch.
