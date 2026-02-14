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

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Load the environment

dotnet tool restore (pull in fable and other dependencies)

### Build and run
Use commands like these

dotnet build gambol.sln
dotnet fable src/Client
dotnet fable watch src/Client
dotnet run --project src/Server
    The app will be available at **http://localhost:5115**.
dotnet test gambol.sln
