# github-transport architecture

Spec: [[spec.md]]
Updated: 2026-11-13
Sequence: module-build

Sources: 
  - [spec.md](spec.md) User Stories 1–27; 
  - [map.md](map.md) Destination and Decisions so far (transport locks 
  - [01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md)
  – [10 — git Save is commit then push](issues/10-git-save-commit-then-push.md); thin remainder after 
  - [[plan/core-refinement/project.md]]). Checklist: `[x]` already true of the codebase shape; `[ ]` still to build. Does not invent product behavior beyond those locks. After core-refinement, this architecture’s open job is lock workspace, receive files, inform Core — not how Core works through changes. Existing Ambit smart-HTTP helper 
  - [[src/Shared/WorkspaceGitRemote.fs]] (`RemoteName` `ambit`, `/ambit/git/{label}.git`) is not the GitHub remote. Sequence is `module-build`: the stories share a few modules (PathPick, WorkspaceGit git facts, Server Actor, existing desk WebDAV). This is not expand-contract (no wide rename). Tracer-cut would mint one ticket per story and repeat the same hops.

## 1. Story paths

1. **Workspace is a git work tree**
   1. [x] Workspace DataDir work tree is a git work tree (`WorkspaceGit.isRepo`)
   2. [x] No special label required for git home
   3. [x] PathPick and Server Actor treat every such Workspace as a candidate (no extra Kind)

2. **Every Workspace may connect**
   1. [x] PathPick runs for any Workspace work tree
   2. [x] No allowlist and no special label gate

3. **Server-git when a remote exists**
   1. [x] `WorkspaceGit.remoteExists` in [[src/Server/WorkspaceGit.fs]] requires that directory's `.git` through `WorkspaceGit.isRepo`, then reads `git remote`
   2. [x] Plain Save is the Save actor when a remote exists. A local mapping and no remote is desk Save. No local mapping and no remote is Neither. `PathPick.plainSavePath` in [[src/Shared/PathPick.fs]]

4. **Config in git**
   1. [x] `WorkspaceGit.currentBranch` reads the attached branch from that work tree
   2. [x] WorkspaceGit reads `git remote` and current upstream on that work tree
   3. [x] No Server branch map module

5. **Same tracked branch**
   1. [x] git Load pulls the current tracked branch of the whole Workspace work tree
   2. [x] git Save follows Solution 11 — git Save sequence on that same tracked branch of the whole Workspace work tree.
   3. [x] Neither hop is file-level git
   4. [x] Neither hop checkouts, switches, or moves to an older commit

6. **Fast-forward only**
   1. [x] `WorkspaceGit.ensurePushConfig` sets `receive.denyNonFastForwards`
   2. [x] git Save rejects a conflicted or non-FF push
   3. [x] Conflict error names at least one file path
   4. [x] Other failures use a matching short error that reflects git, condensed
   5. [x] Reject does not accept non-overlapping diverge on push (not [[plan/workspace-git/project.md]])
   6. [x] git Load is one merge of the tracked remote branch, completed by committing that merge as part of the pull.
   7. [x] A file only the Server changed stays the Server file.
   8. [x] A file only the remote changed becomes the remote file.
   9. [x] Both sides edited the file, or both added the path. The file contents are the remote file. git writes no conflict markers.
   10. [x] A Server edit and a remote delete remove the file. A Server delete and a remote edit leave the remote file. git writes no conflict markers. That path is resolved to the remote action so the merge commit can be created.
   11. [x] The merge commit includes the remote tip, so a later git Save fast-forwards.
   12. [x] The work tree holds that merge.

7. **Remotes accept push**
   1. [x] Operator sets `git remote` on the work tree so git works
   2. [x] Server Actor pushes that remote; it does not invent a PR-only path

8. **Load**
   1. [x] Person Command Load (`CommandEntry` Load; [[src/Client/Commands.fs]] `loadOp`)
   2. [x] The Load command sends a core message through the mailbox to the actor pool (not Run, not `?git`). It does not send a Command or an Event
   3. [x] Plain Load asks PathPick
   4. [x] Plain Load enters the Load actor. `PathPick.resolveCommand` returns Git for Plain. Desk Load is the explicit desk pre-pick
   5. [x] Git Load informs Core after the pull lands. Desk Load sends a core message whose subject is a Workspace Node, Directory Node, or File Node. The parse push marks that subject Unparsed, then pushes it onto the parse stack. A node inside a File Node uses that File Node. The subject list is the whole selection. The mailbox posts Events. Desktop-mapped WebDAV stays [26 — Desk WebDAV](issues/26-desk-webdav.md). Fetch+Poll stays

9. **Save**
   1. [x] Person Command Save (`CommandEntry` Save; [[src/Client/UpdateSave.fs]] `saveOp`)
   2. [x] Save sends a core message through the same mailbox → actor-pool door as Load
   3. [x] Plain Save asks PathPick
   4. [x] `PathPick.plainSavePath` returns Desk for a local mapping and no remote. Other plain Save enters the Save actor. Download is Save. [[src/Shared/CommandEntry.fs]] `CommandEntry` lists Save keys `Ctrl+S` and `Ctrl+Shift+<`

10. **git Load**
    1. [x] Person names git Load (explicit pre-pick)
    2. [x] PathPick is skipped
    3. [x] Mailbox → actor pool invokes Server Actor; Actor uses Focus as the work tree
    4. [x] git Load is one merge of the tracked remote branch, completed by committing that merge as part of the pull.
    5. [x] A file only the Server changed stays the Server file.
    6. [x] A file only the remote changed becomes the remote file.
    7. [x] Both sides edited the file, or both added the path. The file contents are the remote file. git writes no conflict markers.
    8. [x] A Server edit and a remote delete remove the file. A Server delete and a remote edit leave the remote file. git writes no conflict markers. That path is resolved to the remote action so the merge commit can be created.
    9. [x] The merge commit includes the remote tip, so a later git Save fast-forwards.
    10. [x] The work tree holds that merge.
    11. [x] Reject uses condensed git error (conflict names at least one path)
    12. [x] After files land, Load informs Core of changes (today’s Parse / graph-push hop until [[plan/core-refinement/project.md]] retargets). Fetch+Poll stays
    13. [ ] Thin remainder: lock workspace, receive files, inform revised Core ([[plan/core-refinement/project.md]] owns how Core works through changes)

11. **git Save**
    1. [o] This story is retired. The sequence is Solution 11 — git Save sequence.

12. **desk Load**
    1. [x] Person names desk Load (explicit pre-pick)
    2. [x] PathPick is skipped
    3. [x] Desk file path stays WebDAV Upload / existing `loadOp` desk transit ([[doc/current/workspace-file-sync.md]])
    4. [x] Desk Load sends a core message whose subject is a Workspace Node, Directory Node, or File Node. 
    [x] The parse push marks that subject Unparsed, then pushes it onto the parse stack. 
    [x] The mailbox posts Events. 
    [x] Desktop-mapped WebDAV stays [26 — Desk WebDAV](issues/26-desk-webdav.md). 
    [x] Fetch+Poll stays

13. **desk Save**
    1. [x] Person names desk Save (explicit pre-pick)
    2. [x] PathPick is skipped
    3. [x] The tree keeps the staging desk command. `LoadSaveStart.startCommand` starts Desk. `deskSaveOp` posts `ContinueDeskSave` when `canGitSave` is true

14. **Plain Load prefers git**
    1. [x] Plain Load enters the Load actor. `PathPick.resolveCommand` returns Git for Plain and for Git
    2. [x] `PathPick.plainLoadPath` returns Git when that workspace label is in both `githubLabels` and `localLabels`

15. **Plain Save prefers git**
    1. [x] Plain Save with a remote uses the Save actor Remote arm. That arm commits, pulls, and pushes
    2. [x] Plain Save with a local mapping and no remote uses desk Save
    3. [x] Plain Save with no local mapping and no remote is Neither. Neither persists through `persistFocusWhenNotOrdinary`, then commits through `commitSaveRepository` when `WorkspaceGit.isRepo` finds that folder's `.git`

16. **No automatic pull or push**
    1. [x] No schedule starts git pull or push
    2. [x] No post-Persist git pull or push
    3. [x] No post-Download git pull or push
    4. [x] DailyGitSave / local Server commit is not GitHub push

17. **Inform Core after files land**
    1. [x] desk Load already continues after files land (today’s Parse / graph-push hop)
    2. [x] git Load and plain-git Load continue after a whole-tree pull; they do not stop at file transfer alone
    3. [ ] Remaining job after [[plan/core-refinement/project.md]]: lock workspace, receive files, inform revised Core; Core works through the changes
    4. [ ] Do not specify Parse/Persist stacks on this Project; that home is [[plan/core-refinement/project.md]]
    5. [ ] Parse thread product home stays [[plan/parse-thread/project.md]]

18. **WebDAV remains**
    1. [x] WebDAV Upload / Download stay implemented
    2. [x] git Load/Save do not replace that transit
    3. [x] `downloadOp` in [[src/Client/UpdateWorkspaceDownload.fs]] enqueues `workspace-download`. Save does not call `downloadOp`

19. **Server Actor does the round-trip**
    1. [x] Load or Save Command request reaches mailbox → actor pool → Server Actor
    2. [x] Actor cares about Focus (Workspace / work tree) only
    3. [x] App does not invoke `git` ([[src/Shared/dotnet/GitRun.fs]] is host-side)
    4. [x] App stays thin (no git Actor host)

20. **One Actor shape through Server**
    1. [x] The same Server Actor shape serves every device that maps through Server
    2. [x] No per-App clone protocol

21. **Host git credentials**
    1. [x] Server Actor invokes `git` through WorkspaceGit / GitSave / GitRun
    2. [x] GitRun starts the host `git` process
    3. [x] git loads credentials (credential helper / host setup)
    4. [x] No GitHub credential in appsettings, user-secrets, Graph, or DataDir

22. **Skip list is `.gitignore`**
    1. [x] Skip on the GitHub remote is whatever `.gitignore` already says
    2. [x] No Ambit skip key; the person edits `.gitignore`
    3. [x] Skip is not a hard Ambit default for `.amb`

23. **Backup when `.amb` is excluded**
    1. [x] WebDAV Upload / Download still transfer Directory File `.amb`
    2. [x] Server DataDir / Server git / DailyGitSave still track those notes
    3. [ ] Exclude-from-repo does not mean desk-local-only

24. **Workspace-scoped git**
    1. [x] git Load/Save from Workspace root or a subnode use the same whole-tree pull/push
    2. [x] No file-level git pathspec
    3. [x] Tracked branch is the Workspace work tree’s current branch / upstream

25. **Actor start door**
    1. [x] Not Run and not a `?git` entrée
    2. [x] The Load command sends a core message through the mailbox to the actor pool, which starts the Server Actor when the path is git. 
       [x] Save uses that same door.
    3. [x] Same wiring for Save as Load
    4. [x] Actor input is Focus (Workspace / work tree)

26. **Reject UX**
    1. [x] Conflict: error message names at least one file path
    2. [x] Other failures: matching short error
    3. [x] Text reflects what git reports, condensed
    4. [x] Same reject wording for Load and Save. That wording does not mean pull is fast-forward

27. **Persist stays independent of git Save**
    1. [o] This story is retired. The sequence is Solution 11 — git Save sequence.

Shared segments:
1. [x] Command Load and Save doors ([[src/Shared/CommandEntry.fs]], [[src/Client/Commands.fs]])
2. [x] PathPick (plain Load/Save only)
3. [x] WorkspaceGit remote-exists + tracked branch + pull/push
4. [x] Server Actor git Load / git Save (mailbox → actor pool; Focus = work tree; git Save is Solution 11 — git Save sequence)
5. [x] Desk Load stays `deskLoadOp` in [[src/Client/UpdateWorkspaceLoad.fs]]. Desk Save stays the staging `deskSaveOp` in [[src/Client/UpdateSave.fs]]
6. [x] Host GitRun (no Ambit credential store)
7. [x] Load informs Core after files land (today’s Parse / graph-push hop until [[plan/core-refinement/project.md]] retargets). Fetch+Poll stays
8. [x] Reject UX: condensed git error; conflict names a path

Narrowest shared test seam:
1. [x] `PathPick.plainSavePath` returns Desk for a local mapping and no remote. Other plain Save stays on the Save actor. Plain Load enters the Load actor (pure; no git process)
2. [x] WorkspaceGit remote-exists + tracked-branch push stays fast-forward-only through GitRun on a temp work tree.
3. [x] git Load is one merge of the tracked remote branch, completed by committing that merge as part of the pull.
4. [x] Server Actor git Load / git Save invoke that WorkspaceGit interface; no Ambit credential argument
5. [x] Work-tree gate: a second Persist, pull, or commit waits for the holder and continues after release without overlap — **revoked 2026-09-28**; not current required architecture

## 2. Module map

1. **Command Load/Save**
   File: [[src/Shared/CommandEntry.fs]], [[src/Shared/LoadSaveCommand.fs]], and [[src/Client/Commands.fs]] (existing Load / Save). Spoken names stay Load and Save. A Command is not sent. Pre-picks are git Load / git Save / desk Load / desk Save on the core message — not Git Remote / Git Pull / Git Push.
   1. State
      1. [x] CommandId Load and Save
      2. [x] Path pre-pick: Plain | Git | Desk on `LoadSaveCommandRequest` (not a new primary command name)
   2. Interface
      1. [x] Person Command Load and Save
      2. [x] Plain Save asks `PathPick.plainSavePath`. Plain Load asks `PathPick.plainLoadPath` and `PathPick.resolveCommand`
      3. [x] Explicit Git or Desk skips that plain choice
      4. [x] Download is Save. `CommandEntry` Save keys are `Ctrl+S` and `Ctrl+Shift+<`
      5. [x] No schedule, post-Persist, or post-Download start
      6. [x] The Load command sends a core message through the mailbox to the actor pool (not Run, not `?git`). Save uses that same door
      7. [x] Load informs Core after files land (today’s Parse / graph-push hop until [[plan/core-refinement/project.md]] retargets)
   3. Uses
      1. [x] PathPick
      2. [x] Server Actor (git path) via actor pool
      3. [x] Desk Load/Save (desk path)
      4. [x] Core informed after files land ([[plan/core-refinement/project.md]]); Fetch+Poll stays

2. **PathPick**
   File: new [[src/Shared/PathPick.fs]] (pure choose). Remote-exists fact stays on WorkspaceGit.
   1. State
      1. [x] None durable
   2. Interface
      1. [x] `choose: remoteExists:bool -> Git | Desk` — Git when true, Desk when false
      2. [x] `plainSavePath` returns Desk when the pre-pick is Plain, the workspace label is in `localLabels`, and that label is absent from `githubLabels`. Other plain Save stays on the Save actor
      3. [x] `resolveCommand` returns Git for Plain and for Git, and Desk for Desk
      4. [x] No allowlist, no Workspace name list, no Server branch map
   3. Uses
      1. [x] None (pure)

3. **WorkspaceGit**
   File: [[src/Server/WorkspaceGit.fs]] (extend). Git process stays [[src/Server/GitSave.fs]] / [[src/Shared/dotnet/GitRun.fs]].
   1. State
      1. [x] DataDir work tree `.git` (`isRepo`, `currentBranch`, `ensurePushConfig`)
      2. [x] Remote-exists and current upstream read from that work tree only
      3. [x] One exclusive work-tree gate per Workspace shared with Graph→file Persist — **revoked**; replacement on [[plan/core-refinement/project.md]] ([01 — Persist/git work-tree gate](../core-refinement/issues/01-persist-git-work-tree-gate.md))
   2. Interface
      1. [x] `isRepo` / `currentBranch` / `ensurePushConfig` (FF-only receive)
      2. [x] `remoteExists: workspaceRoot -> Result<bool, string>` reads `git remote` when `isRepo` finds that directory's `.git`. Any remote counts
      3. [x] Tracked branch + upstream from that work tree (no Server map)
      4. [x] git Load uses the current tracked branch of the whole Workspace work tree. The pull is not file-level git.
      5. [x] git Load is one merge of the tracked remote branch, completed by committing that merge as part of the pull.
      6. [x] A file only the Server changed stays the Server file.
      7. [x] A file only the remote changed becomes the remote file.
      8. [x] Both sides edited the file, or both added the path. The file contents are the remote file. git writes no conflict markers.
      9. [x] A Server edit and a remote delete remove the file. A Server delete and a remote edit leave the remote file. git writes no conflict markers. That path is resolved to the remote action so the merge commit can be created.
      10. [x] The merge commit includes the remote tip, so a later git Save fast-forwards.
      11. [x] The work tree holds that merge.
      12. [x] git Save is Solution 11 — git Save sequence.
      13. [x] Skip list is `.gitignore` on that work tree (person edits that file; no Ambit skip key)
      14. [x] Reject error: conflict names at least one file path; other failures a matching short git-condensed message
      15. [x] No checkout, switch, or older-commit move
      16. [x] No GitHub credential argument
      17. [o] The Graph→file Persist exclusion is retired. The sequence is Solution 11 — git Save sequence.
      18. [x] Persist file writes, git Load pull, and git Save commit acquire and release the Workspace work-tree gate — **superseded**; not current required interface
      19. [x] A second caller waits for the gate and continues after release instead of rejecting as busy — **superseded**
   3. Uses
      1. [x] GitSave.runGit / GitSave.commitAll / GitRun.gitExec (host `git`)
      2. [x] Host credential helper (git’s, not Ambit’s)

4. **Server Actor**
   File: new [[src/Server/GithubTransportActor.fs]] (spoken name: Server Actor; not a new Kind). Start door: [07 — Actor start door](issues/07-actor-start-door.md).
   1. State
      1. [x] Live Actor row only while a person-started git Load or git Save runs
      2. [x] No stored GitHub credential
   2. Interface
      1. [x] Pool invokes the Actor from the Load or Save core message on the mailbox (not Run, not `?git`)
      2. [x] Actor cares about Focus only (which Workspace / work tree)
      3. [x] Same wiring for Save as Load
      4. [x] After git Load, the Server Actor informs Core that files changed.
      5. [x] git Load is one merge of the tracked remote branch, completed by committing that merge as part of the pull.
      6. [x] Commit-before-pull already happens.
      7. [x] git Save is Solution 11 — git Save sequence.
      8. [x] Reject UX: conflict names at least one file path; other failures a matching short git-condensed error; same reject wording for Load and Save. That wording does not mean pull is fast-forward
      9. [x] Same Actor shape for every device that maps through Server
      10. [x] Invokes WorkspaceGit; does not call git with an Ambit-supplied token
      11. [x] Acquires and releases the Workspace work-tree gate around git Load pull and git Save commit — **superseded**; exclusive gate revoked ([[plan/core-refinement/issues/01-persist-git-work-tree-gate.md]])
   3. Uses
      1. [x] CoreActorPool / ActorFn ([[src/Server/Core/CoreActorPool.fs]])
      2. [x] WorkspaceGit
      3. [x] No App git host
      4. [x] After git Load files land, inform Core ([[plan/core-refinement/project.md]])

5. **Desk Load/Save**
   File: existing [[src/Client/UpdateWorkspaceLoad.fs]], [[src/Client/UpdateSave.fs]], [[src/Shared/dotnet/WorkspaceFileSync.fs]]. Shape unchanged for this Project.
   1. State
      1. [x] Desktop map + WebDAV sync ledger
   2. Interface
      1. [x] desk Load: App↔Server file transit (Upload / existing `loadOp`) then inform Core
      2. [x] desk Save stays the staging command. `deskSaveOp` posts `ContinueDeskSave` when `canGitSave` is true. `Api.gitSave` commits the data directory
      3. [x] WebDAV Upload and `downloadOp` remain. Save does not call `downloadOp`
   3. Uses
      1. [x] Desktop workspace-push / WebDAV
      2. [x] Server GitSave for local DataDir commit
      3. [x] Core informed after files land ([[plan/core-refinement/project.md]]); Fetch+Poll stays

6. **App**
   File: Desktop / Browser hosts (existing). Stays thin.
   1. State
      1. [x] No git remote map on the App
   2. Interface
      1. [x] Does not invoke GitRun for GitHub pull or push
      2. [x] Does not host the Server Actor
   3. Uses
      1. [x] Command Load/Save
      2. [x] Desk WebDAV when PathPick or pre-pick is desk

## 3. Seams

1. **PathPick choose**
   Interface on **PathPick**. Tests pass `plainSavePath` for a remote, a local mapping, and neither. Plain Load of a Directory, a File, and a Workspace enters the Load actor. No git process.
   1. [x] Pure `choose` Git vs Desk
   2. [x] `plainSavePath` returns Desk only for a local mapping and no remote

2. **WorkspaceGit git facts**
   Interface on **WorkspaceGit**. Tests cross GitRun on a temp work tree (remote present / absent; FF push accept / reject; `.gitignore` skip).
   1. [x] `remoteExists` + tracked branch
   2. [x] push stays fast-forward-only.
   3. [x] git Load is one merge of the tracked remote branch, completed by committing that merge as part of the pull.
   4. [x] git Save is Solution 11 — git Save sequence.
   5. [x] skip list is `.gitignore`
   6. [x] reject text: conflict names a path; other failures short git-condensed
   7. [x] no credential parameter
   8. [o] The no-Persist call line is retired. The sequence is Solution 11 — git Save sequence.
   9. [x] one gate per Workspace; a second Persist, pull, or commit waits for the holder and continues after release — **revoked 2026-09-28**; not current required seam

3. **Server Actor git Load/Save**
   Interface on **Server Actor**. Tests stub WorkspaceGit. Start door is mailbox → actor pool from Load/Save ([07 — Actor start door](issues/07-actor-start-door.md)).
   1. [x] The Load or Save core message reaches the pool, which invokes the Actor; Focus names the work tree
   2. [x] git Load is one merge of the tracked remote branch, completed by committing that merge as part of the pull.
   3. [x] git Save is Solution 11 — git Save sequence.
   4. [o] The line that kept Persist off git Save is retired. The sequence is Solution 11 — git Save sequence.
   5. [x] reject UX same wording for Load and Save. That wording does not mean pull is fast-forward
   6. [x] no Ambit credential store
   7. [x] pull and commit acquire and release the Workspace work-tree gate — **superseded**; exclusive gate revoked

4. **Desk WebDAV**
   Interface on **Desk Load/Save**. Existing seam. This Project does not redesign it.
   1. [x] Upload / Download / `loadOp` desk transit

5. **Host git credentials**
   Interface on **WorkspaceGit** (invoke git only). Seam is the host `git` process, not an Ambit secret module.
   1. [x] GitRun starts `git`
   2. [x] No appsettings / user-secrets / Graph / DataDir GitHub credential module

## 4. Alternative considered

1. **workspace-git command surface** — Git Remote / Git Pull / Git Push on WorkspaceGit, with non-FF accept of non-overlapping edits ([[plan/workspace-git/spec.md]]). Rejected: [02 — Actor command surface](issues/02-actor-command-surface.md) locks Load/Save plus git* / desk* pre-picks; git push stays fast-forward-only and git Load is a remote-wins merge ([Solution item 4 — Fast-forward push, remote-wins pull](spec.md)).
2. **App-hosted git** — App clones or pushes GitHub. Rejected: Destination keeps the App thin; Server Actor is the git host.
3. **Server branch map** — Ambit stores which Workspace tracks which branch. Rejected: [01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md); config is git on that work tree.
4. **Ambit credential store** — appsettings / user-secrets / Graph / DataDir hold a GitHub token. Rejected: [04 — Credential storage on Server](issues/04-credential-storage-on-server.md); git loads host credentials.
5. **Automatic git** — schedule, post-Persist, or post-Download pull/push. Rejected: [03 — When pull and push fire](issues/03-when-pull-and-push-fire.md); person Commands only.
6. **File-transfer-only Load with no Core handoff** — git Load stops at pull and never informs Core. Rejected: after files land, inform Core; Core works through changes ([[plan/core-refinement/project.md]]). Parse thread home stays [[plan/parse-thread/project.md]].
7. **File-level git** — pull or push a file or subtree because Load/Save was invoked on a subnode. Rejected: [05 — Git Load/Save are Workspace-scoped](issues/05-git-load-save-workspace-scoped.md).
8. **Run / `?git` entrée** — Start the Server Actor from Run or a `?git` Command. Rejected: [07 — Actor start door](issues/07-actor-start-door.md); door is Load or Save via mailbox → actor pool.
9. **Ambit skip key** — A special Ambit config key for `.amb` skip. Rejected: [09 — Skip list is .gitignore](issues/09-gitignore-skip-list.md); skip list is `.gitignore`.
10. **Merge Persist into git Save** — This alternative is retired. The sequence is Solution 11 — git Save sequence.

## 5. Unsettled

1. How desk Save does local sync is unspecified. The tree keeps the staging desk command: `LoadSaveStart.startCommand` for Desk, then `deskSaveOp`.
2. Desk Load is open for the later desk load/save ticket.
3. The ROOT pull label `.` is open for that ticket. `GithubTransportActor.reconcileLabel` uses `.` when the workspace id is `Graph.rootId`. `GraphQuery.trySyncRootByLabel` maps `.` to that root.
