module ViewModelJoinOpsTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Shared.ViewModelJoinOps
open VmTestHelpers
open Xunit

let private owned = ChildNode.owners

let private buildFlat (texts: string list) : Graph * NodeId * NodeId list =
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container" ] graph0
    let cont = contIds.[0]
    let graph2, ids = ModelBuilder.createNodes texts graph1
    let graph3 =
        Graph.replace graph2.root 0 [] (owned [ cont ]) graph2
        |> ModelBuilder.requireOk "buildFlat.root"

    let graph4 =
        Graph.replace cont 0 [] (owned ids) graph3
        |> ModelBuilder.requireOk "buildFlat.cont"

    graph4, cont, ids

let private modelWithSel graph parentNodeId start endd focusIdx : VM =
    let model = emptyModelAt graph parentNodeId

    { model with
        selectedNodes =
            Some
                { range =
                    { parent = model.siteMap.entries.[model.siteMap.rootId]
                      start = start
                      endd = endd }
                  focus = focusIdx }
        mode = Editing ("", EditCaret.Utf16Index 0) }

let private expectApply plan =
    match plan with
    | Some (JoinEditPlan.Apply (ops, text, caret, focusInstanceId)) ->
        ops, text, caret, focusInstanceId
    | _ ->
        Assert.True(false, "expected Apply plan")
        [], "", EditCaret.Utf16Index 0, Sid -1

let private visibleRowInstance index model =
    getVisibleRowInstanceIds model.siteMap |> List.item index

[<Fact>]
let ``joinWithNextPlan joins current text into next node`` () =
    let graph, cont, ids = buildFlat [ "a"; "b" ]
    let model = modelWithSel graph cont 0 1 0
    let currentId = ids.[0]
    let nextId = ids.[1]
    let nextInstId = visibleRowInstance 1 model
    let ops, text, caret, focusInstanceId = expectApply (joinWithNextPlan "a" model)

    Assert.Equal<Op list>(
        [ Op.SetText(nextId, "b", "ab")
          Op.Replace(cont, 0, owned [ currentId ], []) ],
        ops)
    Assert.Equal("ab", text)
    Assert.Equal(EditCaret.Utf16Index 1, caret)
    Assert.Equal(nextInstId, focusInstanceId)

[<Fact>]
let ``joinWithNextPlan removes blank current node`` () =
    let graph, cont, ids = buildFlat [ ""; "b" ]
    let model = modelWithSel graph cont 0 1 0
    let currentId = ids.[0]
    let nextId = ids.[1]
    let nextInstId = visibleRowInstance 1 model
    let ops, text, caret, focusInstanceId = expectApply (joinWithNextPlan " " model)

    Assert.Equal<Op list>([ Op.Replace(cont, 0, owned [ currentId ], []) ], ops)
    Assert.Equal("b", text)
    Assert.Equal(EditCaret.Utf16Index 0, caret)
    Assert.Equal(nextInstId, focusInstanceId)

[<Fact>]
let ``joinWithNextPlan restores caret when current has children`` () =
    let graph0, cont, ids = buildFlat [ "a"; "b"; "child" ]
    let currentId = ids.[0]
    let childId = ids.[2]
    let graph =
        Graph.replace currentId 0 [] (owned [ childId ]) graph0
        |> ModelBuilder.requireOk "current child"

    let model = modelWithSel graph cont 0 1 0

    Assert.Equal(Some JoinEditPlan.RestoreCaret, joinWithNextPlan "a" model)

[<Fact>]
let ``joinWithPreviousPlan joins current text into previous node`` () =
    let graph, cont, ids = buildFlat [ "a"; "b" ]
    let model = modelWithSel graph cont 1 2 1
    let prevId = ids.[0]
    let currentId = ids.[1]
    let prevInstId = visibleRowInstance 0 model
    let ops, text, caret, focusInstanceId = expectApply (joinWithPreviousPlan "b" model)

    Assert.Equal<Op list>(
        [ Op.SetText(prevId, "a", "ab")
          Op.Replace(cont, 1, owned [ currentId ], []) ],
        ops)
    Assert.Equal("ab", text)
    Assert.Equal(EditCaret.Utf16Index 1, caret)
    Assert.Equal(prevInstId, focusInstanceId)

[<Fact>]
let ``joinWithPreviousPlan moves current children to previous leaf`` () =
    let graph0, cont, ids = buildFlat [ "a"; "b"; "child" ]
    let prevId = ids.[0]
    let currentId = ids.[1]
    let child = ChildNode.owner ids.[2]
    let graph =
        Graph.replace currentId 0 [] [ child ] graph0
        |> ModelBuilder.requireOk "current child"

    let model = modelWithSel graph cont 1 2 1
    let prevInstId = visibleRowInstance 0 model
    let ops, text, caret, focusInstanceId = expectApply (joinWithPreviousPlan "b" model)

    Assert.Equal<Op list>(
        [ Op.SetText(prevId, "a", "ab")
          Op.Replace(prevId, 0, [], [ child ])
          Op.Replace(cont, 1, owned [ currentId ], []) ],
        ops)
    Assert.Equal("ab", text)
    Assert.Equal(EditCaret.Utf16Index 1, caret)
    Assert.Equal(prevInstId, focusInstanceId)

[<Fact>]
let ``joinWithPreviousPlan focuses previous ref instance instead of owner instance`` () =
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container"; "shared"; "new" ] graph0
    let cont = contIds.[0]
    let sharedId = contIds.[1]
    let newId = contIds.[2]
    let sharedRef = ChildNode.reference sharedId
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ cont ]) graph1
        |> ModelBuilder.requireOk "root"

    let graph =
        let children = (owned [ sharedId ]) @ [ sharedRef ] @ (owned [ newId ])

        Graph.replace cont 0 [] children graph2
        |> ModelBuilder.requireOk "cont"

    let model = modelWithSel graph cont 2 3 2
    let refInstId = visibleRowInstance 1 model
    let ownerInstId = visibleRowInstance 0 model
    let ops, text, caret, focusInstanceId = expectApply (joinWithPreviousPlan "" model)

    Assert.Equal<Op list>(
        [ Op.Replace(cont, 2, owned [ newId ], []) ],
        ops)
    Assert.Equal("shared", text)
    Assert.Equal(EditCaret.Utf16Index 6, caret)
    Assert.NotEqual(ownerInstId, focusInstanceId)
    Assert.Equal(refInstId, focusInstanceId)
