# Plan or doc change — EventId serial

Requirement named in chat as a review follow-up of [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) plus Alan (BeforeAll is not necessary; seed uses `getEventLog ()`) and [code-review-48-eventid-zero-and-positive-int](code-review-48-eventid-zero-and-positive-int.md) as a signal, not authority. One requirement: place the EventId serial / Zero-or-positive-Int rule, and strip clauses that overspecify tests and callers. Seed-fail `getEventsSince` stubs and [History.fs](src/Shared/History.fs) file growth were not this requirement.

Workplace: `scripts/gitstatus.sh` showed HEAD `dev`. Extra git: working-tree diff of [Core creation architecture](plan/core-creation/arch.md) and [EventId serial](.agents/rules/core-api.md).

## 1. Owning layer

**Architecture.** Highest layer whose invariant must change: [Core creation architecture](plan/core-creation/arch.md) Module **Ev** and Module **EventLog**, with architecture-adjacent [EventId serial](.agents/rules/core-api.md). Cross-ticket coupling (who assigns stored serials; Zero vs stored Int; next of Zero; get-all is Zero) belongs in architecture. Tickets consume that shape.

The user named the problem in chat (review follow-up), not a file. Named artifacts differed from the owning layer: the ticket, the combined review, Alan’s remarks, and the policy section.

## 2. Check upward

1. **Spec — no change needed.** There is no Project `spec.md`. [Implementation Planning and Record](plan/core-creation/issues/Implementation%20Planning%20and%20Record.md) Phase 2 does not state EventId Zero vs stored Int. It says arch.md supersedes disagreement. No forcing clause to keep or strip.
2. **Map — no change needed.** [Core creation Wayfinder](plan/core-creation/map.md) destination is the Graph-agent Core increment. It does not state the EventId serial.

No clause above architecture forced the test/caller ugliness. The overspecified clauses lived on the ticket and in [EventId serial](.agents/rules/core-api.md) (and, vs `HEAD`, the old BeforeAll / `EventId.next` caller ban).

## 3. Scope

Declared before the edits in this run:

1. **map** — no-change. [Core creation Wayfinder](plan/core-creation/map.md).
2. **spec** — no-change. No `spec.md`. [Implementation Planning and Record](plan/core-creation/issues/Implementation%20Planning%20and%20Record.md) unchanged.
3. **architecture** — change. [Core creation architecture](plan/core-creation/arch.md) Module **Ev**, Module **EventLog**, Chosen Event destination.
4. **policy** (architecture-adjacent, not the map) — change. [EventId serial](.agents/rules/core-api.md).
5. **ticket** — change. [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) only. No retrofit of other tickets.
6. **code** — no-change this pass. Flag mismatches. Do not grind tests or EventId builders.

## 4. Apply

Top-down. Load-bearing invariant placed up; specific caller/test choices placed down.

1. **Architecture** — [Core creation architecture](plan/core-creation/arch.md) `Updated: 2026-09-18`.
   1. Module **Ev** State — `EventId` is Zero (draft and get-all basis) or a positive stored Int; EventLog assigns stored serials; next of Zero is Zero. Replaces “EventId is the log position.”
   2. Module **EventLog** Interface — empty `nextId` is the first assignable stored Int, not next of Zero; append stamps a unique positive Int; EventLog is the only assigner of stored event ids; `since` of Zero is every stored Int (no BeforeAll); restore keeps `nextId` past every merged stored Int.
   3. Chosen Event destination — one serial is EventId: Zero or a positive stored Int; EventLog assigns stored serials; next of Zero is Zero.
   4. Not added — `EventId.firstStored`; `fromJson` of `n <= 0`; “only EventLog may call `EventId.next`”; “tests that are not EventId builder tests do not check event id numbers.” Ev Interface `fromJson` / `toJson` — only serializing uses these — left as the existing Ev codec rule, not an EventId test ban.
2. **Policy** — [EventId serial](.agents/rules/core-api.md) now matches that shape. Stripped: “Only EventLog may call `EventId.next` on a stored Int”; “Only serializing should use the fromJson/toJson functions”; BeforeAll (already gone in the ticket-48 working tree; `HEAD` still had BeforeAll). Kept: Zero or positive Int; next of Zero is Zero; EventLog assigns stored serials; get-all and drafts use `EventId.zero`; wire 0 and positive wire int. Client and Shared mint `EventId.zero` or rebuild from the wire; they do not assign stored serials.
3. **Ticket** — [48 — EventId Zero and positive Int](plan/core-creation/issues/48-eventid-zero-and-positive-int.md) consumes architecture. F# DU stays as this ticket’s realization. Removed EventLog item “Only EventLog next on stored Int.” Wire item no longer bans `fromJson` as a constructor. Tests item is “do not lock stored serials” and “obtain stored ids from EventLog,” not “do not check event id numbers.” Comment logged.

## 5. Ratchet

Architecture grew by phrases on existing Module map items and one clause on Chosen Event destination. It is not more conditional. It does not admit only the F# DU, `firstStored`, or one test style. Policy shrank versus the ticket-48 working-tree caller bans. Ticket caller/test clauses thinned. Upper layers were not restored after a strip-upward failure.

## 6. Flagged lower artifacts

Code and neighbors were not edited. They no longer match the placed rule in these ways:

1. **EventId.firstStored** — [History.fs](src/Shared/History.fs) public `firstStored = Int 1` and [EventLog.fs](src/Shared/EventLog.fs) `empty.nextId = EventId.firstStored`. Architecture requires a stored Int on empty `nextId`, not a new EventId name. Code choice; not architecture.
2. **fromJson of n <= 0** — [History.fs](src/Shared/History.fs) maps every `n <= 0` to Zero. Architecture and policy specify wire 0 and a positive wire int only. Negative wire is unspecified. Do not add it upward.
3. **EventId.next on stored Int in tests** — [EventTests.fs](tests/Shared.Tests/EventTests.fs) restore/advancePast fixtures (review signal). Architecture does not ban the function name in tests. Ticket: other tests obtain stored ids from EventLog.
4. **fromJson as a stored-id constructor in non-builder tests** — [PersistHandlersRestoreTests.fs](tests/Server.Tests/PersistHandlersRestoreTests.fs) `fromJson 1`; [DbAgentTests.fs](tests/Server.Tests/DbAgentTests.fs) `fromJson` 9/4/6; other suites still `Equal(EventId.fromJson N, …)` (review signal). Ticket: do not lock stored serials.
5. **Glossary** — [CONTEXT.md](CONTEXT.md) event id still Avoids `EventId.next` outside EventLog via the old caller ban. Not a stack layer. Stale relative to stripped policy.
6. **Field-shape report** — [event-abstraction.md](event-abstraction.md) still `type EventId = EventId of int`. Historical; reports are not authority; not edited.
7. **Project index one-liner** — [project.md](plan/core-creation/project.md) issue 48 line still says tests check event id numbers only in EventId builder tests. Not a stack layer. Not edited.
8. **Older tickets** — not rewritten (no-retrofit).

## 7. Outcome

Edited layers: architecture, architecture-adjacent policy, ticket 48. Did not stop. Did not edit map, spec, or code.
