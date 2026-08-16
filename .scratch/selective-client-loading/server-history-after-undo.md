# Does the Server still record a History list?

**Yes — the Server no longer records a History list.** Slice 5 deleted the undo/redo stacks that used to fill `History.past` / `History.future`. Apply still mutates the graph and appends durable Changes; it does not append to those lists.

Contract: [[undo-spec.md]] item 1 — Server applies, persists, confirms, Polls, and Loads ordinary Changes and keeps no Undo state.

## 1. Undo/Redo stack (deleted)

Gone: `History.addChange`, `History.undo` / `History.redo` (the stacks), `applyAction`, `ChangeRequest`. `History.applyChange` now only applies the Change and validates ownership; it does not record History.

```629:639:src/Shared/History.fs
    let applyChangeTrusted (change: Change) (state: State) : ApplyResult =
        Change.apply change state

    let applyChange (change: Change) (state: State) : ApplyResult =
        match applyChangeTrusted change state with
        | ApplyResult.Invalid _ as err -> err
        | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
        | ApplyResult.Changed s ->
            match validateOwnershipForChange s.graph change with
            | Error msg -> ApplyResult.Invalid(state, msg)
            | Ok () -> ApplyResult.Changed s
```

`Change.apply` walks Ops against the graph and returns the same `State` shape; it never writes `past`/`future`. FileAgent and DbAgent call `History.applyChange`, then bump `revision` and persist the Change. They never touch `state.history`.

`Op.undo` / `Change.undo` still exist as invert-apply of one Change against a graph. Those are not stacks and the Server apply path does not call them.

Browser undo/redo lives in [[src/Shared/ClientHistory.fs]] (`ClientHistory.past` / `future` of `HistoryRecord`). That is client-only.

## 2. `State.history` field (unused empty leftover)

Still present so Graph apply still has a `State` value:

```29:38:src/Shared/History.fs
type History =
    { past: Change list
      future: Change list
      nextId: int }

type State =
    { graph: Graph
      history: History
      revision: Revision }
```

`History.empty` is `{ past = []; future = []; nextId = 0 }`. Every Server (and most Shared) `State` construction sets `history = History.empty` — Database load, DocumentLoader, SavePrep, ModelBuilder, SyncLogic projection. Nothing on the Server path assigns `past` or `future`. Those lists are not a recorded History; they stay empty.

`History.newChange` still reads `nextId` to mint a Change. With empty History that id is always 0; it is not stack recording.

Client `VM` / `ClientSyncState` use a different field: `history: ClientHistory`, not `State.history`.

## 3. ChangeLog / durable ordered Changes (still there)

This is not a History stack. The Server still appends complete Changes for Poll, Load, persist, and dedup.

- FileAgent: `ChangeLog.appendEntries` after apply; `GetChangesSince` reads the log.
- DbAgent: `Database.appendChangeWithTx` into the `changes` table; `GetChangesSince` reads payloads.
- Api `getPoll` returns those Changes when revision is ahead.

Undo and Redo arrive as ordinary Changes on that same log. The Server does not know they were undo.
