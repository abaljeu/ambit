# github-transport

Labels: wayfinder:map

## 1. Destination

A Server Actor pulls from and pushes to GitHub for a Workspace label whose DataDir work tree is already the git home. Round-trip v1 is fast-forward only. The App stays thin. Skip of Directory File `.amb` on that remote is optional, from repo configuration. Parse after files land stays on [[plan/parse-actor/project.md]].

## 2. Notes

This Project is a transport-layer connector leg for the **external GitHub remote**. It is not Ambit↔Ambit WebDAV Upload/Download. Those stay on [[plan/transport-layer/project.md]] / [[plan/auto-download-persisted-files/project.md]]. File-shaped file→Graph stays [[plan/parse-actor/project.md]]. This is not [[plan/git-protocol/project.md]] (this repo’s Desktop git procedure).

Chapter home (product objective only): [[plan/roadmap/epics/chapters/send-to-and-from-github.md]] on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. Technical locks live on this Project. Cite [[src/Server/WorkspaceGit.fs]] (`ensurePushConfig` / `receive.denyNonFastForwards`). Leftover `doc/roadmap/workspace-file-sync.md` is deleted. Implemented WebDAV Upload / Download is [[doc/current/workspace-file-sync.md]]. [[plan/roadmap/epics/agent-chat-managed-context.md]] depends on that Chapter. It does not own it.

The 2026-09-26 lock names the Server Actor a **Peer Actor**. Actor is the glossary word. Peer Actor here means that Server Actor, not a new Kind.

Prior spec [[plan/workspace-git/project.md]] is not this home. Grill its Git Remote / Git Push / Git Pull surface as a candidate on [02 — Actor command surface](issues/02-actor-command-surface.md). Do not inherit that spec’s non-FF accept.

Frontier grilling: [01 — Which Workspace labels and remotes](issues/01-which-workspace-labels-and-remotes.md), [02 — Actor command surface](issues/02-actor-command-surface.md), [03 — When pull and push fire](issues/03-when-pull-and-push-fire.md), [04 — Credential storage on Server](issues/04-credential-storage-on-server.md).

Skills: [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/domain-modeling/SKILL.md]], [[.agents/skills/project-work/SKILL.md]].

## 3. Decisions so far

1. **GitHub is the outside channel** — Key repos use GitHub. The Workspace label’s DataDir work tree **is** the git home (`.git` already established).
2. **Operator sets remote and credentials** — The operator sets `git remote` and credentials so git works. Remotes accept push (not PR-only). Policy is **fast-forward only**. A conflicted or non-FF push is rejected. Server `ensurePushConfig` sets `receive.denyNonFastForwards`.
3. **Server Peer Actor does the round-trip** — A Server-side Peer Actor does pull and push (round-trip v1). The App stays thin. The App is not the git Actor host.
4. **One Actor shape through Server** — The same Actor shape serves every device that maps through Server. This is not a per-App clone protocol.
5. **Optional `.amb` skip from repo configuration** — Directory File / Ambit note paths named `.amb` may be skipped on the GitHub remote when repo configuration says so. They are not a hard-skip default and not the same class as `.git/`. When excluded, offsite backup of those notes is Ambit Server DataDir (WebDAV Upload/Download + Server git / daily save), not the mapped repo remote. Do not treat “exclude from repo sync” as “notes are desk-local only.” WebDAV Upload/Download and Server DataDir tracking of `.amb` stay Ambit↔Ambit graph persistence ([[doc/current/workspace-graph.md]]) and the backup path when the repo remote does not take the notes.
6. **Not this repo’s git procedure** — This Project is not [[plan/git-protocol/project.md]].

## 4. Not yet specified

1. **Which Workspace labels and remotes** — Which labels connect, and where that config lives. Ticket [01 — Which Workspace labels and remotes](issues/01-which-workspace-labels-and-remotes.md).
2. **Actor command surface** — What Commands the Server Actor receives for remote, pull, and push. Ticket [02 — Actor command surface](issues/02-actor-command-surface.md).
3. **When pull and push fire** — What starts pull vs push in round-trip v1. Ticket [03 — When pull and push fire](issues/03-when-pull-and-push-fire.md).
4. **Credential storage on Server** — Where the Server keeps git credentials for GitHub. Ticket [04 — Credential storage on Server](issues/04-credential-storage-on-server.md).

## 5. Out of scope

1. **Ambit↔Ambit WebDAV** — Implemented [[doc/current/workspace-file-sync.md]]. Redesign stays on transport-layer / [[plan/auto-download-persisted-files/project.md]]. This Project does not replace that transit.
2. **Parse after files land** — Stays [[plan/parse-actor/project.md]].
3. **This repo’s git procedure** — Stays [[plan/git-protocol/project.md]].
4. **Want-driven Graph→Browser** — Stays [[plan/browser-residency/project.md]].
5. **Product code on this chart** — The map finds the way. It does not implement the Actor.
