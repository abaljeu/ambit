module Gambol.Server.Tests.DbAgentNoticeTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private fail label detail =
    Assert.Fail($"{label}: {detail}")
    Unchecked.defaultof<_>

let private postFile (agent: DbAgent) = task {
    let mailbox = admittedHostDb agent
    let id = NodeId.New()
    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body =
            EventBody.Change
                [ Op.NewSpecialNode(id, File, "note.txt") ] }
    let! posted =
        CoreDoor.postEvents (admittedChanges mailbox) [ change ]
        |> Async.StartAsTask
    match posted with
    | Error err -> return fail "post" err
    | Ok _ -> return mailbox, id
}

let private apply (mailbox: MailboxHost) (notice: Notice) = task {
    CoreMailboxApi.postNotice mailbox notice
    let! state = CoreMailboxApi.getState mailbox |> Async.StartAsTask
    match state with
    | Error err -> fail "state" err
    | Ok _ -> ()
}

let private readNode (connStr: string) (id: NodeId) = task {
    let! loaded =
        Database.tryLoadGraphFromProjection connStr |> Async.AwaitTask
    match loaded with
    | Error err -> return fail "db" err
    | Ok (graph, _) ->
        match Map.tryFind id graph.nodes with
        | None -> return fail "row" "node missing from database"
        | Some node -> return node
}

let private freshFile () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let! mailbox, id = postFile agent
    let! node = readNode connStr id
    return connStr, mailbox, id, node
}

[<Fact>]
let ``NodeParsed writes parse state to the database`` () = task {
    let! connStr, mailbox, id, before = freshFile ()
    do! apply mailbox (Notice.NodeParsed id)
    let! after = readNode connStr id
    Assert.Equal(ParseState.Parsed, after.documentState.parseState)
    Assert.Equal(
        before.documentState.persistState,
        after.documentState.persistState)
    Assert.Equal(before.updateTime, after.updateTime)
}

[<Fact>]
let ``MarkUnparsed writes parse state to the database`` () = task {
    let! connStr, mailbox, id, _ = freshFile ()
    do! apply mailbox (Notice.NodeParsed id)
    let! mid = readNode connStr id
    do! apply mailbox (Notice.MarkUnparsed id)
    let! after = readNode connStr id
    Assert.Equal(ParseState.Unparsed, after.documentState.parseState)
    Assert.Equal(
        mid.documentState.persistState,
        after.documentState.persistState)
    Assert.Equal(mid.updateTime, after.updateTime)
}

[<Fact>]
let ``NodePersisting writes persist state to the database`` () = task {
    let! connStr, mailbox, id, before = freshFile ()
    do! apply mailbox (Notice.NodePersisting id)
    let! after = readNode connStr id
    Assert.Equal(PersistState.Unpersisted, after.documentState.persistState)
    Assert.Equal(
        before.documentState.parseState,
        after.documentState.parseState)
    Assert.Equal(before.updateTime, after.updateTime)
}

[<Fact>]
let ``NodePersisted writes persist state to the database`` () = task {
    let! connStr, mailbox, id, _ = freshFile ()
    do! apply mailbox (Notice.NodePersisting id)
    let! mid = readNode connStr id
    let stamp = mid.updateTime.AddHours(3.0)
    do! apply mailbox (Notice.NodePersisted(id, stamp))
    let! after = readNode connStr id
    Assert.Equal(PersistState.Persisted, after.documentState.persistState)
    Assert.Equal(
        mid.documentState.parseState,
        after.documentState.parseState)
    Assert.Equal(NodeUpdateTime.toDbPrecision stamp, after.updateTime)
}

[<Fact>]
let ``NodeStamped writes the stamped time to the database`` () = task {
    let! connStr, mailbox, id, before = freshFile ()
    let stamp = before.updateTime.AddHours(2.0)
    let notice =
        Notice.NodeStamped(
            [ Op.SetUpdateTime(id, before.updateTime, stamp) ],
            Some "saved")
    do! apply mailbox notice
    let! after = readNode connStr id
    Assert.Equal(NodeUpdateTime.toDbPrecision stamp, after.updateTime)
    Assert.Equal(
        before.documentState.parseState,
        after.documentState.parseState)
    Assert.Equal(
        before.documentState.persistState,
        after.documentState.persistState)
}

[<Fact>]
let ``NodeReceived writes the received node to the database`` () = task {
    let! connStr, mailbox, id, _ = freshFile ()
    do! apply mailbox (Notice.NodeParsed id)
    do! apply mailbox (Notice.NodePersisting id)
    let! mid = readNode connStr id
    do! apply mailbox (Notice.NodeReceived id)
    let! after = readNode connStr id
    Assert.Equal(ParseState.Unparsed, after.documentState.parseState)
    Assert.Equal(PersistState.Persisted, after.documentState.persistState)
    Assert.Equal("note.txt", after.text)
    Assert.NotEqual(mid.updateTime, after.updateTime)
}
