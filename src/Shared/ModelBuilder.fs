namespace Gambol.Shared

[<RequireQualifiedAccess>]
module ModelBuilder =
    let createNodes (texts: string list) (graph: Graph) : Graph * NodeId list =
        let folder (currentGraph, currentIds) text =
            let nextGraph, nodeId = Graph.newNode text currentGraph
            nextGraph, nodeId :: currentIds

        let graph2, reversedIds =
            texts
            |> List.fold folder (graph, [])

        graph2, List.rev reversedIds

    let requireOk label result =
        match result with
        | Ok value -> value
        | Error err -> failwith $"{label}: {err}"

    let createDag12 () : Graph =
        let graph0 = Graph.create ()

        let graph1, ids =
            createNodes
                [ "a"; "b"; "c"; "d"; "e"; "f"; "g"; "h"; "i"; "j"; "k" ]
                graph0

        if ids.Length <> 11 then
            failwith $"createDag12: expected 11 ids, got {ids.Length}"

        let id index = ids |> List.item index

        let graph2 =
            Graph.setText graph1.root "" "root" graph1
            |> requireOk "createDag12.setText"

        let replaceInsert parentId newIds graph =
            Graph.replace parentId 0 [] newIds graph
            |> requireOk "createDag12.replace"

        graph2
        |> replaceInsert graph2.root [ id 0; id 1; id 2 ]
        |> replaceInsert (id 0) [ id 3; id 4 ]
        |> replaceInsert (id 1) [ id 5; id 6 ]
        |> replaceInsert (id 2) [ id 7; id 8 ]
        |> replaceInsert (id 3) [ id 9 ]
        |> replaceInsert (id 5) [ id 10 ]

    let createState12 () : State =
        { graph = createDag12 ()
          history = History.empty
          revision = Revision.Zero }

    /// Graph where the same node ("shared") appears under two different parents:
    ///   root
    ///     parent1
    ///       shared      ← same NodeId as...
    ///     parent2
    ///       shared      ← ...this one
    let createSharedNodeGraph () : Graph =
        let graph0 = Graph.create ()
        let graph1, ids = createNodes [ "parent1"; "parent2"; "shared" ] graph0
        let p1 = List.item 0 ids
        let p2 = List.item 1 ids
        let sh = List.item 2 ids

        let replaceInsert parentId newIds graph =
            Graph.replace parentId 0 [] newIds graph
            |> requireOk "createSharedNodeGraph.replace"

        graph1
        |> replaceInsert graph1.root [ p1; p2 ]
        |> replaceInsert p1 [ sh ]
        |> replaceInsert p2 [ sh ]