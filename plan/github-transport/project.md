# github-transport

Stage: chart
Summary: A person maps a key GitHub repo to a Workspace label whose DataDir work tree is already the git home. A Server Actor pulls from and pushes to that GitHub remote (round-trip v1, fast-forward only) so every device that maps through Server sees the same files. The App stays thin. Skip of Directory File `.amb` on that remote is optional, from repo configuration.
Updated: 2026-09-26

**Part of:** [[plan/roadmap/epics/chapters/send-to-and-from-github.md]]
**Part of / under:** [[plan/transport-layer/project.md]] (file transit)

## Notes

- 2026-09-26 — Wayfinder lock in chat. This Project is the **external GitHub remote** path. WebDAV Upload/Download stay Ambit↔Ambit on transport-layer / [[plan/auto-download-persisted-files/project.md]]. Parse after files land stays [[plan/parse-actor/project.md]]. This is not [[plan/git-protocol/project.md]]. Chapter home is [[plan/roadmap/epics/chapters/send-to-and-from-github.md]] on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. That Chapter states the product objective only. Technical locks live here.
- Cite [[src/Server/WorkspaceGit.fs]] (`ensurePushConfig` / `receive.denyNonFastForwards`). Leftover `doc/roadmap/workspace-file-sync.md` is deleted. Implemented WebDAV Upload / Download lives on [[doc/current/workspace-file-sync.md]]. External-git substance lives here.
- Locked Destination (2026-09-26): GitHub is the outside channel for key repos. The Workspace is the git work tree — the Workspace label’s DataDir work tree **is** the git home (`.git` already established). The operator sets `git remote` and credentials so git works. Remotes accept push (not PR-only). Policy is **fast-forward only** — a conflicted or non-FF push is rejected (Server `ensurePushConfig`). A Server-side Peer Actor does pull and push (round-trip v1). The App stays thin and is not the git Actor host. The same Actor shape serves every device that maps through Server. Skip of Directory File `.amb` on the external remote is **optional and config-driven** (repo configuration), not a hard default. When those notes are excluded from the remote, they still back up via WebDAV / DataDir.
- 2026-09-26 — `.amb` skip lock (replaces the 2026-09-20 hard-skip default). Directory File / Ambit note paths named `.amb` may be skipped on the GitHub remote when repo configuration says so. They are not in the same hard-skip class as `.git/`. When excluded, offsite backup of those notes is Ambit Server DataDir (WebDAV Upload/Download + Server git / daily save), not the mapped repo remote. Do not treat “exclude from repo sync” as “notes are desk-local only.” This does not change Ambit WebDAV Upload/Download or Server DataDir tracking of `.amb` as the Directory File artifact ([[doc/current/workspace-graph.md]]). Those stay Ambit↔Ambit graph persistence and the backup path when the repo remote does not take the notes.
- Prior spec [[plan/workspace-git/project.md]] is not this home. That spec’s non-FF accept of non-overlapping edits is not this Destination.
- Map: [[map.md]].
