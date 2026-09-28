# github-transport

Labels: wayfinder:map

## 1. Destination

A Server Actor pulls from and pushes to GitHub for a Workspace whose DataDir work tree is already the git home. Round-trip v1 is fast-forward only. The App stays thin. Skip on that remote is whatever `.gitignore` already says; the person edits that file. Parse after files land stays on [[plan/parse-actor/project.md]].

## 2. Notes

This Project is a transport-layer connector leg for the **external GitHub remote**. It is not Ambit↔Ambit WebDAV Upload/Download. Those stay on [[plan/transport-layer/project.md]] / [[plan/auto-download-persisted-files/project.md]]. File-shaped file→Graph stays [[plan/parse-actor/project.md]]. This is not [[plan/git-protocol/project.md]] (this repo’s Desktop git procedure).

Chapter home (product objective only): [[plan/roadmap/epics/chapters/send-to-and-from-github.md]] on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. Technical locks live on this Project. Cite [[src/Server/WorkspaceGit.fs]] (`ensurePushConfig` / `receive.denyNonFastForwards`). Leftover `doc/roadmap/workspace-file-sync.md` is deleted. Implemented WebDAV Upload / Download is [[doc/current/workspace-file-sync.md]]. [[plan/roadmap/epics/agent-chat-managed-context.md]] depends on that Chapter. It does not own it.

The 2026-09-26 lock names the Server Actor a **Peer Actor**. Actor is the glossary word. Peer Actor here means that Server Actor, not a new Kind.

Prefer **Workspace**, not “label.” Every Workspace’s DataDir work tree is a git work tree. Server-git vs desk: check if a remote exists. If a remote exists, the Server-git path is valid; else desk.

Prior spec [[plan/workspace-git/project.md]] is not this home. Do not inherit that spec’s Git Remote / Git Pull / Git Push as the primary surface. Do not inherit that spec’s non-FF accept.

2026-09-26 grill locks: [01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md)–[05 — Git Load/Save are Workspace-scoped](issues/05-git-load-save-workspace-scoped.md), [07 — Actor start door](issues/07-actor-start-door.md), [08 — Reject UX](issues/08-reject-ux.md), [09 — Skip list is .gitignore](issues/09-gitignore-skip-list.md), [10 — git Save is commit then push](issues/10-git-save-commit-then-push.md). Status `done`. Later: [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md) (`needs-info`). Report: [[reports/grill-locks-01-04-2026-09-26.md]].

2026-09-28 grill locks: [17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md), [19 — Parsed/Unparsed and Persisted/Unpersisted](issues/19-file-newer-graph-newer.md). Status `done`. Special nodes carry Parsed|Unparsed and Persisted|Unpersisted. Git Load: Unparsed on Workspace → pull → push Workspace onto the one long-lived Parse actor. Upload is the same path. Workspace/directory Parse reconciles immediate members only, then Unparsed on children that need work, then Parsed. Graph edit → Unpersisted; Persist later writes and clears it. No Reconciling cascade. No Conflicted state. [16 — Persist/git work-tree gate](issues/16-persist-git-work-tree-gate.md) is a prior lock; standing beside these axes is open.

Skills: [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/domain-modeling/SKILL.md]], [[.agents/skills/project-work/SKILL.md]].

## 3. Decisions so far

1. **GitHub is the outside channel** — Key repos use GitHub. Every Workspace’s DataDir work tree **is** a git work tree (the git home; `.git` already established).
2. **Operator sets remote and credentials** — The operator sets `git remote` and credentials so git works. Remotes accept push (not PR-only). Policy is **fast-forward only**. A conflicted or non-FF push is rejected. Server `ensurePushConfig` sets `receive.denyNonFastForwards`.
3. **Server Peer Actor does the round-trip** — A Server-side Peer Actor does pull and push (round-trip v1). The App stays thin. The App is not the git Actor host.
4. **One Actor shape through Server** — The same Actor shape serves every device that maps through Server. This is not a per-App clone protocol.
5. **Skip list is `.gitignore`** — Skip on the GitHub remote is whatever `.gitignore` already says. The person edits that file. No special Ambit config key. When `.amb` or other notes are listed there, they are skipped on the remote; they are not a hard-skip default and not a separate class from other ignore rules. When excluded, offsite backup of those notes is Ambit Server DataDir (WebDAV Upload/Download + Server git / daily save), not the mapped repo remote. Do not treat “exclude from repo sync” as “notes are desk-local only.” WebDAV Upload/Download and Server DataDir tracking of `.amb` stay Ambit↔Ambit graph persistence ([[doc/current/workspace-graph.md]]) and the backup path when the repo remote does not take the notes.
6. **Not this repo’s git procedure** — This Project is not [[plan/git-protocol/project.md]].
7. [01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md) — Every Workspace (all have git). Server-git when a remote exists; no allowlist. Config is `git remote` + current branch / upstream on that work tree. Same tracked branch for pull and push. No Server branch map in v1.
8. [02 — Actor command surface](issues/02-actor-command-surface.md) — Person Commands are Load and Save. Secondary pre-picks: git Load / git Save and desk Load / desk Save. Plain Load/Save = git* when a remote exists, else desk*. Do not inherit workspace-git’s Git Remote / Git Pull / Git Push or non-FF accept.
9. [03 — When pull and push fire](issues/03-when-pull-and-push-fire.md) — No automatic pull or push in v1. Person Load/Save (and explicit git*/desk*) only. When a remote exists, plain Load/Save prefer git first. WebDAV Upload/Download remains. Load keeps today’s Load → Parse coupling (Parse is not autonomous yet). Do not redesign around a future autonomous Parse.
10. [04 — Credential storage on Server](issues/04-credential-storage-on-server.md) — Ambit does not store GitHub credentials. The Actor invokes `git`; git loads credentials (credential helper / host setup). On Server that is the host’s git.
11. [05 — Git Load/Save are Workspace-scoped](issues/05-git-load-save-workspace-scoped.md) — git Load/Save always pull/push the whole Workspace work tree / tracked branch, never file-level git, wherever Load/Save is invoked (Workspace root or a subnode). Parse still runs on the selection where appropriate after files land. Selection-parse nuance is later: [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md).
12. [07 — Actor start door](issues/07-actor-start-door.md) — Not Run and not a `?git` entrée. Load or Save Command → load/save command request → mailbox → actor pool → GitHub Peer Actor. The Actor cares about Focus (Workspace / work tree) only. Same wiring for Save as Load.
13. [08 — Reject UX](issues/08-reject-ux.md) — Conflict: error message naming at least one file path. Other failures: matching short error. Reflect what git reports, condensed. Same for Load and Save.
14. [09 — Skip list is .gitignore](issues/09-gitignore-skip-list.md) — No Ambit skip key. Skip list is `.gitignore`; the person edits that file.
15. [10 — git Save is commit then push](issues/10-git-save-commit-then-push.md) — git Save is `git commit` of the work-tree edits, then push. Graph→file Persist already happens independently; git Save does not own or replace that path. Do not merge Persist into git Save.
16. [16 — Persist/git work-tree gate](issues/16-persist-git-work-tree-gate.md) — 2026-09-26: each Workspace work tree has one exclusive gate shared by Graph→file Persist, git Load pull, and git Save commit. The second caller queues; contention waits. Persist stays independent of git Save. git Save remains commit, then push. The 2026-09-28 axes model does not mention this gate; whether it still stands is Not yet specified.
17. [17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md) — Git Load: set Unparsed on the Workspace Node, pull files, push the Workspace onto the Parse actor. Nobody starts an Actor after pull. Client Upload is the same path (land on disk → Unparsed → push Parse).
18. [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md) — One long-lived Parse actor. Core pushes a reconcile target onto its stack; others may push too. Parse = disk → graph for Workspace, Directory, and File. Workspace/directory Parse reconciles immediate members only, then Unparsed on children that need updating, then marks this node Parsed. Actor home: [[plan/parse-actor/project.md]].
19. [19 — Parsed/Unparsed and Persisted/Unpersisted](issues/19-file-newer-graph-newer.md) — Special nodes carry two independent axes: Parsed|Unparsed and Persisted|Unpersisted. Parse = disk → graph. Persist = graph → disk. Graph edit → Unpersisted; Persist later writes and clears Unpersisted. No Conflicted state.

## 4. Not yet specified

1. **Ticket 16 gate vs the two axes** — Does the 2026-09-26 exclusive Persist/git work-tree gate still stand beside Unparsed / Unpersisted? The later model does not mention it and does not revoke it.
2. **Who runs Persist, and when** — Core function vs actor, and what triggers Persist of Unpersisted nodes.
3. **Parse stack vs selection Parse** — [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md) stays `needs-info`. Push-on-stack does not close 06.
4. **git Save while Unparsed / Unpersisted** — Save commit during those markers is not refined.

## 5. Out of scope

1. **Ambit↔Ambit WebDAV** — Implemented [[doc/current/workspace-file-sync.md]]. Redesign stays on transport-layer / [[plan/auto-download-persisted-files/project.md]]. This Project does not replace that transit.
2. **Parse after files land** — Stays [[plan/parse-actor/project.md]].
3. **This repo’s git procedure** — Stays [[plan/git-protocol/project.md]].
4. **Want-driven Graph→Browser** — Stays [[plan/browser-residency/project.md]].
5. **Product code on this chart** — The map finds the way. It does not implement the Actor.
6. **Checkout / switch branch / older commits** — Future. Not this chart.
7. **Autonomous Parse rearchitecture** — Independent Parse is not in place. Do not redesign Load around a future autonomous Parse. That work stays [[plan/parse-actor/project.md]]. Until it lands, Load keeps today’s Load → Parse coupling.
