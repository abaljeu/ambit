# 02 — Delete of an Owned Node that has a self-Ref hangs

**Status:** ready-for-agent

## What happened

A Node had a Ref to itself in its Children. Delete of that Node's Owned appearance did not finish. The App stayed busy until the process was stopped.

## What I expected

Delete of the Owned appearance finishes. The Node leaves its current parent. A self-Ref is not a second home. The command returns.

## Steps to reproduce

1. Create a Normal Node under ROOT (or any ordinary parent).
2. Add a Ref to that same Node as one of its Children (the Node contains a reference to itself).
3. Select the Owned appearance of that Node (the child under the parent, not the self-Ref row).
4. Delete.
5. The command does not return.

## Additional context

Delete of the self-Ref row only is a different gesture and is not this report. Delete of some other Owned child while a sibling self-Ref exists is also a different case.

Delete currently treats the self-Ref as another placement and tries to make it the Owned home, then removes the original Owned row. That makes the Node own itself, and a later owner-chain walk may never return. Delete must not promote a self-Ref. Move to TRASH (or drop self-Refs first, then treat as last appearance) is the intended outcome.

## Comments

Sibling: [[01-delete-any-ref-succeeds.md]]. Database Owned-cycle repair is a separate Project: [[plan/owner-edge-db-repair/spec.md]].
