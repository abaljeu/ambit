module Gambol.Server.Tests.DbAgentFailureTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode

let private changedBody () =
    let childId = NodeId.New()
    let change =
        {
            id = 0
            changeId = Guid.NewGuid()
            ops =
                [
                    Op.NewNode(childId, "failure probe")
                    Op.Replace(
                        Graph.rootId,
                        0,
                        [],
                        [ { ref = Ownership.Owner; id = childId } ])
                ]
        }
    Encode.toString 0 (
        Serialization.encodeChangeBatch { changes = [ change ] })

let private freshState () : State =
    { graph = Graph.create ()
      history = History.empty
      revision = Revision 0 }

/// Reproduces the wedged-mailbox bug: an uncaught exception thrown from the live-persist
/// step (e.g. the real IndexOutOfRangeException surfaced via DocumentPersistence.persistGraphOps
/// -> OutlineDocumentWarm) must not kill the DbAgent mailbox loop. The specific pending
/// reply must get an Error, the exception must be logged, and the mailbox must keep
/// serving subsequent requests.
[<Fact>]
let ``persistence exception is logged replied and mailbox survives`` () = task {
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let throwingPersist : string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string> =
        fun _ _ _ _ ->
            raise (InvalidOperationException("injected persistence failure"))
    let agent =
        DbAgent.createForTestWithDependencies
            (freshState ())
            (Some dataDir)
            throwingPersist
            (fun _ -> Ok [])
    let! postResult =
        DbAgent.postChange agent (changedBody ())
        |> Async.StartAsTask
        |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
    match postResult with
    | Ok _ -> Assert.Fail("Expected persistence failure.")
    | Error error ->
        Assert.Contains("Internal server error in DbAgent PostChange", error)
        Assert.Contains($"(dataDir={dataDir})", error)

    let log = IO.File.ReadAllText logPath
    Assert.Contains("EXCEPTION source=DbAgent operation=PostChange", log)
    Assert.Contains("context=bodyLength=", log)
    Assert.Contains("type=System.InvalidOperationException", log)
    Assert.Contains("message=injected persistence failure", log)
    Assert.Contains("stack=", log)

    let! stateJson =
        DbAgent.getState agent
        |> Async.StartAsTask
        |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
    Assert.Contains("\"revision\":0", stateJson)
}
