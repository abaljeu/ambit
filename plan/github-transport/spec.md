# github-transport

Updated: 2026-09-29

Sources: [map.md](map.md) Destination and Decisions so far; [01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md)–[10 — git Save is commit then push](issues/10-git-save-commit-then-push.md); [project.md](project.md). Core handoff after files land: [[plan/core-refinement/project.md]]. Chapter: [Send to and from GitHub](../roadmap/epics/chapters/send-to-and-from-github.md). This spec synthesizes transport locks. It does not invent new product behavior.

## 1. Problem Statement

1. **GitHub is off Load and Save** — A person whose Workspace DataDir work tree is already a git work tree cannot send files to GitHub or bring files from GitHub through the same Load and Save Commands they already use.
2. **Desk path is Ambit↔Ambit only** — WebDAV Upload and Download move files between the App and Server. They do not talk to the GitHub remote.
3. **No Server-git when a remote exists** — When that work tree has a remote, the person has no Server Actor that pulls and pushes the tracked branch on that remote.
4. **Config and credentials already live in git** — The operator already sets `git remote` and host credentials so git works. The person does not want a second Server branch map or GitHub secrets stored in Ambit.

## 2. Solution

1. **Server Peer Actor** — A Server-side Peer Actor does pull and push (round-trip v1). Actor is the glossary word. Peer Actor here means that Server Actor, not a new Kind. The App stays thin. The App is not the git Actor host. The same Actor shape serves every device that maps through Server.
2. **Workspace is the git home** — Prefer Workspace, not “label.” Every Workspace’s DataDir work tree is a git work tree. Any Workspace connects (all have git). Server-git applies when a remote exists — no allowlist, no special label. Else desk.
3. **Config is git on that work tree** — Config is `git remote` plus the current branch / upstream. There is no separate Server branch map in v1. Pull and push use the same tracked branch.
4. **Fast-forward only** — Remotes accept push (not PR-only). A conflicted or non-FF push is rejected. Server `ensurePushConfig` sets `receive.denyNonFastForwards`. Conflict reject names at least one file path. Other failures use a matching short error that reflects what git reports, condensed. Same for Load and Save.
5. **Load and Save** — Person-facing Commands are Load and Save (same Ambit command names). Explicit secondary pre-picks are git Load / git Save and desk Load / desk Save. Plain Load/Save is git* when a remote exists, else desk*. Start door is not Run and not a `?git` entrée: Load or Save Command → load/save command request → mailbox → actor pool → GitHub Peer Actor. The Actor cares about Focus (Workspace / work tree) only. Same wiring for Save as Load.
6. **Person-started only** — No automatic pull or push in v1 (no schedule, post-Persist, or post-Download git). Person Load/Save and explicit git*/desk* only. When a remote exists, plain Load/Save prefer git first.
7. **Thin handoff to Core** — After files land, this Project locks the workspace, receives the files, and informs the revised Core of changes. Core works through the changes ([[plan/core-refinement/project.md]]). The WebDAV Upload/Download path remains. When a remote exists, plain Load/Save still prefer git first. Parse Actor home stays [[plan/parse-thread/project.md]].
8. **Host git credentials** — Ambit does not store GitHub credentials in appsettings, user-secrets, Graph, or DataDir. The Actor invokes `git`. git loads credentials (credential helper / host setup). On Server that is the host’s git.
9. **Skip list is `.gitignore`** — No special Ambit config key. Skip on the GitHub remote is whatever `.gitignore` already says; the person edits that file. When `.amb` or other notes are listed there, they are skipped on the remote. When excluded, offsite backup of those notes is Ambit Server DataDir (WebDAV Upload/Download + Server git / daily save), not the mapped repo remote.
10. **Git Load/Save are Workspace-scoped** — Wherever Load/Save is invoked from (Workspace root or a subnode), git pull/push always operates on the whole Workspace work tree / tracked branch — never file-level git. After files land, inform Core ([[plan/core-refinement/project.md]]).
11. **git Save is commit then push** — git Save is `git commit` of the work-tree edits, then push. Graph→file Persist already happens independently. git Save does not own or replace Persist. Do not merge Persist into git Save.

## 3. User Stories

1. **Workspace is a git work tree** — As a person, I want every Workspace’s DataDir work tree to be a git work tree, so that GitHub transport has a git home without a special label.
2. **Every Workspace may connect** — As a person, I want any Workspace to use Server-git when a remote exists, so that there is no allowlist.
3. **Server-git when a remote exists** — As a person, I want the Server-git path when a remote exists and the desk path when it does not, so that one check chooses the path.
4. **Config in git** — As an operator, I want config to live in git on that work tree (`git remote` plus current branch / upstream), so that Ambit does not keep a Server branch map in v1.
5. **Same tracked branch** — As a person, I want pull and push to use the same tracked branch of the whole Workspace work tree, so that round-trip v1 does not switch branches or go file-level.
6. **Fast-forward only** — As a person, I want a conflicted or non-FF push rejected with an error that names at least one file path, so that I see what git refused.
7. **Remotes accept push** — As an operator, I want remotes that accept push (not PR-only), so that the Server Actor can push the tracked branch.
8. **Load** — As a person, I want Command Load to send a load command request through the mailbox to the actor pool, so that the GitHub Peer Actor runs when PathPick chooses git, then informs Core after files land.
9. **Save** — As a person, I want Command Save to use the same mailbox → actor-pool wiring as Load, so that the GitHub Peer Actor runs when PathPick chooses git.
10. **git Load** — As a person, I want an explicit git Load pre-pick, so that I pull the whole Workspace work tree / tracked branch (even from a subnode), then inform Core that files changed.
11. **git Save** — As a person, I want an explicit git Save pre-pick, so that I `git commit` the work-tree edits and then push the whole Workspace work tree / tracked branch (even from a subnode), never file-level git.
12. **desk Load** — As a person, I want an explicit desk Load pre-pick, so that I take the desk file path and then inform Core after files land.
13. **desk Save** — As a person, I want an explicit desk Save pre-pick, so that I take the desk file path when I choose desk.
14. **Plain Load prefers git** — As a person, I want plain Load to be git Load when a remote exists and desk Load otherwise, so that I do not pick a path when the default is enough, and both still inform Core after files land.
15. **Plain Save prefers git** — As a person, I want plain Save to be git Save when a remote exists and desk Save otherwise, so that I do not pick a path when the default is enough.
16. **No automatic pull or push** — As a person, I want pull and push only when I run Load or Save (or an explicit git*/desk* pre-pick), so that no schedule, post-Persist, or post-Download git starts a round-trip.
17. **Inform Core after files land** — As a person, I want Load forms to inform Core after files land, so Core can work through the changes ([[plan/core-refinement/project.md]]).
18. **WebDAV remains** — As a person, I want WebDAV Upload and Download to remain, so that Ambit↔Ambit file transit stays available beside Server-git.
19. **Server Peer Actor does the round-trip** — As a person, I want a Server-side Peer Actor, started from Load or Save (not Run), to pull and push using Focus as the Workspace / work tree, so that the App stays thin and is not the git Actor host.
20. **One Actor shape through Server** — As a person, I want the same Actor shape on every device that maps through Server, so that this is not a per-App clone protocol.
21. **Host git credentials** — As an operator, I want the Actor to invoke `git` and git to load host credentials, so that Ambit does not store GitHub credentials in appsettings, user-secrets, Graph, or DataDir.
22. **Skip list is `.gitignore`** — As a person, I want the GitHub remote to skip whatever `.gitignore` already says, so that I edit that file and Ambit does not invent a skip key.
23. **Backup when `.amb` is excluded** — As a person, I want excluded `.amb` notes to stay on Server DataDir through WebDAV Upload/Download and Server git / daily save, so that exclude-from-repo is not desk-local-only.
24. **Workspace-scoped git** — As a person, I want git Load and git Save to always pull or push the whole Workspace work tree / tracked branch, so that invoking Load/Save from a subnode never becomes file-level git.
25. **Actor start door** — As a person, I want Load or Save (not Run, not `?git`) to start the GitHub Peer Actor through mailbox → actor pool, so that Focus names the work tree and Save uses the same door as Load.
26. **Reject UX** — As a person, I want a conflict reject to name at least one file path and other failures to show a matching short git-condensed error, so that Load and Save rejects reflect what git reported.
27. **Persist stays independent of git Save** — As a person, I want Graph→file Persist to keep running on its own path, so that git Save only commits work-tree edits and then pushes, and does not own or replace Persist.

## 4. Out of Scope

1. **Checkout / switch / older commits** — This spec does not checkout, switch branch, or move to older commits. That work is future, not github-transport v1.
2. **Core revision after files land** — This spec does not define how Core works through changes. That home is [[plan/core-refinement/project.md]].
3. **Parse actor implementation** — This spec does not implement the Parse actor. That home stays [[plan/parse-thread/project.md]].
4. **WebDAV redesign** — This spec does not replace Ambit↔Ambit WebDAV Upload/Download. Redesign stays on [[plan/transport-layer/project.md]] / [[plan/auto-download-persisted-files/project.md]]. Implemented path: [[doc/current/workspace-file-sync.md]].
5. **This repo’s git procedure** — This spec is not [[plan/git-protocol/project.md]]. Committed Decision [[doc/Decisions/0002-git-protocol.md]] stays that Desktop procedure.
6. **Want-driven Graph→Browser** — This spec does not change residency. That work stays [[plan/browser-residency/project.md]].
7. **workspace-git command surface** — This spec does not inherit [[plan/workspace-git/project.md]] Git Remote / Git Pull / Git Push as the primary surface, and does not inherit that spec’s non-FF accept.
8. **Run / `?git` entrée** — This spec does not start the GitHub Peer Actor from Run or a `?git` Command. The door is Load or Save ([07 — Actor start door](issues/07-actor-start-door.md)).
9. **Merge Persist into git Save** — This spec does not merge Graph→file Persist into git Save. Persist stays independent. git Save is `git commit` of work-tree edits, then push ([10 — git Save is commit then push](issues/10-git-save-commit-then-push.md)).
10. **File-level git** — This spec does not pull or push a file or subtree as a git path. Git Load/Save are Workspace-scoped ([05 — Git Load/Save are Workspace-scoped](issues/05-git-load-save-workspace-scoped.md)).

## 5. Further Notes

1. **Spoken names** — Prefer Workspace, not “label.” Use Actor, not Agent, for the Server Peer Actor ([[CONTEXT.md]]). Load and Save are the existing Command names.
2. **Chapter home** — Product objective only: [Send to and from GitHub](../roadmap/epics/chapters/send-to-and-from-github.md) on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. Technical locks for transit live here; Core revision lives on [[plan/core-refinement/project.md]].
3. **Cite** — [[src/Server/WorkspaceGit.fs]] (`ensurePushConfig` / `receive.denyNonFastForwards`). Implemented WebDAV Upload / Download: [[doc/current/workspace-file-sync.md]].
4. **Architecture** — [[arch.md]]. Sequence `module-build`.
5. **Start door, reject UX, `.gitignore` (2026-09-26)** — Alan, Github Sync room: mailbox → actor pool from Load/Save; condensed git errors (conflict names a path); skip list is `.gitignore` with no Ambit key.
6. **git Save is commit then push (2026-09-26)** — Alan, Github Sync: git Save is `git commit` of work-tree edits, then push. Persist stays independent. Do not merge Persist into git Save.
7. **Split to core-refinement (2026-09-29)** — Alan: after [[plan/core-refinement/project.md]] completes, this Project only locks the workspace, receives files, and informs Core. Core works through the changes.
