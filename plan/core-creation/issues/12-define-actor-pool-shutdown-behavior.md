# Define Actor-pool shutdown behavior

**Type:** grilling
**Status:** done
Blocked by: 09, 10, 11
Actual: 95m

## Question

When the Server or Core shuts down, how are running and queued Actors cancelled or awaited, how are already-enqueued Change batches treated, and what terminal job information must remain observable before process exit without adding process crash isolation?

## Answer

Core has two events. Crash isolation stays out of scope.

When a mutating Post attempts a transaction and gets a TCP or transport error, Core Rejects that Change and treats the Database as down. Already-enqueued mailbox items apply ([[10-define-actor-cancellation-and-output-admission.md]]). Do not drop persistable siblings. Each sibling apply may fail the same way, or succeed if the Database returned. There is no clear-the-mailbox API. If the mailbox emptied, the next mutating Post or launch is the probe: success applies from the live Graph; TCP fail Rejects that one and the Database stays down. Delete-actor still drops the in-memory registry (number, credential) and skips persist of lock-off. Command launch Rejects. Running Actors may continue; they cannot persist Changes. Query, state, Poll, and Graph reads stay admitted. That refuse is a system error: the same Reject as [[13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] / readOnly (unavailable). It is not the auth refuse ([[10-define-actor-cancellation-and-output-admission.md]]).

Lock-present is a field on the live Node. It is not written to the Database. A fresh read or a new process has lock off. The live Graph lasts for the process. This amends the persist story in [[11-define-actor-finish-and-failure-behavior.md]]. Clients still see lock on the live Node through state, Fetch, or Query. SQL create, update, and select statements do not include the lock field — no Graph-wide strip-on-write, no post-load clear pass, and no SELECT * or generic serializer that sneaks the field in — and History still never carries lock ([[11-define-actor-finish-and-failure-behavior.md]]).

On host-stop, Core refuses new Posts with that same system-error Reject, applies the remaining mailbox including delete-actor, and cancels running Actors with a CancellationToken. The process exits when the mailbox is idle or the host default ShutdownTimeout fires. ShutdownTimeout is not a named Core number. No extra terminal job facts remain observable before exit.

Grill notes: [[plan/core-creation/reports/grill-issue-12-shutdown.md]].

## Comments

- Grilling started. Round 1 is in [[plan/core-creation/reports/grill-issue-12-shutdown.md]]. Blockers 09, 10, and 11 are resolved, so this ticket is unblocked. Facts: [[plan/core-creation/reports/grill-issue-12-facts.md]].
- Alan opened: DB stop is the common case (Actors may work; Changes Reject). Shutdown is irrevocable and immediate (cancel token; disable the mailbox). Formal Q1 not yet locked.
- Q1: A. Two events. DB stop vs process shutdown. Crash stays out. See the grill report.
- Q2: B. Database has stopped, so already-enqueued mailbox items Reject / drop without apply. See the grill report.
- Q3: A. Host-stop window: refuse new Posts; apply remaining mailbox including delete-actor; cancel Actors; exit when idle or timeout. See the grill report.
- Q4: A. On DB stop, skip persist; drop in-memory registry. Alan: locks need not live in the Database. DB may return; until then no Changes path. See the grill report.
- Q5: Lock-present is on the live Node, not written to the Database. Fresh read is lock off. Live Graph lasts for the process. Amends 11 persist. See the grill report.
- Q6: A. When the Database returns, writable Changes resume from the live Graph. Dropped outage items stay gone. See the grill report.
- Q7: A. No Command launch while the Database is down. Launch Rejects. Running Actors may continue; they cannot persist Changes. See the grill report.
- Q8: A. Nothing extra observable before process exit. After drain or timeout, exit. See the grill report.
- Q9: C. Not a dedicated hook and not startup-only. A mutating write gets a TCP error → that Change is cancelled/Rejected and Core treats the Database as down. On a new mutating Post or launch, check the Database; if it is up, resume. Query / state / Poll / Graph reads stay admitted (issue 13). See the grill report.
- Q10: A. ShutdownTimeout is not a named Core number. Host default. See the grill report.
- Q11: A. Same Reject as issue 13 / readOnly (unavailable). Not Unauthorized. Mutating Posts and launch only. See the grill report.
- Correction (2026-09-05): DB-down / TCP-fail / “check the DB” applies to mutating posts only. Asking for Graph data is okay. Matches issue 13 file-backed reads and Q5 live Graph. See the grill report.
- Q12: A. The next mutating Post or launch is the probe. Success applies (Q6). TCP fail Rejects that one; stay down. Reads do not probe. See the grill report.
- Q13: A. Keep Q2B. When one apply gets a TCP error, drop already-enqueued mailbox siblings. They are not the probe. See the grill report.
- Q14: A. Lock. Status resolved. See the grill report.
- Persist how (2026-09-06): Q5 semantics stay; SQL create, update, and select omit the lock field. See the grill report.
- Amend (2026-09-06): revert Q2B / Q13A. Keep one mailbox rule: already-enqueued items apply (10). TCP fail Rejects that one mutating Change and marks Database down; siblings stay. Q6 "dropped outage items stay gone" was that old drop-siblings world. No mailbox-clear API. System error stays distinct from 10 auth refuse. Status stays resolved.

## Time

- 2026-09-05 15m — started grill; first question is whether DB stop and process shutdown are two events (from chat)
- 2026-09-05 10m — recorded Q1=A and Q2=B; Azure host-stop signal facts for Q3 (from chat)
- 2026-09-05 10m — recorded Q3=A and Q4=A; lock-not-in-DB press (from chat)
- 2026-09-05 5m — recorded Q5: live Node lock, not persisted (from chat)
- 2026-09-05 5m — recorded Q6=A (from chat)
- 2026-09-05 10m — recorded Q7=A and Q8=A; leftover frontier is live DB detect, timeout, refuse word (from chat)
- 2026-09-05 10m — recorded Q9=C, Q10=A, Q11=A; TCP-fail detect; leftover is probe and mailbox siblings (from chat)
- 2026-09-05 5m — recorded read-vs-mutate: TCP-fail and probe are mutating Posts and launch only; reads stay admitted (from chat)
- 2026-09-05 5m — recorded Q12=A and Q13=A; frontier empty; await lock (from chat)
- 2026-09-06 5m — locked contract; Q14=A; resolved (from chat)
- 2026-09-06 5m — lock-present persist how: SQL omit lock field (from chat)
- 2026-09-06 10m — revert drop-siblings (Q2B / Q13A); keep apply-already-enqueued; system error stays out of auth refuse (from chat)
- 2026-09-06 5m — Comments: Q6 dropped-outage line is the old drop-siblings world (from chat)
