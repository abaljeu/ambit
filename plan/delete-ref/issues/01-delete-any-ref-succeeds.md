# 01 — Delete of any Ref succeeds

**Status:** agent-done

## What happened

Delete on a Ref to the Workspaces Node did not remove that Ref. The appearance stayed in the parent Children. Delete of the Owned Workspaces Node was not requested.

## What I expected

Delete of any Ref removes that appearance only. The Owned Node stays where it is. A Ref to Workspaces is not a special case.

## Steps to reproduce

1. Place a Ref to the Workspaces Node in some Children list (not the Owned Workspaces child under ROOT).
2. Select that Ref.
3. Delete.
4. The Ref is still there.

## Additional context

Delete of the Owned Workspaces Node under ROOT must still be refused. The same failure likely applies to a Ref to SYSTEM, TRASH, or a Workspace Node. Delete of a Ref to a Normal Node is the intended success path. Status may still show Delete Ok while the Ref remains.

## Comments

Classifier now blocks only Owned system folders and Workspace Nodes. A Ref to those targets is LocalDeleteRefOnly. Owned delete of Workspaces under ROOT is still empty. Shared tests: Ref to Workspaces, Ref to a named Workspace Node; existing Owned workspace / TRASH-in-range cases still cancel.
