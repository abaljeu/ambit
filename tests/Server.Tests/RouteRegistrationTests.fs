module Gambol.Server.Tests.RouteRegistrationTests

open System.IO
open Xunit
open Gambol.Server
open Gambol.Server.Tests.TestBackend

/// Writes a valid nested workspace, then makes the workspace's own directory
/// listing unreadable so `DocumentLoader.tryLoadState` genuinely errors (not
/// a silent skip, as an oversized artifact gets in `readDirectoryFileTexts`).
let private dataDirWithUnreadableWorkspaceListing () =
    let dataDir = newTempDir ()
    let graph, _, _, _, _ =
        DocumentPersistenceTests.Fixtures.graphWithNestedDocs ()
    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> DocumentPersistenceTests.Fixtures.requireOk "write"
    |> ignore
    let homeAmbPath = Path.Combine(dataDir, "home", ".amb")
    File.SetUnixFileMode(homeAmbPath, UnixFileMode.None)
    dataDir, homeAmbPath

let private restoreReadable (path: string) =
    File.SetUnixFileMode(path, UnixFileMode.UserRead ||| UnixFileMode.UserWrite)

[<Fact>]
let ``an unreadable workspace listing makes FileAgent.create throw`` () =
    let dataDir, homeAmbPath = dataDirWithUnreadableWorkspaceListing ()
    try
        let ex = Assert.Throws<exn>(fun () -> FileAgent.create dataDir |> ignore)
        Assert.Contains("is denied", ex.Message)
    finally
        restoreReadable homeAmbPath

[<Fact>]
let ``composing routes over that failure logs a STARTUP-EXCEPTION entry`` () =
    let dataDir, homeAmbPath = dataDirWithUnreadableWorkspaceListing ()
    try
        let thrown =
            try
                createDbModeWithoutConnectionClientForDir dataDir |> ignore
                None
            with ex -> Some ex
        Assert.True(thrown.IsSome, "expected composing the app to throw")

        let text = File.ReadAllText(ErrorLog.logPath dataDir)
        Assert.Contains("STARTUP-EXCEPTION", text)
        Assert.Contains("RouteRegistration.registerPersistenceAndRoutes", text)
        Assert.Contains("is denied", text)
    finally
        restoreReadable homeAmbPath
