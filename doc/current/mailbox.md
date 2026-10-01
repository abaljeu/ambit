# Mailbox

Category: Capability

See Also:

- [Core](core.md)
- [File agent](file-agents.md)
- [Db agent](db-agents.md)
- [Actors](actors.md)
- [Parse and persist](parse-persist.md)

The mailbox is the one Core queue.

## Job

[x] Core mailbox: the callable door over `MailboxProcessor<CoreMsg>`. Contract type: [CoreMsg](../../src/Server/Core/CoreMsg.fs). [Core mailbox](../../src/Server/Core/CoreMailbox.fs).

[x] Core mailbox backend: the shared loop. The file agent and the db agent do not keep a second copy of that loop. [Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs).

[x] Every mailbox message finishes quickly. Work that would hold the mailbox is an Actor. The Actor runs off the queue and posts more fast messages. [Core mailbox messages clear fast](../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md).

[x] Mailbox event log: stores `ActorStart` and `ActorStop`. Undo and Redo stay Change-only. Actor events are not Undo targets.

## Messages

[x] Fast Actor messages on this loop: `StartActor` and `ActorStop`.

[x] Persist handlers do not dispatch Actor cases. The file agent and the db agent do not start a mailbox.

## Explanation

The queue stays short so one slow body cannot block every other message. That slow body is an Actor.
