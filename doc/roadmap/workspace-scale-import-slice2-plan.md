# Workspace Git transport — implementation record
(File is misnamed.  It is correctely characterized as implementation of [[workspaces-checklist.md]], Git section.)

Category: Sync
Status: Complete (G0–G7)
See also: [[workspaces-checklist]], [[git-sync-gateway]], [[workspace-scale-import]], [[doc/current/workspace-local-mapping]], [[doc/current/desktop-local-files]], [[doc/current/workspace-stage-plan]], [[doc/current/sync-mvp]], [[future-merge-sync]], [[workspace-name-verbatim]]

Implementation record for the **Git** section of [[workspaces-checklist]]. Protocol and locked product decisions live in [[git-sync-gateway]]; this doc owns the ordered G0–G7 Git transport work, checklist mapping, and the handoff to Lazy Load. Do not treat history plans under [[doc/history/workspaces/plans]] as current truth.

## What it gives you

- Each server workspace folder `DataDir/{label}/` is its own git repo; desktop mapped roots are clones that push/pull against the server.
- Smart HTTPS git gateway on the server with **stock** `git-upload-pack` / `git-receive-pack` wire paths; remote name `ambit`.
- Client commands: connect remote, clone, pull/push via desktop (stock `git pull`/`git push` against gateway URLs); sync status (ahead / behind / dirty).
- Explicit file-tree transport only — live graph editing stays on HTTP change batches ([[doc/current/sync-mvp]]). No single-file GET on the git gateway.

## What it avoids for now

- Server-side git merge / rebase / conflict resolution (except optional later “allow any FF” if locked).
- Graph multi-client merge ([[future-merge-sync]]) — separate track.
- Browser-only git (no desktop host).
- Branch switching UI; git object model in the outline.
- Bulk zip/rsync workspace transfer.
- File/directory create/move placement work ([[workspace-file-directory-placement]]) — parallel, out of scope here.
- Disk-to-graph stub reconciliation, expand-to-parse, and freshness UI; these respond after Git changes and belong to Lazy Load ([[lazy-load]]).

## Requirements inventory (checklist → requirement)

| Checklist item | Requirement | Primary owner |
| --- | --- | --- |
| Init empty repo in a new server directory | On workspace create (or first persist under that label), `git init` inside `DataDir/{label}/`; set `receive.denyNonFastForwards=true`; optional default `.gitignore` | Server |
| Commit all files to repo on server **[checked today]** | **Reinterpret:** today’s [[src/Server/GitSave.fs]] commits the **whole** `DataDir` if `DataDir/.git` exists. Target is **per-workspace** commit (JIT before fetch; optional explicit save scoped to `{label}/`). Treat the checkmark as legacy monolithic save, not proof that per-workspace Git transport was done. | Server |
| Smart HTTPS git endpoints | Smart HTTP at **`/ambit/git/{label}.git`** with stock service paths **`git-upload-pack`** / **`git-receive-pack`**; custom policy (JIT / reject-dirty) is server middleware; no single-file GET | Server gateway |
| Push special semantics | **workspace-push (prose):** accept only when client is **current** (fast-forward). **Dirty-tree = reject-dirty** (locked G0). Wire path = `git-receive-pack`. | Server gateway |
| Desktop: Clone / pull / push | Stock `git` against gateway URLs — **no path-mapping helper**; auth still G4 | Desktop |
| Client: Connect workspace remote | One-time: write/update mapping + `git remote add/set-url ambit <gateway-url>` + credentials path | Client + Desktop |
| Client: Clone into local folder | UI → desktop clone into picked folder → mapping | Client + Desktop |
| Client: Pull / Push | UI → desktop `git pull`/`git push` on `ambit`; surface errors (non-FF, dirty server, auth) | Client + Desktop |
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
| Desktop map | label → absolute local path | [[doc/current/workspace-local-mapping]] — load + Get/Put + folder picker (G6); client Connect/Clone/Pull/Push (G7) |
| Remote name | `ambit` | Not `origin` — preserves user’s existing upstream. Align gateway doc wording. |
| HTTPS gateway | Server only | Auth separate from browser cookie; token or SSH later |
| Graph HTTP sync | Existing `/ambit/changes` + poll | Unchanged |
| Shared | Only pure helpers if any (e.g. status parse, URL shape) | No git subprocess in Shared |

### What is NOT Git’s job

- Authoritative graph merge across clients ([[future-merge-sync]], checklist “Multi-client graph merge”).
- Creating/updating outline nodes by itself — after push, disk is current; **graph** needs a separate reconcile (sync-tree / lazy load — checklist Lazy Load).
- Replacing live collaborative editing over HTTP.
- File/Directory create/move placement rules under Workspace.

## Recorded decisions and deferred question

1. **Push when server working tree is dirty (FF-eligible client)** — **Locked (G0): reject-dirty.** Reject push when server working tree is dirty; do **not** JIT-commit on push. JIT commit remains only before fetch/pull. Recorded in [[git-sync-gateway]]. (Checklist previously said JIT-then-accept; that wording is cleared.)
2. **Legacy `GitSave` (whole-DataDir repo)** — retire, ignore, or keep as optional ops-only tool once per-workspace repos exist? Recommendation: do not init `DataDir/.git` going forward; leave existing capability until per-workspace save/JIT replaces the UX need. (Substrate Option A locked; this is retire-vs-ops-only only.)
3. **Auth v0** — **Locked (G4): HTTPS PAT** via HTTP Basic on the gateway; issue after cookie login at `GET /ambit/git-token`. SSH deferred.

## Dependencies / sequencing

| Prerequisite | Status | Needed by |
| --- | --- | --- |
| `DataDir/{label}/` live-save | Done (Stage 7) | G1+ |
| Verbatim workspace folder names (no `@`) | Done (name-verbatim A+B); G0 corrected gateway + scale-import docs | G1+ |
| Desktop mapping load + resolve | Done | G5–G7 |
| Folder picker + mapping Get/Put API | Done (G6 desktop) | G7 (done) |
| App login / session | Done | G4 issues git-scoped credential |
| Disk-to-graph reconciliation and expand-to-parse / freshness UI | Planned under Lazy Load | Not required by the Git gateway or G0–G7 transport |
| File/directory create/move placement work | Parallel | **Do not block Git**; do not edit those docs/code from this track |

## Ordered work items

### G0 — Doc lock (no code) ✅

- **Done:** Locked dirty push = **reject-dirty** in [[git-sync-gateway]] (no JIT on push; JIT only before fetch/pull).
- **Done:** Struck legacy label-prefixed disk paths in the gateway and Git transport summary in [[workspace-scale-import]]; use `DataDir/{label}/`.
- **Done:** Remote name **`ambit`** in those docs.
- **Done:** Checklist Git section links here + gateway; push bullet no longer contradicts reject-dirty.
- **Done (substrate):** Locked **Option A** — subprocess stock `git` everywhere; `WorkspaceGit` reuses GitSave patterns; gateway delegates wire to `git`; Shared = pure only. Recorded in [[git-sync-gateway]] Locked decisions.

### G1 — Per-workspace `git init` ✅

- **Done:** `WorkspaceGit.ensureInit` — `git init` in workspace root; set `receive.denyNonFastForwards`; skip if `.git` present.
- **Done:** Called from `DocumentPersistence.writeDocument` when creating a Workspace document directory.
- Success: creating workspace `home` yields `DataDir/home/.git`; no requirement that `DataDir/.git` exists.
- Tests: Server.Tests `WorkspaceGitTests` — init idempotence, path under label, denyNonFastForwards, writeDocument wire-up.

### G2 — Workspace-scoped git helpers ✅

- **Done:** `WorkspaceGit.statusPorcelain` / `isDirty` / `commitAll` under `{label}/` via subprocess (reuses `GitSave.runGit` / `commitAll`).
- **Done:** Commit message format locked: `{base} | client: {hint}` via `ClientIdentity.formatCommitMessage` (e.g. `rev 42 | client: Win32; Mozilla/5.0…`). Omit `| client: …` when hint absent.
- **Done:** Legacy `/ambit/save` still commits whole `DataDir` via `GitSave` (deferred per-label save); passes `X-Gambol-Client` into the message.
- Success: dirty tree under one label can be committed without touching sibling workspaces.
- Tests: Server.Tests `WorkspaceGitTests` — porcelain dirty, scoped commit + client hint, sibling isolation; Shared.Tests message format; endpoint save includes client hint.
- Shared-first: pure `ClientIdentity.formatCommitMessage`; I/O stays in Server.

### G3 — Smart HTTPS gateway v0 (local/dev) ✅

- **Done — Locked URL:** `/ambit/git/{label}.git/…` with stock service paths **`git-upload-pack`** / **`git-receive-pack`**. No single-file GET.
- **Done — Stock git wire:** vanilla `git pull`/`git push` against the gateway URL hit these paths natively. Custom policy is server middleware (not custom path names). Subprocess = `git upload-pack` / `git receive-pack`.
- **Done:** Before upload-pack (workspace-pull): flush → JIT if dirty. Before receive-pack (workspace-push): reject-dirty; FF + `updateInstead`.
- **Done:** Auth stub was cookie (or open when Auth empty); replaced by G4 PAT. Legacy `/ambit/save` unchanged.
- **Done tests:** `GitGatewayTests` — stock service names, reject unknown custom name, dirty reject, JIT on pull advertise. Full pack round-trip deferred with G5 desktop ops.

### G4 — Git auth ✅

- **Done — Locked:** HTTPS PAT (not SSH). Derived via `AuthToken.deriveGitToken` (distinct from browser cookie).
- **Done:** `GET /ambit/git-token` after cookie login returns `{ username, token }`; when Auth empty, reports disabled (gateway open).
- **Done:** Gateway uses `IsGitAuthenticated` — HTTP Basic with username + PAT only; browser cookie alone → 401 + `WWW-Authenticate: Basic realm="Gambol Git"`.
- **Done:** Desktop store path documented in [[git-sync-gateway]] (credential helper / GCM); client Connect/Clone wires issue+store (G7).
- Success: push/pull works with stored Basic credential; cookie alone is insufficient when Auth is enabled.
- Tests: `AuthTokenTests` (derive/parse); `GitGatewayTests` — unauthenticated / cookie-only / wrong PAT reject; Basic PAT accept; git-token issue.

### G5 — Desktop git operations ✅

- **Done:** `DesktopGit.setAmbitRemote` / `setAmbitRemoteForLabel` (local path + workspace label → remote `ambit`).
- **Done:** Stock `git` against gateway URLs — **no** service-name rewrite / path-mapping helper.
- **Done:** Desktop endpoints: `POST /_desktop/git-remote|git-pull|git-push|git-status|git-clone|git-credential`.
- **Done:** Capability `git.git` (`canGit`) when host has `git` on PATH.
- **Done:** PAT store via `git credential approve` (`/_desktop/git-credential`); client connect UX in G7.
- Success: with mapping + remote preconfigured, desktop pull/push/status round-trip through G3 gateway.
- Does **not** require folder picker yet (clone takes an explicit `path`).
- Placement: pure URL/status parse in Shared; subprocess ops in `Gambol.Shared.DotNet` (`DesktopGit`); HTTP wiring in Desktop.
- Tests: Shared.Tests `DesktopGitTests` + `WorkspaceGitRemoteTests` (status parse / remote URL).

### G6 — Mapping API + folder picker ✅

- **Done:** Folder picker `POST /_desktop/pick-folder` (optional `requireGit`); `POST /_desktop/detect-git`.
- **Done:** `GET` / `PUT /_desktop/workspace-mappings` (full replace or single `{label,path}` upsert); persists `%LocalAppData%/Gambol/config.json` and updates in-memory map.
- **Done:** Shared `WorkspaceLocalMapping.encode` / `saveToFile` / `upsert` / `tryGitRoot`.
- **Done (G7):** Client Connect/Clone/Pull/Push/status commands (gated on `canGit`).
- Success (desktop): mapping and folder browse work without hand-editing `config.json`.
- Tests: Shared.Tests `WorkspaceLocalMappingTests` — encode round-trip, upsert, tryGitRoot.

### G7 — Client commands + sync status ✅

- **Done:** Commands: Connect remote, Clone workspace, Download (pull), Upload (push), Git status.
- **Done:** Wire to desktop endpoints (G5 git ops + G6 picker/mappings) + `GET /ambit/git-token` → credential store.
- **Done:** Gated on desktop `git.git` (`canGit`); hidden from command palette when not on desktop / git unavailable.
- **Done:** Focus must be under a named workspace (not ROOT); errors surface in `#cmd-last-result`.
- Success: checklist Client Git bullets satisfied for happy path + main reject paths; user can go from zero mapping to a working `ambit` remote without editing `config.json` by hand.
- Tests: Shared.Tests — `formatStatusLine`, `canDesktopGit`, `tryWorkspaceGitLabel`; manual desktop smoke for dialogs.

### Handoff — response after Git changes

The former G8 is not a Git transport step. [[lazy-load]] is the canonical project document and records the decision, implemented create-only increment, and remaining capabilities.

- After successful server receive, **create-only disk-to-graph stub reconciliation is implemented** for added paths through standard server graph changes. Moves, renames, and deletes remain.
- After desktop pull, **expand-to-parse and freshness UI** reports the local file as current, unparsed, older, or newer without making pull itself a graph operation.
- G7 is therefore the end of this implementation sequence. Remaining work is tracked under [[workspaces-checklist]] § Lazy Load, not as unfinished Git work.

### Deferred policy option — “any fast-forward”

- Only if product wants explicit confirmation beyond denyNonFastForwards.
- Otherwise mark checklist item cancelled or absorbed into G3.

## Tests (by work item)

| Work item | Where |
| --- | --- |
| G1–G2 | Server.Tests — init, scoped commit, porcelain |
| G3–G4 | Server integration — gateway FF/dirty/auth |
| G5 | Shared.Tests — `DesktopGitTests` (temp repos), status/URL contract; Desktop endpoints in `DesktopGitEndpoints` |
| G6 | Shared.Tests — mapping encode/upsert/`tryGitRoot`; Desktop `WorkspaceMappingEndpoints` + `FolderPicker` (manual smoke for dialog) |
| G7 | Shared.Tests — status format / label / capability gate; Client `UpdateWorkspaceGit` (manual desktop smoke for picker) |

## Out of scope / non-goals

- Implementing FileNodeOps / file-directory create/move placement or flipping those checklist items.
- Server merge for files; conflict marker nodes for git.
- Replacing [[doc/current/sync-mvp]] HTTP sync.
- Implementing disk-to-graph reconciliation, expand-to-parse, or freshness UI from this Git transport plan.
- Migrating leftover local legacy marker-prefixed folders under `data/` ([[workspace-name-verbatim]] non-goal).

## Doc placement

| Doc | Role |
| --- | --- |
| **This file** | Completed implementation record for Git transport checklist items G0–G7 |
| [[git-sync-gateway]] | Protocol, flows, locked decisions (paths / `ambit` / reject-dirty locked in G0) |
| [[workspaces-checklist]] | Living checkboxes; Git section links here + gateway |
| [[lazy-load]] | Canonical Lazy Load project, Git boundary, current status, and remaining capabilities |
| [[workspace-scale-import]] | Parent workspace-scale summary; points at gateway + this record |
| [[doc/index]] | Status line for Git workspace sync |

When a work item ships, mark checklist boxes and move durable behavior into [[doc/current/]] (or extend desktop/mapping current docs); do not leave implemented behavior only in roadmap.

## Assumptions

- Restart baseline: no `WorkspaceGit.fs`, no gateway routes, no desktop git endpoints (history plans describe discarded work).
- “Commit all files… [x]” on the checklist means legacy GitSave only until G1–G2 reinterpret it.
- Desktop remains required for clone/pull/push; pure web clients keep using graph HTTP only.
