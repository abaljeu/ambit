# 30 — GitHub save path: missing workspace upward scan and workspace upload

**Status**: defined

One job: A person who runs GitHub Save on a node inside a Workspace gets that Workspace by the same upward scan GitHub Load uses, and GitHub Save then uploads that Workspace. The report is Alan's voice on 2026-11-08.

Type:

[ ] bug-fixing

Blockers:

[ ] none

## Inputs

### Working

[ ] **Save actor** — `saveActorFunction` in `src/Server/GithubTransportActor.fs`.
[ ] **Load actor** — `loadActorFunction` in the same file.
[ ] **Shared body** — `runBody` calls `workspaceFromFocus` for Load and for Save.
[ ] **Workspace walk** — `GraphQuery.enclosingWorkspace` in `src/Shared/GraphQuery.fs` walks the owner chain to a Workspace node.
[ ] **Load subject walk** — `LoadSubject.enclosingSubject` in `src/Shared/PathPick.fs` walks the owner chain to the nearest Workspace, Directory, or File.
[ ] **Save step** — `runSaveStep` takes `workspaceId` and calls `saveTracked` on the work-tree root.
[ ] **Save git calls** — `saveTracked` calls `commitAll` with the message "gambol: git Save", then `pushTracked`.
[ ] **Read point** — The observations below are from staging commit `fcc01cd0`.
[ ] **Prior lock** — [10 — git Save is commit then push](10-git-save-commit-then-push.md) stays as written.

### Reference

[ ] **Rename translation** — [StartActorDeprecated gap](../../actor-as-client/reports/startactor-deprecated-gap.md).
[ ] **Load and Save functions note** — [Git Load and Save actor functions](../../actor-as-client/reports/git-load-and-save-actor-functions.md).
[ ] **Spec item 11** — [github-transport spec](../spec.md). This ticket leaves that spec as written.

## Reported

[ ] **Upward scan** — Alan reports that the Save function skips the upward Workspace scan and that Load scans upward. Save is to use that same upward scan.
[ ] **Upload with a Workspace in hand** — Alan reports that Save skips the Workspace upload even when a Workspace is provided or left in. Alan gives the cause as unknown.
[ ] **Focus, then git save** — Alan intends an explicit save of the focus node, then a handoff to git save for the Workspace upload.
[ ] **Stage order** — Alan's git save sequence, copied as spoken: commit -> pull -> optimistic merge -> save -> push.
[ ] **Stage 1** — commit.
[ ] **Stage 2** — pull.
[ ] **Stage 3** — optimistic merge.
[ ] **Stage 4** — save.
[ ] **Stage 5** — push.
[ ] **Separate stages** — Each stage stays its own stage, in that order.
[ ] **Scope** — This ticket is the GitHub save path. Alan says the other path is isolated higher up.

## Observed on this read

[ ] **Shared workspace walk** — `runBody` calls `workspaceFromFocus` for Load and for Save. `workspaceFromFocus` calls `GraphQuery.enclosingWorkspace`.
[ ] **Extra Load walk** — After that shared walk, Load calls `LoadSubject.enclosingSubject`. The body of `runSaveStep` is the `saveTracked` call.
[ ] **Workspace id on the Save step** — `runSaveStep` names the parameter `workspaceId`. The body calls `saveTracked` on the root.
[ ] **Save git order in source** — `saveTracked` calls `commitAll`, then `pushTracked`.
[ ] **Load and the Workspace node** — `finishLoadOn` calls `changes.loadNode` with the Workspace id.
[ ] **Cause stays open** — These observations stay facts about the read. The cause of the skipped upload stays unknown.

## Spoken context

[ ] **Old StartActor** — Alan says the old StartActor is renamed "StartActor deprecated". `Order.StartActorDeprecated` of `ActorStart` is in `src/Shared/History.fs`.
[ ] **ActorLoad** — Alan says ActorLoad is renamed StartActor. The source case is `Order.StartActor` of `ActorOrder`. This ticket keeps the spoken name ActorLoad.
[ ] **Load-save home** — Alan says load-save moved onto the new StartActor.
[ ] **File split** — Alan says `GithubTransportActor.fs` separates Load and Save at the head, with shared functionality below. This read places `loadActorFunction` and `saveActorFunction` after the shared helpers.
[ ] **ActorStart third field** — Alan says the ActorStart third parameter is the run definition id. The third field of `ActorStart` is `runDefinitionId`. The fields in order are `zoomId`, `focusId`, `runDefinitionId`, `graphIds`, `eventId`.
[ ] **ActorOrder second field** — Alan says the ActorOrder second parameter is the actor node id. The second field of `ActorOrder` is `actorNodeId`. The fields in order are `actor`, `actorNodeId`, `actorName`.
[ ] **Gap report layout** — The StartActorDeprecated gap report records an `ActorOrder` field `focusId`. The current `ActorOrder` fields are the three names above.
[ ] **FirstFactorStart** — Alan says "FirstFactorStart is a struct". A name search of `src/` found no `FirstFactorStart`. This ticket keeps that spoken name.

## Do not load

[ ] **Other path** — Desk Save, and the path Alan isolates higher up, stay out of this ticket.
[ ] **Other agent's note** — A dated definition note under `plan/actor-as-client` stays with the other agent.
[ ] **Named reports** — The StartActorDeprecated gap report and the Git Load and Save actor functions note stay as they are.
[ ] **Prior lock file** — 10 — git Save is commit then push, the spec, and the map decision that records that lock stay as they are.
[ ] **Source** — This filing writes the ticket, the project note, and the map pointer.
[ ] **Stage text** — The five spoken stages stay in Alan's order, as five stages. This ticket gives them no new names.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Answer each question in Investigation. Mark each answer as reported, intended, or verified.
3. [ ] Keep the five stages in Alan's order. Keep each stage separate.
4. [ ] Name the cause of the skipped upload only from evidence.
5. [ ] Stop before a source edit.

## Investigation

[ ] **Which Save** — Which function is the Save Alan means?
[ ] **Which scan** — Which upward scan does Load use that Save is to share?
[ ] **Which value is in hand** — What is the Workspace value Alan says is provided or left in?
[ ] **Which upload** — What is the Workspace upload: a git push of the work tree, a post of the Workspace node, or both?
[ ] **Why the skip** — Why does Save skip that upload when the Workspace value is already in hand?
[ ] **Where the other path sits** — Which higher file isolates the other path?
[ ] **Stage map** — Which existing function, if any, is each of the five stages?
[ ] **Optimistic merge** — What does optimistic merge name in this repo?
[ ] **Two saves** — What is the stage named save, beside the explicit save of the focus node?
[ ] **Spoken struct** — Does a later note define FirstFactorStart?

## Outputs

[ ] `plan/github-transport/issues/30-github-save-path-missing-workspace-upward-scan-and-workspace-upload.md`
[ ] The 2026-11-08 note on [github-transport](../project.md).
[ ] The open-item line in [github-transport map](../map.md).

## Stop when

[ ] Record the stop at `plan/github-transport/reports/30-github-save-path-missing-workspace-upward-scan-and-workspace-upload-stop.md`, then ask.
[ ] Stop when the work edits source under this filing.
[ ] Stop when the work leaves the GitHub save path.
[ ] Stop when a stage is reordered, merged, or given a new name.
[ ] Stop when an observation in this ticket is written down as the cause.

## Check

[ ] GitHub Save uses the same upward Workspace scan as GitHub Load.
[ ] When the Workspace value is already in hand, GitHub Save uploads that Workspace.
[ ] GitHub Save saves the focus node, then hands off to git save for the Workspace upload.
[ ] The git save sequence is commit, then pull, then optimistic merge, then save, then push. Each stage stays separate.
[ ] The change stays on the GitHub save path.
[ ] The stop report names the cause from evidence, or its first line says the cause is still unknown.
[ ] This filing runs no tests. A later build runs `scripts/agent-test.sh` with no arguments.
[ ] That later build runs `scripts/agent-test.sh --filter` for each test that build names:
  - `<Project>`: ``<test name>``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/github-transport/reports/30-github-save-path-missing-workspace-upward-scan-and-workspace-upload-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
