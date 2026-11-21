module Gambol.Shared.Tests.ChangeAmendmentTests

open System
open Xunit
open Gambol.Shared

let private applyFirst ops state =
    match ChangeAmendment.applyOps ops state with
    | ApplyResult.Changed st, false, _ -> st
    | other -> failwith $"first: {other}"

let private postedEvent commandName ops =
    { id = EventId.fromJson 5
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = commandName
      body = EventBody.Change ops }

let private applyPosted graphOnly commandName ops state =
    let event = postedEvent commandName ops
    let result, amended, applied =
        ChangeAmendment.applyPosted graphOnly event.id event ops state
    result, amended, applied, event

let private assertStoredDiffers
    (posted: Op list)
    (applied: Op list)
    (event: Ev)
    =
    Assert.NotEqual<Op list>(posted, applied)
    let stored = { event with body = EventBody.Change applied }
    let postedOps: Op list option = Ev.ops event
    let storedOps: Op list option = Ev.ops stored
    Assert.NotEqual(postedOps, storedOps)

let private stateWithChild (text: string) =
    let initialState =
        { graph = Graph.create ()
          eventId = EventId.zero }

    let childId = NodeId.New()
    let ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ]

    match ChangeValidation.applyOps ops initialState with
    | ApplyResult.Changed st -> st, childId
    | _ -> failwith "bootstrap failed"

[<Fact>]
let ``applyOps amends stale SetText collision`` () =
    let state, nodeId = stateWithChild "x0"
    let opsA = [ Op.SetText(nodeId, "x0", "xA") ]
    let state =
        match ChangeAmendment.applyOps opsA state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"changeA: {other}"

    let opsB = [ Op.SetText(nodeId, "x0", "xB") ]
    let result, amended, applied = ChangeAmendment.applyOps opsB state
    Assert.True(amended)
    Assert.NotEqual<Op list>(opsB, applied)
    match result with
    | ApplyResult.Changed st ->
        Assert.Equal("xA", st.graph.nodes.[nodeId].text)
        Assert.Equal(1, (Graph.children st.graph nodeId).Length)
    | _ -> failwith $"changeB result: {result}"

[<Fact>]
let ``applyOps amends stale SetClasses with set delta`` () =
    let state, nodeId = stateWithChild "tagged"
    let prior = CssClass.ofList [ "a"; "b" ]
    let setup = [ Op.SetClasses(nodeId, CssClass.empty, prior) ]
    let state =
        match ChangeAmendment.applyOps setup state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"setup: {other}"

    let opsA = [ Op.SetClasses(nodeId, prior, CssClass.ofList [ "b" ]) ]
    let state =
        match ChangeAmendment.applyOps opsA state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"changeA: {other}"

    let opsB =
        [ Op.SetClasses(nodeId, prior, CssClass.ofList [ "a"; "b"; "c" ]) ]
    let result, amended, applied = ChangeAmendment.applyOps opsB state
    Assert.True(amended)
    Assert.NotEqual<Op list>(opsB, applied)
    match result with
    | ApplyResult.Changed st ->
        let classes = st.graph.nodes.[nodeId].cssClasses |> CssClass.toList |> Set.ofList
        Assert.Equal<Set<string>>(Set.ofList [ "b"; "c" ], classes)
    | _ -> failwith $"changeB result: {result}"

let private stateWithParentChild (parentText: string) (childText: string) =
    let initialState =
        { graph = Graph.create ()
          eventId = EventId.zero }

    let parentId = NodeId.New()
    let childId = NodeId.New()
    let ops =
        [ Op.NewNode(parentId, parentText)
          Op.NewNode(childId, childText)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner parentId ])
          Op.Replace(parentId, [], [ ChildNode.owner childId ]) ]

    match ChangeValidation.applyOps ops initialState with
    | ApplyResult.Changed st -> st, parentId, childId
    | _ -> failwith "bootstrap failed"

[<Fact>]
let ``applyOps amends stale Replace collision`` () =
    let state, parentId, child0 = stateWithParentChild "p" "c0"
    let childA = NodeId.New()
    let opsA =
        [ Op.NewNode(childA, "a")
          Op.Replace(
              parentId,
              [ ChildNode.owner child0 ],
              [ ChildNode.owner childA ]) ]
    let state =
        match ChangeAmendment.applyOps opsA state with
        | ApplyResult.Changed st, false, _ -> st
        | other -> failwith $"changeA: {other}"

    let childB = NodeId.New()
    let opsB =
        [ Op.NewNode(childB, "b")
          Op.Replace(
              parentId,
              [ ChildNode.owner child0 ],
              [ ChildNode.owner childB ]) ]
    let result, amended, applied = ChangeAmendment.applyOps opsB state
    Assert.True(amended)
    Assert.NotEqual<Op list>(opsB, applied)
    match result with
    | ApplyResult.Changed st ->
        let children = Graph.children st.graph parentId
        Assert.Equal(2, children.Length)
        Assert.Contains(ChildNode.owner childA, children)
        Assert.Contains(ChildNode.owner childB, children)
    | _ -> failwith $"changeB result: {result}"

let private stateWithNamed (name: string) =
    let initialState =
        { graph = Graph.create ()
          eventId = EventId.zero }
    let childId = NodeId.New()
    let ops =
        [ Op.NewNode(childId, "body")
          Op.SetName(childId, "", name)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ]
    match ChangeValidation.applyOps ops initialState with
    | ApplyResult.Changed st -> st, childId
    | _ -> failwith "bootstrap failed"

[<Fact>]
let ``applyPosted amended SetText stores ops that differ from posted`` () =
    let state, nodeId = stateWithChild "x0"
    let state = applyFirst [ Op.SetText(nodeId, "x0", "xA") ] state
    let posted = [ Op.SetText(nodeId, "x0", "xB") ]
    let doors =
        [ false, "Edit"
          true, "Edit"
          false, "Parse"
          true, "Parse" ]
    for graphOnly, commandName in doors do
        let result, amended, applied, event =
            applyPosted graphOnly commandName posted state
        Assert.True(amended)
        assertStoredDiffers posted applied event
        match result with
        | ApplyResult.Changed _ -> ()
        | other -> failwith $"result: {other}"

[<Fact>]
let ``applyPosted amended SetName stores ops that differ from posted`` () =
    let state, nodeId = stateWithNamed "alpha"
    let state = applyFirst [ Op.SetName(nodeId, "alpha", "nameA") ] state
    let posted = [ Op.SetName(nodeId, "alpha", "nameB") ]
    let result, amended, applied, event =
        applyPosted false "Edit" posted state
    Assert.True(amended)
    assertStoredDiffers posted applied event
    match result with
    | ApplyResult.Changed st ->
        Assert.Equal(
            "nameA",
            Filename.tryValue st.graph.nodes.[nodeId].name |> Option.get)
    | other -> failwith $"result: {other}"

[<Fact>]
let ``applyPosted amended SetClasses stores ops that differ from posted`` () =
    let state, nodeId = stateWithChild "tagged"
    let prior = CssClass.ofList [ "a"; "b" ]
    let state =
        applyFirst [ Op.SetClasses(nodeId, CssClass.empty, prior) ] state
    let state =
        applyFirst [ Op.SetClasses(nodeId, prior, CssClass.ofList [ "b" ]) ] state
    let posted =
        [ Op.SetClasses(nodeId, prior, CssClass.ofList [ "a"; "b"; "c" ]) ]
    let result, amended, applied, event =
        applyPosted false "Edit" posted state
    Assert.True(amended)
    assertStoredDiffers posted applied event
    match result with
    | ApplyResult.Changed st ->
        let classes =
            st.graph.nodes.[nodeId].cssClasses |> CssClass.toList |> Set.ofList
        Assert.Equal<Set<string>>(Set.ofList [ "b"; "c" ], classes)
    | other -> failwith $"result: {other}"

[<Fact>]
let ``applyPosted amended Replace stores ops that differ from posted`` () =
    let state, parentId, child0 = stateWithParentChild "p" "c0"
    let childA = NodeId.New()
    let state =
        applyFirst
            [ Op.NewNode(childA, "a")
              Op.Replace(
                  parentId,
                  [ ChildNode.owner child0 ],
                  [ ChildNode.owner childA ]) ]
            state
    let childB = NodeId.New()
    let posted =
        [ Op.NewNode(childB, "b")
          Op.Replace(
              parentId,
              [ ChildNode.owner child0 ],
              [ ChildNode.owner childB ]) ]
    let result, amended, applied, event =
        applyPosted false "Edit" posted state
    Assert.True(amended)
    assertStoredDiffers posted applied event
    match result with
    | ApplyResult.Changed st ->
        let children = Graph.children st.graph parentId
        Assert.Equal(2, children.Length)
        Assert.Contains(ChildNode.owner childA, children)
        Assert.Contains(ChildNode.owner childB, children)
    | other -> failwith $"result: {other}"

[<Fact>]
let ``applyPosted amended mixed list stores ops that differ from posted`` () =
    let state, nodeId = stateWithChild "x0"
    let state = applyFirst [ Op.SetText(nodeId, "x0", "xA") ] state
    let extra = NodeId.New()
    let posted =
        [ Op.NewNode(extra, "keep")
          Op.SetText(nodeId, "x0", "xB") ]
    let result, amended, applied, event =
        applyPosted false "Edit" posted state
    Assert.True(amended)
    assertStoredDiffers posted applied event
    match result with
    | ApplyResult.Changed _ -> Assert.Equal(4, applied.Length)
    | other -> failwith $"result: {other}"

[<Fact>]
let ``applyPosted without amendment stores the posted ops`` () =
    let state, nodeId = stateWithChild "x0"
    let posted = [ Op.SetText(nodeId, "x0", "xA") ]
    let result, amended, applied, event =
        applyPosted false "Edit" posted state
    Assert.False(amended)
    Assert.Equal<Op list>(posted, applied)
    let storedOps: Op list option =
        Ev.ops { event with body = EventBody.Change applied }
    Assert.Equal(Ev.ops event, storedOps)
    match result with
    | ApplyResult.Changed _ -> ()
    | other -> failwith $"result: {other}"

[<Fact>]
let ``applyPosted no-op SetText collision amends to empty ops`` () =
    let state, nodeId = stateWithChild "x0"
    let state = applyFirst [ Op.SetText(nodeId, "x0", "xB") ] state
    let posted = [ Op.SetText(nodeId, "x0", "xB") ]
    let result, amended, applied, event =
        applyPosted false "Edit" posted state
    Assert.True(amended)
    assertStoredDiffers posted applied event
    Assert.Equal<Op list>([], applied)
    match result with
    | ApplyResult.Unchanged _ -> ()
    | other -> failwith $"result: {other}"
