# Gambol

A Workflowy-style outline editor built with full-stack F#. Tree-structured text outlines with editing, indentation, persistence, and sync.

## Architecture

| Layer      | Technology                          |
|------------|-------------------------------------|
| Client     | F# compiled to JavaScript via Fable |
| Server     | ASP.NET Core (minimal API), Npgsql  |
| Shared     | Pure F# domain model                |
| Desktop    | WPF + WebView2, local HTTP proxy    |
| Tests      | xUnit                               |

The **Shared** project contains the domain model and is referenced by both client and server. The **Client** project is compiled from F# to JavaScript using Fable and served as static files under `/ambit`. The **Server** project is an ASP.NET Core app that serves the client and exposes the HTTP API.

Full layer diagram, sync, and persistence modes: [[doc/arch.md]].

HTTP contract (implemented `/ambit/*` routes): [[doc/api.md]].

## Persistence

- **`Persistence:Mode`** — `db` (default): PostgreSQL is authority; `file`: on-disk snapshot + `.log` is authority (optional DB mirror).
- **`DB_CONNECTION_STRING`** — required for `db` mode; optional in `file` mode for mirroring/seed.
- Snapshots and change logs are written automatically after accepted changes (no `POST /save`).

See [[doc/future/postgres-environments.md]] for local dev and Azure Flexible Server setup.

## Desktop

Optional shell for local filesystem import while using the same web UI against the cloud server.

```bash
bash scripts/desktop.sh run
```

Or VS Code task **desktop: Run** (see `.vscode/tasks.json`).

## Ambit

`ambit/` contains a different implementation with slightly different semantics.

Notable differences:

- a multi-column definition scheme
- the backlink scheme is not the same
- the implementation architecture is different

Never modify anything in ambit.
Do not assume ambit's code is definitive for gambol behavior.
Do reference ambit for proposing definitions of gambol behavior.

## Running

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Load the environment

dotnet tool restore (pull in fable and other dependencies)

### Build

Use commands like these:

```bash
dotnet build gambol.sln
dotnet fable src/Client --outDir src/Server/wwwroot
dotnet fable watch src/Client --outDir src/Server/wwwroot
dotnet run --project src/Server
```

The app will be available at **http://localhost:5115/ambit** (not the site root).

```bash
dotnet test gambol.sln
```

### Dev (VS Code)

Run the default build task (`Ctrl+Shift+B`) to start Fable watch and the server together.
Both use the correct `--outDir` so `fable_modules` lands in `wwwroot` alongside the compiled JS.

### Custom domain (PHP proxy) with JS/CSS loaded directly from Azure

Fable emits **relative** `import` paths (e.g. `./fable_modules/...`). The browser resolves those against the URL of **`Program.js`**. If `Program.js` is loaded as `https://your-domain/ambit/Program.js`, every chunk stays on your domain (including through a PHP proxy). To load the module graph from Azure, the HTML must point `Program.js` (and CSS) at the Azure origin.

- **Template file** is `src/Server/wwwroot/gambol.template.html` (not `gambol.html`). A file named `gambol.html` under `wwwroot` would be served as a **static** `/ambit/gambol.html` with **no** URL rewrites — a common reason everything still hits the proxy domain.
- **`PublicAssetBase`** — optional override: origin only, no trailing slash (e.g. `https://yourapp.azurewebsites.net`). On **Azure App Service** in **Production**, if this is unset or empty, it **defaults to `https://` + `WEBSITE_HOSTNAME`**, so rewritten asset URLs target the app host without extra JSON.
- **`JsModuleCorsOrigins`** — comma-separated allowed `Origin` values for cross-origin ES modules. If **empty** but a public asset base is in effect (configured or auto), the server defaults to **`*`** for `/ambit/*.js` so a proxied HTML origin can load the module graph.

Deployment: [[doc/deploy-azure.md]], [[doc/deploy-wordpress.md]].
