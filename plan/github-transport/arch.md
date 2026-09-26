# github-transport architecture

Spec: [[spec.md]]
Updated: 2026-09-26
Sequence: module-build

Sources: [spec.md](spec.md) User Stories 1–26; [map.md](map.md) Destination and Decisions so far 1–14 ([01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md)–[05 — Git Load/Save are Workspace-scoped](issues/05-git-load-save-workspace-scoped.md), [07 — Actor start door](issues/07-actor-start-door.md)–[09 — Skip list is .gitignore](issues/09-gitignore-skip-list.md)). Later: [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md) (`needs-info`; not v1). Checklist: `[x]` already true of the codebase shape; `[ ]` still to build. Does not invent product behavior beyond those locks. Existing Ambit smart-HTTP helper [[src/Shared/WorkspaceGitRemote.fs]] (`RemoteName` `ambit`, `/ambit/git/{label}.git`) is not the GitHub remote. Sequence is `module-build`: the stories share a few modules (PathPick, WorkspaceGit git facts, Peer Actor, existing desk WebDAV). This is not expand-contract (no wide rename). Tracer-cut would mint one ticket per story and repeat the same hops.

## 1. Story paths

1. **Workspace is a git work tree**
   1. [x] Workspace DataDir work tree is a git work tree (`WorkspaceGit.isRepo`)
   2. [x] No special label required for git home
   3. [ ] PathPick and Peer Actor treat every such Workspace as a candidate (no extra Kind)

2. **Every Workspace may connect**
   1. [ ] PathPick runs for any Workspace work tree
   2. [ ] No allowlist and no special label gate

3. **Server-git when a remote exists**
   1. [ ] WorkspaceGit reports whether a remote exists on that work tree
   2. [ ] PathPick chooses git when a remote exists, else desk

4. **Config in git**
   1. [x] `WorkspaceGit.currentBranch` reads the attached branch from that work tree
   2. [ ] WorkspaceGit reads `git remote` and current upstream on that work tree
   3. [ ] No Server branch map module

5. **Same tracked branch**
   1. [ ] git Load pulls the current tracked branch of the whole Workspace work tree
   2. [ ] git Save pushes the same tracked branch of the whole Workspace work tree
   3. [ ] Neither hop is file-level git
   4. [ ] Neither hop checkouts, switches, or moves to an older commit

6. **Fast-forward only**
   1. [x] `WorkspaceGit.ensurePushConfig` sets `receive.denyNonFastForwards`
   2. [ ] git Save rejects a conflicted or non-FF push
   3. [ ] Conflict error names at least one file path
   4. [ ] Other failures use a matching short error that reflects git, condensed
   5. [ ] Reject does not accept non-overlapping diverge (not [[plan/workspace-git/project.md]])

7. **Remotes accept push**
   1. [x] Operator sets `git remote` on the work tree so git works
   2. [ ] Peer Actor pushes that remote; it does not invent a PR-only path

8. **Load**
   1. [x] Person Command Load (`CommandEntry` Load; [[src/Client/Commands.fs]] `loadOp`)
   2. [ ] Load command request → mailbox → actor pool (not Run, not `?git`)
   3. [ ] Plain Load asks PathPick
   4. [ ] PathPick git → pool invokes GitHub Peer Actor (Focus = Workspace / work tree); PathPick desk → desk Load
   5. [x] After files land, Load invokes Parse / graph-push as today’s `loadOp` already does (`parseFileOp` / directory reconcile / Fetch+Poll)

9. **Save**
   1. [x] Person Command Save (`CommandEntry` Save; [[src/Client/UpdateSave.fs]] `saveOp`)
   2. [ ] Save command request uses the same mailbox → actor-pool door as Load
   3. [ ] Plain Save asks PathPick
   4. [ ] PathPick git → pool invokes GitHub Peer Actor (Focus = Workspace / work tree); PathPick desk → desk Save

10. **git Load**
    1. [ ] Person names git Load (explicit pre-pick)
    2. [ ] PathPick is skipped
    3. [ ] Mailbox → actor pool invokes Peer Actor; Actor uses Focus as the work tree
    4. [ ] Peer Actor pulls the whole Workspace work tree / tracked branch (invocation node does not narrow git)
    5. [ ] Reject uses condensed git error (conflict names at least one path)
    6. [x] After files land, Parse / graph-push on the selection where appropriate, as today’s desk Load (`parseFileOp` / directory reconcile / Fetch+Poll)
    7. [ ] Selection-parse nuance after whole-tree pull is later ([06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md)); do not expand in v1

11. **git Save**
    1. [ ] Person names git Save (explicit pre-pick)
    2. [ ] PathPick is skipped
    3. [ ] Same mailbox → actor-pool door as Load; Actor uses Focus as the work tree
    4. [ ] Peer Actor pushes the whole Workspace work tree / same tracked branch (invocation node does not narrow git)
    5. [ ] Reject uses condensed git error (conflict names at least one path)

12. **desk Load**
    1. [ ] Person names desk Load (explicit pre-pick)
    2. [ ] PathPick is skipped
    3. [x] Desk file path stays WebDAV Upload / existing `loadOp` desk transit ([[doc/current/workspace-file-sync.md]])
    4. [x] Existing Load → Parse coupling stays (`parseFileOp` / directory reconcile / Fetch+Poll)

13. **desk Save**
    1. [ ] Person names desk Save (explicit pre-pick)
    2. [ ] PathPick is skipped
    3. [x] Desk file path stays the existing desk Save / WebDAV transit (not a GitHub push)

14. **Plain Load prefers git**
    1. [ ] Plain Load with a remote exists → same hops as **git Load**
    2. [ ] Plain Load with no remote → same hops as **desk Load**

15. **Plain Save prefers git**
    1. [ ] Plain Save with a remote exists → same hops as **git Save**
    2. [ ] Plain Save with no remote → same hops as **desk Save**

16. **No automatic pull or push**
    1. [ ] No schedule starts git pull or push
    2. [ ] No post-Persist git pull or push
    3. [ ] No post-Download git pull or push
    4. [x] DailyGitSave / local Server commit is not GitHub push

17. **Load keeps Parse**
    1. [x] desk Load already invokes Parse / graph-push after files land
    2. [ ] git Load and plain-git Load use that same Parse / graph-push coupling after a whole-tree pull; they do not stop at file transfer
    3. [ ] Parse runs on the selection where appropriate (selection-scoped parse after whole-tree pull)
    4. [ ] Do not expand the selection-parse nuance in v1 ([06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md))
    5. [ ] Do not redesign Load around a future autonomous Parse ([[plan/parse-actor/project.md]] stays elsewhere)

18. **WebDAV remains**
    1. [x] WebDAV Upload / Download stay implemented
    2. [ ] git Load/Save do not replace that transit
    3. [ ] desk Load/Save keep using it

19. **Server Peer Actor does the round-trip**
    1. [ ] Load or Save Command request reaches mailbox → actor pool → GitHub Peer Actor
    2. [ ] Actor cares about Focus (Workspace / work tree) only
    3. [x] App does not invoke `git` ([[src/Shared/dotnet/GitRun.fs]] is host-side)
    4. [ ] App stays thin (no git Actor host)

20. **One Actor shape through Server**
    1. [ ] The same Peer Actor shape serves every device that maps through Server
    2. [ ] No per-App clone protocol

21. **Host git credentials**
    1. [ ] Peer Actor invokes `git` through WorkspaceGit / GitSave / GitRun
    2. [x] GitRun starts the host `git` process
    3. [ ] git loads credentials (credential helper / host setup)
    4. [ ] No GitHub credential in appsettings, user-secrets, Graph, or DataDir

22. **Skip list is `.gitignore`**
    1. [ ] Skip on the GitHub remote is whatever `.gitignore` already says
    2. [ ] No Ambit skip key; the person edits `.gitignore`
    3. [ ] Skip is not a hard Ambit default for `.amb`

23. **Backup when `.amb` is excluded**
    1. [x] WebDAV Upload / Download still transfer Directory File `.amb`
    2. [x] Server DataDir / Server git / DailyGitSave still track those notes
    3. [ ] Exclude-from-repo does not mean desk-local-only

24. **Workspace-scoped git**
    1. [ ] git Load/Save from Workspace root or a subnode use the same whole-tree pull/push
    2. [ ] No file-level git pathspec
    3. [ ] Tracked branch is the Workspace work tree’s current branch / upstream

25. **Actor start door**
    1. [ ] Not Run and not a `?git` entrée
    2. [ ] Load or Save Command → load/save command request → mailbox → actor pool → GitHub Peer Actor
    3. [ ] Same wiring for Save as Load
    4. [ ] Actor input is Focus (Workspace / work tree)

26. **Reject UX**
    1. [ ] Conflict: error message names at least one file path
    2. [ ] Other failures: matching short error
    3. [ ] Text reflects what git reports, condensed
    4. [ ] Same for Load and Save

Shared segments:
1. [x] Command Load and Save doors ([[src/Shared/CommandEntry.fs]], [[src/Client/Commands.fs]])
2. [ ] PathPick (plain Load/Save only)
3. [ ] WorkspaceGit remote-exists + tracked branch + pull/push
4. [ ] Peer Actor git Load / git Save (mailbox → actor pool; Focus = work tree)
5. [x] Desk WebDAV / `loadOp` / desk Save ([[src/Shared/dotnet/WorkspaceFileSync.fs]], [[src/Client/UpdateWorkspaceLoad.fs]], [[src/Client/UpdateSave.fs]])
6. [x] Host GitRun (no Ambit credential store)
7. [x] Existing Load → Parse / graph-push (`parseFileOp` / directory reconcile / Fetch+Poll)
8. [ ] Reject UX: condensed git error; conflict names a path

Narrowest shared test seam:
1. [ ] PathPick: remote exists → git; else desk (pure; no git process)
2. [ ] WorkspaceGit remote-exists + tracked-branch pull/push FF-only through GitRun on a temp work tree
3. [ ] Peer Actor git Load / git Save invoke that WorkspaceGit interface; no Ambit credential argument

## 2. Module map

1. **Command Load/Save**
   File: [[src/Shared/CommandEntry.fs]] and [[src/Client/Commands.fs]] (existing Load / Save). Spoken names stay Load and Save. Pre-picks are git Load / git Save / desk Load / desk Save — not Git Remote / Git Pull / Git Push.
   1. State
      1. [x] CommandId Load and Save
      2. [ ] Path pre-pick: Plain | Git | Desk (encoding unsettled only as a field; not a new primary command name)
   2. Interface
      1. [x] Person Command Load and Save
      2. [ ] Plain Load/Save ask PathPick
      3. [ ] Explicit Git or Desk skips PathPick
      4. [ ] No schedule, post-Persist, or post-Download start
      5. [ ] Load or Save Command → load/save command request → mailbox → actor pool (not Run, not `?git`)
      6. [x] Load still invokes Parse / graph-push as today’s desk Load does
   3. Uses
      1. [ ] PathPick
      2. [ ] Peer Actor (git path) via actor pool
      3. [x] Desk Load/Save (desk path)
      4. [x] Existing Parse hops (`parseFileOp` / directory reconcile / Fetch+Poll)

2. **PathPick**
   File: new [[src/Shared/PathPick.fs]] (pure choose). Remote-exists fact stays on WorkspaceGit.
   1. State
      1. [ ] None durable
   2. Interface
      1. [ ] `choose: remoteExists:bool -> Git | Desk` — Git when true, Desk when false
      2. [ ] No allowlist, no Workspace name list, no Server branch map
   3. Uses
      1. [ ] None (pure)

3. **WorkspaceGit**
   File: [[src/Server/WorkspaceGit.fs]] (extend). Git process stays [[src/Server/GitSave.fs]] / [[src/Shared/dotnet/GitRun.fs]].
   1. State
      1. [x] DataDir work tree `.git` (`isRepo`, `currentBranch`, `ensurePushConfig`)
      2. [ ] Remote-exists and current upstream read from that work tree only
   2. Interface
      1. [x] `isRepo` / `currentBranch` / `ensurePushConfig` (FF-only receive)
      2. [ ] `remoteExists: workspaceRoot -> Result<bool, string>` from `git remote` (any remote counts)
      3. [ ] Tracked branch + upstream from that work tree (no Server map)
      4. [ ] Pull the current tracked branch of the whole Workspace work tree (git Load); never file-level git
      5. [ ] Push the same tracked branch of the whole Workspace work tree (git Save); reject conflicted or non-FF; never file-level git
      6. [ ] Skip list is `.gitignore` on that work tree (person edits that file; no Ambit skip key)
      7. [ ] Reject error: conflict names at least one file path; other failures a matching short git-condensed message
      8. [ ] No checkout, switch, or older-commit move
      9. [ ] No GitHub credential argument
   3. Uses
      1. [x] GitSave.runGit / GitRun.gitExec (host `git`)
      2. [ ] Host credential helper (git’s, not Ambit’s)

4. **Peer Actor**
   File: new [[src/Server/GithubTransportActor.fs]] (spoken name: Server Peer Actor; not a new Kind). Start door: [07 — Actor start door](issues/07-actor-start-door.md).
   1. State
      1. [ ] Live Actor row only while a person-started git Load or git Save runs
      2. [ ] No stored GitHub credential
   2. Interface
      1. [ ] Pool invokes the Actor from a Load or Save command request on the mailbox (not Run, not `?git`)
      2. [ ] Actor cares about Focus only (which Workspace / work tree)
      3. [ ] Same wiring for Save as Load
      4. [ ] git Load: pull whole Workspace work tree / tracked branch (any invocation node), then today’s Load → Parse / graph-push on the selection
      5. [ ] git Save: push whole Workspace work tree / same tracked branch; FF-only reject; never file-level git
      6. [ ] Reject UX: conflict names at least one file path; other failures a matching short git-condensed error; same for Load and Save
      7. [ ] Same Actor shape for every device that maps through Server
      8. [ ] Invokes WorkspaceGit; does not call git with an Ambit-supplied token
   3. Uses
      1. [x] CoreActorPool / ActorFn ([[src/Server/Core/CoreActorPool.fs]])
      2. [ ] WorkspaceGit
      3. [x] No App git host
      4. [x] Existing Load → Parse / graph-push after git Load files land

5. **Desk Load/Save**
   File: existing [[src/Client/UpdateWorkspaceLoad.fs]], [[src/Client/UpdateSave.fs]], [[src/Shared/dotnet/WorkspaceFileSync.fs]]. Shape unchanged for this Project.
   1. State
      1. [x] Desktop map + WebDAV sync ledger
   2. Interface
      1. [x] desk Load: App↔Server file transit (Upload / existing `loadOp`) then Parse / graph-push
      2. [x] desk Save: existing desk Save / Server local GitSave commit (not GitHub push)
      3. [x] WebDAV Upload / Download remain
   3. Uses
      1. [x] Desktop workspace-push / WebDAV
      2. [x] Server GitSave for local DataDir commit
      3. [x] Existing Parse hops (`parseFileOp` / directory reconcile / Fetch+Poll)

6. **App**
   File: Desktop / Browser hosts (existing). Stays thin.
   1. State
      1. [x] No git remote map on the App
   2. Interface
      1. [x] Does not invoke GitRun for GitHub pull or push
      2. [ ] Does not host the Peer Actor
   3. Uses
      1. [x] Command Load/Save
      2. [x] Desk WebDAV when PathPick or pre-pick is desk

## 3. Seams

1. **PathPick choose**
   Interface on **PathPick**. Tests pass `remoteExists` true/false. No git process.
   1. [ ] Pure `choose` Git vs Desk

2. **WorkspaceGit git facts**
   Interface on **WorkspaceGit**. Tests cross GitRun on a temp work tree (remote present / absent; FF push accept / reject; `.gitignore` skip).
   1. [ ] `remoteExists` + tracked branch
   2. [ ] pull / push FF-only
   3. [ ] skip list is `.gitignore`
   4. [ ] reject text: conflict names a path; other failures short git-condensed
   5. [ ] no credential parameter

3. **Peer Actor git Load/Save**
   Interface on **Peer Actor**. Tests stub WorkspaceGit. Start door is mailbox → actor pool from Load/Save ([07 — Actor start door](issues/07-actor-start-door.md)).
   1. [ ] Load/Save command request → pool invokes Actor; Focus names the work tree
   2. [ ] git Load → pull
   3. [ ] git Save → push
   4. [ ] reject UX same for Load and Save
   5. [ ] no Ambit credential store

4. **Desk WebDAV**
   Interface on **Desk Load/Save**. Existing seam. This Project does not redesign it.
   1. [x] Upload / Download / `loadOp` desk transit

5. **Host git credentials**
   Interface on **WorkspaceGit** (invoke git only). Seam is the host `git` process, not an Ambit secret module.
   1. [x] GitRun starts `git`
   2. [ ] No appsettings / user-secrets / Graph / DataDir GitHub credential module

## 4. Alternative considered

1. **workspace-git command surface** — Git Remote / Git Pull / Git Push on WorkspaceGit, with non-FF accept of non-overlapping edits ([[plan/workspace-git/spec.md]]). Rejected: [02 — Actor command surface](issues/02-actor-command-surface.md) locks Load/Save plus git* / desk* pre-picks; Destination is FF-only.
2. **App-hosted git** — App clones or pushes GitHub. Rejected: Destination keeps the App thin; Server Peer Actor is the git host.
3. **Server branch map** — Ambit stores which Workspace tracks which branch. Rejected: [01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md); config is git on that work tree.
4. **Ambit credential store** — appsettings / user-secrets / Graph / DataDir hold a GitHub token. Rejected: [04 — Credential storage on Server](issues/04-credential-storage-on-server.md); git loads host credentials.
5. **Automatic git** — schedule, post-Persist, or post-Download pull/push. Rejected: [03 — When pull and push fire](issues/03-when-pull-and-push-fire.md); person Commands only.
6. **File-transfer-only Load / autonomous Parse** — git Load stops at pull and leaves Parse to a future autonomous actor. Rejected: independent Parse is not in place; keep today’s Load → Parse coupling until [[plan/parse-actor/project.md]] lands elsewhere.
7. **File-level git** — pull or push a file or subtree because Load/Save was invoked on a subnode. Rejected: [05 — Git Load/Save are Workspace-scoped](issues/05-git-load-save-workspace-scoped.md).
8. **Run / `?git` entrée** — Start the Peer Actor from Run or a `?git` Command. Rejected: [07 — Actor start door](issues/07-actor-start-door.md); door is Load or Save via mailbox → actor pool.
9. **Ambit skip key** — A special Ambit config key for `.amb` skip. Rejected: [09 — Skip list is .gitignore](issues/09-gitignore-skip-list.md); skip list is `.gitignore`.

## 5. Unsettled

1. **git Save vs existing Persist / GitSave** — How git Save meets [[src/Server/GitSave.fs]] local commit and Persist. Recorded on [map.md](map.md) Not yet specified. Do not invent that composition here.
