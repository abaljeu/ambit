# Dissolve the decision record in CONTEXT.md

Date: 2026-11-04. Branch `cursor/actor-as-client-arch-work-7c6b`, draft PR #521. Not committed.

The `## Decisions 2026-11-03` section of `plan/actor-as-client/CONTEXT.md` had 43 entries. Each row below names the entry by its old line number, says whether it was dropped or moved, and names the file that holds it now.

## Count

- Dropped as redundant: 41.
- Moved: 2. Line 21 went to `arch.md`. Line 53 went to `map.md` section 5.
- Corrected in their home: four lines in `map.md` section 6, for entries 38, 48, 50, and 51.

## Per entry

| Line | Mark | Entry | Result | Covered by or moved to |
|---|---|---|---|---|
| 13 | [x] | `startFunction` is the one pool start | Dropped | `arch.md` CoreActorPool `[x] startFunction`; ticket 15 One job |
| 14 | [o] | `startActor` is the original start | Dropped | `arch.md` CoreActorPool `[o] startActor` |
| 15 | [o] | `startPeerActor` was never supposed to exist | Dropped | `arch.md` CoreActorPool `[o] startPeerActor` |
| 16 | [o] | Input is `FunctionRun` and `CoreChangesDoor` | Dropped | Replaced by line 17; `arch.md` `FunctionActor` type |
| 17 | [x] | `FunctionActor` takes `ActorChanges`; `CoreDoor.actorChanges`; door, host, and caller stay in Server | Dropped | `arch.md` sections 2 and 3 CoreDoor; ticket 06 Do not load |
| 18 | [x] | Find and Move posts `Order.ActorOrder`, which runs the Search Actor | Dropped | Ticket 09 lines on `recordSearchStart`; `map.md` section 6 line for 09 now says it too |
| 19 | [o] | `ActorOrder` carries `commandNodeId` and `commandName` | Dropped | Replaced by lines 21 and 22 |
| 20 | [o] | Those names are provisional | Dropped | Replaced by lines 21 and 22 |
| 21 | [x] | The word after `?` is the actor name; `actorNameFromText` already extracts it | Moved | `arch.md` new `### CommandRequest` with `actorNameFromText : string -> string option`, and a module map line. The "word after `?`" half was already on `arch.md` |
| 22 | [x] | The `?` line is the actor node; `actorNodeId`; wire key stays `"commandId"` | Dropped | `arch.md` `ActorOrder` note, `FunctionStart` rename line, `encodeActorOrder`; ticket 17 |
| 23 | [x] | `isCommandText` keeps its name; editor `CommandId` and the wake field are not renamed | Dropped | `arch.md` naming debt line; ticket 17 Do not load |
| 24 | [x] | `FunctionActor` is a Shared type | Dropped | `arch.md` section 2 move line |
| 25 | [x] | `?test` Run is `execRunOp`; `postActorStart` posts `ActorOrder`; AI uses the same path | Dropped | `arch.md` Commands, TestActor, `postActorStart`, flow path 2; tickets 07 and 08 |
| 26 | [o] | Start provides `ActorStart` of five fields | Dropped | Superseded by `ActorOrder` in `arch.md` |
| 27 | [o] | One start takes a function and FocusId only | Dropped | Superseded by `ActorOrder` in `arch.md` |
| 28 | [o] | `Order.RunActor` stores the command name and FocusId | Dropped | `arch.md` `[o] Order.RunActor`; tickets 06–16 forbid it |
| 29 | [o] | `commandName` locates the actor function | Dropped | Replaced by line 31; ticket 08 forbids the lookup |
| 30 | [o] | Endpoint builds `FunctionActor`, `commandName` version | Dropped | Replaced by line 31 |
| 31 | [x] | Each endpoint builds its `FunctionActor`; `actorName` logged as-is; no wire lookup | Dropped | `arch.md` RunAgentActor, TestActor, Api, RouteRegistration, EventJson |
| 32 | [x] | Every Order member is logged as-is; `stubActor` on wire read | Dropped | `arch.md` Bounds and EventJson `stubActor` |
| 33 | [x] | A wire-read `ActorOrder` is a log record; nothing re-runs `stubActor` | Dropped | Ticket 06 Do not load. See the unsure list |
| 34 | [o] | A new `CoreMsg` case is the start path | Dropped | Tickets 06, 08, 09, 15 Do not load |
| 35 | [x] | `ActorOrder` includes the actor function; `postActorOrder` queues it | Dropped | `arch.md` `ActorOrder` type and MailboxHost |
| 36 | [x] | The pool mints the Actor session id | Dropped | `arch.md` `putFunctionStart`; ticket 08 |
| 37 | [ ] | LLM HTTP session is in the AI Actor closure | Dropped | Ticket 08 note and Process step 2 |
| 38 | [o] | `src/Server/ActorDispatch.fs` dispatcher | Dropped | Tickets 06–15 forbid it. `map.md` section 6 line for 10 still named it, so that line now says `postLoadSaveCommand` puts `Order.ActorOrder` on the queue |
| 39 | [ ] | The pool name tables leave | Dropped | Ticket 15 One job and Process step 2 |
| 40 | [ ] | Anything else the function needs is in the closure | Dropped | `map.md` aim 4; `arch.md` SearchActor; ticket 10 Process step 3 |
| 41 | [x] | An Actor is a .NET thread-pool thread running an actor function | Dropped | `arch.md` flow path 1; `project.md` Summary |
| 42 | [o] | An Actor is the actor function | Dropped | Replaced by line 41 |
| 43 | [ ] | `runSchedule` becomes `launchPending` on 15 | Dropped | `arch.md` CoreActorPool; ticket 15 |
| 44 | [o] | Cutover adds `Order.RunActor` | Dropped | Replaced by ticket 06 |
| 45 | [ ] | Cutover delete list | Dropped | Ticket 15 One job and ticket 16 One job |
| 46 | [o] | `Order.StartActor` is a logged start | Dropped | Ticket 16 |
| 47 | [o] | `Order.LoadSave` is the git log | Dropped | Tickets 10 and 16 |
| 48 | [ ] | Find and Move logs `Order.RunActor`; 09 deletes `RecordSearchStart` | Dropped | Stale. Ticket 09 has the `Order.ActorOrder` form. `map.md` section 6 line for 09 had the same stale text and now says `Order.ActorOrder` |
| 49 | [ ] | 111 shrinks; Ambot2 owns it; it runs after 15 | Dropped | `map.md` section 6 line for 111 |
| 50 | [ ] | Ambot2 is writing the `PostGraphOnly` removal now | Dropped, home corrected | Stale. That removal is 146 and landed on staging as `6ada0c49` (PR #520). `map.md` section 6 line is now `[x]` and says so |
| 51 | [ ] | 112 is being rewritten as a ticket only, not ready to build | Dropped, home corrected | Stale. The ticket rewrite landed (PR #516). 112 is built at `e08b8c46` on `ambot2/112-place-cancel-through-placeorder` and is not on staging. `map.md` section 6 line now says so and names 06, 15, and 16 as waiting on it, matching their blockers |
| 52 | [ ] | Map section 6 records that order; no edits to 111–118 | Dropped | `map.md` section 6 last line |
| 53 | [o] | The browser `ActorStart` payload changes in this cutover | Moved | `map.md` section 5 item 6, written as the exclusion: the payload does not change in this cutover |
| 54 | [ ] | Per-run endpoints | Dropped | `arch.md` Bounds out of scope. See the unsure list |
| 55 | [ ] | Remote or cross-process start is out of scope | Dropped | `arch.md` Bounds; `map.md` section 4 item 1 |

## Other edits

- `map.md` section 6: the link titles for 09 and 10 now use the tickets' current titles, 09 — Start the query command and 10 — Start git Load and Save. The 06 title is now 06 — Log ActorOrder.
- `CONTEXT.md` Sequence: `tickets` became `ticket`, which is the element name in `plan/context/project-work.md`.

## Unsure

- Line 33: dropped because ticket 06 covers it. It is a lasting fact, so it might belong on `arch.md` EventJson once ticket 06 is built.
- Line 53: carried over. `map.md` aim 4, Note 8, and section 4 item 3 still say `ActorStart` drops `graphIds`, from the 2026-09-27 Start surface lock. That conflicts with the new exclusion.
- Line 54: `arch.md` puts per-run endpoints out of scope, but each handler now builds its own `FunctionActor`, which is close to per-run endpoints.
- Line 51: the brief and the old line said "after PR #514". PR #514 has landed on staging as `19f089fa`, so that clause was dropped.

## Not changed, and still a decision record

- `project.md` Notes 2026-11-03 has the old decision list, with `Order.RunActor` and `ActorDispatch.fs` marked `[ ]`. Its Summary line also says `Order.RunActor`.
- `README.md` has `## Decisions 2026-11-03` and `## Order`, both stale in the same way.
- `function-shaped-start.md` has the same stale list.
- `map.md` section 2 Note 1 says `Order.RunActor` stores the command name.

## Sequence check

`plan/context/project-work.md` has no `tickets` element. Its element is `ticket`, and `ticket-work2.md` is listed as the ticket-folder variant. `project.md` names no sequence, so the default applies. The default puts spec and slicing in the list, and the Project has neither listed. This is an internal Project with no `spec.md`. The tickets were split into folders, which is slicing work in `slicing-work2.md`. Slicing was not added because the gap is not obvious. The map and arch rows link to the Project's own files, and the ticket row links to the work file.

## Second pass, after Alan's answers on 2026-11-04

### 1. The other decision records

- `README.md`: I removed `## Decisions 2026-11-03` and `## Order`. The entries match the CONTEXT.md list above, so the same results apply. The 111 and 112 order lines already live in `map.md` section 6, with corrected status. I added one link, "[Arch](arch.md) is the architecture."
- `project.md`:
  - Notes for 2026-11-03: the list is gone. The remaining bullet says Alan unparked the Project, the architecture is on Arch, and tickets 06 through 17 are the cutover tickets, ordered in Map section 6.
  - The extra entries were all already covered:
    - "keeps no reader for old rows" is in ticket 16.
    - "Tickets 07, 08, 09, 10, and 80 write `Order.RunActor`" was stale. The tickets forbid `Order.RunActor`.
    - "callers move onto `startFunction`" is in tickets 11 to 14.
    - The `[o]` September hard-cut line is in `map.md` section 2.
  - Summary: the last sentence now says `Order.ActorOrder` carries that function, and the log stores it as-is.
  - `Updated` is now 2026-11-04. The link to function-shaped start now points to Arch.
- `function-shaped-start.md`: every line was covered by `arch.md` or was stale, so I deleted it with `git rm`. Three links now point to `../actor-as-client/arch.md`. They were in `plan/core-creation/arch.md`, `plan/core-creation/project.md`, and `plan/core-creation/issues/02-core-actor-pool.md`.
- `map.md` section 2:
  - Note 1 now names Arch and says section 6 holds the ticket order. It no longer mentions `Order.RunActor` or "on the Project file".
  - The `[ ]` implement-path line and section 4 item 2 now name tickets 06 through 17 and 07 through 10, because the Project file no longer lists them.

### 2. graphIds

- `map.md`: aim 2, aim 4, Note 8, section 3 item 2, section 4 item 3, and section 5 item 6 now say the same thing. The end state has no `graphIds`. This cutover's intermediate state keeps the browser `ActorStart` payload, `graphIds` included.
- `issues/04-start-surface.md`: Answer 4 says the same thing now.
- `arch.md` does not mention `graphIds`. Tickets 06 to 17 do not mention it either.

### 3. Sequence

- `CONTEXT.md` now has `- slicing: [[plan/context/slicing-work2.md]]` between arch and ticket.

### 4. Wire-read ActorOrder

- `arch.md` EventJson now has a line after `decodeActorOrder`: `[ ] A wire-read ActorOrder is a log record, used like any other logged Order member. stubActor is never run.`

### 5. Ticket 17 size

- The ticket file now has `[x] Alan approved the over-limit size on 2026-11-04. The build does not stop for size.`

### 6. Ticket 17 folder rename

- I renamed `issues/17-rename-command-node-id` to `issues/17-rename-command-id-to-actor-node-id` with `git mv`, and renamed its ticket file to `17-rename-command-id-to-actor-node-id-ticket.md`.
- I updated the blocker link in ticket 06 and the two report paths inside ticket 17. `rg` finds no other use of the old slug outside this report.

### Still open

- `issues/04-start-surface.md` and `issues/05-expand-contract-first-aspect.md` still have `## Decisions 2026-11-03` lists. Those lists still have `Order.RunActor` and `ActorDispatch.fs` marked `[ ]`. 05 line 49 also says `Order.RunActor`. Alan's list did not name them, so I left them as they are.

## Third pass: tickets 04 and 05

### 04 — Start surface

- I removed `## Decisions 2026-11-03`. Most of its entries match the CONTEXT.md list above and were dropped on the same grounds.
- Two `[o]` entries had no other home, and the 2026-11-03 Comment depends on them. I moved them under Answer, in this ticket's own `[o]` line style:
  - Answer 2's name on the function: the pool cancels by FocusId, through 112.
  - Answer 3's fog for an in-process registry: each endpoint builds its own `FunctionActor`, so there is no registry. Remote start stays out of scope in `map.md` section 4.
- I dropped "named registry for remote start is in this cutover". `map.md` section 4 item 1 and the `arch.md` Bounds already cover it.
- The 2026-11-03 Comment now points to those two `[o]` lines and to Arch.

### 05 — Expand-contract first aspect

- I removed `## Decisions 2026-11-03`. All of its entries were covered or stale. Its last line, about the section 3 hard cut, is covered by the corrected line 49.
- I corrected three inline `[o]` lines:
  - Section 2 hypothesis: it now says each endpoint builds its own `FunctionActor` and posts `Order.ActorOrder`, and that there is no dispatcher. The name tables leave on 15.
  - Section 3 hard cut (old line 49): `Order.RunActor` became `Order.ActorOrder`, and the Detail link now points to Arch.
  - Section 5 dumb dispatch layer: it now says each endpoint builds its own `FunctionActor`.

### What rg still finds (excluding reports/)

- `Decisions`:
  - `map.md` section 3 is "Decisions so far". It is the map's own section for the September 27 locks from 03 and 04, not a dated decision record.
  - `map.md` lines 9 and 23 use the phrase in prose.
  - `map.md` line 24 and ticket 03 link to `doc/Decisions/0004-...`.
- `Order.RunActor`:
  - `arch.md` line 79 has the `[o]` marker.
  - Tickets 06, 07, 08, 09, 10 and 15 say not to add or write it.
  - Ticket 16 says it is obsolescent.
- `ActorDispatch.fs`: tickets 06 through 15 all say "Do not add it."
- Every hit left is either a guard against reintroducing the name or an `[o]` marker. None states it as current design.
