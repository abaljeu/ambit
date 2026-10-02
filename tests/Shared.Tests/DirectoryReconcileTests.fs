module DirectoryReconcileTests

open System
open System.IO
open Gambol.Shared
open Xunit

let private applyOps (graph: Graph) (ops: Op list) : Graph =
    let state = { graph = graph; eventId = EventId.zero }
    ops
    |> List.fold (fun s op ->
        match Op.apply op s with
        | ApplyResult.Changed next
        | ApplyResult.Unchanged next -> next
        | ApplyResult.Invalid(_, msg) -> failwith msg) state
    |> fun s -> s.graph

let private addWorkspace label graph =
    let id, ops = FileNodeOps.planCreateWorkspace graph label
    id, applyOps graph ops

let private addDirectory graph parentId name =
    let id, ops = FileNodeOps.planCreateOwnedDirectory graph parentId name
    id, applyOps graph ops

let private addFile graph parentId name =
    let id, ops = FileNodeOps.planCreateOwnedFile graph parentId name
    id, applyOps graph ops

let private addNormal graph parentId text =
    let id = NodeId.New()
    let kids = Graph.children graph parentId
    let ops =
        [ Op.NewNode(id, text)
          ChildListWire.append parentId kids [ ChildNode.owner id ] ]
    id, applyOps graph ops

let private tempDir () =
    let dir =
        Path.Combine(
            Path.GetTempPath(),
            "gambol-dir-reconcile-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(dir) |> ignore
    dir

let private writeDisk (dataDir: string) (relative: string) (text: string) =
    let parts =
        relative.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
    let full = Path.Combine(Array.append [| dataDir |] parts)
    Directory.CreateDirectory(Path.GetDirectoryName full) |> ignore
    File.WriteAllText(full, text)
    full

let private plan dataDir graph directoryId =
    match
        DirectoryReconcile.planDirectoryReconcile
            { dataDir = dataDir
              graph = graph
              directoryId = directoryId }
    with
    | Ok planned -> planned
    | Error err -> failwith err

let private ownedFileNames (graph: Graph) (parentId: NodeId) =
    Graph.children graph parentId
    |> List.choose (fun child ->
        if child.ref <> Ownership.Owner then
            None
        else
            match graph.nodes.[child.id].kind with
            | Special File -> Filename.tryValue graph.nodes.[child.id].name
            | _ -> None)

let private stamp (graph: Graph) (nodeId: NodeId) (time: DateTime) =
    let node = graph.nodes.[nodeId]
    applyOps
        graph
        [ Op.SetUpdateTime(nodeId, node.updateTime, time) ]

[<Fact>]
let ``missing disk files append alphabetically under the directory`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph1 = Graph.create () |> addWorkspace "home"
        let dirId, graph2 = addDirectory graph1 wsId "docs"
        let zId, graph3 = addFile graph2 dirId "z.txt"
        let zPath = writeDisk dataDir "home/docs/z.txt" "z"
        writeDisk dataDir "home/docs/m.txt" "m" |> ignore
        writeDisk dataDir "home/docs/a.txt" "a" |> ignore
        writeDisk dataDir "home/docs/.amb" "NOT-A-MEMBER-LIST" |> ignore
        let mtime = File.GetLastWriteTimeUtc zPath
        let graph4 = stamp graph3 zId mtime
        let planned = plan dataDir graph4 dirId
        let graph5 = applyOps graph4 planned.ops
        Assert.Equal<string>(
            [ "z.txt"; "a.txt"; "m.txt" ],
            ownedFileNames graph5 dirId)
        Assert.Empty(planned.push)
    finally
        Directory.Delete(dataDir, true)

[<Fact>]
let ``disk-newer file below immediate children is named for push`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph1 = Graph.create () |> addWorkspace "home"
        let dirId, graph2 = addDirectory graph1 wsId "docs"
        let graph3 =
            applyOps
                graph2
                [ Op.SetDocumentState(dirId, Unparsed, Current) ]
        let normalId, graph4 = addNormal graph3 dirId "section"
        let fileId, graph5 = addFile graph4 normalId "deep.txt"
        let old = DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        let graph6 = stamp graph5 fileId old
        let path = writeDisk dataDir "home/docs/deep.txt" "newer"
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow)
        let planned = plan dataDir graph6 dirId
        let graph7 = applyOps graph6 planned.ops
        Assert.Equal<NodeId>([ fileId ], planned.push)
        Assert.False(
            Graph.children graph7 dirId
            |> List.exists (fun child -> child.id = fileId))
        Assert.Equal<string>(
            [ "deep.txt" ],
            ownedFileNames graph7 normalId)
    finally
        Directory.Delete(dataDir, true)

[<Fact>]
let ``workspace uses the same directory reconcile`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph = Graph.create () |> addWorkspace "home"
        writeDisk dataDir "home/m.txt" "m" |> ignore
        writeDisk dataDir "home/a.txt" "a" |> ignore
        writeDisk dataDir "home/.amb" "NOT-A-MEMBER-LIST" |> ignore
        let planned = plan dataDir graph wsId
        let graph2 = applyOps graph planned.ops
        Assert.Equal<string>(
            [ "a.txt"; "m.txt" ],
            ownedFileNames graph2 wsId)
        Assert.Empty(planned.push)
    finally
        Directory.Delete(dataDir, true)
