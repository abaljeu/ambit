# Resolve the merge on ticket 64

## Merge state

This is a merge, not a rebase. The checked-out branch is `staging` (`8120426e`). The incoming branch is `dev` (`3df8ae7a`). The merge message is `Merge branch 'dev' into staging`.

- **Ours** — `staging` (HEAD). `staging` matches `origin/staging`.
- **Theirs** — `dev` (MERGE_HEAD). This is the incoming side.

`origin/staging` has no new commits in this merge. The incoming change is local `dev`.

The conflicted file is [64 — Merge PostEvent and PostGraphOnly into Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md). It has two hunks. It is the ticket that was open at the start of this run.

## Incoming change

`dev` has one commit that `staging` does not have:

- `3df8ae7a` — rewrite core refinement tickets

That commit rewrites three tickets and adds one report:

- [64 — Merge PostEvent and PostGraphOnly into Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md)
- [65 — Log Undo and Redo as the target](../issues/65-log-undo-and-redo-as-the-target.md) (the name on `dev`; `staging` uses the renamed file)
- [66 — Move Actor orders onto Order and Notice](../issues/66-move-actor-orders-onto-order-and-notice.md)
- [One home for tickets 64, 65, and 66](one-home-tickets-64-66.md)

The rewrite gives each ticket one home. Context becomes lists. Acceptance criteria name the four bodies that stay on `PostEvent`. See also on ticket 64 gains a link to [66 — Move Actor orders onto Order and Notice](../issues/66-move-actor-orders-onto-order-and-notice.md). The file-skip rule in that rewrite is `commandName` `"Parse"`. Decision 18 **ChangeOrigin** is absent from that commit. [Message shape](../cqrs-messages.md) on `dev` has no `ChangeOrigin`.

[65 — Undo and Redo resolve their ops from the target](../issues/65-undo-and-redo-resolve-their-ops-from-the-target.md), [66 — Move Actor orders onto Order and Notice](../issues/66-move-actor-orders-onto-order-and-notice.md), and the report merged cleanly. They are already in the index from the merge. This run left them as they were.

## Commits already on staging

These commits are on `staging` and absent from `dev`. The first two touch ticket 64:

- `aad85fc1` — Record ChangeOrigin on the placed Order.Change. (#287). Edit and Parse are `ChangeOrigin` on the placed order. `CoreEventDispatch` reads that origin. The logged Change holds the op list. `commandName` stays the command label.
- `5d0df7c5` — Rename ticket 65 to Undo and Redo resolve their ops from the target. (#288).
- `ae045638` — Move Session off the core mailbox (#282). This commit does not change ticket 64.
- `ed042237` — Open file shows text files instead of downloading (#286). This commit does not change ticket 64.
- `8120426e` — Merge branch `staging` of the GitHub remote into `staging`.

Decision 18 **ChangeOrigin** is in [Message shape](../cqrs-messages.md) on `staging`.

## Hunk 1 — Context through acceptance criteria

**Staging.** Context, What to build, and Acceptance Criteria use `ChangeOrigin` (`Edit | Parse`). Origin `Parse` keeps the file skip that `PostGraphOnly` uses today. Origin `Edit` writes document files and posts `Notice.NodeUnpersisted`.

**Dev.** The same sections are lists. The file skip still uses `commandName` `Some "Parse"`. The What to build intro and the `### 1. Change` heading are gone. Acceptance criteria use short titles. The four bodies stay on `PostEvent`.

**Resolution.** Alan asked for the `dev` list layout with the staging behaviour. The working tree uses that layout. Context lists the two doors and the three parse posters. What to build is the four checklist items, with no intro paragraph and no `### 1. Change` heading. Acceptance criteria keep the short titles from `3df8ae7a`. The file-skip rule is decision 18 **ChangeOrigin**. Origin `Parse` keeps the file skip. Origin `Edit` writes document files and posts `Notice.NodeUnpersisted`. The text does not use `commandName` `"Parse"` as that key.

**Trade-off.** The short title on **Edit** still says `an ordinary Change writes the document file`. The sentence after it says the placed change is origin `Edit`. That title is the layout's title. The behaviour is origin `Edit`. This correction adds no other behaviour.

## Hunk 2 — See also

**Staging.** See also links [65 — Undo and Redo resolve their ops from the target](../issues/65-undo-and-redo-resolve-their-ops-from-the-target.md). The common ancestor had no link to ticket 66. Staging only renamed ticket 65.

**Dev.** See also links the old ticket 65 name and adds [66 — Move Actor orders onto Order and Notice](../issues/66-move-actor-orders-onto-order-and-notice.md).

**Resolution.** See also keeps the renamed ticket 65 link and adds the ticket 66 link. The renamed file is the file on disk. Context names ticket 66. The ticket 66 link is the addition from `dev`. This correction leaves hunk 2 as that resolution.

## Checks

The resolution edits one plan Markdown file. It changes no source. `dotnet test` did not run.

## Git

This run did not stage, commit, continue, or abort. The index still has the three unmerged stages for ticket 64. Alan reviews the working tree, then stages that file and commits to finish the merge, or aborts.
