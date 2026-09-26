# github-transport

Updated: 2026-09-26

Sources: [map.md](map.md) Destination and Decisions so far 1–10; [01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md); [02 — Actor command surface](issues/02-actor-command-surface.md); [03 — When pull and push fire](issues/03-when-pull-and-push-fire.md); [04 — Credential storage on Server](issues/04-credential-storage-on-server.md); [project.md](project.md). Chapter: [Send to and from GitHub](../roadmap/epics/chapters/send-to-and-from-github.md). This spec synthesizes those locks. It does not invent new product behavior.

## 1. Problem Statement

1. **GitHub is off Load and Save** — A person whose Workspace DataDir work tree is already a git work tree cannot send files to GitHub or bring files from GitHub through the same Load and Save Commands they already use.
2. **Desk path is Ambit↔Ambit only** — WebDAV Upload and Download move files between the App and Server. They do not talk to the GitHub remote.
3. **No Server-git when a remote exists** — When that work tree has a remote, the person has no Server Actor that pulls and pushes the tracked branch on that remote.
4. **Config and credentials already live in git** — The operator already sets `git remote` and host credentials so git works. The person does not want a second Server branch map or GitHub secrets stored in Ambit.

## 2. Solution

1. **Server Peer Actor** — A Server-side Peer Actor does pull and push (round-trip v1). Actor is the glossary word. Peer Actor here means that Server Actor, not a new Kind. The App stays thin. The App is not the git Actor host. The same Actor shape serves every device that maps through Server.
2. **Workspace is the git home** — Prefer Workspace, not “label.” Every Workspace’s DataDir work tree is a git work tree. Any Workspace connects (all have git). Server-git applies when a remote exists — no allowlist, no special label. Else desk.
3. **Config is git on that work tree** — Config is `git remote` plus the current branch / upstream. There is no separate Server branch map in v1. Pull and push use the same tracked branch.
4. **Fast-forward only** — Remotes accept push (not PR-only). A conflicted or non-FF push is rejected. Server `ensurePushConfig` sets `receive.denyNonFastForwards`.
5. **Load and Save** — Person-facing Commands are Load and Save (same Ambit command names). Explicit secondary pre-picks are git Load / git Save and desk Load / desk Save. Plain Load/Save is git* when a remote exists, else desk*.
6. **Person-started only** — No automatic pull or push in v1 (no schedule, post-Persist, or post-Download git). Person Load/Save and explicit git*/desk* only. When a remote exists, plain Load/Save prefer git first.
7. **Load transfers files only** — All three Load forms transfer files only on this spec. The WebDAV Upload/Download path remains. Parse after files land stays on [[plan/parse-actor/project.md]]. Parse autonomy and Graph sync autonomy stay independent. This spec does not adapt Load for parse or graph.
8. **Host git credentials** — Ambit does not store GitHub credentials in appsettings, user-secrets, Graph, or DataDir. The Actor invokes `git`. git loads credentials (credential helper / host setup). On Server that is the host’s git.
9. **Optional `.amb` skip** — Directory File / Ambit note paths named `.amb` may be skipped on the GitHub remote when repo configuration says so. They are not a hard-skip default and not the same class as `.git/`. When excluded, offsite backup of those notes is Ambit Server DataDir (WebDAV Upload/Download + Server git / daily save), not the mapped repo remote.

## 3. User Stories

1. **Workspace is a git work tree** — As a person, I want every Workspace’s DataDir work tree to be a git work tree, so that GitHub transport has a git home without a special label.
2. **Every Workspace may connect** — As a person, I want any Workspace to use Server-git when a remote exists, so that there is no allowlist.
3. **Server-git when a remote exists** — As a person, I want the Server-git path when a remote exists and the desk path when it does not, so that one check chooses the path.
4. **Config in git** — As an operator, I want config to live in git on that work tree (`git remote` plus current branch / upstream), so that Ambit does not keep a Server branch map in v1.
5. **Same tracked branch** — As a person, I want pull and push to use the same tracked branch, so that round-trip v1 does not switch branches.
6. **Fast-forward only** — As a person, I want a conflicted or non-FF push rejected, so that the remote does not take a diverging history.
7. **Remotes accept push** — As an operator, I want remotes that accept push (not PR-only), so that the Server Actor can push the tracked branch.
8. **Load** — As a person, I want Command Load, so that I bring files through the chosen path.
9. **Save** — As a person, I want Command Save, so that I send files through the chosen path.
10. **git Load** — As a person, I want an explicit git Load pre-pick, so that I pull from the GitHub remote when I choose git.
11. **git Save** — As a person, I want an explicit git Save pre-pick, so that I push to the GitHub remote when I choose git.
12. **desk Load** — As a person, I want an explicit desk Load pre-pick, so that I take the desk file path when I choose desk.
13. **desk Save** — As a person, I want an explicit desk Save pre-pick, so that I take the desk file path when I choose desk.
14. **Plain Load prefers git** — As a person, I want plain Load to be git Load when a remote exists and desk Load otherwise, so that I do not pick a path when the default is enough.
15. **Plain Save prefers git** — As a person, I want plain Save to be git Save when a remote exists and desk Save otherwise, so that I do not pick a path when the default is enough.
16. **No automatic pull or push** — As a person, I want pull and push only when I run Load or Save (or an explicit git*/desk* pre-pick), so that no schedule, post-Persist, or post-Download git starts a round-trip.
17. **Load transfers files only** — As a person, I want all three Load forms to transfer files only, so that this spec does not fold Parse or Graph sync into Load.
18. **WebDAV remains** — As a person, I want WebDAV Upload and Download to remain, so that Ambit↔Ambit file transit stays available beside Server-git.
19. **Server Peer Actor does the round-trip** — As a person, I want a Server-side Peer Actor to pull and push, so that the App stays thin and is not the git Actor host.
20. **One Actor shape through Server** — As a person, I want the same Actor shape on every device that maps through Server, so that this is not a per-App clone protocol.
21. **Host git credentials** — As an operator, I want the Actor to invoke `git` and git to load host credentials, so that Ambit does not store GitHub credentials in appsettings, user-secrets, Graph, or DataDir.
22. **Optional `.amb` skip** — As an operator, I want Directory File `.amb` skipped on the GitHub remote only when repo configuration says so, so that skip is optional and not a hard default.
23. **Backup when `.amb` is excluded** — As a person, I want excluded `.amb` notes to stay on Server DataDir through WebDAV Upload/Download and Server git / daily save, so that exclude-from-repo is not desk-local-only.

## 4. Out of Scope

1. **Checkout / switch / older commits** — This spec does not checkout, switch branch, or move to older commits. That work is future, not github-transport v1.
2. **Parse and Graph sync autonomy** — This spec does not adapt Load for parse or graph. Parse after files land stays [[plan/parse-actor/project.md]]. Graph sync stays its own autonomy.
3. **WebDAV redesign** — This spec does not replace Ambit↔Ambit WebDAV Upload/Download. Redesign stays on [[plan/transport-layer/project.md]] / [[plan/auto-download-persisted-files/project.md]]. Implemented path: [[doc/current/workspace-file-sync.md]].
4. **This repo’s git procedure** — This spec is not [[plan/git-protocol/project.md]]. Committed Decision [[doc/Decisions/0002-git-protocol.md]] stays that Desktop procedure.
5. **Want-driven Graph→Browser** — This spec does not change residency. That work stays [[plan/browser-residency/project.md]].
6. **workspace-git command surface** — This spec does not inherit [[plan/workspace-git/project.md]] Git Remote / Git Pull / Git Push as the primary surface, and does not inherit that spec’s non-FF accept.
7. **Actor wiring and reject UX** — How the Server Actor is composed, and how a person sees a rejected non-FF push or a failed pull, stay unsettled on [map.md](map.md) Not yet specified. This spec does not invent that surface.
8. **git Save vs existing Persist / GitSave** — How git Save meets the current Server commit path stays unsettled on [map.md](map.md) Not yet specified. This spec does not invent that composition.

## 5. Further Notes

1. **Spoken names** — Prefer Workspace, not “label.” Use Actor, not Agent, for the Server Peer Actor ([[CONTEXT.md]]). Load and Save are the existing Command names.
2. **Chapter home** — Product objective only: [Send to and from GitHub](../roadmap/epics/chapters/send-to-and-from-github.md) on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. Technical locks live here.
3. **Cite** — [[src/Server/WorkspaceGit.fs]] (`ensurePushConfig` / `receive.denyNonFastForwards`). Implemented WebDAV Upload / Download: [[doc/current/workspace-file-sync.md]].
4. **Next** — Architecture is [[.agents/skills/to-arch/SKILL.md]] (HITL). This spec does not run to-arch or to-tickets.
