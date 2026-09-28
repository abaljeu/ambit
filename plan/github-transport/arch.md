# github-transport architecture

Spec: [[spec.md]]
Updated: 2026-09-28
Sequence: module-build

Sources: [spec.md](spec.md) User Stories 1–27; [map.md](map.md) Destination and Decisions so far 1–20 ([01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md)–[10 — git Save is commit then push](issues/10-git-save-commit-then-push.md), [16 — Persist/git work-tree gate](issues/16-persist-git-work-tree-gate.md) exclusive gate revoked, [17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md)–[19 — Parsed/Unparsed and Persisted/Unpersisted](issues/19-file-newer-graph-newer.md), [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md) locked). Checklist: `[x]` already true of the codebase shape; `[ ]` still to build. Does not invent product behavior beyond those locks. Existing Ambit smart-HTTP helper [[src/Shared/WorkspaceGitRemote.fs]] (`RemoteName` `ambit`, `/ambit/git/{label}.git`) is not the GitHub remote. Sequence is `module-build`: the stories share a few modules (PathPick, WorkspaceGit git facts, Peer Actor, existing desk WebDAV). This is not expand-contract (no wide rename). Tracer-cut would mint one ticket per story and repeat the same hops.

## 1. Story paths

1. **Workspace is a git work tree**
   1. [x] Workspace DataDir work tree is a git work tree (`WorkspaceGit.isRepo`)
   2. [x] No special label required for git home
   3. [x] PathPick and Peer Actor treat every such Workspace as a candidate (no extra Kind)

2. **Every Workspace may connect**
   1. [x] PathPick runs for any Workspace work tree
   2. [x] No allowlist and no special label gate

3. **Server-git when a remote exists**
   1. [x] WorkspaceGit reports whether a remote exists on that work tree
   2. [x] PathPick chooses git when a remote exists, else desk

4. **Config in git**
   1. [x] `WorkspaceGit.currentBranch` reads the attached branch from that work tree
   2. [x] WorkspaceGit reads `git remote` and current upstream on that work tree
   3. [x] No Server branch map module

5. **Same tracked branch**
   1. [x] git Load pulls the current tracked branch of the whole Workspace work tree
   2. [x] git Save commits work-tree edits, then pushes the same tracked branch of the whole Workspace work tree
   3. [x] Neither hop is file-level git
   4. [x] Neither hop checkouts, switches, or moves to an older commit

6. **Fast-forward only**
   1. [x] `WorkspaceGit.ensurePushConfig` sets `receive.denyNonFastForwards`
   2. [x] git Save rejects a conflicted or non-FF push
   3. [x] Conflict error names at least one file path
   4. [x] Other failures use a matching short error that reflects git, condensed
   5. [x] Reject does not accept non-overlapping diverge (not [[plan/workspace-git/project.md]])

7. **Remotes accept push**
   1. [x] Operator sets `git remote` on the work tree so git works
   2. [x] Peer Actor pushes that remote; it does not invent a PR-only path

8. **Load**
   1. [x] Person Command Load (`CommandEntry` Load; [[src/Client/Commands.fs]] `loadOp`)
   2. [x] Load command request → mailbox → actor pool (not Run, not `?git`)
   3. [x] Plain Load asks PathPick
   4. [x] PathPick git → pool invokes GitHub Peer Actor (Focus = Workspace / work tree); PathPick desk → desk Load
   5. [x] After files land, Load reaches Parse / graph-push. Required handoff is Unparsed → push onto the one Parse actor ([17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md)); directory-reconcile worker withdrawn. Fetch+Poll stays

9. **Save**
   1. [x] Person Command Save (`CommandEntry` Save; [[src/Client/UpdateSave.fs]] `saveOp`)
   2. [x] Save command request uses the same mailbox → actor-pool door as Load
   3. [x] Plain Save asks PathPick
   4. [x] PathPick git → pool invokes GitHub Peer Actor (Focus = Workspace / work tree); PathPick desk → desk Save

10. **git Load**
    1. [x] Person names git Load (explicit pre-pick)
    2. [x] PathPick is skipped
    3. [x] Mailbox → actor pool invokes Peer Actor; Actor uses Focus as the work tree
    4. [x] Peer Actor pulls the whole Workspace work tree / tracked branch (invocation node does not narrow git)
    5. [x] Reject uses condensed git error (conflict names at least one path)
    6. [x] After files land, Load reaches Parse / graph-push. Required handoff is Unparsed on Workspace → pull → push onto the one Parse actor; directory-reconcile worker withdrawn. Fetch+Poll stays
    7. [ ] Selection Load pushes the selection onto the same Parse stack ([06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md)); no special path or priority

11. **git Save**
    1. [x] Person names git Save (explicit pre-pick)
    2. [x] PathPick is skipped
    3. [x] Same mailbox → actor-pool door as Load; Actor uses Focus as the work tree
    4. [x] Peer Actor `git commit`s the work-tree edits (`GitSave.commitAll`)
    5. [x] Then pushes the whole Workspace work tree / same tracked branch (invocation node does not narrow git)
    6. [x] Does not invoke or replace Graph→file Persist
    7. [x] Reject uses condensed git error (conflict names at least one path)

12. **desk Load**
    1. [x] Person names desk Load (explicit pre-pick)
    2. [x] PathPick is skipped
    3. [x] Desk file path stays WebDAV Upload / existing `loadOp` desk transit ([[doc/current/workspace-file-sync.md]])
    4. [x] Desk Load reaches Parse after files land. Required handoff is Unparsed → push onto Parse (Upload is the same path); directory-reconcile worker withdrawn. Fetch+Poll stays

13. **desk Save**
    1. [x] Person names desk Save (explicit pre-pick)
    2. [x] PathPick is skipped
    3. [x] Desk file path stays the existing desk Save / WebDAV transit (not a GitHub push)

14. **Plain Load prefers git**
    1. [x] Plain Load with a remote exists → same hops as **git Load**
    2. [x] Plain Load with no remote → same hops as **desk Load**

15. **Plain Save prefers git**
    1. [x] Plain Save with a remote exists → same hops as **git Save**
    2. [x] Plain Save with no remote → same hops as **desk Save**

16. **No automatic pull or push**
    1. [x] No schedule starts git pull or push
    2. [x] No post-Persist git pull or push
    3. [x] No post-Download git pull or push
    4. [x] DailyGitSave / local Server commit is not GitHub push

17. **Load keeps Parse**
    1. [x] desk Load already invokes Parse / graph-push after files land
    2. [x] git Load and plain-git Load reach Parse after a whole-tree pull; they do not stop at file transfer
    3. [ ] Required handoff is Unparsed → push onto the one Parse actor ([17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md)); no directory-reconcile worker
    4. [ ] Client Load on a file node pushes the selection onto that same stack ([06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md))
    5. [ ] Do not start a Parse Actor after pull; Parse home stays [[plan/parse-actor/project.md]]

18. **WebDAV remains**
    1. [x] WebDAV Upload / Download stay implemented
    2. [x] git Load/Save do not replace that transit
    3. [x] desk Load/Save keep using it

19. **Server Peer Actor does the round-trip**
    1. [x] Load or Save Command request reaches mailbox → actor pool → GitHub Peer Actor
    2. [x] Actor cares about Focus (Workspace / work tree) only
    3. [x] App does not invoke `git` ([[src/Shared/dotnet/GitRun.fs]] is host-side)
    4. [x] App stays thin (no git Actor host)

20. **One Actor shape through Server**
    1. [x] The same Peer Actor shape serves every device that maps through Server
    2. [x] No per-App clone protocol

21. **Host git credentials**
    1. [x] Peer Actor invokes `git` through WorkspaceGit / GitSave / GitRun
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
    2. [x] Load or Save Command → load/save command request → mailbox → actor pool → GitHub Peer Actor
    3. [x] Same wiring for Save as Load
    4. [x] Actor input is Focus (Workspace / work tree)

26. **Reject UX**
    1. [x] Conflict: error message names at least one file path
    2. [x] Other failures: matching short error
    3. [x] Text reflects what git reports, condensed
    4. [x] Same for Load and Save

27. **Persist stays independent of git Save**
    1. [x] Graph→file Persist already runs on its own path
    2. [x] git Save does not own or replace Persist
    3. [x] git Save composition is `git commit` of work-tree edits, then push
    4. [x] Do not merge Persist into git Save

28. **Per-Workspace work-tree gate queues without overlap** — **Revoked 2026-09-28.** [16 — Persist/git work-tree gate](issues/16-persist-git-work-tree-gate.md) exclusive gate is superseded by Unparsed/Unpersisted ([19 — Parsed/Unparsed and Persisted/Unpersisted](issues/19-file-newer-graph-newer.md)). The `[x]` lines below record what shipped on [12 — Run the Workspace git tracked-branch round-trip](issues/12-workspace-git-tracked-branch-round-trip.md) / [13 — Run git Load and Save through the Server Peer Actor](issues/13-peer-actor-runs-git-load-save.md); they are not current required architecture.
    1. [x] Graph→file Persist, git Load pull, and git Save commit acquire one exclusive gate for that Workspace work tree — **superseded**; not current required architecture
    2. [x] A second caller waits until the holder releases the gate, then continues — **superseded**
    3. [x] Contention does not reject as busy — **superseded**
    4. [x] The gate coordinates work-tree mutation without merging Persist into git Save — **superseded**; Persist stays independent of git Save ([10 — git Save is commit then push](issues/10-git-save-commit-then-push.md))

Shared segments:
1. [x] Command Load and Save doors ([[src/Shared/CommandEntry.fs]], [[src/Client/Commands.fs]])
2. [x] PathPick (plain Load/Save only)
3. [x] WorkspaceGit remote-exists + tracked branch + pull/push
4. [x] Peer Actor git Load / git Save (mailbox → actor pool; Focus = work tree; git Save = commit then push)
5. [x] Desk WebDAV / `loadOp` / desk Save ([[src/Shared/dotnet/WorkspaceFileSync.fs]], [[src/Client/UpdateWorkspaceLoad.fs]], [[src/Client/UpdateSave.fs]])
6. [x] Host GitRun (no Ambit credential store)
7. [x] Load reaches Parse / graph-push after files land. Required handoff is Unparsed → push onto the one Parse actor; directory-reconcile worker withdrawn. Fetch+Poll stays
8. [x] Reject UX: condensed git error; conflict names a path
9. [x] Per-Workspace exclusive work-tree gate shared by Persist, pull, and commit — **revoked 2026-09-28**; shipped on [12 — Run the Workspace git tracked-branch round-trip](issues/12-workspace-git-tracked-branch-round-trip.md) / [13 — Run git Load and Save through the Server Peer Actor](issues/13-peer-actor-runs-git-load-save.md); Unparsed/Unpersisted replace it

Narrowest shared test seam:
1. [x] PathPick: remote exists → git; else desk (pure; no git process)
2. [x] WorkspaceGit remote-exists + tracked-branch pull/push FF-only through GitRun on a temp work tree
3. [x] Peer Actor git Load / git Save invoke that WorkspaceGit interface; no Ambit credential argument
4. [x] Work-tree gate: a second Persist, pull, or commit waits for the holder and continues after release without overlap — **revoked 2026-09-28**; not current required architecture

## 2. Module map

1. **Command Load/Save**
   File: [[src/Shared/CommandEntry.fs]], [[src/Shared/LoadSaveCommand.fs]], and [[src/Client/Commands.fs]] (existing Load / Save). Spoken names stay Load and Save. Pre-picks are git Load / git Save / desk Load / desk Save on the load/save command request — not Git Remote / Git Pull / Git Push.
   1. State
      1. [x] CommandId Load and Save
      2. [x] Path pre-pick: Plain | Git | Desk on `LoadSaveCommandRequest` (not a new primary command name)
   2. Interface
      1. [x] Person Command Load and Save
      2. [x] Plain Load/Save ask PathPick
      3. [x] Explicit Git or Desk skips PathPick
      4. [x] No schedule, post-Persist, or post-Download start
      5. [x] Load or Save Command → load/save command request → mailbox → actor pool (not Run, not `?git`)
      6. [x] Load still reaches Parse / graph-push after files land. Required handoff is Unparsed → push onto Parse; directory-reconcile worker withdrawn
   3. Uses
      1. [x] PathPick
      2. [x] Peer Actor (git path) via actor pool
      3. [x] Desk Load/Save (desk path)
      4. [x] Parse actor stack after files land ([17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md)); Fetch+Poll stays

2. **PathPick**
   File: new [[src/Shared/PathPick.fs]] (pure choose). Remote-exists fact stays on WorkspaceGit.
   1. State
      1. [x] None durable
   2. Interface
      1. [x] `choose: remoteExists:bool -> Git | Desk` — Git when true, Desk when false
      2. [x] No allowlist, no Workspace name list, no Server branch map
   3. Uses
      1. [x] None (pure)

3. **WorkspaceGit**
   File: [[src/Server/WorkspaceGit.fs]] (extend). Git process stays [[src/Server/GitSave.fs]] / [[src/Shared/dotnet/GitRun.fs]].
   1. State
      1. [x] DataDir work tree `.git` (`isRepo`, `currentBranch`, `ensurePushConfig`)
      2. [x] Remote-exists and current upstream read from that work tree only
      3. [x] One exclusive work-tree gate per Workspace shared with Graph→file Persist — **revoked 2026-09-28**; Unparsed/Unpersisted replace it ([16 — Persist/git work-tree gate](issues/16-persist-git-work-tree-gate.md))
   2. Interface
      1. [x] `isRepo` / `currentBranch` / `ensurePushConfig` (FF-only receive)
      2. [x] `remoteExists: workspaceRoot -> Result<bool, string>` from `git remote` (any remote counts)
      3. [x] Tracked branch + upstream from that work tree (no Server map)
      4. [x] Pull the current tracked branch of the whole Workspace work tree (git Load); never file-level git
      5. [x] git Save: `git commit` work-tree edits (`GitSave.commitAll`), then push the same tracked branch of the whole Workspace work tree; reject conflicted or non-FF; never file-level git
      6. [x] Skip list is `.gitignore` on that work tree (person edits that file; no Ambit skip key)
      7. [x] Reject error: conflict names at least one file path; other failures a matching short git-condensed message
      8. [x] No checkout, switch, or older-commit move
      9. [x] No GitHub credential argument
      10. [x] Does not invoke Graph→file Persist
      11. [x] Persist file writes, git Load pull, and git Save commit acquire and release the Workspace work-tree gate — **superseded**; not current required interface
      12. [x] A second caller waits for the gate and continues after release instead of rejecting as busy — **superseded**
   3. Uses
      1. [x] GitSave.runGit / GitSave.commitAll / GitRun.gitExec (host `git`)
      2. [x] Host credential helper (git’s, not Ambit’s)

4. **Peer Actor**
   File: new [[src/Server/GithubTransportActor.fs]] (spoken name: Server Peer Actor; not a new Kind). Start door: [07 — Actor start door](issues/07-actor-start-door.md).
   1. State
      1. [x] Live Actor row only while a person-started git Load or git Save runs
      2. [x] No stored GitHub credential
   2. Interface
      1. [x] Pool invokes the Actor from a Load or Save command request on the mailbox (not Run, not `?git`)
      2. [x] Actor cares about Focus only (which Workspace / work tree)
      3. [x] Same wiring for Save as Load
      4. [x] git Load: pull whole Workspace work tree / tracked branch (any invocation node), then Unparsed on Workspace → push onto the one Parse actor ([17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md))
      5. [x] git Save: `git commit` work-tree edits, then push whole Workspace work tree / same tracked branch; FF-only reject; never file-level git; Persist stays independent
      6. [x] Reject UX: conflict names at least one file path; other failures a matching short git-condensed error; same for Load and Save
      7. [x] Same Actor shape for every device that maps through Server
      8. [x] Invokes WorkspaceGit; does not call git with an Ambit-supplied token
      9. [x] Acquires and releases the Workspace work-tree gate around git Load pull and git Save commit — **superseded**; exclusive gate revoked ([16 — Persist/git work-tree gate](issues/16-persist-git-work-tree-gate.md))
   3. Uses
      1. [x] CoreActorPool / ActorFn ([[src/Server/Core/CoreActorPool.fs]])
      2. [x] WorkspaceGit
      3. [x] No App git host
      4. [x] After git Load files land, Unparsed → push onto Parse ([17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md))

5. **Desk Load/Save**
   File: existing [[src/Client/UpdateWorkspaceLoad.fs]], [[src/Client/UpdateSave.fs]], [[src/Shared/dotnet/WorkspaceFileSync.fs]]. Shape unchanged for this Project.
   1. State
      1. [x] Desktop map + WebDAV sync ledger
   2. Interface
      1. [x] desk Load: App↔Server file transit (Upload / existing `loadOp`) then Unparsed → push onto Parse
      2. [x] desk Save: existing desk Save / Server local GitSave commit (not GitHub push)
      3. [x] WebDAV Upload / Download remain
   3. Uses
      1. [x] Desktop workspace-push / WebDAV
      2. [x] Server GitSave for local DataDir commit
      3. [x] Parse actor stack after files land; directory-reconcile worker withdrawn; Fetch+Poll stays

6. **App**
   File: Desktop / Browser hosts (existing). Stays thin.
   1. State
      1. [x] No git remote map on the App
   2. Interface
      1. [x] Does not invoke GitRun for GitHub pull or push
      2. [x] Does not host the Peer Actor
   3. Uses
      1. [x] Command Load/Save
      2. [x] Desk WebDAV when PathPick or pre-pick is desk

## 3. Seams

1. **PathPick choose**
   Interface on **PathPick**. Tests pass `remoteExists` true/false. No git process.
   1. [x] Pure `choose` Git vs Desk

2. **WorkspaceGit git facts**
   Interface on **WorkspaceGit**. Tests cross GitRun on a temp work tree (remote present / absent; FF push accept / reject; `.gitignore` skip).
   1. [x] `remoteExists` + tracked branch
   2. [x] pull / push FF-only
   3. [x] git Save: commit work-tree edits, then push
   4. [x] skip list is `.gitignore`
   5. [x] reject text: conflict names a path; other failures short git-condensed
   6. [x] no credential parameter
   7. [x] no Persist call
   8. [x] one gate per Workspace; a second Persist, pull, or commit waits for the holder and continues after release — **revoked 2026-09-28**; not current required seam

3. **Peer Actor git Load/Save**
   Interface on **Peer Actor**. Tests stub WorkspaceGit. Start door is mailbox → actor pool from Load/Save ([07 — Actor start door](issues/07-actor-start-door.md)).
   1. [x] Load/Save command request → pool invokes Actor; Focus names the work tree
   2. [x] git Load → pull
   3. [x] git Save → commit work-tree edits, then push
   4. [x] Persist is not invoked from git Save
   5. [x] reject UX same for Load and Save
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

1. **workspace-git command surface** — Git Remote / Git Pull / Git Push on WorkspaceGit, with non-FF accept of non-overlapping edits ([[plan/workspace-git/spec.md]]). Rejected: [02 — Actor command surface](issues/02-actor-command-surface.md) locks Load/Save plus git* / desk* pre-picks; Destination is FF-only.
2. **App-hosted git** — App clones or pushes GitHub. Rejected: Destination keeps the App thin; Server Peer Actor is the git host.
3. **Server branch map** — Ambit stores which Workspace tracks which branch. Rejected: [01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md); config is git on that work tree.
4. **Ambit credential store** — appsettings / user-secrets / Graph / DataDir hold a GitHub token. Rejected: [04 — Credential storage on Server](issues/04-credential-storage-on-server.md); git loads host credentials.
5. **Automatic git** — schedule, post-Persist, or post-Download pull/push. Rejected: [03 — When pull and push fire](issues/03-when-pull-and-push-fire.md); person Commands only.
6. **File-transfer-only Load / start a Parse Actor after pull** — git Load stops at pull and starts a new Parse Actor, or leaves Parse to a later redesign. Rejected: nobody starts an Actor after pull; Core pushes the Workspace onto the one long-lived Parse actor ([17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md)). Parse home stays [[plan/parse-actor/project.md]].
7. **File-level git** — pull or push a file or subtree because Load/Save was invoked on a subnode. Rejected: [05 — Git Load/Save are Workspace-scoped](issues/05-git-load-save-workspace-scoped.md).
8. **Run / `?git` entrée** — Start the Peer Actor from Run or a `?git` Command. Rejected: [07 — Actor start door](issues/07-actor-start-door.md); door is Load or Save via mailbox → actor pool.
9. **Ambit skip key** — A special Ambit config key for `.amb` skip. Rejected: [09 — Skip list is .gitignore](issues/09-gitignore-skip-list.md); skip list is `.gitignore`.
10. **Merge Persist into git Save** — Make git Save own or replace Graph→file Persist. Rejected: [10 — git Save is commit then push](issues/10-git-save-commit-then-push.md); Persist already happens independently; composition is `git commit` then push.

## 5. Unsettled

None.
