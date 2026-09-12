# Core mailbox messages clear fast

Status: accepted. Locked 2026-09-07 in chat; applies to Core Actor pool and cancel/finish.

Every Core mailbox message must finish quickly in the mailbox processor. Work that would hold the mailbox is an **Actor**: it runs off-queue and only posts more fast messages (Changes, finish/cancel markers, and similar).

## Cancel and finish

**Cancel** is a fast mailbox message. When it runs it signals the Actor (`CancellationToken`) and updates fast state (for example drop the job credential so later Posts fail the normal active-source check). It does not scan or specially rewrite already-queued items.

**Finish / drop** uses the same idea: Actor stop enqueues a Core-only finish/delete-actor marker (not a Change). When that marker clears the queue after earlier Posts from that sender, Core drops the public number, removes the credential if still present, and clears lock-present.

Cancel and natural finish share that drop path. There is no rival cleanup mechanism and no cancel-specific reject family.

## Admission without a special cancel reject

FIFO stands:

- [x] Messages **ahead** of cancel on the queue still apply.
- [x] Messages **behind** cancel are processed after cancel has already run, so the ordinary active-source / active-credential check rejects them immediately.

That is the same admission gate as any inactive sender, not a separate cancel-after-enqueue rule. This amends the reading of [[plan/core-creation/issues/10-define-actor-cancellation-and-output-admission.md]] and guides [[plan/core-creation/issues/17-cancel-a-job.md]]: cancel is the fast mailbox message; refuse is the normal active-source check after prior messages.

## See also

- [[plan/core-creation/issues/18-finish-and-drop.md]] — finish enqueues marker; drop when cleared
- [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]] — delete-actor on any Actor stop
