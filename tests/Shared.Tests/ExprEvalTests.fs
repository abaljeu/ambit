module ExprEvalTests

open Gambol.Shared
open Xunit

let private nodeId (n: int) =
    NodeId(System.Guid(n, 0s, 0s, 0uy, 0uy, 0uy, 0uy, 0uy, 0uy, 0uy, 0uy))

let private node (n: int) : Node = Node.Create(nodeId n)

let private nodeAnswer (n: int) : ExprAnswer = ExprAnswer.Node(node n)

let private textAnswer (text: string) : ExprAnswer = ExprAnswer.Text text

let private nodeIds (answers: ExprAnswer list) =
    answers
    |> List.map (function
        | ExprAnswer.Node n -> n.id
        | _ -> failwith "expected Node answer")

let private assertNodeIds (expected: int list) (answers: ExprAnswer list) =
    let actual = nodeIds answers |> List.map (fun (NodeId g) -> g.ToString())
    let expectedIds =
        expected
        |> List.map (fun n -> nodeId n)
        |> List.map (fun (NodeId g) -> g.ToString())
    Assert.Equal<string list>(expectedIds, actual)

// ---- bind ----

[<Fact>]
let ``bind concatenates left-to-right in order`` () =
    let left _ = ExprEval.ofList [ nodeAnswer 1; nodeAnswer 2 ]
    let right input =
        match input with
        | ExprAnswer.Node n when n.id = nodeId 1 -> ExprEval.singleton (nodeAnswer 10)
        | ExprAnswer.Node n when n.id = nodeId 2 ->
            ExprEval.ofList [ nodeAnswer 20; nodeAnswer 21 ]
        | _ -> ExprEval.empty
    assertNodeIds
        [ 10; 20; 21 ]
        (ExprEval.toList (ExprEval.bind left right (nodeAnswer 0)))

// ---- OR ----

[<Fact>]
let ``OR concatenates operand sequences and may repeat`` () =
    let left _ = ExprEval.singleton (nodeAnswer 1)
    let right _ = ExprEval.ofList [ nodeAnswer 1; nodeAnswer 2 ]
    assertNodeIds
        [ 1; 1; 2 ]
        (ExprEval.toList (ExprEval.orEval left right (nodeAnswer 0)))

// ---- AND ----

[<Fact>]
let ``AND keeps left order with at-most-once intersection`` () =
    let left _ =
        ExprEval.ofList [ nodeAnswer 1; nodeAnswer 2; nodeAnswer 1; nodeAnswer 3 ]
    let right _ = ExprEval.ofList [ nodeAnswer 3; nodeAnswer 2; nodeAnswer 1 ]
    assertNodeIds
        [ 1; 2; 3 ]
        (ExprEval.toList (ExprEval.andEval left right (nodeAnswer 0)))

[<Fact>]
let ``AND intersects Text answers by string equality`` () =
    let left _ = ExprEval.ofList [ textAnswer "a"; textAnswer "b"; textAnswer "a" ]
    let right _ = ExprEval.ofList [ textAnswer "b"; textAnswer "a" ]
    let result = ExprEval.toList (ExprEval.andEval left right (nodeAnswer 0))
    Assert.Equal<string list>(
        [ "a"; "b" ],
        result |> List.choose (function ExprAnswer.Text t -> Some t | _ -> None))

// ---- NOT ----

[<Fact>]
let ``NOT yields input when operand is empty`` () =
    let inner _ = ExprEval.empty
    let input = nodeAnswer 1
    Assert.Equal<ExprAnswer list>(
        [ input ],
        ExprEval.toList (ExprEval.notEval inner input))

[<Fact>]
let ``NOT yields nothing when operand succeeds`` () =
    let inner _ = ExprEval.singleton (nodeAnswer 1)
    Assert.Equal<ExprAnswer list>(
        [],
        ExprEval.toList (ExprEval.notEval inner (nodeAnswer 0)))

// ---- Answer equality ----

[<Fact>]
let ``Answer equality uses Node identity not appearance`` () =
    let a = nodeAnswer 1
    let b = ExprAnswer.Node { (node 1) with text = "different" }
    Assert.True(ExprAnswer.equal a b)

// ---- catalog ----

[<Fact>]
let ``catalog stub row registers and invokes through core`` () =
    let row =
        { spellings = [ "echo" ]
          slot = None
          signature = ExprSignature.Fixed(ExprAnswerType.Node, ExprAnswerType.Node)
          evaluate =
            fun _ input ->
                match input with
                | ExprAnswer.Node _ -> ExprEval.singleton input
                | _ -> ExprEval.empty }
    let catalog = ExprCatalog.empty |> ExprCatalog.register row
    let found = ExprCatalog.lookup "echo" catalog |> Option.get
    let input = nodeAnswer 5
    Assert.Equal<ExprAnswer list>(
        [ input ],
        ExprEval.toList (ExprCatalog.invoke ExprBoundSlot.NoArgument found input))

// ---- unloaded walk ----

[<Fact>]
let ``child answers on Unloaded node yields empty sequence`` () =
    let parent =
        Node.Create(
            NodeId.New(),
            childrenStatus = Unloaded,
            children = []
        )
    let child = node 1
    let graph =
        Graph.create ()
        |> fun g ->
            let nodes =
                g.nodes
                |> Map.add parent.id parent
                |> Map.add child.id child
            Graph.fromNodes g.root nodes
    Assert.Equal<ExprAnswer list>([], ExprEval.toList (ExprWalk.childAnswers graph parent))

[<Fact>]
let ``child answers on Loaded node yields Children in order`` () =
    let child1 = node 1
    let child2 = node 2
    let parent =
        Node.Create(
            NodeId.New(),
            children = [ ChildNode.owner child1.id; ChildNode.owner child2.id ]
        )
    let graph =
        Graph.create ()
        |> fun g ->
            let nodes =
                g.nodes
                |> Map.add parent.id parent
                |> Map.add child1.id child1
                |> Map.add child2.id child2
            Graph.fromNodes g.root nodes
    let result = ExprEval.toList (ExprWalk.childAnswers graph parent)
    Assert.Equal<NodeId list>([ child1.id; child2.id ], nodeIds result)

[<Fact>]
let ``take two from a three-hit stream leaves the third unforced`` () =
    let thirdForced = ref false
    let third =
        ExprEval.delay (fun () ->
            thirdForced.Value <- true
            Some(nodeAnswer 3, ExprEval.empty))
    let stream = ExprEval.cons (nodeAnswer 1) (ExprEval.cons (nodeAnswer 2) third)
    let taken, leftover = ExprEval.take 2 stream
    assertNodeIds [ 1; 2 ] taken
    Assert.False(thirdForced.Value)
    match leftover with
    | None -> failwith "expected leftover cursor"
    | Some rest ->
        assertNodeIds [ 3 ] (ExprEval.toList rest)
        Assert.True(thirdForced.Value)

[<Fact>]
let ``descendant take two resumes at the late unique child`` () =
    let late = node 3
    let childA = node 1
    let childB = node 2
    let parent =
        Node.Create(
            NodeId.New(),
            children =
                [ ChildNode.owner childA.id
                  ChildNode.owner childB.id
                  ChildNode.owner late.id ])
    let graph =
        Graph.create ()
        |> fun g ->
            let nodes =
                g.nodes
                |> Map.add parent.id parent
                |> Map.add childA.id childA
                |> Map.add childB.id childB
                |> Map.add late.id late
            Graph.fromNodes g.root nodes
    let stream = ExprWalk.descendantAnswers graph (ExprAnswer.Node parent)
    let taken, leftover = ExprEval.take 2 stream
    Assert.Equal<NodeId list>([ childA.id; childB.id ], nodeIds taken)
    match leftover with
    | None -> failwith "expected leftover walk"
    | Some rest ->
        Assert.Equal<NodeId list>([ late.id ], nodeIds (ExprEval.toList rest))
