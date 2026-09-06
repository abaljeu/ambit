# Grill issue 09 — Core Command launch contract

Date: 2026-09-05

Session: closed. Q1–Q17 recorded. Alan locked the contract. [[../issues/09-define-core-command-launch-contract.md]] is Status resolved. Named may-change: later Core may pass a larger subgraph.

## What I read

Project files: [[plan/core-creation/project.md]], [[plan/core-creation/map.md]], all issues 01–13, especially 03, 05, 01, 02, 08–12. Glossary: [[CONTEXT.md]]. Decision: [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]. Assessment: [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]. Reports: [[asynchronous-core-task-manager-facts.md]], [[kernel-fsproj.md]], [[issue-02-ready-from-chat.md]], [[chart-core-wayfinder-map.md]], [[background-file-parse-facts.md]]. Server Core today has Changes only ([[src/Server/Core/CoreChanges.fs]], [[src/Server/Core/CoreRuntime.fs]]). There is no Command launch, job registry, or CancellationToken. Browser [[src/Shared/CommandEntry.fs]] is a different Command.

## Blockers 03 and 05

Both are Type grilling, Status resolved. [[../issues/03-define-typed-core-changes-contract.md]] locks typed `Change list` input, Normal vs Graph-only, acceptance facts, and HTTP-free results. [[../issues/05-place-core-changes-in-existing-projects.md]] locks Core under [[src/Server/Core/]], GraphAgentHandle as the initial typed Interface, and that GraphAgentHandle is not the final four-call Core API. Issue 09 Blocked by 03 and 05 is therefore clear. Status was open; this session claims it.

Issue 01 is done (produce path). Issue 02 stays needs-info and waits on 09–12. Tickets 10–12 stay blocked on 09.

## Weakest assumption

The issue question assumes Command can select and launch an Actor definition. Definitions stay outside Core. No registration seam is locked. If that stays vague, the typed input will either list Parse, shell, and Agent inside Core, or become a closure with no stable Interface. Identity, initial state, and retained job facts all hang on that object. Command-as-launch vs a fifth API is a related root, but the ticket already names Command; the rotting joint is the select mechanism.

## Design tree

Locked. See ## Locked contract below. Cancel, finish, and shutdown stay in 10–12. Parse, shell, and Agent behavior stay out.

## Round 1

❓ **Q1** - **How Command names work Core does not own**: Core API Command is not Browser CommandEntry. Actor definitions stay outside Core. Command must still launch one job off the apply mailbox. Which object does Command receive?

A. A registered name plus arguments. Composition registers definitions with the pool. Command does not contain Parse, shell, or Agent cases.

B. A caller-supplied work unit (function or Async). Command only spawns. Every call site selects.

C. Launch is not Command. A fifth surface or pool helper spawns. Command stays later or never.

➡️ A. Matches [[kernel-fsproj.md]] (name plus target, definition registered with the pool), the four-call Core API, definitions out, and Core as a container that receives subobjects at composition ([[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]). Tests register a test Actor the same way.

**Answer (Alan, 2026-09-05):** A. Arguments are Revision and a range like SiteNodeRange in [[src/Shared/ViewModel.fs]] (parent, start, endd). Parent is NodeId, not SiteEntry.

## Round 2

❓ **Q2** - **Why NodeId at launch when SiteNodeRange exists because NodeId is ambiguous in a DAG?**

**Answer (Alan, 2026-09-05):** The Command is passed to Core. Core extracts the subgraph defined by start and endd and passes that Graph to the named Actor.

Parent press (became Q3): extraction by parent NodeId plus start/endd still does not pick which DAG occurrence of that NodeId. SiteEntry exists for that.

## Round 3

❓ **Q3** - **Extraction by parent NodeId plus start/endd still does not pick which DAG occurrence of that NodeId.**

**Answer (Alan, 2026-09-05):** It does not extract the parent occurrence. It extracts the parent. Then it extracts child occurrences. Parent is the Node (NodeId), not a site-map occurrence. Children are then taken as occurrences.

Parent press (became Q4): SiteNodeRange indexes children under a site-map occurrence of the parent. Alan indexes the Node's children.

## Round 4

❓ **Q4** - **SiteNodeRange indexes children under a site-map occurrence of the parent. Alan indexes the Node's children.**

**Answer (Alan, 2026-09-05):** They never differ. start/endd always mean the same list.

Parent press (became Q5): what enforces that invariant.

## Round 5

❓ **Q5** - **What enforces that the site-map child list and the Node child list never differ?**

**Answer (Alan, 2026-09-05):** This invariant is universal. It is how the view actually functions to edit the model. It is a document-model / view law, not a Command-only restriction.

Parent move (became Q6): whether (Revision, parent NodeId, start, endd) is the argument of every Core Command launch or only subgraph Actors.

## Round 6

❓ **Q6** - **Is (Revision, parent NodeId, start, endd) the argument of every Core Command launch or only subgraph Actors?**

**Answer (Alan, 2026-09-05):** Every. That tuple is the argument of every Core Command launch, not only subgraph Actors.

Parent press (became Q7): the no-selection / whole-revision case.

## Round 7

❓ **Q7** - **Is no selection whole-revision, another shape, or forbidden?**

**Answer (Alan, 2026-09-05):** Forbidden. No selection is not whole-revision and not another shape. Core Command launch without parent/start/endd is forbidden.

Parent press (became Q8): whether a caret (start == endd) is also forbidden or a valid empty span.

## Round 8

❓ **Q8** - **Is a caret (start == endd) also forbidden or a valid empty span?**

**Answer (Alan, 2026-09-05):** Same thing. A caret is the same as no selection: forbidden. Launch requires a non-empty span.

Parent move (became Q9): Core-owned job identity that launch returns.

## Round 9

❓ **Q9** - **What Core-owned job identity does launch return?**

**Answer (Alan, 2026-09-05):** It returns a pool slot if that slot is stable. There is an internal id that is not returned.

Parent press (became Q10): what stable means (recycled slots aliasing jobs).

## Round 10

❓ **Q10** - **What does stable mean when recycled slots can alias jobs?**

**Answer (Alan, 2026-09-05):** So long as the task is running, a query about that number works to identify the task. The pool slot is a valid handle only while the job runs. It is not a durable job id.

Parent press (became Q11): post-completion query, fail vs silent hit on a reused slot.

## Round 11

❓ **Q11** - **After the task ends, does a query on that number fail or silently hit a new task?**

**Answer (Alan, 2026-09-05):** Best to fail. After the task ends, a query on that number must fail. It must not silently hit a new task.

Parent press (became Q12): if the slot is reused and occupied by a new task, does the old caller's number still fail (needs generation / not a bare index) or only fail while empty.

## Round 12

❓ **Q12** - **If the slot is reused and occupied, does the old caller's number still fail?**

**Answer (Alan, 2026-09-05):** Store a map number to Actor. Do not reuse numbers. Stale queries fail because the number is gone and is never reassigned, not via generation on a recycled index.

Parent press (became Q13): why an internal unreturned id still exists if the public number is unique and never reused.

## Round 13

❓ **Q13** - **Why does an internal unreturned id still exist if the public number is unique and never reused?**

**Answer (Alan, 2026-09-05):** Authentication for it to send messages. The internal unreturned id is a credential so the running Actor can send messages. The public number is the caller query key.

Parent pin (became Q14): who receives that id at launch (Actor initial state, never the Command caller).

## Round 14

❓ **Q14** - **Does launch put the send credential only in the Actor's initial state, never in the Command caller's return value?**

**Answer (Alan, 2026-09-05):** Yes. Launch puts the send credential only in the Actor's initial state, never in the Command caller's return value.

Parent press (became Q15): what else Core retains on the running job.

## Round 15

❓ **Q15** - **What else does Core retain on the running job?**

**Answer (Alan, 2026-09-05):** All of them and nothing else. Core retains number→Actor, send credential, span, Revision, and registered name. Nothing else.

Parent press (became Q16): whether Actor initial state is only subgraph plus credential (Core keeps the rest).

## Round 16

❓ **Q16** - **Is Actor initial state only subgraph plus send credential, with Core keeping span, Revision, and registered name?**

**Answer (Alan, 2026-09-05):** Yes. Actor initial state is subgraph plus send credential. Core keeps span, Revision, and registered name.

Parent ask (became Q17): lock the full 09 contract or name what stays open.

## Round 17

❓ **Q17** - **Lock the full 09 contract, or name what stays open?**

**Answer (Alan, 2026-09-05):** What might change is sending more subgraph. Lock.

## Locked contract

Command in (every launch): registered Actor name plus Revision, parent NodeId, and a non-empty start/endd span (SiteNodeRange shape; parent is NodeId, not SiteEntry). No selection is forbidden. Caret (start == endd) is forbidden. Site-map child list and Node child list never differ (universal view-edits-model law). Core extracts the parent Node, then child occurrences in that span, and passes that subgraph to the named Actor. Command does not contain Parse, shell, or Agent cases.

Launch returns to caller: a public number (map number→Actor). Numbers are never reused. Query by that number identifies the task while it runs. After the task ends, query fails. Internal send-auth credential is not returned to the caller.

Later (issue 11): "after the task ends" means after delete-actor applies, not when the Actor Task returns. Number, registry, and lock last until that apply.

Actor initial state: extracted subgraph plus send credential. Named may-change: later Core may pass a larger subgraph. Same credential. Core does not copy span, Revision, or name into the Actor.

Core retains and nothing else: number→Actor, send credential, span, Revision, registered name.

## Why this question first

Input shape, return value, and retained job facts are not askable until this object is known. A name registry, a closure, and a fifth API each imply a different contract. Asking identity first would freeze a handle on the wrong seam.

## What a good answer pins

Whether Core Command is the launch seam. Whether Core holds a name-to-definition map filled at composition, or receives a function per call. That Parse, shell, and Agent cases do not leak into the Command type. Enough to specify typed input in the next round without guessing registration.

## File updates

Project Stage stays active. This is an issue-level grill; [[.cursor/skills/project-work/SKILL.md]] says do not change the project's Stage for that. Resolved [[../issues/09-define-core-command-launch-contract.md]] and added a gist to [[plan/core-creation/map.md]] Decisions so far. Did not regenerate [[plan/index.md]].
