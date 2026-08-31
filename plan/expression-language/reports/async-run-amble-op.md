# Can `runAmbleOp` easily be made async?

Investigation only. No product code changed. Written from `selective-client-sync` (clean tree) into this project's reports folder; preferred home is [[plan/expression-language/]] because Run / Expr eval lives here. Related: [[expr-eval-pull-enumerator-impl.md]], [[expr-eval-pull-enumerator.md]], [[run-changes-not-effective.md]].

## Verdict

**Possible but not easy** if the goal is real UI responsiveness during heavy Run. A one-line "make it async" wrapper is easy and does **not** fix long freezes.

- Easy: defer the whole op with `setTimeout` / a new `Continue*` Effect so the key handler returns and one frame can paint "busy". The CPU work still runs as one sync block on the main thread afterward.
- Not easy: keep the UI interactive while eval walks a large Graph. That needs time-sliced pulls of the existing `ExprEval.Stream`, a Run-in-progress model, cancel/race rules, and staged apply — a Client + Shared consumer redesign, not a signature change on `runAmbleOp`.

## 1. Call path (sync on the UI / dispatch thread)

`runAmbleOp` is registered as command `Exec` via `keyAlways runAmbleOp` in [[src/Client/Commands.fs]].

Keyboard path:

1. [[src/Client/Controller.fs]] `handleKey` → `dispatchResolvedKey`
2. `dispatch (ApplyOp (withDiagnostic … runAmbleOp))` (or bare `ApplyOp`)
3. [[src/Client/App.fs]] `dispatch` calls [[src/Client/Update.fs]] `update`, which for `ApplyOp op` runs `op model` **synchronously**
4. Same turn: `patchDOM` / chrome render, then `runEffects`

There is no Elmish `Cmd`. There is no worker. The update function returns `VM * Effect list` and completes before paint yields, except where an Effect itself schedules later work.

So yes: Run today blocks the same main-thread turn that handles the key and patches the DOM.

## 2. What `runAmbleOp` does

[[src/Client/UpdateAmbleRun.fs]]:

1. `commitIfEditing` — may read the edit DOM and `applyAndPost` a text Change (sync CPU + enqueue `SubmitPendingBatch`).
2. `AmbleRun.runPlanOnNode` — Shared pure plan from the focus Node's text ([[src/Shared/AmbleRun.fs]] → [[src/Shared/ExprRun.fs]]).
3. `applyRunPlan` — `applyAndPost` for the plan ops, `withSiteMap`, optional `AmbleRun.applyUnfold`.

Work profile:

- **CPU-heavy**, not I/O-bound. Eval walks the resident Graph (descendant / tree / content search). Network POST is already an Effect (`SubmitPendingBatch` via async `postJson` in App), so waiting on the server is not the sync problem.
- **Not recursive Client code**; recursion / pull lives in Shared walks and Stream combinators.
- **Already produces Effects** for sync/post — but planning and local apply stay inside the Updater.
- Cap: [[src/Shared/ExprRun.fs]] `maxMaterialisedAnswers = 50` via `ExprEval.take`. That bounds Children written, **not** worst-case walk cost (sparse matches, `AND` materialising the right operand with `toList`, large Loaded trees).

## 3. Existing Client async pattern

Not Elmish `Cmd.ofAsync` / `Cmd.ofPromise`. Pattern is:

- Updater returns quickly with a deferred Effect (`ContinueWorkspacePush`, `ContinueParseFile`, …).
- [[src/Client/App.fs]] `runEffect` uses `setTimeout` (often 50 ms) then `postJson` / fetch, then `dispatch (ApplyOp continuation)`.

Comments in App explicitly say delay past the current frame so UI can paint (e.g. Uploading). Search dialog debounce uses `window.setTimeout` in view code. Clipboard uses promise interop. Boot / client-start-time work uses the same `setTimeout 0` defer idea ([[plan/client-start-time/reports/bucket-3-post-state-work.md]]).

No `requestIdleCallback` / `requestAnimationFrame` Run scheduler today. Dom bindings expose `requestAnimationFrame` but Run does not use it.

## 4. Shared eval and chunking

Pull enumerator **is already in Shared** ([[src/Shared/ExprEval.fs]] `Stream`, `take`, walks that resume). See [[expr-eval-pull-enumerator-impl.md]].

That helps **Search-style paging** and Run's 50-Answer cap. It does **not** by itself yield the browser: Client still pulls up to 50 Answers (and may force large `AND` rights) inside one `runAmbleOp` call.

Chunked / scheduled Run would mean: Client pulls N Answers or works for T ms, schedules another Effect/Msg, repeats, then applies one Change when done (or applies incrementally — much harder with sync/undo).

## 5. Easy vs larger refactor

| Path | Effort | Yields UI? |
| --- | --- | --- |
| Mark Updater "async" in type only | n/a | No |
| New Effect + `setTimeout` then run full `runAmbleOp` body | small | One frame before freeze; freeze remains |
| Status Mode "Running…" then deferred full run | small–medium | Same as above, better feedback |
| Time-slice `ExprEval.pull` / `take` across timeouts/rAF, then one `applyAndPost` | medium–large | Yes, if slices stay short |
| Web Worker for eval | large | Yes for CPU, but Graph cloning / Fable worker boundary / apply races |

There is **no** existing `ContinueAmbleRun` Effect. Adding one is the smallest architectural match to workspace push / parse.

## 6. Smallest change that would actually yield the UI

**Minimum that yields anything useful:**

1. On Exec: sync `commitIfEditing` only if required for text fidelity (or defer carefully — see [[run-commit-edit-before-exec.md]]).
2. Set a visible busy / Running state and return an Effect that `setTimeout`s (0 or 50).
3. Effect callback: `dispatch (ApplyOp completeRunAmbleOp)` that does plan + apply + unfold.

That matches App's Continue* pattern and lets one paint show busy. It does **not** keep the UI responsive during a multi-second walk.

**Minimum that keeps the UI responsive during heavy eval:**

1. Compile once; hold `ExprEval.Stream` (or leftover after partial `take`) in model or a Client cache keyed by revision + focus + text.
2. Each slice: pull a small page or budget, schedule next Effect.
3. When stream empty or hit cap 50: single `applyAndPost` + unfold.
4. Cancel token / generation id so a second Exec or intervening graph Change drops stale completion.

That is the real responsiveness path; Stream already exists for step 1–2.

## 7. Risks

- **Race with other ApplyOps:** between schedule and complete, cursor/edit/poll/ack can change `model`. Completing against a stale VM can double-apply, clobber edits, or post against the wrong revision.
- **Cancelled / superseded runs:** need a generation counter or run id; ignore late completions.
- **Double-apply:** two Exec presses before the first finishes → two Replace Children plans if not gated by Mode or busy flag.
- **commitIfEditing timing:** deferring commit loses the "commit then Run" guarantee unless commit stays sync or the deferred plan re-reads committed text from the model after commit lands.
- **applyAndPost / pending queue:** local apply must stay single-threaded and ordered with other Changes; only the **planning** phase should be sliced. Do not post partial Children mid-run unless that is an explicit product rule.
- **AND / `toList`:** one pull can still do unbounded work; time-slicing at Stream boundaries is incomplete until expensive combinators cooperate.

## 8. Existing plan / docs

- No ticket titled "async amble run". Closest work is Expression Language Stream / Run cap ([[expr-eval-pull-enumerator-impl.md]]) and hang fix for self-Ref unfold ([[plan/expression-language/issues/27-run-star-containing-self-ref-hang.md]]).
- Client responsiveness patterns live under [[plan/client-start-time/]] (defer post-paint work), not Run-specific.
- [[plan/large-node-cursor-perf/]] is DOM/selection cost, not Expr eval.

## WORK.md mutations

None for the parent unless they want a Pending item for time-sliced Run. Suggested artifact if added: this report.
