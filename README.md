# Gambol

A Workflowy-style outline editor built with full-stack F#. Tree-structured text outlines with editing, indentation, persistence, and sync.

## Architecture

| Layer  | Technology                          |
|--------|-------------------------------------|
| Client | F# compiled to JavaScript via Fable |
| Server | ASP.NET Core (minimal API)          |
| Shared | Pure F# domain model                |
| Tests  | xUnit                               |

The **Shared** project contains the domain model and is referenced by both client and server. The **Client** project is compiled from F# to JavaScript using Fable and served as static files. The **Server** project is an ASP.NET Core app that serves the client and exposes an HTTP API.

## Ambit
ambit/ contains a different implementation with slightly different semantics.
Notable differences: 
- a multi-column definition scheme
- the backlink scheme is not the same
- the implementation architecture is different.



Never modify anything in ambit.
Do not assume ambit's code is definitive for gambol behavior.
Do reference ambit for proposing definitions of gambol behavior.

## Running

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Load the environment

dotnet tool restore (pull in fable and other dependencies)

### Build
Use commands like these

dotnet build gambol.sln
dotnet fable src/Client --outDir src/Server/wwwroot
dotnet fable watch src/Client --outDir src/Server/wwwroot
dotnet run --project src/Server
    The app will be available at **http://localhost:5115**.
dotnet test gambol.sln

### Dev (VS Code)
Run the default build task (`Ctrl+Shift+B`) to start Fable watch and the server together.
Both use the correct `--outDir` so `fable_modules` lands in `wwwroot` alongside the compiled JS.
