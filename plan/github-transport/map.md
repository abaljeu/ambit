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

2026-09-26 grill locks: [01 — Which Workspaces and remotes](issues/01-which-workspace-labels-and-remotes.md)–[05 — Git Load/Save are Workspace-scoped](issues/05-git-load-save-workspace-scoped.md), [07 — Actor start door](issues/07-actor-start-door.md), [08 — Reject UX](issues/08-reject-ux.md), [09 — Skip list is .gitignore](issues/09-gitignore-skip-list.md), [10 — git Save is commit then push](issues/10-git-save-commit-then-push.md). Status `done`. [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md) was filed later (locked 2026-09-28). Report: [[reports/grill-locks-01-04-2026-09-26.md]].

2026-09-28 grill locks: [16 — Persist/git work-tree gate](issues/16-persist-git-work-tree-gate.md) (exclusive gate **revoked**), [17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md), [19 — Parsed/Unparsed and Persisted/Unpersisted](issues/19-file-newer-graph-newer.md), [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md). Status `done`. Special nodes carry Parsed|Unparsed and Persisted|Unpersisted. Git Load: Unparsed on Workspace → pull → push Workspace onto the one long-lived Parse actor. Upload is the same path. Workspace/directory Parse reconciles immediate members only, then Unparsed on children that need work, then Parsed. Graph edit → Unpersisted, which feeds Core’s async Persist stack. Persist is not an Actor. Persist of a file is blocked while that node is Unparsed. git Save is permitted while Unparsed or Unpersisted. Client Load on a file node pushes the selection onto the same Parse stack; Git Load’s remaining work does not need a special selection path or priority. No exclusive work-tree gate. No Reconciling cascade. No Conflicted state.

2026-09-28 — Alan lock: Core alone knows where files reside. Everyone else has a relative path. One hardened control point. Cross-cutting home: [[plan/transport-layer/map.md]] Decisions so far. This Project uses relative paths; it does not hold file residence.

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
9. [03 — When pull and push fire](issues/03-when-pull-and-push-fire.md) — No automatic pull or push in v1. Person Load/Save (and explicit git*/desk*) only. When a remote exists, plain Load/Save prefer git first. WebDAV Upload/Download remains. After files land, Load marks Unparsed and pushes onto the one Parse actor ([17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md)). Nobody starts an Actor after pull. Parse home stays [[plan/parse-actor/project.md]].
10. [04 — Credential storage on Server](issues/04-credential-storage-on-server.md) — Ambit does not store GitHub credentials. The Actor invokes `git`; git loads credentials (credential helper / host setup). On Server that is the host’s git.
11. [05 — Git Load/Save are Workspace-scoped](issues/05-git-load-save-workspace-scoped.md) — git Load/Save always pull/push the whole Workspace work tree / tracked branch, never file-level git, wherever Load/Save is invoked (Workspace root or a subnode). Parse still runs on the selection where appropriate after files land. Selection-parse: [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md).
12. [07 — Actor start door](issues/07-actor-start-door.md) — Not Run and not a `?git` entrée. Load or Save Command → load/save command request → mailbox → actor pool → GitHub Peer Actor. The Actor cares about Focus (Workspace / work tree) only. Same wiring for Save as Load.
13. [08 — Reject UX](issues/08-reject-ux.md) — Conflict: error message naming at least one file path. Other failures: matching short error. Reflect what git reports, condensed. Same for Load and Save.
14. [09 — Skip list is .gitignore](issues/09-gitignore-skip-list.md) — No Ambit skip key. Skip list is `.gitignore`; the person edits that file.
15. [10 — git Save is commit then push](issues/10-git-save-commit-then-push.md) — git Save is `git commit` of the work-tree edits, then push. Graph→file Persist already happens independently; git Save does not own or replace that path. Do not merge Persist into git Save.
16. [16 — Persist/git work-tree gate](issues/16-persist-git-work-tree-gate.md) — **Revoked 2026-09-28.** The exclusive Persist/git work-tree gate does not stand. Unparsed / Unpersisted replace it. History: 2026-09-26 named one queued exclusive gate per Workspace for Persist, pull, and Save commit.
17. [17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md) — Git Load: set Unparsed on the Workspace Node, pull files, push the Workspace onto the Parse actor. Nobody starts an Actor after pull. Client Upload is the same path (land on disk → Unparsed → push Parse).
18. [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md) — One long-lived Parse actor. Core pushes a reconcile target onto its stack; others may push too. Parse = disk → graph for Workspace, Directory, and File. Workspace/directory Parse reconciles immediate members only, then Unparsed on children that need updating, then marks this node Parsed. Persist is not an Actor. Actor home: [[plan/parse-actor/project.md]].
19. [19 — Parsed/Unparsed and Persisted/Unpersisted](issues/19-file-newer-graph-newer.md) — Special nodes carry two independent axes: Parsed|Unparsed and Persisted|Unpersisted. Parse = disk → graph. Persist = graph → disk. Graph edit → Unpersisted and feeds Core’s async Persist stack. Persist of a file is blocked while that node is Unparsed. git Save is permitted while Unparsed or Unpersisted. No Conflicted state.
20. [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md) — Client Load on a file node (selection) pushes selected nodes onto the one Parse actor’s stack. Parse processes them in the normal course of stack processing. Git Load may still have work it triggered; selection does not need a special path or priority beyond push-on-stack.
21. **Core alone knows where files reside** — Locked 2026-09-28 (Alan). One hardened control point: Core alone knows where files reside. Everyone else has a relative path. Git Load/Save, Parse, and Persist on this Project use relative paths. Cross-cutting home: [[plan/transport-layer/map.md]].

## 4. Not yet specified

None.

## 5. Out of scope

1. **Ambit↔Ambit WebDAV** — Implemented [[doc/current/workspace-file-sync.md]]. Redesign stays on transport-layer / [[plan/auto-download-persisted-files/project.md]]. This Project does not replace that transit.
2. **Parse after files land** — Stays [[plan/parse-actor/project.md]].
3. **This repo’s git procedure** — Stays [[plan/git-protocol/project.md]].
4. **Want-driven Graph→Browser** — Stays [[plan/browser-residency/project.md]].
5. **Product code on this chart** — The map finds the way. It does not implement the Actor.
6. **Checkout / switch branch / older commits** — Future. Not this chart.
7. **Parse actor implementation** — This map does not implement the Parse actor. That home stays [[plan/parse-actor/project.md]]. Git Load marks Unparsed and pushes onto the one long-lived Parse actor ([17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md)). Do not start a Parse Actor after pull.
