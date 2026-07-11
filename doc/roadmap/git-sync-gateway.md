# Git sync gateway

Category: Sync
See also: [[workspace-scale-import-slice2-plan]], [[workspace-scale-import]], [[workspaces-checklist]], [[doc/roadmap/workspace-file-persistence.md]], [[doc/roadmap/workspace-file-model.md]], [[doc/current/desktop-local-files.md]], [[doc/current/workspace-local-mapping.md]], [[doc/current/persistence-model.md]], [[doc/roadmap/future-merge-sync.md]]

**Slice 2** of [[workspace-scale-import]] — follows slice 1 (outliner ↔ files on one machine). Ordered shippable slices: [[workspace-scale-import-slice2-plan]].

Target design for synchronizing a workspace repo between the server `DataDir` and a desktop local checkout. **Workspace == repo.** One workspace label maps to one git repository rooted at `DataDir/{label}/` (verbatim label; no `@` prefix on disk — [[workspace-name-verbatim]]).

This doc records decisions that upcoming persistence and workspace work should respect. It does not supersede [[doc/current/sync-mvp.md]] for live graph editing over HTTP; git sync is a coarse, explicit file-tree transport layered on top.

## What it gives you

- A desktop user maps a workspace label to a local directory that is a git checkout with remote name **`ambit`** pointing at the server gateway (not `origin`).
- Gateway smart HTTP uses **stock** service paths **`git-upload-pack`** (fetch / workspace-pull) and **`git-receive-pack`** (push / workspace-push). Pack wire protocol and URL/service names are stock; **custom policy is server middleware**.
- **Stock `git pull`/`git push` can target gateway URLs** — no desktop path-mapping helper. Auth: HTTPS PAT via HTTP Basic (G4); cookie alone is not enough.
- **workspace-push** (prose) accepts only **fast-forward** updates when the server working tree is **clean** (**reject-dirty** — no JIT on push).
- Gateway does not read PostgreSQL, merge, or serve single-file GET — sync trees via pack protocol; file reads stay app/desktop APIs.
- After a successful pull into the local tree, the outliner marks affected files **stale** and offers reparse (see [[workspace-scale-import]]).

## What it avoids for now

- Server-side merge, rebase, or conflict resolution for git.
- Git integration inside the graph change log or `DbAgent`.
- Browser-only git (client-side merge requires desktop local filesystem + git binary).
- Branch switching UI, git object model in the outline, or repo-wide search.
- Replacing HTTP change batches for live editing; two layers coexist.

## Locked decisions

| Topic | Decision |
| --- | --- |
| Repo root | `DataDir/{workspaceLabel}/` — same tree as live-save / [[doc/roadmap/workspace-file-persistence.md]] (verbatim label) |
| `.git` location | **Inside** `{label}/` (e.g. `DataDir/home/.git`). Import and tree browse skip `.git` per [[workspace-scale-import]]. |
| Remote name | **`ambit`** (not `origin`) |
| Service path names | **Locked G3:** URL / `?service=` use stock **`git-upload-pack`** / **`git-receive-pack`**. Custom policy (JIT / reject-dirty) is middleware; subprocesses are stock `git upload-pack` / `git receive-pack`. |
| Stock git CLI | **Can target gateway URLs natively** for pull/push (auth still G4). No desktop path-mapping helper. |
| Pull (server → client) | On `git-upload-pack` (workspace-pull): **JIT commit on server first**, then serve upload-pack. |
| Dirty push policy | **reject-dirty** on `git-receive-pack` (workspace-push); no JIT on push. JIT only before upload-pack. Locked G0. |
| Push (client → server) | `git-receive-pack`: reject unless server working tree is clean; FF only (`receive.denyNonFastForwards`). |
| Desktop transport | Pack protocol over smart HTTP to stock service paths — **not** a bespoke JSON pack API. |
| Git substrate | **Option A locked:** subprocess to stock `git` for pack I/O. |
| Module shape | `WorkspaceGit` + `GitGateway`; legacy `GitSave` / `/ambit/save` stays ops-only until retired. |
| Commit message | `{base} | client: {X-Gambol-Client hint}`; omit client segment when hint absent. Locked G2. |
| Gateway URL | **`/ambit/git/{label}.git`** — `info/refs?service=git-upload-pack\|git-receive-pack`, POST `…/git-upload-pack`, `…/git-receive-pack`. **No** single-file GET. |
| Gateway | Thin ASP.NET: auth, flush, optional JIT, then delegate wire to `git upload-pack` / `git receive-pack`. |
| Auth (G3 vs G4) | **Locked G4:** HTTPS PAT via HTTP Basic (`username` + `deriveGitToken`). Issue at `GET /ambit/git-token` after cookie login. Cookie alone does **not** authenticate smart HTTP. When Auth empty, gateway open. SSH deferred. |
| Desktop + Shared | Desktop runs stock `git` against gateway; Shared = pure URL/service helpers only. |
| Module boundary | `DocumentPersistence` writes files; git gateway runs git. **Only coupling:** JIT before pull, clean-tree check before push. |
| Path moves | Filesystem moves under `{label}/` should be real renames where possible so git history stays coherent. |

## On-disk layout

```text
{DataDir}/
  home/                  ← workspace == repo work tree (verbatim label)
    .git/                ← inside {label}/
    src/
      lib.fs
    docs/
      specs/
        .amb
```

Local desktop mapping ([[doc/current/workspace-local-mapping.md]]) points label `home` at a separate directory that is a **clone** of the server repo, not the server path itself.

## Protocol flows

### workspace-pull via git-upload-pack (server → client)

```mermaid
%%{init: {'themeVariables': {'fontSize': '20px'}}}%%
sequenceDiagram
  participant Client as Desktop git
  participant GW as Git gateway
  participant FS as DataDir/{label}
  participant DP as DocumentPersistence

  Client->>GW: git fetch / pull
  GW->>DP: ensure flushed to disk (workspace)
  GW->>FS: JIT commit if dirty
  GW->>Client: upload-pack (smart HTTP or SSH)
  Client->>Client: git merge (local)
  Client->>Client: mark stale files in outliner
```

1. User triggers pull in Gambol (desktop runs stock `git pull` / fetch against `ambit`).
2. Gateway ensures graph edits for that workspace are **persisted to disk** (flush).
3. Gateway runs **JIT commit** on the server repo if the working tree has uncommitted changes (autosaved files).
4. Client receives pack / merges locally. User resolves conflicts locally if any.
5. Client marks changed paths stale for reparse.

### workspace-push via git-receive-pack (client → server)

```mermaid
%%{init: {'themeVariables': {'fontSize': '20px'}}}%%
sequenceDiagram
  participant Client as Desktop git
  participant GW as Git gateway
  participant FS as DataDir/{label}

  Client->>GW: git push ambit
  GW->>FS: working tree clean?
  alt dirty
    GW->>Client: reject (dirty tree — reject-dirty)
  else clean
    GW->>FS: receive-pack (FF only)
    GW->>Client: ok
  end
```

1. User commits locally on desktop (manual `git commit` — not automatic on every edit).
2. Desktop POSTs pack to **`git-receive-pack`**. Gateway refuses if server working tree is **dirty** (**reject-dirty**; no JIT commit on push).
3. Gateway refuses **non-fast-forward** pushes. Client must pull (merge locally) and push again.
4. No server merge: push only moves `HEAD` when it is a strict ancestor update and the tree was clean before receive.

**Client should be current:** non-FF push is rejected; user merges on desktop first.

## JIT commit (server)

The only intentional cross-layer action on the server before pull. Push never JIT-commits (**reject-dirty**).

When the gateway is about to serve `upload-pack` / respond to fetch for `{label}`:

1. Confirm `DocumentPersistence` has flushed pending graph writes for that workspace.
2. If `git status --porcelain` is non-empty under `DataDir/{label}/`, run a commit, e.g. `git add -A` and `git commit` with message from `ClientIdentity.formatCommitMessage` (base e.g. `gambol: autosave before workspace-pull`, plus `| client: …` when a hint is available).
3. Proceed with fetch.

Properties:

- Pull always sees **committed** server state plus whatever was just autosaved to disk.
- Commit message is machine-generated and includes the weak client hint when known; user-facing commit remains a separate manual action on desktop before push.
- `.git` and gitignored paths follow normal git rules; graph import still skips `.git` in the outline.

## Git gateway module

Standalone concern — subprocess to `git` or thin wrapper around **`git http-backend`** / **`git receive-pack`** / **`git upload-pack`**. No REST-shaped "git API" exists; native remotes speak smart HTTP or SSH.

### Responsibilities

| Responsibility | Owner |
| --- | --- |
| Resolve `DataDir/{label}/`, auth, rate limits | Gateway |
| `rev-parse`, `status --porcelain`, JIT commit, receive-pack, upload-pack | Gateway via `git -C …` |
| Graph ops, revision, PostgreSQL | Existing `/ambit` stack |
| Write `.amb` / source files from graph | `DocumentPersistence` |

### Fast-forward enforcement

Configure the repo:

```text
receive.denyNonFastForwards = true
```

Pre-receive hook may additionally verify `expectedBase` if the transport exposes it; stock git behavior is sufficient for FF-only.

### Clean-tree enforcement (push)

Pre-check (or hook): if `git status --porcelain` is non-empty in the work tree, reject with a clear message (`server working tree dirty; workspace-pull or wait for autosave flush`). Ensures workspace-push does not race uncommitted server autosaves.

### URL shape (locked G3)

```text
https://collaborative-systems.org/ambit/git/home.git/info/refs?service=git-upload-pack
https://collaborative-systems.org/ambit/git/home.git/info/refs?service=git-receive-pack
https://collaborative-systems.org/ambit/git/home.git/git-upload-pack
https://collaborative-systems.org/ambit/git/home.git/git-receive-pack
```

Advertisement `# service=` lines and Content-Types use stock names (`application/x-git-upload-pack-advertisement`, etc.). Server subprocesses remain `git upload-pack` / `git receive-pack`.

**Compatibility:** stock `git pull`/`git push` against this URL layout work for wire paths. Auth: HTTP Basic with the git PAT from `/ambit/git-token` (not the browser cookie). No desktop helper is required for path mapping.

## Credentials (HTTPS PAT — locked G4)

GitHub CLI (`gh auth login`) stores credentials so **`git push` / `git pull` work without re-prompting**. Gambol uses the same ergonomics with an HTTPS PAT:

| Mechanism | Notes |
| --- | --- |
| **HTTPS + PAT** | Deterministic git-scoped token from `Auth:Username` / `Auth:Password` via `AuthToken.deriveGitToken` (HMAC over `git:{username}` — **not** the browser cookie value). |
| **Issue** | After normal login (cookie session): `GET /ambit/git-token` → JSON `{ "username", "token" }`. When Auth is empty, response reports `disabled` and the gateway is open. |
| **Wire auth** | Smart HTTP expects `Authorization: Basic` with that username and PAT. Cookie alone → 401 + `WWW-Authenticate: Basic realm="Gambol Git"`. |
| **Credential helper** | Store username + PAT in Git Credential Manager / `git credential` for the gateway host. Desktop: `POST /_desktop/git-credential` (G5); client connect UX wires issue+store in G7. |
| **SSH** | Deferred. |
| **Not sufficient** | Browser session cookie alone does not authenticate git smart HTTP. |

Example store (manual until desktop connect UX):

```text
printf "protocol=https\nhost=collaborative-systems.org\nusername=alice\npassword=<token-from-git-token>\n\n" | git credential approve
```

Reuse existing app auth to **issue** the PAT after `/ambit/login`, but do not conflate graph API session with git wire auth.

## Implications for upcoming implementation

### [[doc/roadmap/workspace-file-persistence.md]] / Stage 7–8

- Persist only under `DataDir/{label}/…`; never write into `.git`.
- Flush semantics must be well-defined so JIT commit sees a consistent tree.
- Path move handler: prefer rename syscalls so git tracks renames.

### [[workspace-scale-import]]

- Skip `.git` in outline tree; gitignored files not auto-imported (unchanged).
- Stale marking after client pull is required for correctness.

### [[doc/current/desktop-local-files.md]]

- Add capability flags for `git` / `remoteConfigured` when desktop can run git.
- Pull/Push UI shells out to local git in the mapped workspace root (or prompts setup).

### [[doc/roadmap/future-merge-sync.md]]

- Graph merge at the server remains a separate track. **File repos** use git on the client; do not add server-side file merge to `DbAgent`.

## Implementation steps

Ordered slices and checklist mapping: [[workspace-scale-import-slice2-plan]].

1. **Stage 7 live-save** — `DataDir/{label}/` on Azure `/home` — implemented; see [[doc/current/workspace-stage-plan.md]] §7.
2. **Init repo** — on workspace creation or first persist, `git init` inside `{label}/`; optional default `.gitignore` for local artifacts.
3. **Gateway v0** — smart HTTP or SSH endpoint per workspace; FF-only; **reject-dirty** on push; JIT commit before upload-pack only.
4. **Desktop remote setup** — map label → local clone; `ambit` URL; credential helper or SSH key docs.
5. **JIT commit + flush hook** — gateway calls flush then commit before fetch; integration test with dirty tree → pull sees commit.
6. **UI** — Pull / Push at workspace root; surface `behind` / `ahead` / `dirty` from `git status -sb`.
7. **Stale after pull** — wire file nodes to reparse prompt when disk hash/mtime changes.

## Tests

- **Shared / path**: canonical paths under `{label}/` exclude `.git` from import walk (when import exists).
- **Server integration** (later): push rejected when work tree dirty (reject-dirty); push rejected on non-FF; JIT commit creates commit when porcelain non-empty before fetch; FF push updates `HEAD` and files on disk match commit.

## Non-goals

- Server-side `git merge` or conflict markers in files.
- Automatic commit on every graph edit (only JIT commit before pull on server; manual commit on desktop before push).
- Hosting multiple branches in the UI (single default branch; `main` unless configured).
