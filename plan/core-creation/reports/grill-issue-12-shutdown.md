# Grill issue 12 — Actor-pool shutdown behavior

Date: 2026-09-05

Session: closed. Q1–Q13 recorded. Q14=A. Alan locked the contract. [[../issues/12-define-actor-pool-shutdown-behavior.md]] is Status resolved. Facts: [[grill-issue-12-facts.md]].

## What I read

Project files: [[plan/core-creation/project.md]], [[plan/core-creation/map.md]], issues 09–13. Glossary: [[CONTEXT.md]]. Locked: [[../issues/09-define-core-command-launch-contract.md]], [[../issues/10-define-actor-cancellation-and-output-admission.md]], [[../issues/11-define-actor-finish-and-failure-behavior.md]]. Parallel: [[../issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]]. Facts: [[grill-issue-12-facts.md]].

## Blockers 09, 10, 11

All three are Type grilling, Status resolved. Issue 12 Blocked by those is therefore clear. Status was open; this session claims it as grilling. Crash isolation stays out of scope. Issue 02 stays needs-info until this contract locks.

## Weakest assumption

The ticket treats "Server or Core shuts down" as one drain: cancel or await Actors, treat the mailbox, keep some job facts until exit. Alan opened with two events. **DB stop** (process stays up): Actors may keep working; Changes Reject. **Shutdown** (irrevocable, immediate): send a CancellationToken and disable the mailbox.

Issue 13 and today's `readOnly` wrapper Reject writes when the Database is unavailable at startup; there is no mid-process DB-stop hook and no mailbox-disable API ([[grill-issue-12-facts.md]]). Issue 10 already applies messages that are in the mailbox. Issue 11 delete-actor is a mailbox item after Actor stop. Those collide with "disable the mailbox" and with "Changes Reject" if the mailbox still has work.

## Design tree

Locked. See ## Locked contract. Q14=A. Crash isolation stays out.

## Why this question first

If DB stop and shutdown are one event, later questions invent a drain that Alan already split. Pin the split, then pin what "Changes Reject" does to the mailbox.

## Round 1

Alan (2026-09-05), before numbered questions: common case is DB stop — Actors may work; Changes Reject. Shutdown is irrevocable and immediate — send a cancel token and disable the mailbox.

❓ **Q1** - **Are DB stop and process shutdown two Core events on this ticket?**

A. Yes. DB stop: process stays up; Actors may keep running; new writable Changes Reject (same refuse idea as issue 13 / `readOnly`). Shutdown: irrevocable; CancellationToken to running Actors; disable the mailbox. Crash isolation stays out.

B. No. This ticket is process shutdown only. DB-unavailable Reject stays issue 13 / startup `readOnly`.

C. Name another split.

➡️ A. Matches Alan's open. Issue 13 is startup unavailability; this ticket can own the live DB-stop rule and shutdown.

**Answer (Alan, 2026-09-05):** A.

## Round 2

❓ **Q2** - **On DB stop, already-enqueued mailbox items?**

A. Still apply (10). Later Posts Reject. delete-actor can still run (lock-off is a write).

B. Reject / drop without apply, including items already in the mailbox. delete-actor waits or is skipped — name which.

C. Name another rule.

➡️ A. “Actors may work” plus 10’s apply-already-enqueued. Reject is the live Post path, not a mailbox wipe.

**Answer (Alan, 2026-09-05):** B. The Database has stopped, so those items cannot apply.

## Host-stop facts (for Q3)

Alan asked: Azure has been told to stop or restart the Server process; is there a signal we can delay until work is ready?

Yes, bounded. Not unbounded.

- This Server is ASP.NET `app.Run()` ([[src/Server/Server.fs]]). Generic Host treats Ctrl+C, SIGTERM, and IIS/ANCM stop as graceful stop.
- `IHostApplicationLifetime.ApplicationStopping` fires. `IHostedService.StopAsync` is the delay point: the host waits until it returns or `HostOptions.ShutdownTimeout` (default 5 seconds). Then it exits anyway.
- [[src/Server/DailyGitSave.fs]] already takes `IHostApplicationLifetime` but registers `ApplicationStarted` only. There is no `ApplicationStopping` or drain `StopAsync` today.
- `WEBSITE_SITE_NAME` is already the Azure App Service marker in [[src/Server/Server.fs]]. App Service sends a graceful stop, then kills after its own limit.
- VM kill, crash, or timeout expiry: no further delay. That is crash isolation, out of scope.
- [[src/Desktop/LocalProxy.fs]] sets `ShutdownTimeout` to 1 second on a different host. Production Server does not set it.

So: we can hook stop and finish work inside a timeout we choose. We cannot wait until “ready” if that exceeds the host or Azure limit. There is no handler yet; the signal already exists.

## Round 3

❓ **Q3** - **In that host-stop window, what does Core do?**

A. Refuse new Posts. Apply what is already in the mailbox (10), including delete-actor. Cancel running Actors. Exit when the mailbox is idle or the timeout fires.

B. Cancel tokens and disable the mailbox immediately (drop remaining items). Exit when the host says so.

C. Same as A, but name a longer `ShutdownTimeout` as part of the contract.

➡️ A.

**Answer (Alan, 2026-09-05):** A.

❓ **Q4** - **On DB stop, delete-actor and lock-off?**

A. Skip persist. Still drop the in-memory registry (number, credential). Leave Node lock-present as last written.

B. Skip persist and skip in-memory cleanup.

C. Name another rule.

➡️ A.

**Answer (Alan, 2026-09-05):** A. Plus: there is no reason to have locks in the Database. The Database may come back; until then Core cannot deal with Changes.

That amends [[../issues/11-define-actor-finish-and-failure-behavior.md]] if lock-present was a persisted Node field.

## Round 4

❓ **Q5** - **Where does lock-present live?**

A. Core memory only, not a Node field.

B. Keep 11: persist lock-present on the Node to the Database.

C. Name another place.

➡️ A.

**Answer (Alan, 2026-09-05):** Neither A nor B. Lock-present is a field on the Node in the live Graph. That field is not written to the Database. A fresh read (new process, load from Database) has lock off. The live Graph lasts for the process. Amends 11: clients still see lock on the Node through live state / Fetch / Query; persist and History never carry it.

## Round 5

❓ **Q6** - **If the Database returns while this process is still up, do writable Changes resume?**

A. Yes. New Posts apply from the live Graph. Dropped mailbox items from the outage stay gone. Persist still omits lock-present.

B. No. Stay Reject until the next process start.

C. Name another rule.

➡️ A.

**Answer (Alan, 2026-09-05):** A.

Later (2026-09-06): Q6 option A still resumes from the live Graph. "Dropped mailbox items stay gone" was the old Q2B world. Siblings stay; only the TCP-failed Change is Rejected.

## Round 6

❓ **Q7** - **May Command launch while the Database is down?**

A. No. Launch Rejects. Running Actors may continue; they cannot persist Changes.

B. Yes. Launch still writes lock-present on the live Node and starts the Actor.

C. Name another rule.

➡️ A. Launch exists to change the Graph. There is no Changes path until the Database returns.

**Answer (Alan, 2026-09-05):** A.

❓ **Q8** - **Before process exit, must any extra job fact stay observable?** 11 already has no job result.

A. Nothing extra. After drain or timeout, exit.

B. Name a fact that must remain.

➡️ A. Terminal observability is 11 plus live locks until exit. No new record.

**Answer (Alan, 2026-09-05):** A.

## Round 7

Ticket question is answered, but three assumptions are still silent.

❓ **Q9** - **How does Core learn that the Database stopped or returned mid-process?** Today handle selection is startup-only ([[grill-issue-12-facts.md]]). Q6 and Q7 need a live flip.

A. Mid-process hook. Down applies Q2/Q4/Q7. Return applies Q6. Startup `readOnly` is not enough.

B. Startup-only (issue 13). Q6 and Q7 apply only after a new process.

C. Name another signal.

➡️ A. Q6 is “returns while this process is still up.”

**Answer (Alan, 2026-09-05):** C. Keep it simple. Attempt a transaction; a TCP error cancels that Change; the Database is down. On a new request, check the Database; if it is up, resume. Not A (no dedicated hook). Not B (not startup-only). If that is not enough for earlier answers, simplify those choices.

**Correction (Alan, 2026-09-05):** DB-down / TCP-fail / “check the DB on a new request” applies to mutating posts only. Asking for Graph data is okay. Matches [[../issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] file-backed reads and Q5 live Graph for the process. Probe and Reject are for mutating Posts and Command launch (Q7). Query / state / Poll / Graph reads stay admitted.

Q1–Q8 still hold as consequences, with one trim: Q4 does not wipe running-Actor registry on the first TCP fail. “Cancel the change” is that failed mutating Post (Reject), not Actor CancellationToken (Q7 Actors may continue). Q6 “returns while this process is up” is the next mutating Post or launch, not a hook and not a read.

❓ **Q10** - **Is ShutdownTimeout a named Core number?**

A. No. Use the host default (5s). Exit at idle or that timeout. Q3 already.

B. Yes. Name the seconds as contract.

➡️ A. Q3 rejected naming a longer timeout.

**Answer (Alan, 2026-09-05):** A.

❓ **Q11** - **What refuse do Browser Post, Actor Post, and Command launch share while the Database is down?**

A. Same Reject as issue 13 / `readOnly` (unavailable). Not Unauthorized (10 is inactive source). Host-stop refuse is the same Reject family.

B. Unauthorized.

C. Split them.

➡️ A. Matches Q7 Launch Rejects and Changes Reject.

**Answer (Alan, 2026-09-05):** A. Mutating Posts and launch only. Reads stay admitted.

## Round 8

Read-vs-mutate is settled. Probe and Reject are mutating Posts and launch only.

❓ **Q12** - **What is “check the DB” on a new mutating Post or launch?** Reads do not probe.

A. That Post or launch is the probe. Success applies (Q6). TCP fail Rejects that one; stay down.

B. A cheap ping first. Ping fail → Reject with no transaction. Ping ok → then the transaction.

C. Name another check.

➡️ A. Same path as how we learned it was down.

❓ **Q13** - **When one apply gets a TCP error, already-enqueued mailbox siblings?**

A. Drop them (keep Q2B). They are not the probe. Only a later mutating Post or launch may resume.

B. Leave them. Only the failed Change Rejects. Siblings try on their own. Simplifies Q2B.

➡️ A. Once down, persistable mailbox cannot apply. Probe is a later mutating request.

**Answer (Alan, 2026-09-05):** 12A, 13A.

## Locked contract

Two Core events. Crash isolation stays out of scope.

When a mutating Post attempts a transaction and gets a TCP or transport error, Core Rejects that Change and treats the Database as down. Already-enqueued mailbox items apply ([[../issues/10-define-actor-cancellation-and-output-admission.md]]). Do not drop persistable siblings. Each sibling apply may fail the same way, or succeed if the Database returned. There is no clear-the-mailbox API. If the mailbox emptied, the next mutating Post or launch is the probe: success applies from the live Graph; TCP fail Rejects that one and the Database stays down. Delete-actor still drops the in-memory registry (number, credential) and skips persist of lock-off. Command launch Rejects. Running Actors may continue; they cannot persist Changes. Query, state, Poll, and Graph reads stay admitted. That refuse is a system error: the same Reject as [[../issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] / readOnly (unavailable). It is not the auth refuse ([[../issues/10-define-actor-cancellation-and-output-admission.md]]).

Lock-present is a field on the live Node. It is not written to the Database. A fresh read or a new process has lock off. The live Graph lasts for the process. This amends the persist story in [[../issues/11-define-actor-finish-and-failure-behavior.md]]. Clients still see lock on the live Node through state, Fetch, or Query. SQL create, update, and select statements do not include the lock field — no Graph-wide strip-on-write, no post-load clear pass, and no SELECT * or generic serializer that sneaks the field in — and History still never carries lock ([[../issues/11-define-actor-finish-and-failure-behavior.md]]).

On host-stop, Core refuses new Posts with that same system-error Reject, applies the remaining mailbox including delete-actor, and cancels running Actors with a CancellationToken. The process exits when the mailbox is idle or the host default ShutdownTimeout fires. ShutdownTimeout is not a named Core number. No extra terminal job facts remain observable before exit.

## Round 9

❓ **Q14** - **Lock this contract, or name what stays open?**

A. Lock.

B. Lock, and name a may-change.

C. Not yet (name what stays open).

➡️ A. The ticket question is answered. Two events. Crash stays out.

**Answer (Alan, 2026-09-06):** A. Lock.

## Post-lock (Q5 how)

Alan (2026-09-06): Q5 semantics stay (lock on the live Node, not in the Database; a fresh read has lock off). The how: SQL create, update, and select statements do not include the lock field. No Graph-wide strip-on-write. No post-load clear pass. Do not let SELECT * or a generic serializer sneak the field in. History still never carries lock ([[../issues/11-define-actor-finish-and-failure-behavior.md]]).

## Post-lock (mailbox and refuse)

Alan (2026-09-06), after an implementation-complexity review: revert Q2B / Q13A. Keep one mailbox rule from issue 10 — already-enqueued items apply. A TCP fail Rejects that one mutating Change and marks Database down. Siblings stay; each apply may fail the same way or succeed if the Database returned. If the mailbox emptied, the next mutating Post is still the probe. Host-stop still applies remaining items including delete-actor. No clear-the-mailbox API. Adapter cookie fail (HTTP 401) and Core inactive-sender (Unauthorized) are one auth refuse (amends 10). TCP / Database-unavailable / readOnly Reject stays a system error, not that refuse. Status stays resolved.
