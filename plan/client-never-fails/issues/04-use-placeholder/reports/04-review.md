# 04 — Use a placeholder

Ticket: [[plan/client-never-fails/issues/04-use-placeholder/04-use-placeholder-ticket.md]]
Spec: [[plan/client-never-fails/spec.md]]
Arch: [[plan/client-never-fails/arch.md]]

Range: `git diff f652d52^ 008ff751` for the files in commit `f652d52` and commit `008ff751`.

## 1. Standards

- **Scan.** The standards scan on that diff printed two `BARE_ID` lines. Both lines are the color `#444444`. [[.agents/rules/refer-by-name.md]] covers a ticket id. The color is a color value. The scan printed no line-length, function-length, file-length, tab, mutable, or banned-phrase hit.
- **Limits.** Each function this diff touches is 40 lines or fewer. `execRunOp` in `src/Client/Commands.fs` is 40 lines (124–163). Each touched source file stays under 800 lines. `src/Client/Commands.fs` goes from 429 lines to 440. `src/Client/UpdateOps.fs` goes from 777 lines to 776.
- **Selection ids.** **Judgement.** Duplicated Code. The same id list appears in three hunks. Source: [[plan/context/SMELLS.md]].
  - `copyOp` in `src/Client/Commands.fs` builds `rangeChildren model.graph sel.range |> List.map (fun child -> child.id)`.
  - `onCopy` in `src/Client/Controller.fs` builds that same expression.
  - `selectedNodeIds` in `src/Client/UpdateOps.fs` builds that same expression.
- **Class seed.** **Judgement.** Duplicated Code. `tryInitialUserClasses` in `src/Shared/CssClassToggle.fs` treats a missing Header as `None` with `Map.tryFind`. `nodeHeaderIsAbsent` in `src/Shared/CommandRequest.fs` is that same test. Source: plan/context/SMELLS.md. The hunk is `| None -> None`.
- **Class list append.** **Judgement.** Overcomplication. That same function grows the class list with `@` on each id. Source: plan/context/SMELLS.md. The hunk is `read (acc @ userClassNames node) rest`.

## 2. Spec

- No findings.

## 3. Arch

- **Commands stay unchanged.** **Blocker.** Arch section 1, story path 4, step 3 says Copy, Edit classes, and Exec stay unchanged. The diff changes `copyOp`, `onCopy`, `openCssClassPromptOp`, `submitCssClassPromptOp`, `execRunOp`, `shouldExec`, and `runPlanOnNode` so a missing Header returns the same model. Spec story 4 says those three Commands are unavailable. The ticket says the same.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 3615` printed `DONE` and `/tmp/ambit-full-suite-20261101-123700.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 733, skipped 1, failed 0. `tests/Shared.Tests` passed 1923, skipped 3, failed 0.
- **Selection fact.** ``isEntrySelected is true for a placeholder SiteEntry with no Header`` builds a child whose id is absent from `graph.nodes`, places that SiteEntry in a selection, and asserts `isEntrySelected` is true.
- **Expand fact.** ``expandEntry leaves a placeholder row expanded`` calls `expandEntry` on a collapsed row whose id has no child-list key and asserts that row's `expanded` is true.
- **Want fact.** ``compose wants a placeholder id with no child list after parent expand`` asserts `Want.compose` returns that id. `expandEntry` runs on the placeholder instance. `buildSiteMapFrom` already expands the zoom root, so that id is already on the Included walk.
- **Edit fact.** ``startEditInstanceAtPos leaves Selecting when Header is absent`` removes the Header, calls `startEditInstanceAtPos`, and asserts the mode is `Selecting`.
- **Delete fact.** ``classifyDeleteForChildSpan moves a placeholder child with no Header to TRASH`` classifies that child as `MoveToTrash`, applies the ops, and finds the id under TRASH with the Header still absent.
- **Copy fact.** ``Copy does nothing when the selected row Header is absent`` asserts `anyNodeHeaderIsAbsent` and `serializeSubtree` returns `""`.
- **Edit classes fact.** ``Edit classes does nothing when the selected row Header is absent`` asserts `tryInitialUserClasses` is `None` for a missing Header, and `Some "note"` for a present Header whose user class is `note`.
- **Exec fact.** ``Exec does nothing when the focused row Header is absent`` asserts `nodeHeaderIsAbsent`, `shouldExec` is false, and `runPlanOnNode` is `Ok` with an empty plan.

## 5. Miss

- **Commands stay unchanged.** Blocker. Arch item 1. Story path 4 step 3 says Copy, Edit classes, and Exec stay unchanged. The code makes those Commands unavailable.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Problem.** `plan/client-never-fails/arch.md`, story path 4, step 3, says Copy, Edit classes, and Exec stay unchanged because each needs the Node text.
- **Fix.** Reword that step so those three Commands are unavailable on a placeholder Bullet because each needs the Node text. This matches spec story 4.
- **Accepted.** The code follows spec story 4 and the ticket. The arch sentence is the stale line.
- **Problem.** `collectSubtree` in `src/Shared/Paste.fs`, and Cut through `onCut` in `src/Client/Controller.fs`, still read `graph.nodes.[nodeId]` when a selected child has no Header.
- **Fix.** Give Cut the same absent-Header return as Copy. This ticket changes Copy, Edit classes, and Exec only.
- **Accepted.** Cut still indexes the node map. This ticket's Outputs name Copy, Edit classes, and Exec.
- **Problem.** `plan/client-never-fails/map.md`, Absent display, says Copy, Edit classes, and Exec stay unchanged.
- **Fix.** Use the spec story 4 sentence: those three Commands are unavailable on a placeholder Bullet because each needs the Node text.
- **Accepted.** Commit `f652d52` left that map sentence in place. Commit `008ff751` reworded spec story 4 and left the map sentence.
