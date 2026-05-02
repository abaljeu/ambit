module Gambol.Server.Tests.TestBackend

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Gambol.Server
open Gambol.Server.Tests.TestDbConfigTests

type BackendKind = File | Db

let private testConnEnv = "TEST_DB_CONNECTION_STRING"

/// Resolves the DB connection string, or fails with a clear error message.
/// DB tests are not skippable — configure TEST_DB_CONNECTION_STRING to run them.
let requireDbConnStr () =
    match TestDbConfig.resolveFrom
            (fun () -> Environment.GetEnvironmentVariable(testConnEnv) |> Option.ofObj)
            AppContext.BaseDirectory with
    | Some s -> s
    | None ->
        failwith (
            $"DB tests require {testConnEnv} env var " +
            "or DB_CONNECTION_STRING in appsettings.Development.json.")

let resetTestDatabase (connStr: string) : Task<unit> =
    task {
        do! Database.initSchema connStr |> Async.AwaitTask
        use conn = new Npgsql.NpgsqlConnection(connStr)
        do! conn.OpenAsync()
        use cmd = conn.CreateCommand()
        cmd.CommandText <-
            "TRUNCATE TABLE changes, node_children, nodes, graph RESTART IDENTITY CASCADE;"
        let! _ = cmd.ExecuteNonQueryAsync()
        return ()
    }

let newTempDir () =
    let dir = Path.Combine(Path.GetTempPath(), $"gambol-test-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

/// Create a test client pointing at the given data directory (file backend, no DB).
let createClientForDir (tempDir: string) =
    let priorDb = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    try
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)
        let factory =
            (new WebApplicationFactory<Program>())
                .WithWebHostBuilder(fun builder ->
                    builder.ConfigureAppConfiguration(fun _ config ->
                        config.AddInMemoryCollection(
                            dict [
                                "DataDir", tempDir
                                "DB_CONNECTION_STRING", ""
                                "Auth:Username", ""
                                "Auth:Password", ""
                            ]
                        ) |> ignore
                    ) |> ignore
                )
        factory.CreateClient()
    finally
        if isNull priorDb then
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)
        else
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorDb)

/// Create a test client with a fresh empty temp dir (file backend).
let createFileClient () = newTempDir () |> createClientForDir

/// Create a test client using the DB backend.
/// Caller must have already called resetTestDatabase before creating the client.
let createDbClient (connStr: string) =
    DatabaseSetup.resetAgentCacheForTest ()
    let priorDb = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    try
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", connStr)
        let factory =
            (new WebApplicationFactory<Program>())
                .WithWebHostBuilder(fun builder ->
                    builder.ConfigureAppConfiguration(fun _ config ->
                        config.AddInMemoryCollection(
                            dict [
                                "DataDir", newTempDir ()
                                "DB_CONNECTION_STRING", connStr
                                "Auth:Username", ""
                                "Auth:Password", ""
                            ]
                        ) |> ignore
                    ) |> ignore
                )
        factory.CreateClient()
    finally
        if isNull priorDb then
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)
        else
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorDb)

/// Like createDbClient but clears the DB agent cache first, simulating a process restart.
/// Use when testing server behaviour after restart without resetting the database.
let createDbClientNoReset (connStr: string) =
    DatabaseSetup.resetAgentCacheForTest ()
    createDbClient connStr
