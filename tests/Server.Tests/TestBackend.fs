module Gambol.Server.Tests.TestBackend

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Npgsql
open Gambol.Server
open Gambol.Server.Tests.TestDbConfigTests

type BackendKind = File | Db

let private testConnEnv = "TEST_DB_CONNECTION_STRING"

let private quoteIdentifier (identifier: string) =
    "\"" + identifier.Replace("\"", "\"\"") + "\""

let private ensureTestDatabaseExists (connStr: string) =
    task {
        let builder = NpgsqlConnectionStringBuilder(connStr)
        let database = builder.Database

        if String.IsNullOrWhiteSpace(database) then
            failwith "DB tests require a connection string with a Database value."

        let adminBuilder = NpgsqlConnectionStringBuilder(connStr)
        adminBuilder.Database <- "postgres"

        use conn = new NpgsqlConnection(adminBuilder.ConnectionString)
        do! conn.OpenAsync()

        use existsCmd = conn.CreateCommand()
        existsCmd.CommandText <- "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @name);"
        existsCmd.Parameters.AddWithValue("name", database) |> ignore
        let! existsObj = existsCmd.ExecuteScalarAsync()
        let exists = existsObj :?> bool

        if not exists then
            use createCmd = conn.CreateCommand()
            createCmd.CommandText <- "CREATE DATABASE " + quoteIdentifier database + ";"
            let! _ = createCmd.ExecuteNonQueryAsync()
            return ()
    }
    |> fun t -> t.GetAwaiter().GetResult()

let setDatabaseAllowConnections (connStr: string) (allowConnections: bool) : Task<unit> =
    task {
        let builder = NpgsqlConnectionStringBuilder(connStr)
        let database = builder.Database

        if String.IsNullOrWhiteSpace(database) then
            failwith "DB tests require a connection string with a Database value."

        let adminBuilder = NpgsqlConnectionStringBuilder(connStr)
        adminBuilder.Database <- "postgres"

        use conn = new NpgsqlConnection(adminBuilder.ConnectionString)
        do! conn.OpenAsync()

        if not allowConnections then
            use terminateCmd = conn.CreateCommand()
            terminateCmd.CommandText <-
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @database AND pid <> pg_backend_pid();
                """
            terminateCmd.Parameters.AddWithValue("database", database) |> ignore
            let! _ = terminateCmd.ExecuteNonQueryAsync()
            ()

        use allowCmd = conn.CreateCommand()
        allowCmd.CommandText <-
            "ALTER DATABASE " + quoteIdentifier database +
            " WITH ALLOW_CONNECTIONS " + string allowConnections + ";"
        let! _ = allowCmd.ExecuteNonQueryAsync()
        return ()
    }

/// Resolves the DB connection string, or fails with a clear error message.
/// DB tests are not skippable. If TEST_DB_CONNECTION_STRING is unset, use a
/// sibling test DB derived from appsettings.Development.json.
let requireDbConnStr () =
    match TestDbConfig.resolveFrom
            (fun () -> Environment.GetEnvironmentVariable(testConnEnv) |> Option.ofObj)
            AppContext.BaseDirectory with
    | Some s ->
        ensureTestDatabaseExists s
        s
    | None ->
        failwith (
            $"DB tests require {testConnEnv} env var " +
            "or DB_CONNECTION_STRING in appsettings.Development.json. " +
            "When using appsettings.Development.json, tests derive a sibling *_test database.")

let resetTestDatabase (connStr: string) : Task<unit> =
    task {
        do! Database.initSchema connStr |> Async.AwaitTask
        use conn = new NpgsqlConnection(connStr)
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

let private suppressDailyGitSave (dataDir: string) =
    DailyGitSave.writeStamp
        dataDir
        (DailyGitSave.formatUtcDay DateTime.UtcNow)
    |> ignore

/// Create a test client pointing at the given data directory (file backend, no DB).
/// GET `/ambit/state` returns the scoped ROOT bootstrap graph; use `?scope=full` for total-load tests.
let createClientForDir (tempDir: string) =
    suppressDailyGitSave tempDir
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
                                "Persistence:Mode", "file"
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

/// File-backend client with Auth:Username / Auth:Password set (cookie + git PAT).
let createClientForDirWithAuth
    (tempDir: string)
    (username: string)
    (password: string)
    =
    suppressDailyGitSave tempDir
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
                                "Persistence:Mode", "file"
                                "DB_CONNECTION_STRING", ""
                                "Auth:Username", username
                                "Auth:Password", password
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

let createDbClientForDir (connStr: string) (tempDir: string) =
    suppressDailyGitSave tempDir
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
                                "DataDir", tempDir
                                "Persistence:Mode", "Db"
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

let createFileModeWithDbClientForDir (connStr: string) (tempDir: string) =
    suppressDailyGitSave tempDir
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
                                "DataDir", tempDir
                                "Persistence:Mode", "file"
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

/// Create a test client using the DB backend.
/// Caller must have already called resetTestDatabase before creating the client.
let createDbClient (connStr: string) =
    createDbClientForDir connStr (newTempDir ())

/// Like createDbClient but clears the DB agent cache first, simulating a process restart.
/// Use when testing server behaviour after restart without resetting the database.
let createDbClientNoReset (connStr: string) =
    DatabaseSetup.resetAgentCacheForTest ()
    createDbClient connStr

let createDbModeWithoutConnectionClientForDir (tempDir: string) =
    suppressDailyGitSave tempDir
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
                                "Persistence:Mode", "Db"
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

let createDbModeWithoutConnectionClient () =
    createDbModeWithoutConnectionClientForDir (newTempDir ())
