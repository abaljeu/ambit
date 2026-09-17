module OpListApplyTests

open System
open Gambol.Shared
open Xunit

let private browser = Authority "Browser"

let private event commandName body : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = browser
      commandName = commandName
      body = body }

let private textState () : State * NodeId =
    let graph, ids =
        ModelBuilder.createNodes [ "old" ] (Graph.create ())
    { graph = graph; eventId = EventId.zero }, List.head ids

[<Fact>]
let ``Ev.apply applies EventBody Change ops`` () =
    let state, nodeId = textState ()
    let ops = [ Op.SetText(nodeId, "old", "new") ]
    let ev = event "Edit node" (EventBody.Change ops)
    match Ev.apply ev state with
    | ApplyResult.Changed after ->
        Assert.Equal("new", after.graph.nodes.[nodeId].text)
    | other -> failwithf "expected Changed, got %A" other

[<Fact>]
let ``Ev.inverseOps inverts EventBody ops and drops NewNode`` () =
    let nodeId = NodeId.New()
    let created = NodeId.New()
    let ops =
        [ Op.NewNode(created, "x")
          Op.SetText(nodeId, "old", "new") ]
    let ev = event "Edit" (EventBody.Change ops)
    match Ev.inverseOps ev with
    | Some inverse ->
        Assert.Equal<Op list>([ Op.SetText(nodeId, "new", "old") ], inverse)
    | None -> failwith "expected inverse ops"

[<Fact>]
let ``Change.apply still applies leftover Change`` () =
    let state, nodeId = textState ()
    let change =
        { id = EventId.fromJson 0
          submissionId = Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "old", "new") ] }
    match Change.apply change state with
    | ApplyResult.Changed after ->
        Assert.Equal("new", after.graph.nodes.[nodeId].text)
    | other -> failwithf "expected Changed, got %A" other

[<Fact>]
let ``ChangeValidation.applyOps applies an Op list`` () =
    let state, nodeId = textState ()
    let ops = [ Op.SetText(nodeId, "old", "new") ]
    match ChangeValidation.applyOps ops state with
    | ApplyResult.Changed after ->
        Assert.Equal("new", after.graph.nodes.[nodeId].text)
    | other -> failwithf "expected Changed, got %A" other

[<Fact>]
let ``ChangeValidation.applyOps empty list is Unchanged`` () =
    let state = { graph = Graph.create (); eventId = EventId.zero }
    match ChangeValidation.applyOps [] state with
    | ApplyResult.Unchanged after -> Assert.Equal(state.graph, after.graph)
    | other -> failwithf "expected Unchanged, got %A" other

[<Fact>]
let ``ChangeAmendment.applyOps amends stale SetText collision`` () =
    let state, nodeId = textState ()
    let first = [ Op.SetText(nodeId, "old", "xA") ]
    let state =
        match ChangeAmendment.applyOps first state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"first: {other}"
    let stale = [ Op.SetText(nodeId, "old", "xB") ]
    let result, amended, applied = ChangeAmendment.applyOps stale state
    Assert.True(amended)
    Assert.NotEqual<Op list>(stale, applied)
    match result with
    | ApplyResult.Changed st ->
        Assert.Equal("xA", st.graph.nodes.[nodeId].text)
        Assert.Equal(1, st.graph.nodes.[nodeId].children.Length)
    | _ -> failwith $"stale result: {result}"

[<Fact>]
let ``PersistStamp.appendToLastEvent appends stamp ops onto last Ev`` () =
    let nodeId = NodeId.New()
    let stamp = DateTime(2026, 7, 22, 15, 30, 0, DateTimeKind.Utc)
    let first = event "A" (EventBody.Change [])
    let second =
        event "B" (EventBody.Change [ Op.SetText(nodeId, "old", "new") ])
    let stampOps =
        [ Op.SetUpdateTime(nodeId, DateTime.MinValue, stamp) ]
    match PersistStamp.appendToLastEvent [ first; second ] stampOps with
    | [ a; b ] ->
        Assert.Equal(first.body, a.body)
        match b.body with
        | EventBody.Change ops ->
            Assert.Equal<Op list>(
                [ Op.SetText(nodeId, "old", "new")
                  Op.SetUpdateTime(nodeId, DateTime.MinValue, stamp) ],
                ops)
        | other -> failwithf "expected Change body, got %A" other
    | other -> failwithf "expected two events, got %A" other

[<Fact>]
let ``PersistStamp.appendToLast leftover Change still compiles`` () =
    let change =
        { id = EventId.fromJson 0
          submissionId = Guid.NewGuid()
          ops = [] }
    let stamp =
        [ Op.SetUpdateTime(NodeId.New(), DateTime.MinValue, DateTime.UtcNow) ]
    match PersistStamp.appendToLast [ change ] stamp with
    | [ stamped ] -> Assert.Equal(1, stamped.ops.Length)
    | other -> failwithf "expected one change, got %A" other
