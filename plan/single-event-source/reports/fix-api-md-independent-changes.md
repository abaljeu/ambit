# Fix api.md independent Changes

Surgical public-contract correction in [[doc/api.md]]. Product rule as of 2026-09-16: Changes in a list are independent. A later reject does not roll back earlier items. `previewTransportBatch` is gone from CoreMailbox.

## Line changed

[[doc/api.md]] POST `/ambit/changes` request bullets, one line.

**Old:** Multiple changes in one batch are applied in order; all must succeed or none are applied (`400` on failure leaves state unchanged).

**Old meaning:** An HTTP `ChangeBatch` is all-or-nothing. One reject returns 400 and leaves server state as it was before the POST.

**New:** Multiple changes in one batch are applied in order. Changes in a list are independent; a later reject does not roll back earlier items.

**New meaning:** The server still applies the list in order. Earlier accepted items stay applied when a later item is rejected. HTTP 400 on that reject does not undo them.

No other line in that section contradicted the new sentence. The 400 example and “other failures” list still name reject reasons; they do not say state is unchanged.

## Other current docs

Searched [[doc/]] for all-or-nothing / none-are-applied / leaves-state-unchanged / HTTP batch atomicity as a live contract.

Only this line stated that lie as current. Left the Revision-tracking bullet “or the batch is rejected (`400`)”: it is a revision-mismatch 400, not a claim that later rejects roll back earlier items.

Did not edit [[plan/core-creation/arch.md]]. Did not rewrite [[doc/api.md]]. Did not chase clusters 3/4.
