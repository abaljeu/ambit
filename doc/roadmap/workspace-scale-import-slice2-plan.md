# Workspace Git sync — implementation plan (Slice 2)

Category: Sync
Status: In progress (G0 done; substrate Option A locked; G1 done)
See also: [[workspaces-checklist]], [[git-sync-gateway]], [[workspace-scale-import]], [[doc/current/workspace-local-mapping]], [[doc/current/desktop-local-files]], [[doc/current/workspace-stage-plan]], [[doc/current/sync-mvp]], [[future-merge-sync]], [[workspace-name-verbatim]]

Concrete rollout for the **Git** section of [[workspaces-checklist]]. Protocol and locked product decisions live in [[git-sync-gateway]]; this doc owns **ordered shippable slices**, checklist mapping, and sequencing. Do not treat history plans under [[doc/history/workspaces/plans]] as current truth.

## What it gives you

- Each server workspace folder `DataDir/{label}/` is its own git repo; desktop mapped roots are clones that push/pull against the server.
- Smart HTTPS git gateway on the server; desktop shells stock `git` with remote name `ambit`.
- Client commands: connect remote, clone, pull, push, sync status (ahead / behind / dirty).
- Explicit file-tree transport only — live graph editing stays on HTTP change batches ([[doc/current/sync-mvp]]).

## What it avoids for now

- Server-side git merge / rebase / conflict resolution (except optional later “allow any FF” if locked).
- Graph multi-client merge ([[future-merge-sync]]) — separate track.
- Browser-only git (no desktop host).
- Branch switching UI; git object model in the outline.
- Bulk zip/rsync workspace transfer.
- File/directory placement Slice B ([[workspace-file-directory-placement]]) — parallel, out of scope here.
- Lazy-load / sync-tree completeness beyond the minimum needed after push (see slice G8).

## Requirements inventory (checklist → requirement)

| Checklist item | Requirement | Primary owner |
| --- | --- | --- |
| Init empty repo in a new server directory | On workspace create (or first persist under that label), `git init` inside `DataDir/{label}/`; set `receive.denyNonFastForwards=true`; optional default `.gitignore` | Server |
| Commit all files to repo on server **[checked today]** | **Reinterpret:** today’s [[src/Server/GitSave.fs]] commits the **whole** `DataDir` if `DataDir/.git` exists. Target is **per-workspace** commit (JIT before fetch; optional explicit save scoped to `{label}/`). Treat the checkmark as legacy monolithic save, not Slice 2 done. | Server |
| Smart HTTPS git endpoints | Native smart HTTP (`info/refs`, `git-upload-pack`, `git-receive-pack`) per workspace repo; not a custom pack REST API | Server gateway |
| Push special semantics | Accept only when client is **current** (fast-forward). **Dirty-tree = reject-dirty** (locked G0). | Server gateway |
| Desktop: Clone / Pull / Push | Shell `git clone` / `git pull ambit` / `git push ambit` in mapped (or chosen) local root | Desktop |
| Client: Connect workspace remote | One-time: write/update mapping + `git remote add/set-url ambit <gateway-url>` + credentials path | Client + Desktop |
| Client: Clone into local folder | UI → desktop clone into picked folder → mapping | Client + Desktop |
| Client: Pull / Push | UI → desktop git; surface errors (non-FF, dirty server, auth) | Client + Desktop |
| Client: Show sync status | `git status -sb` (or equivalent) → ahead / behind / local changes; capability flags | Desktop → Client |
| Maybe allow any fast-forward merge | Soft: if locked, document that any FF update is accepted (already implied by denyNonFastForwards); no non-FF merge on server | Deferred |

## Architecture

```text
Browser Client          Desktop LocalProxy           Server
──────────────          ──────────────────           ──────
Connect/Clone/          folder picker,               DocumentPersistence → DataDir/{label}/
Pull/Push/status  →    mapping CRUD,                WorkspaceGit (init/status/commit)
commands                git clone/pull/push/status   Git HTTPS gateway (http-backend or
                        against remote `ambit`         upload-pack / receive-pack + hooks)
                              │                              │
                              └──── smart HTTP ──────────────┘
```

| Concern | Where | Notes |
| --- | --- | --- |
| Repo root on server | `DataDir/{label}/` with `.git` inside | Verbatim label ([[workspace-name-verbatim]], [[doc/current/workspace-stage-plan]] §7). |
| Desktop map | label → absolute local path | [[doc/current/workspace-local-mapping]] — load exists; picker + Get/Put API still open on checklist |
| Remote name | `ambit` | Not `origin` — preserves user’s existing upstream. Align gateway doc wording. |
| HTTPS gateway | Server only | Auth separate from browser cookie; token or SSH later |
| Graph HTTP sync | Existing `/ambit/changes` + poll | Unchanged |
| Shared | Only pure helpers if any (e.g. status parse, URL shape) | No git subprocess in Shared |

### What is NOT Git’s job

- Authoritative graph merge across clients ([[future-merge-sync]], checklist “Multi-client graph merge”).
- Creating/updating outline nodes by itself — after push, disk is current; **graph** needs a separate reconcile (sync-tree / lazy load — checklist Lazy Load).
- Replacing live collaborative editing over HTTP.
- Placement rules for File/Directory under Workspace (Slice B).

## Blockers needing user decision

1. **Push when server working tree is dirty (FF-eligible client)** — **Locked (G0): reject-dirty.** Reject push when server working tree is dirty; do **not** JIT-commit on push. JIT commit remains only before fetch/pull. Recorded in [[git-sync-gateway]]. (Checklist previously said JIT-then-accept; that wording is cleared.)
2. **Legacy `GitSave` (whole-DataDir repo)** — retire, ignore, or keep as optional ops-only tool once per-workspace repos exist? Recommendation: do not init `DataDir/.git` going forward; leave existing capability until per-workspace save/JIT replaces the UX need. (Substrate Option A locked; this is retire-vs-ops-only only.)
3. **Auth v0** — HTTPS PAT via credential helper vs SSH-first on Azure? Recommendation: HTTPS token first (matches “smart HTTPS” checklist item); SSH as follow-up.

## Dependencies / sequencing

| Prerequisite | Status | Needed by |
| --- | --- | --- |
| `DataDir/{label}/` live-save | Done (Stage 7) | G1+ |
| Verbatim workspace folder names (no `@`) | Done (name-verbatim A+B); G0 corrected gateway + scale-import docs | G1+ |
| Desktop mapping load + resolve | Done | G5–G7 |
| Folder picker + mapping Get/Put API | Open (Desktop mapping checklist) | G6 connect/clone UX (G5 can use pre-edited config) |
| App login / session | Done | G4 issues git-scoped credential |
| Slice 1 tree browse / stale / reparse | Partial / planned | G8 post-pull parse invalidation; gateway itself does not require full Slice 1 |
| Placement Slice B | Parallel | **Do not block Git**; do not edit those docs/code from this track |

## Ordered slices

### G0 — Doc lock (no code) ✅

- **Done:** Locked dirty push = **reject-dirty** in [[git-sync-gateway]] (no JIT on push; JIT only before fetch/pull).
- **Done:** Struck legacy label-prefixed disk paths in gateway + [[workspace-scale-import]] Slice 2 blurb; use `DataDir/{label}/`.
- **Done:** Remote name **`ambit`** in those docs.
- **Done:** Checklist Git section links here + gateway; push bullet no longer contradicts reject-dirty.
- **Done (substrate):** Locked **Option A** — subprocess stock `git` everywhere; `WorkspaceGit` reuses GitSave patterns; gateway delegates wire to `git`; Shared = pure only. Recorded in [[git-sync-gateway]] Locked decisions.

### G1 — Per-workspace `git init` ✅

- **Done:** `WorkspaceGit.ensureInit` — `git init` in workspace root; set `receive.denyNonFastForwards`; skip if `.git` present.
- **Done:** Called from `DocumentPersistence.writeDocument` when creating a Workspace document directory.
- Success: creating workspace `home` yields `DataDir/home/.git`; no requirement that `DataDir/.git` exists.
- Tests: Server.Tests `WorkspaceGitTests` — init idempotence, path under label, denyNonFastForwards, writeDocument wire-up.

### G2 — Workspace-scoped git helpers

- Server module (e.g. `WorkspaceGit`) for `isRepo`, `status --porcelain`, `commitAll` under `{label}/` via `git -C`.
- Reuse patterns from [[src/Server/GitSave.fs]] but **scoped**; do not expand GitSave to mean whole DataDir for new work.
- Success: dirty tree under one label can be committed without touching sibling workspaces.
- Shared-first: only if a pure status DTO/parser is useful to both Desktop and Server; otherwise keep I/O in Server/Desktop.

### G3 — Smart HTTPS gateway v0 (local/dev)

- Routes under `/ambit/git/{label}.git/…` (exact path TBD; one shape only).
- Before upload-pack: flush DocumentPersistence for that workspace → JIT commit if porcelain non-empty.
- Before/during receive-pack: enforce FF + **locked** dirty-tree policy (hook or pre-check).
- Success: from a machine with `git`, clone/pull/push against a running local server for one label (auth may be temporarily open or basic in G3, tightened in G4).
- Tests: integration — dirty reject or JIT-then-accept per lock; non-FF reject; FF push updates files on disk.

### G4 — Git auth

- Issue git-scoped token (or document SSH) after normal login; desktop stores via credential helper.
- Gateway rejects unauthenticated smart HTTP.
- Success: push/pull works with stored credential; browser cookie alone is insufficient (as designed).

### G5 — Desktop git operations

- Endpoints (names illustrative): pull / push / status / clone against mapped root and `ambit`.
- Capability flags (extend [[src/Shared/DesktopCapabilities.fs]]): e.g. `canGit`, `remoteConfigured` when host has git + mapping.
- Success: with mapping + remote preconfigured by hand, desktop pull/push/status round-trip through G3 gateway.
- Does **not** require folder picker yet.

### G6 — Connect / clone UX + mapping API

- Folder picker; Get/Put workspace mappings (checklist Desktop mapping items).
- Connect: set `ambit` URL, credentials, optional initial pull or push.
- Clone: pick empty/new folder, clone from gateway, write mapping.
- Success: user can go from zero mapping to a working `ambit` remote without editing `config.json` by hand.

### G7 — Client commands + sync status

- Commands: Connect, Clone, Pull (Download), Push (Upload), status line/indicator.
- Wire to desktop endpoints; clear errors for non-FF / dirty server / auth failure.
- Success: checklist Client Git bullets satisfied for happy path + main reject paths.

### G8 — Post-push / post-pull graph follow-up (thin)

- After successful push: trigger minimal disk→graph reconcile for that workspace (sync-tree or agreed stub) so poll delivers new File/Directory nodes — ties checklist Lazy Load “on successful push…”.
- After pull: refresh local file metadata / invalidate parse for changed paths (history note: pull makes disk **current**, not “stale” in the Slice 1 external-edit sense). Full lazy-load program stays under Lazy Load checklist.
- Success: push of a new file eventually visible as a graph stub; pull of changed content does not leave silently wrong parse children.

### G9 — Optional “any fast-forward” (soft)

- Only if product wants explicit confirmation beyond denyNonFastForwards.
- Otherwise mark checklist item cancelled or absorbed into G3.

## Tests (by slice)

| Slice | Where |
| --- | --- |
| G1–G2 | Server.Tests — init, scoped commit, porcelain |
| G3–G4 | Server integration — gateway FF/dirty/auth |
| G5 | Desktop or Server.Tests with temp repos; contract JSON for status |
| G6–G7 | Prefer thin Shared DTOs + manual/desktop smoke; avoid heavy UI automation unless already present |
| G8 | Shared planner tests if reconcile is pure; else Server.Tests |

## Out of scope / non-goals

- Implementing FileNodeOps / placement or flipping Slice B checklist items.
- Server merge for files; conflict marker nodes for git.
- Replacing [[doc/current/sync-mvp]] HTTP sync.
- Creating missing Slice 1 plan file or claiming Slice 1 fully done without a separate audit.
- Migrating leftover local legacy marker-prefixed folders under `data/` ([[workspace-name-verbatim]] non-goal).

## Doc placement

| Doc | Role |
| --- | --- |
| **This file** | Implementation plan and slice tracking for Git checklist items |
| [[git-sync-gateway]] | Protocol, flows, locked decisions (paths / `ambit` / reject-dirty locked in G0) |
| [[workspaces-checklist]] | Living checkboxes; Git section links here + gateway |
| [[workspace-scale-import]] | Parent Slice 2 blurb; points at gateway + this plan |
| [[doc/index]] | Status line for Git workspace sync |

When a slice ships, mark checklist boxes and move durable behavior into [[doc/current/]] (or extend desktop/mapping current docs); do not leave implemented behavior only in roadmap.

## Assumptions

- Restart baseline: no `WorkspaceGit.fs`, no gateway routes, no desktop git endpoints (history plans describe discarded work).
- “Commit all files… [x]” on the checklist means legacy GitSave only until G1–G2 reinterpret it.
- Desktop remains required for clone/pull/push; pure web clients keep using graph HTTP only.
