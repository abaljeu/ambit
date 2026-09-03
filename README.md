# Ambit

A Workflowy-style outline editor built with full-stack F#. Tree-structured text outlines with editing, indentation, persistence, and sync.

## Status
The code is pre-alpha in very active development.  The master branch is not current.  If you want to try the code, ask and I'll point to a good place you can use.

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

## Running

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 18 or later

### Load the environment

dotnet tool restore (pull in fable and other dependencies)
npm ci (once: install pinned esbuild; only again if `package-lock.json` changes)

### Build

Use commands like these:

```bash
dotnet build gambol.sln
dotnet fable src/Client --outDir src/Server/wwwroot
npm run bundle
dotnet fable watch src/Client --outDir src/Server/wwwroot
dotnet run --project src/Server
```

The app will be available at **http://localhost:5215/ambit** (not the site root).

```bash
dotnet test gambol.sln
```

### Dev (VS Code)

Run the default build task (`Ctrl+Shift+B`) to start Fable watch, the esbuild bundle watch, and the server together. Fable writes modules into `wwwroot`; run `npm run bundle` when you need a fresh `Program.bundle.js` outside that task. Open `/ambit?debug=1` to load unbundled modules when debugging.

## Persistence

- **`Persistence:Mode`** — `db` (default): PostgreSQL is authority; `file`: on-disk snapshot + `.log` is authority (optional DB mirror).
- **`DB_CONNECTION_STRING`** — required for `db` mode; optional in `file` mode for mirroring/seed.
- Snapshots and change logs are written automatically after accepted changes (no `POST /save`).

See [[doc/reference/postgres-environments.md]] for local dev and Azure Flexible Server setup.
See [[doc/current/persistence-model.md]] for schema and mode rules.

## Desktop

Optional shell for local filesystem import while using the same web UI against the cloud server.

```bash
scripts/desktop.sh run
```

Or VS Code task **desktop: Run (cloud)** / **desktop: Run (local)** (see [[.vscode/tasks.json]]).

## Custom domain (cPanel → Azure)

Production uses a PHP transparent proxy on the custom domain; JS/CSS load directly from Azure. Details: [[doc/reference/cpanel-transparent-proxy.md]]. Azure deploy: [[doc/reference/deploy-azure.md]].
