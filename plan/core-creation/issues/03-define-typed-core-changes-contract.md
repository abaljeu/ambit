# Define the typed Core Changes contract

Type: grilling
Status: resolved
Blocked by:
Actual: 30m

## Question

What typed object contract does Core Changes accept for one Change and a batch from Browser-originated and Server-originated callers, and what result distinguishes accepted output, produced Change sequence and Revision, Reject, and changeId deduplication without exposing JSON or HTTP concerns?

## Answer

Core Changes accepts a typed `Change list`. One Change uses a one-item list. An empty list and a Change with no effect remain Rejects.

The normal and Graph-only operations have the same typed contract. Normal Changes come from the Browser or a Server Actor. They apply to the Graph and History and retain the current document validation and persistence behavior. Graph-only Changes are reserved for Parse because they started from files. Parse reads files and produces the Changes before the call. Core applies those Changes to the Graph and History without writing them back to documents. Lazy-load reconciliation and git-push reconciliation invoke Parse.

Both operations preserve the current batch order, all-or-nothing Reject behavior, amendment, persistence stamps, Revision changes, acknowledgement, timeout, and persistence-mode behavior. A repeated `changeId` returns the stored accepted Change through the normal success path and does not advance Revision. Deduplication has no separate result case.

The typed asynchronous result distinguishes Reject from acceptance. The accepted value contains the final Revision, acknowledged Changes in input order, `externalChanges`, the persistence message, and readiness. The HTTP Adapter adds build and protocol fields and performs JSON decode and encode. Existing text Reject details remain unchanged.

Exact module names, project placement, and HTTP adaptation remain for [[04-separate-http-adapter-from-core-changes]] and [[05-place-core-changes-in-existing-projects]].

The requirement above to preserve persistence-mode behavior is superseded by [[13-delete-runtime-mirror-and-remove-production-persistence-mode.md]]. This does not change the typed Core Changes contract.

## Time

- 2026-09-05 30m — grilled and resolved the typed Core Changes contract
