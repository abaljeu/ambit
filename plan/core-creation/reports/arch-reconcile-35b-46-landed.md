# Arch reconcile after 35b and 46 landed

See also: [Core creation architecture](../arch.md), [35b — Browser Run hello](../issues/35b-browser-run-hello.md), [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md).

## What this pass checked

Checked every hello-cut item that is true on this checkout (Land 46). No application code.

1. Story path **Browser Run hello** hops 1–11 — [35b — Browser Run hello](../issues/35b-browser-run-hello.md)
2. Shared segment **StartActor through HTTP / Core / Pool** — [35b — Browser Run hello](../issues/35b-browser-run-hello.md)
3. Module **Included descendant id list** State, Interface, Uses, and Seam **Included descendant id list** — [35b — Browser Run hello](../issues/35b-browser-run-hello.md)
4. Module **Browser Run** remaining Command items and Uses — [35b — Browser Run hello](../issues/35b-browser-run-hello.md)
5. Module **HTTP Adapter** remaining Command items and Uses — [35b — Browser Run hello](../issues/35b-browser-run-hello.md)
6. Module Uses that the landed modules already have — prior hello and Event tickets
7. EventLog durability / load reconcile prose (Feature recover, module **EventLog** Interface 9, module **PersistHandlers** Interface 4, Alternative **Chosen Event destination**) — [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md)

Rewrote Implementation status: Story path **Outside Core lifecycle proof**, Story path **Browser Run hello**, and EventLog persist/reconcile are implemented. Hello-cut checkboxes are closed.

Ticket status: [35b — Browser Run hello](../issues/35b-browser-run-hello.md) is `done`. [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md) stays `done`.

## Remaining unchecked → covering ticket

| Arch item | Covering ticket |
| --- | --- |
| (none) | — |

Grep of [Core creation architecture](../arch.md) finds no `[ ]` checkboxes.

## Not checkbox-shaped remaining text

These are not unchecked items. Do not invent tickets for them.

1. Unsettled **Cherry-pick Undo** — already "Deferred past hello; not a ticket yet." Arch Feature line: do not open Wayfinder map tickets for Unsettled.
2. Unsettled **Restore older file versions via actual git** — already "Deferred past hello; not a ticket yet."
3. Alternative **Deferred past hello** — Cancel, live query, host-stop, post-twice, duplicate terminal, Interrupted restart. Record only. Existing tickets outside this hello cut already name those jobs ([16 — Track running job](../issues/16-track-running-job.md), [17 — Cancel a job](../issues/17-cancel-a-job.md), [18 — Finish and drop](../issues/18-finish-and-drop.md), [19 — Database down and host stop](../issues/19-database-down-and-host-stop.md), [28 — Drain actor lifecycle on host stop](../issues/28-drain-actor-lifecycle-on-host-stop.md)). They do not own an unchecked arch checkbox.

## Orphaned

None. No remaining unchecked arch item lacks a covering ticket.
