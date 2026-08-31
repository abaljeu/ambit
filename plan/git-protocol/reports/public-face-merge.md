# Public face merge

Editing pass that brings the `origin/master` presentation into the work line. Input analysis: [[.scratch/git-protocol/reports/public-face-reconciliation.md]]. No git operations were run; only file edits. Working tree changes from other work were left alone.

## Task 1 — merged [[README.md]]

One file now serves as both the public landing page and the accurate developer entry point. Order: title, one-line description, `## Status`, `## Architecture`, `## Running`, `## Persistence`, `## Desktop`, `## Custom domain (cPanel → Azure)`. Status leads, before build instructions, because this is a landing page.

### Taken from the public version (`origin/master`)

| Item | Treatment |
|---|---|
| Title `# Ambit` | adopted verbatim |
| `## Status` section (pre-alpha, master is not current, ask for a good branch) | adopted verbatim, including the author's spacing; moved up to sit directly under the description |
| Removal of the `## Ambit` section | applied; the section is gone |

### Kept from the work line

Architecture table rows for **Desktop** (WPF + WebView2, local HTTP proxy) and Npgsql on the **Server** row; the statement that the Client is served under `/ambit`; links to [[doc/arch.md]] and [[doc/api.md]]; the Node.js 18 prerequisite; `npm ci`; `npm run bundle`; the app URL on port 5215 with the `/ambit` path; the `?debug=1` unbundled-module note; the whole **Persistence** section (`Persistence:Mode`, `DB_CONNECTION_STRING`, automatic snapshot and change-log writes, links to [[doc/reference/postgres-environments.md]] and [[doc/current/persistence-model.md]]); the whole **Desktop** section; the **Custom domain** section with links to [[doc/reference/cpanel-transparent-proxy.md]] and [[doc/reference/deploy-azure.md]].

### Stale facts corrected, and how each was verified

| Claim | Where it was wrong | Correction | Verification |
|---|---|---|---|
| App URL port | public version said `http://localhost:5115` at the site root | `http://localhost:5215/ambit` | `applicationUrl` in [[src/Server/Properties/launchSettings.json]] is port 5215 for both profiles; [[scripts/desktop.sh]] sets the local app URL to the same host and `/ambit` path; [[src/Desktop/Desktop.fs]] agrees; the `wait for server` task in [[.vscode/tasks.json]] polls the same URL |
| No npm step at all | public version | `npm ci` and `npm run bundle` retained | [[package.json]] defines only `bundle` and `bundle:watch`; [[package-lock.json]] is present, so `npm ci` is the correct install command |
| VS Code task name | work-line version named a task **desktop: Run**, which does not exist | now names **desktop: Run (cloud)** and **desktop: Run (local)** | [[.vscode/tasks.json]] labels are `desktop: Run (cloud)` and `desktop: Run (local)`; `bash scripts/desktop.sh run` is still a valid action per the script usage text |
| Default build task contents | work-line version said the default task starts "Fable watch and the server", and separately called esbuild watch optional | now says Fable watch, the esbuild bundle watch, and the server | the default task `dev: Watch + Run` in [[.vscode/tasks.json]] depends on `fable: Watch Client`, `esbuild: Watch Client` and `server: Run` |

Facts re-checked and found correct, so carried over unchanged: .NET 10 SDK prerequisite (`net10.0` in [[src/Server/Gambol.Server.fsproj]]); `dotnet tool restore` for Fable ([[.config/dotnet-tools.json]] pins the `fable` tool); Npgsql on the Server ([[src/Server/Gambol.Server.fsproj]] package reference); WPF plus WebView2 on Desktop ([[src/Desktop/Gambol.Desktop.fsproj]]); `/ambit?debug=1` serving unbundled `Program.js` ([[src/Server/RouteRegistration.fs]] reads the `debug` query value and selects `Program.js` instead of `Program.bundle.js`); every wikilink target in the file exists.

Nothing was invented. No feature claim was added beyond what the two versions already stated.

## Task 2 — deletes

[[dockerfile]] and [[.htaccess]] were removed from the working tree with the Delete tool. Neither path exists any more. No `git rm`, no commit.

## Task 3 — cPanel doc pruning

[[doc/reference/cpanel-transparent-proxy.md]]:

- header `See also:` line — dropped the wikilink to the deleted file
- request-flow item 1 — replaced the Apache match expression with a plain statement that Apache matches the `/ambit` path and routes to [[proxy.php]] with the subpath in `path`
- upload instruction — now names [[proxy.php]] only
- the `### .htaccess` subsection and its two configuration bullets — removed, and replaced by a `### Web-server configuration` subsection that states the Apache configuration is kept outside this repository, that the operator holds the current copy, that it installs in the cPanel document root beside [[proxy.php]], and what its two jobs are in plain words

Nothing else in that document changed. The `### proxy.php` subsection, the Azure cooperation section, the redirect-rewriting section and the troubleshooting list are untouched, because [[proxy.php]] stays tracked and those sections describe tracked code.

[[doc/README.md]] — the reference index entry no longer names the deleted file; it reads "custom domain forwarding via cPanel and [[proxy.php]]".

No configuration text, match expression, redirect pattern or path pattern from the deleted file was copied into any file, including this report.

## Verification greps

Whole-tree, case-insensitive, including untracked files.

| Term | Result |
|---|---|
| `htaccess` | two hits, both deliberate (below) |
| `dockerfile` / `Dockerfile` | no hits outside the two report files in this directory |
| `5115` | two hits, both deliberate (below) |

`git grep` over tracked files confirms the same picture, and both deleted paths are absent from the working tree.

### Deliberately left

- [[.github/prompts/plan-workspaceGitRobustness.prompt.md]] line 35 — a historical prompt document that describes the production request path in one line. It publishes no configuration text. It is a record of a past plan, not guidance that points a reader at a live file.
- [[doc/reference/dev-debug-workflow.md]] line 19 — names 5115 explicitly as "the old dev port" in a Windows port-reservation note. The reference is correct as written.
- [[doc/history/mvp.md]] line 167 — a history document that records the MVP acceptance step at the port and path of that time. History documents are allowed to be out of date.

### One item that needs a decision

[[.scratch/git-protocol/reports/public-face-reconciliation.md]] quotes the deleted file's two Apache match expressions in its findings (sections 2 and the recommendation). `.scratch/` is not gitignored, so committing that report publishes the very configuration detail this pass removed from [[doc/reference/cpanel-transparent-proxy.md]]. The file is currently untracked, so nothing is published yet. It was not edited here, because the task scope did not include it. Recommendation: scrub those expressions from that report, or exclude the report from the commit.
