# Grill round 5

Poll pin finished first: Poll = POST `/changes` with empty Changes; neither clears History; today's Poll clear is **debt**. Home: [[unified-messaging.md]].

Frontier from [[whats-left.md]]. Do not re-ask accepted pins. Deferred this round: merge.md-as-a-whole (hangs on Q1), exact type fields, job API / Shell product, Parse refactor plan, Load/`/state` parked.

## User-facing (speak unchanged)

Poll pin recorded: Poll = POST `/changes` with an **empty** Change list. Same handler and receive-apply. **Neither** clears History. Today's Poll-with-tail clear (`applySyncResponse`) is software debt.

❓ **Q1** - **Leftover pending after the 200 list**: Client still has local pending that was **not** in this POST. Poll is the same path with an empty body, so the same hole. After rewind+replay of the Server list, what happens to that leftover pending?

A) Keep it. Next POST of those Changes is a later newest Actor; Server amends (200 Merge). Do not Client-merge unsent pending in this increment.

B) Client amends leftover pending locally against the new Local Graph before the next POST.

C) Drop leftover pending.

➡️ **A.** Wipe is gone. Server is the amendment authority. Same rule for Poll.

❓ **Q2** - **Inner apply for Server Actors**: Parse/Shell already have Change objects. How do they enter apply?

A) Same mailbox inner apply (objects in). HTTP `/changes` decodes, then calls that. Not POST-to-self. Not a new public HTTP API.

B) Keep Parse-style JSON into `postGraphOnlyChange`.

➡️ **A.** [[in-process-apply.md]] already recommends this.

❓ **Q3** - **Whose Change may Undo invert?**: Global order makes unrestricted Undo possible. Desirability is still open.

A) Increment-2: only this process's own History entries (the **amended** own Change after replay). Not other Actors' Changes.

B) Any Change in the global order (unrestricted).

C) Leave unrestricted desirability open; do not pin A or B.

➡️ **A.** Cancel ≠ Undo. Unrestricted of *others'* work is the see-and-understand question — not this increment.

## Answers

- **Q1 → B, then superseded.** First answer was Client-amend leftover pending. **Superseded** by [[grill-round-6.md]]: send **unamended** pending; Server amends on apply. Not C (drop).
- **Q2 → don't-care.** Inner apply vs Parse-style JSON is not a pin. Either is fine. Do not lock A or B.
- **Q3 → C.** Unrestricted Undo desirability stays **open**. Do not pin increment-2 Undo to own History only.

Homes: [[merge.md#Client correction]], [[unified-messaging.md]], [[whats-left.md]], [[undo.md#Unrestricted Undo desirability]].

## WORK.md

Add this file to the Active [[project.md]] related list. Stage `charting`.
