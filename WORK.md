# Work Board

Live actionable work only. Empty sections mean nothing is known pending there. Git history is the audit trail; completed items are deleted, not archived.

## Legend

Each entry is one actionable item: a link to the durable source or target, a concise expected outcome, and optional owner or blocker detail.

Entry format:

```
- [[path/to/artifact]] — expected outcome (owner: root-agent-id)
```

Mutations for delegated workers to return to their parent: `add`, `move`, `block`, `remove`.

## Active

Work currently being executed.

- [[plan/daily-git-save/project.md]] — once-per-UTC-day background `commitAll` after listen; git subprocess only (no DbAgent wait) (artifacts: [[src/Server/DailyGitSave.fs]])
- [[plan/owner-edge-db-repair/spec.md]] — extend startup sweep: ACID repair of `node_children` Owned tree (GC unreachable; promote Ref when reachable node has no owner) (artifacts: [[plan/owner-edge-db-repair/implement.md]], [[src/Shared/ProjectionOwnershipRepair.fs]])

## Pending

Work ready to start but not yet claimed.

- [[.cursor/skills/update-matt-skills/scripts/merge-to-live.sh]] — align with git-protocol (`dev`/`ready`, no `w/*`); done forks under [[plan/done/update-matt-skills/forks/]] still describe `w/*`
- [[.agents/skills/git-guardrails-claude-code/SKILL.md]] — hooks may block [[scripts/merge.sh]], [[scripts/push.sh]], and cloud push of `ready`
- [[.cursor/skills/git-master/SKILL.md]] — name the tag convention on `master` and who applies a tag
- [[plan/llm-connector/map.md]] — chart Run `?` pack, LLM call, and write-back
- [[plan/document-formats/map.md]] — chart remaining document formats (XML and other draft codecs)
- [[plan/end-user-wiki/map.md]] — chart the end-user wiki (describe the software)
- [[plan/architecture/map.md]] — chart the architecture wiki (how it is coded and run)
- [[plan/marketing-wiki/map.md]] — chart the marketing wiki (uses, GitLab-level browsable; not a campaign)
- [[tmp/load-performance-audit.md]] — secondary: ensure ledger reuse on already-synced Load (Mask path); diagnose empty-ledger resets (artifacts: [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] needsSeed, [[src/Shared/dotnet/WorkspaceFileSync.fs]] ensureLedgerSeeded)
- [[tmp/load-performance-audit.md]] — skip workspace-inventory when Unloaded (empty stub path) (artifacts: [[src/Client/UpdateWorkspaceSync.fs]], [[src/Shared/WorkspaceUploadStructure.fs]])
- [[tmp/load-performance-audit.md]] — defer/narrow path-sync ledger waterfall after push (artifacts: [[src/Client/App.fs]] runWorkspacePathSyncSnapshot, [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] liveStatusRows)
- [[doc/reference/dev-debug-workflow.md]] — document watch: prefer `/ambit?debug=1`; after esbuild rebuild hard-reload (Ack on CodeOutdated does not unblock)
- [[plan/glossary-directory-file/rename-isMarker.md]] — optional remaining speech/doc sweep for informal “marker” (Directory File sense); `isMarker` / related API renames done
- [[plan/large-node-cursor-perf/delete-children-cost.md]] — profile/optimize delete among large siblings (fromNodes + SiteMap rematch / structural DOM plan) (parent: [[plan/large-node-cursor-perf/project.md]])

## Blocked

Work that cannot proceed until a named dependency or decision is resolved.
