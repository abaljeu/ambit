module Gambol.Server.Tests.TestBackend

open System
open System.IO
open System.Net.Http
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Npgsql
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestDbConfigTests

let private testConnEnv = "TEST_DB_CONNECTION_STRING"

/// Request Cookie from AuthToken. Same helper for empty Auth and named Auth.
let withAuthCookie username password (client: HttpClient) =
    client.DefaultRequestHeaders.Add(
        "Cookie",
        AuthToken.cookieHeaderValue username password)
    client

/// Development boot seed uses deriveToken("",""); request-carried cookie must match.
let withDevelopmentCookie (client: HttpClient) =
    withAuthCookie "" "" client

/// Mailbox Browser Caller for the same AuthToken secret the cookie carries.
let browserCallerFromAuth username password =
    BrowserRequestCreds.callerFromSecret (
        Credential(AuthToken.deriveToken username password))

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

/// Stops the previous server, waits for its threads, then clears the database.
let resetTestDatabase (connStr: string) : Task<unit> =
    ServerTestReset.resetBetweenTests connStr

let newTempDir () =
    let dir = Path.Combine(Path.GetTempPath(), $"gambol-test-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let testAuthority = Authority "Test"

let testSecret = Credential "test-secret"

let testCaller =
    { authority = testAuthority
      name = "test"
      secret = testSecret }

/// Admitted Callers Session copies when the host starts.
let admittedCredentials =
    CoreCredentials.ofCallers (Set.singleton testCaller)

let admittedHostFile (file: FileAgent) =
    CoreMailbox.host
        (CoreActorPool.create ())
        (FileAgent.persist file)
        admittedCredentials

let admittedHostDb (db: DbAgent) =
    CoreMailbox.host
        (CoreActorPool.create ())
        (DbAgent.persist db)
        admittedCredentials

let createAdmittedFile (dataDir: string) =
    let host = CoreMailbox.createFile dataDir admittedCredentials
    let handle = CoreDoor.create true host testCaller
    host, handle

/// Same as createAdmittedFile. Third value is the admitted Browser secret.
let createAdmittedFileWithCredentials (dataDir: string) =
    let host = CoreMailbox.createFile dataDir admittedCredentials
    let handle = CoreDoor.create true host testCaller
    host, handle, testSecret

let admittedChanges (host: MailboxHost) =
    CoreDoor.create true host testCaller

let private cannedHandlers
    (readState: unit -> Result<State, string>)
    (eventsSince: EventId -> Ev list)
    (apply: Ev -> bool -> Result<CoreChangesAccepted, string>)
    : PersistHandlers =
    { getState = readState
      getEventId = fun () ->
          readState () |> Result.map (fun state -> state.eventId)
      getEventsSince = fun after -> Ok(eventsSince after)
      getEventLog =
          fun () ->
              eventsSince EventId.zero
              |> EventLog.adoptNewestHead
              |> Ok
      appendEvent = fun _ -> Ok ()
      applyEvent = apply
      replaceGraph = fun _ _ -> ()
      noteDiffDone = ignore
      noteParsed = ignore
      noteStamped = ignore
      noteWriteFailed = ignore }

let private cannedFilling readState eventsSince apply : PersistFilling =
    { handlers = cannedHandlers readState eventsSince apply
      onError = fun _ _ _ -> ()
      formatError = id
      isReady = fun () -> true
      dispose = ignore
      until = None
      bindPersistFinished = ignore }

/// Mailbox whose reads and apply are supplied by the test.
let hostCanned
    (pool: CoreActorPool)
    (readState: unit -> Result<State, string>)
    (eventsSince: EventId -> Ev list)
    (apply: Ev -> bool -> Result<CoreChangesAccepted, string>)
    : MailboxHost =
    CoreMailbox.host
        pool
        (cannedFilling readState eventsSince apply)
        admittedCredentials

/// Door whose getState and getEventsSince come from the mailbox persist.
let doorForReads
    (readState: unit -> Result<State, string>)
    (eventsSince: EventId -> Ev list)
    : CoreChangesDoor =
    let pool = CoreActorPool.create ()
    let host =
        hostCanned
            pool
            readState
            eventsSince
            (fun _ _ -> Error "unused")
    CoreDoor.create true host testCaller

/// Persist throw becomes the mailbox error reply, with the message in the text.
let doorForThrownRead (message: string) : CoreChangesDoor =
    let readState () = failwith message
    let pool = CoreActorPool.create ()
    let filling =
        { cannedFilling readState (fun _ -> []) (fun _ _ -> Error "unused") with
            formatError = fun operation ->
                $"Internal server error in {operation}: {message}" }
    let host = CoreMailbox.host pool filling admittedCredentials
    CoreDoor.create true host testCaller

/// Mailbox that accepts Change and ActorStop without a File graph.
let acceptingHost (pool: CoreActorPool) : MailboxHost =
    let state =
        { graph = Graph.create ()
          eventId = EventId.zero }
    hostCanned
        pool
        (fun () -> Ok state)
        (fun _ -> [])
        (fun event _ ->
            Ok(CoreChanges.accepted event.id true [ event ] false None))

let private suppressDailyGitSave (dataDir: string) =
    DailyGitSave.writeStamp
        dataDir
        (DailyGitSave.formatUtcDay DateTime.UtcNow)
    |> ignore

/// Auth-disabled factory without cookie — for refuse-without-cookie facts.
let createClientForDirWithoutCookie (tempDir: string) =
    suppressDailyGitSave tempDir
    let connStr = requireDbConnStr ()
    resetTestDatabase connStr |> fun t -> t.GetAwaiter().GetResult()
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

/// HTTP client for a data directory on the test Database.
/// GET `/ambit/state` returns the scoped ROOT bootstrap graph;
/// use `?scope=full` for total-load tests.
/// Carries the development `gambol_auth` cookie (request-carried; no closed-over fallback).
let createClientForDir (tempDir: string) =
    createClientForDirWithoutCookie tempDir |> withDevelopmentCookie

/// HTTP client for a data directory with Auth:Username / Auth:Password (cookie + git PAT).
let createClientForDirWithAuth
    (tempDir: string)
    (username: string)
    (password: string)
    =
    suppressDailyGitSave tempDir
    let connStr = requireDbConnStr ()
    resetTestDatabase connStr |> fun t -> t.GetAwaiter().GetResult()
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
                                "DB_CONNECTION_STRING", connStr
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
        factory.CreateClient() |> withDevelopmentCookie
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
        factory.CreateClient() |> withDevelopmentCookie
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
                                "Persistence:Mode", "mirror"
                                "DB_CONNECTION_STRING", ""
                                "Auth:Username", ""
                                "Auth:Password", ""
                            ]
                        ) |> ignore
                    ) |> ignore
                )
        factory.CreateClient() |> withDevelopmentCookie
    finally
        if isNull priorDb then
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)
        else
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorDb)

let createDbModeWithoutConnectionClient () =
    createDbModeWithoutConnectionClientForDir (newTempDir ())

let eventOps (event: Ev) =
    Ev.ops event |> Option.defaultValue []

let changeEvent commandName (id: EventId) submissionId ops : Ev =
    { id = id
      submissionId = submissionId
      authority = Authority "Browser"
      commandName = commandName
      body = EventBody.Change ops }

/// Persisted changes also append Notice.NodePersisted. These are the other events.
let changesOnly (events: Ev list) =
    events
    |> List.filter (fun event ->
        match event.body with
        | EventBody.Notice _ -> false
        | _ -> true)

/// Poll the event log until `ready` is true, or two seconds pass.
let waitForNotices
    (read: unit -> Task<Ev list>)
    (ready: Ev list -> bool)
    =
    let timeout = TimeSpan.FromSeconds(2.0)
    let start = DateTime.UtcNow
    let rec poll () = task {
        let! events = read ()
        if ready events || (DateTime.UtcNow - start) >= timeout then
            return events
        else
            do! Task.Delay 20
            return! poll ()
    }
    poll ()

let wireEvent submissionId ops : Ev =
    { id = EventId.zero
      submissionId = submissionId
      authority = Authority ""
      commandName = ""
      body = EventBody.Change ops }

let applyChange (event: Ev) (state: State) =
    ChangeValidation.applyOps (eventOps event) state

let addRootChildEvent text : NodeId * Ev =
    let childId = NodeId.New()
    childId,
    changeEvent
        ""
        EventId.zero
        (Guid.NewGuid())
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ]
