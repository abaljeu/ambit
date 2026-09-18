Code review of `HEAD` (`394ad65`), section 1 of issue 34. Later mailbox direction (handlers, credentials, `LaunchRequest`) is still in flight with [Replace LaunchRequest](6873313f-57a5-4434-86dd-6cfa0b27d143) and is not part of this range.

## Standards

Range: `394ad65` vs `HEAD^`. F# measure (`measure-fs-size.sh --diff HEAD^`): **no added lines >100**; files ≤400 (`CoreMailboxBackend.fs` 343, `CoreActorPool.fs` 275). New helpers ≤17 lines.

### Hard

**`dispatch`** (`src/Server/Core/CoreMailboxBackend.fs` 217–257, 41 lines) — `.agents/rules/fsharp-source.md` “40 lines or less per function.” Was 38 at `HEAD^`; extracting `dispatchPostChange` was not enough after the new `StartActor` / `ActorStop` arms.

### Judgement (SMELLS.md)

**Parameter Explosion** — `dispatch` now 6 args, `dispatchPostChange` 7. `ActorMailboxHandlers` is the right grouping (named type); leftover `authority`+`secret` is a pre-existing **Data Clump**.

**Duplicated Code** — every test in `CoreMsgActorCasesTests.fs` repeats temp-dir / host / `try`/`finally` dispose.

## Spec

### (a) Missing or partial

**ActorStop does not append ActorFinished.** Spec: “`ActorStop` of `ActorResult` — `ActorSucceeded` only in this slice; append ActorFinished on History; drop live row and secret; request terminate without waiting.” Drop/cancel via `CoreActorPool.finish` are present. Nothing appends ActorFinished. The implement report moved that to section 4; section 1 still names the append (same sentence on arch CoreMsg Interface 5). History *module* work (sequence, Undo) can stay in section 4; the mailbox case was required to trigger the append.

### (b) Scope creep

**Legacy `launch` writes a live row** (`PutLive` after plan). Section 1 asked for mailbox Actor cases and “live-row check against CoreActorPool table,” not a change to the old launch path.
Old launch path should be deleted.

**`startActor` mints a secret and writes a live row** (secret never returned; no expand / select / schedule). Spec item 1 is only “async handoff to CoreActorPool.startActor.” “create identities and secret; write live row” is section 3. Useful so `fromPool` can admit; still extra pool-start behavior.

### (c) Implemented but wrong

**StartActor handoff is synchronous.** Spec: “validate caller Authority and secret; async handoff to CoreActorPool.startActor.” `34b`: “hand the accepted start to CoreActorPool without holding the mailbox.” `dispatchStartActor` does `actors.startActor request |> Async.RunSynchronously` on the mailbox thread, then replies. That holds the loop (and will deadlock if later `startActor` posts back to the mailbox).

---

**Summary:** Standards — 2 hard (worst: `dispatch` at 41 lines), several judgement. Spec — 1 missing, 2 creep, 1 wrong (worst: StartActor holds the mailbox with `RunSynchronously`). This report is not approval; ticket status is unchanged.