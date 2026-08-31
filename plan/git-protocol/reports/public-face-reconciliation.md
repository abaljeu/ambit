# Public face reconciliation

Read-only analysis of the divergence between `origin/master` (27ecd8f, the public face) and the work line `ready` (705f3c9). Merge base is d960315. `origin/master` is 4 commits ahead of the base; `ready` is 479 commits ahead.

Status of this report: sections 1, 2, 3 and 5 are **complete**. Section 4 is **partly complete** — see the gap note in that section. The investigation stopped early on instruction of the requester. The delete decision for [[dockerfile]] and [[.htaccess]] is already made and is not re-evaluated here.

## Findings

### 1. The two READMEs — complete

The public README is not an independent document. It is the merge-base README with three edits. `git diff d960315 origin/master -- README.md` shows the whole of the public change:

- title changed from `# Gambol` to `# Ambit`
- the `## Ambit` section (the note about the `ambit/` sibling implementation) removed
- a new `## Status` section added: pre-alpha, master is not current, ask for a good branch

Everything else in the public README is frozen at the state of d960315. The work line then advanced [[README.md]] through 7 commits (+91 / -51 lines).

Content present in the `ready` README and **absent** from the `origin/master` README:

| Content | Operational value |
|---|---|
| Architecture rows for **Desktop** (WPF + WebView2, local HTTP proxy) and Npgsql on the Server row | Two layers of the system are invisible in the public table |
| Client is served under `/ambit`, and the app URL is `http://localhost:5215/ambit`, not the site root | High. The public README still states port **5115** and the site root. Those build instructions are stale and would mislead a reader |
| Links to [[doc/arch.md]] and [[doc/api.md]] | Entry points into the reference documentation |
| Whole **Persistence** section: `Persistence:Mode` (`db` default, `file` alternative), `DB_CONNECTION_STRING`, automatic snapshot and change-log writes, links to [[doc/reference/postgres-environments.md]] and [[doc/current/persistence-model.md]] | High. Without it there is no statement that a database is needed to run |
| Whole **Desktop** section: `bash scripts/desktop.sh run` and the VS Code **desktop: Run** task | Setup step for the desktop shell |
| Prerequisite **Node.js 18 or later** | High. Build fails without it |
| Build steps `npm ci` and `npm run bundle` | High. The bundle step is required; the public README has no npm step at all |
| Dev guidance: Fable writes modules into `wwwroot`, run `npm run bundle` for a fresh `Program.bundle.js`, open `/ambit?debug=1` for unbundled modules | Debugging workflow |
| Whole **Custom domain (cPanel → Azure)** section, with links to [[doc/reference/cpanel-transparent-proxy.md]] and [[doc/reference/deploy-azure.md]] | Production topology |
| The `## Ambit` section | Deliberately removed on the public side. Do not restore it publicly |

Conclusion: adopting the public README wholesale on the work line loses every item above and also **imports stale instructions** (port 5115, no npm, no bundle). The lost items must be relocated, not dropped.

### 2. Did the work line touch the deleted files? — complete

Yes. Both files changed after the merge base. `git log --oneline d960315..ready -- dockerfile .htaccess`:

- `ca3ff0f` cleanup login, proxy to cloud, diagnostic guid — rewrote the web-server configuration to proxy through [[proxy.php]] instead of a 302 to Azure, and normalised line endings
- `03e1996` UI for running arbitrary commands — added a redirect rule to the web-server configuration, and changed the [[dockerfile]] entrypoint from `Gambol.Server.dll` to `Ambit.Server.dll`

Because both sides changed these paths, a merge of `origin/master` into the work line produces a **modify/delete conflict** on each, not a clean delete.

### 3. Is anything still using them? — complete

**[[dockerfile]]: no reference anywhere in the repository.** A case-insensitive search for `dockerfile` across the whole tree returns only the file itself. A scoped `git grep` over [[scripts]], [[.vscode]], [[package.json]], [[Directory.Build.props]] and [[gambol.sln]] returns nothing. [[.github]] contains only a `prompts/` directory — there are no workflows, so no CI path exists at all. The only `docker` hits in the tree are unrelated: [[doc/reference/postgres-environments.md]] lines 15, 23, 25 and 47 describe an optional `docker-compose.dev.yml` for a local PostgreSQL instance, and that file does not exist in the repository; the remaining hits are prose examples inside skill files ([[.agents/skills/qa/SKILL.md]] line 74, [[.agents/skills/ubiquitous-language/SKILL.md]] lines 75 and 77, [[.agents/skills/wizard/SKILL.md]] line 20).

Further evidence that [[dockerfile]] is dead: its `ENTRYPOINT` is `dotnet Ambit.Server.dll`, but the server project is [[src/Server/Gambol.Server.fsproj]] and no `.fsproj` sets `AssemblyName`, so the produced assembly is `Gambol.Server.dll`. The image would fail to start. The file also targets Fly.io (`# Fly.io sets PORT`), and Fly.io appears nowhere else in the repository.

Neither deploy path uses a container. [[scripts/_deploy.sh]] builds with Fable, runs `dotnet publish`, tars the output and copies it over SSH to `abaljeu@collaborative-systems.org`; it never mentions Docker. [[scripts/azure.sh]] only calls `az webapp restart` and `az postgres flexible-server start|stop|show`. [[doc/reference/deploy-azure.md]] describes a Kudu zip-push deploy onto a .NET 10 Linux Web App.

**Deleting [[dockerfile]] breaks no build, deploy or CI path.**

**[[.htaccess]]: no build, deploy or CI reference either, but it is a live production artifact.** The scoped `git grep` over [[scripts]], [[.vscode]], [[package.json]], [[Directory.Build.props]] and [[gambol.sln]] returns nothing, so no automated process reads it. All references are documentation:

- [[doc/reference/cpanel-transparent-proxy.md]] lines 4, 25, 31, 33 and 36 — the file is the Apache side of the production custom-domain proxy; line 31 states "Upload repo-root [[.htaccess]] and [[proxy.php]] to the cPanel document root after changes"
- [[doc/README.md]] line 45 — index entry for that reference document
- [[.github/prompts/plan-workspaceGitRobustness.prompt.md]] line 35 — describes the `.htaccess` → `proxy.php` → Azure request path
- [[plan/client-start-time/research.md]] lines 157, 182, 188 and 275 — same production path

**Deleting [[.htaccess]] breaks no build, deploy or CI path.** The operational cost is different: the repository stops holding the versioned copy of the live Apache rewrite rules that [[doc/reference/cpanel-transparent-proxy.md]] tells the operator to upload. [[proxy.php]] stays at the repository root, so after the delete the documented pair is split — half tracked, half not. Two follow-up items therefore belong with the delete:

1. keep the rewrite rules somewhere durable, for example inline in [[doc/reference/cpanel-transparent-proxy.md]] (section `### .htaccess` already partly describes them) or as a non-public copy
2. update the wikilinks in [[doc/reference/cpanel-transparent-proxy.md]] and [[doc/README.md]] so they do not point at a deleted path

### 4. Full divergence set — partly complete

`git diff --stat origin/master ready`, restricted to the paths that exist on `origin/master`, reports **106 files changed, 15066 insertions, 7075 deletions**. Almost all of that is ordinary work-line progress, not presentation.

Paths that exist on `origin/master` and are **absent** from `ready` (`git diff --name-status --diff-filter=D origin/master ready`):

- `.copilot-instructions` — legacy agent instruction file, superseded by [[.cursor/rules]] and [[.cursor/copilot-instructions.md]]
- `.cursor` — on `origin/master` this is a **47-line regular blob**, not a directory; on `ready` it is a directory tree
- `ambit` — on `origin/master` this is a **submodule gitlink** at 830e823; on `ready` it is untracked
- `data/gambol`, `data/gambol-snapshot.txt` and three `data/gambol.bak.*` files — stale sample data
- `doc/deploy-azure.md`, `doc/deploy-wordpress.md`, `doc/plan.md`, `doc/sync-mvp.md` — moved or retired documents
- `scripts/deploy.ps1`, `scripts/start.sh` — retired scripts, but note that [[scripts/_deploy.sh]] line 39 still uploads `scripts/start.sh`, which no longer exists on the work line; this is a pre-existing work-line problem, unrelated to the public face
- `src/Server/wwwroot/gambol.html`, `src/Shared/ModelOps.fs` — retired source

Presentation-oriented files: there is **no LICENSE file on either side**. [[.github]] holds only `prompts/` on both sides, so there are no issue templates, no workflows and no funding file. No images or docs landing page exist on the public side that the work line lacks. The only public-only content is the three items the four public commits produced.

**Gap left open:** the contents of `origin/master:.copilot-instructions` and the `origin/master:.cursor` blob were not read. Both are legacy agent-instruction artifacts that the work line replaced with [[.cursor/rules]] and [[.cursor/skills]], so they are very unlikely to hold public presentation value, but that was not confirmed.

### 5. Predicted conflict set — complete

The four public commits touch exactly three paths (`git diff --name-status d960315 origin/master`): `M README.md`, `D dockerfile`, `D .htaccess`. Every other path on `origin/master` sits at its merge-base state, so the three-way merge takes the work-line side without a conflict.

A `--no-ff` merge of `origin/master` into the work line therefore conflicts on exactly three paths:

| Path | Conflict type | Cause |
|---|---|---|
| [[README.md]] | content conflict | both sides edited the same regions after d960315 |
| [[dockerfile]] | modify/delete | deleted on `origin/master`, modified by `03e1996` on the work line |
| [[.htaccess]] | modify/delete | deleted on `origin/master`, modified by `ca3ff0f` and `03e1996` on the work line |

Everything else resolves automatically to the work-line version. There is no risk of the merge silently reverting work-line code.

## Recommendation

All three options assume the settled decisions: [[dockerfile]] and [[.htaccess]] leave the work line, and the public presentation comes into the work line. The reference check in section 3 confirms that neither delete breaks a build, a deploy or CI.

### Option A — adopt the public README wholesale, relocate the developer content

Take the `origin/master` README as [[README.md]] on `dev`, delete both files, and move every item from the section 1 table into [[doc]].

```bash
git checkout dev
git merge --no-ff origin/master        # 3 conflicts, as predicted in section 5
git checkout --theirs README.md        # public version wins
git rm dockerfile .htaccess            # accept the public deletes
# then move the lost developer content into doc/ before committing
```

Cost: a new developer setup document must be written in [[doc]] — realistically [[doc/README.md]] gains a "Build and run" entry pointing at a new page that holds the Node.js prerequisite, `npm ci`, `npm run bundle`, port 5215, the `/ambit` path, the Persistence section and the Desktop section.

Risk: **high if done carelessly.** The public README carries stale build instructions (port 5115, no npm step). Adopting it wholesale publishes wrong instructions until they are corrected, and any developer content missed during the move is silently lost. Also, once [[README.md]] is the public file, every future developer-facing edit must remember not to land there.

### Option B — split the file: public [[README.md]], developer [[doc/README.md]]

Same merge and same deletes, but the developer content moves to a clearly named file first, so the split is explicit and permanent.

```bash
git checkout dev
git mv README.md doc/developing.md     # or fold into the existing doc/README.md
git commit -m "move developer readme to doc"
git merge --no-ff origin/master        # README.md now conflicts only as add/add or resolves clean
git rm dockerfile .htaccess
```

Cost: one extra commit, plus a one-line pointer in the public [[README.md]] ("Developer setup: [[doc/developing.md]]") so the content is discoverable.

Risk: low. The move is recorded as a rename, so nothing is lost and the history follows the content. The public file becomes unambiguously public, which removes the recurring "which README am I editing" problem.

### Option C — recommended: Option B, plus repair the public README and stage the [[.htaccess]] content

Do Option B, then make two corrections in the same change set.

1. **Fix the stale facts in the public README before it becomes canonical.** Correct the port to 5215, correct the URL to `/ambit`, and add the Node.js prerequisite. The public README can stay short and marketing-shaped and still be correct. Leave the `## Status` section and the removed `## Ambit` section exactly as the human wrote them.
2. **Preserve the [[.htaccess]] rules where the operator will find them.** Before the delete, inline the current rewrite rules into the `### .htaccess` section of [[doc/reference/cpanel-transparent-proxy.md]], and update the wikilinks in that document and in [[doc/README.md]] line 45 so they do not point at a deleted path. This is the only real loss the deletes cause, and it costs one paragraph to avoid.

Cost: roughly one extra editing pass over two documents.

Risk: lowest of the three. It fixes the root cause named in the brief — the presentation now lives on the work line, so future squashes onto `master` no longer reintroduce the long README or the two deleted files — and it does not trade a merge problem for a documentation problem.

One point applies to all options. After the merge lands on `dev` and reaches `master`, the recurring conflict disappears **only if the public README stays byte-identical between the two branches**. Keep [[README.md]] public-only from that point on, and route all developer edits to the file chosen in Option B.
