# github-transport

Stage: chart
Summary: A person maps a key GitHub repo to a Workspace label whose DataDir work tree is already the git home. A Server Actor pulls from and pushes to that GitHub remote (round-trip v1, fast-forward only) so every device that maps through Server sees the same files. The App stays thin. Directory Files stay off that remote.
Updated: 2026-09-26

**Part of:** [[plan/roadmap/epics/chapters/ambit-keeps-consistency-with-desktop-repo-for-agentic-work.md]]
**Part of / under:** [[plan/transport-layer/project.md]] (file transit)

## Notes

- 2026-09-26 — Wayfinder lock in chat. This Project is the **external GitHub remote** path. WebDAV Upload/Download stay Ambit↔Ambit on transport-layer / [[plan/auto-download-persisted-files/project.md]]. Parse after files land stays [[plan/parse-actor/project.md]]. This is not [[plan/git-protocol/project.md]].
- Cite [[doc/roadmap/workspace-file-sync.md]] and [[src/Server/WorkspaceGit.fs]] (`ensurePushConfig` / `receive.denyNonFastForwards`).
- Locked Destination (2026-09-26): GitHub is the outside channel for key repos. The Workspace label’s DataDir work tree **is** the git home (`.git` already established). The operator sets `git remote` and credentials so git works. Remotes accept push (not PR-only). Policy is **fast-forward only** — a conflicted or non-FF push is rejected (Server `ensurePushConfig`). A Server-side Peer Actor does pull and push (round-trip v1). The App stays thin and is not the git Actor host. The same Actor shape serves every device that maps through Server. Hard-skip `.amb` on the external remote per Chapter lock. Ambit notes back up via WebDAV / DataDir, not the repo remote.
- Prior spec [[plan/workspace-git/project.md]] is not this home. That spec’s non-FF accept of non-overlapping edits is not this Destination.
- Map: [[map.md]].
