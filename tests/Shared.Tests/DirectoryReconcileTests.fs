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

let private makeDiskDir (dataDir: string) (relative: string) =
    let parts =
        relative.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
    let full = Path.Combine(Array.append [| dataDir |] parts)
    Directory.CreateDirectory(full) |> ignore
    full

let private setsDocumentState (ops: Op list) =
    ops
    |> List.exists (fun op ->
        match op with
        | Op.SetDocumentState _ -> true
        | _ -> false)

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

let private withParsed (graph: Graph) (nodeId: NodeId) =
    let node = Node.withParseState ParseState.Parsed graph.nodes.[nodeId]
    { graph with nodes = Map.add nodeId node graph.nodes }

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
let ``disk-newer reconcile ops leave the file Current`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph1 = Graph.create () |> addWorkspace "home"
        let dirId, graph2 = addDirectory graph1 wsId "docs"
        let fileId, graph3 = addFile graph2 dirId "note.txt"
        let graph4 =
            applyOps
                graph3
                [ Op.SetDocumentState(fileId, Unparsed, Current) ]
        let old = DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        let graph5 = stamp graph4 fileId old
        let path = writeDisk dataDir "home/docs/note.txt" "newer"
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow)
        let planned = plan dataDir graph5 dirId
        let graph6 = applyOps graph5 planned.ops
        let setsState =
            planned.ops
            |> List.exists (fun op ->
                match op with
                | Op.SetDocumentState _ -> true
                | _ -> false)
        Assert.False(setsState)
        Assert.Equal<NodeId>([ fileId ], planned.push)
        Assert.Equal(Current, graph6.nodes.[fileId].documentState)
    finally
        Directory.Delete(dataDir, true)

[<Fact>]
let ``disk-newer directory is named for push and a current sibling is not`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph1 = Graph.create () |> addWorkspace "home"
        let dirId, graph2 = addDirectory graph1 wsId "docs"
        let graph3 =
            applyOps
                graph2
                [ Op.SetDocumentState(dirId, Unparsed, Current) ]
        let newerId, graph4 = addDirectory graph3 dirId "newer"
        let currentId, graph5 = addDirectory graph4 dirId "kept"
        let graph6 = withParsed (withParsed graph5 newerId) currentId
        let old = DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        let diskTime = DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        let graph7 = stamp graph6 newerId old
        let graph8 = stamp graph7 currentId diskTime
        let newerPath = makeDiskDir dataDir "home/docs/newer"
        let keptPath = makeDiskDir dataDir "home/docs/kept"
        Directory.SetLastWriteTimeUtc(newerPath, diskTime)
        Directory.SetLastWriteTimeUtc(keptPath, diskTime)
        let planned = plan dataDir graph8 dirId
        let graph9 = applyOps graph8 planned.ops
        Assert.False(setsDocumentState planned.ops)
        Assert.Equal<NodeId>([ newerId ], planned.push)
        Assert.DoesNotContain(dirId, planned.push)
        Assert.DoesNotContain(currentId, planned.push)
        Assert.Equal(Current, graph9.nodes.[newerId].documentState)
        Assert.Equal(ParseState.Parsed, graph9.nodes.[newerId].parseState)
    finally
        Directory.Delete(dataDir, true)

let private createdDirectoryId (ops: Op list) (name: string) =
    ops
    |> List.tryPick (fun op ->
        match op with
        | Op.NewSpecialNode(id, Directory, found) when found = name ->
            Some id
        | _ -> None)

[<Fact>]
let ``missing disk directory is created Unparsed and named for push`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph1 = Graph.create () |> addWorkspace "home"
        let dirId, graph2 = addDirectory graph1 wsId "docs"
        let graph3 = withParsed graph2 dirId
        let fileId, graph4 = addFile graph3 dirId "z.txt"
        let zPath = writeDisk dataDir "home/docs/z.txt" "z"
        let graph5 = stamp graph4 fileId (File.GetLastWriteTimeUtc zPath)
        makeDiskDir dataDir "home/docs/m" |> ignore
        makeDiskDir dataDir "home/docs/a" |> ignore
        makeDiskDir dataDir "home/docs/a/leaf" |> ignore
        let planned = plan dataDir graph5 dirId
        let graph6 = applyOps graph5 planned.ops
        let aId =
            match createdDirectoryId planned.ops "a" with
            | Some id -> id
            | None -> failwith "missing directory a"
        let mId =
            match createdDirectoryId planned.ops "m" with
            | Some id -> id
            | None -> failwith "missing directory m"
        Assert.False(setsDocumentState planned.ops)
        Assert.Equal(Special Directory, graph6.nodes.[aId].kind)
        Assert.Equal(Unparsed, graph6.nodes.[aId].documentState)
        Assert.Equal(ParseState.Unparsed, graph6.nodes.[aId].parseState)
        Assert.Equal(PersistState.Persisted, graph6.nodes.[aId].persistState)
        Assert.Equal<NodeId>([ aId; mId ], planned.push)
        Assert.DoesNotContain(dirId, planned.push)
        Assert.DoesNotContain(wsId, planned.push)
        Assert.True(createdDirectoryId planned.ops "leaf" |> Option.isNone)
        Assert.Equal<string>(
            [ "z.txt"; "a"; "m" ],
            Graph.children graph6 dirId
            |> List.choose (fun child ->
                Filename.tryValue graph6.nodes.[child.id].name))
    finally
        Directory.Delete(dataDir, true)

[<Fact>]
let ``disk-newer directory below immediate children is named for push`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph1 = Graph.create () |> addWorkspace "home"
        let dirId, graph2 = addDirectory graph1 wsId "docs"
        let graph3 = withParsed graph2 dirId
        let normalId, graph4 = addNormal graph3 dirId "section"
        let notesId, graph5 = addDirectory graph4 normalId "notes"
        let graph6 = withParsed graph5 notesId
        let old = DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        let graph7 = stamp graph6 notesId old
        let path = makeDiskDir dataDir "home/docs/notes"
        Directory.SetLastWriteTimeUtc(
            path,
            DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc))
        let planned = plan dataDir graph7 dirId
        let graph8 = applyOps graph7 planned.ops
        Assert.Equal<NodeId>([ notesId ], planned.push)
        Assert.True(createdDirectoryId planned.ops "notes" |> Option.isNone)
        Assert.Contains(
            notesId,
            Graph.children graph8 normalId |> List.map (fun child -> child.id))
        Assert.DoesNotContain(dirId, planned.push)
        Assert.DoesNotContain(wsId, planned.push)
    finally
        Directory.Delete(dataDir, true)

[<Fact>]
let ``disk-newer file stays named for push beside a directory`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph1 = Graph.create () |> addWorkspace "home"
        let dirId, graph2 = addDirectory graph1 wsId "docs"
        let graph3 = withParsed graph2 dirId
        let fileId, graph4 = addFile graph3 dirId "note.txt"
        let notesId, graph5 = addDirectory graph4 dirId "notes"
        let graph6 = withParsed (withParsed graph5 fileId) notesId
        let old = DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        let graph7 = stamp (stamp graph6 fileId old) notesId old
        let filePath = writeDisk dataDir "home/docs/note.txt" "newer"
        let dirPath = makeDiskDir dataDir "home/docs/notes"
        let diskTime = DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        File.SetLastWriteTimeUtc(filePath, diskTime)
        Directory.SetLastWriteTimeUtc(dirPath, diskTime)
        let planned = plan dataDir graph7 dirId
        Assert.Equal<NodeId>([ fileId; notesId ], planned.push)
        Assert.DoesNotContain(dirId, planned.push)
        Assert.DoesNotContain(wsId, planned.push)
        Assert.False(setsDocumentState planned.ops)
    finally
        Directory.Delete(dataDir, true)

[<Fact>]
let ``workspace names a disk-newer directory for push`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph1 = Graph.create () |> addWorkspace "home"
        let keptId, graph2 = addDirectory graph1 wsId "kept"
        let newerId, graph3 = addDirectory graph2 wsId "newer"
        let graph4 = withParsed (withParsed graph3 keptId) newerId
        let diskTime = DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        let old = DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        let graph5 = stamp (stamp graph4 keptId diskTime) newerId old
        let keptPath = makeDiskDir dataDir "home/kept"
        let newerPath = makeDiskDir dataDir "home/newer"
        Directory.SetLastWriteTimeUtc(keptPath, diskTime)
        Directory.SetLastWriteTimeUtc(newerPath, diskTime)
        makeDiskDir dataDir "home/added" |> ignore
        let planned = plan dataDir graph5 wsId
        let graph6 = applyOps graph5 planned.ops
        let addedId =
            match createdDirectoryId planned.ops "added" with
            | Some id -> id
            | None -> failwith "missing directory added"
        Assert.Equal<NodeId>([ newerId; addedId ], planned.push)
        Assert.DoesNotContain(keptId, planned.push)
        Assert.DoesNotContain(wsId, planned.push)
        Assert.Equal(Unparsed, graph6.nodes.[addedId].documentState)
        Assert.Equal(ParseState.Unparsed, graph6.nodes.[addedId].parseState)
        Assert.Equal(Current, graph6.nodes.[newerId].documentState)
    finally
        Directory.Delete(dataDir, true)

[<Fact>]
let ``missing directory attaches while the reconciled node is Unparsed`` () =
    let dataDir = tempDir ()
    try
        let wsId, graph1 = Graph.create () |> addWorkspace "home"
        let dirId, graph2 = addDirectory graph1 wsId "docs"
        let graph3 =
            applyOps
                graph2
                [ Op.SetDocumentState(dirId, Unparsed, Current) ]
        let normalId, graph4 = addNormal graph3 dirId "section"
        let fileId, graph5 = addFile graph4 normalId "note.txt"
        let graph6 =
            applyOps
                graph5
                [ Op.SetDocumentState(dirId, Current, Unparsed) ]
        let old = DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        let graph7 = stamp graph6 fileId old
        let filePath = writeDisk dataDir "home/docs/note.txt" "newer"
        File.SetLastWriteTimeUtc(
            filePath,
            DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc))
        makeDiskDir dataDir "home/docs/extra" |> ignore
        let planned = plan dataDir graph7 dirId
        let graph8 = applyOps graph7 planned.ops
        let extraId =
            match createdDirectoryId planned.ops "extra" with
            | Some id -> id
            | None -> failwith "missing directory extra"
        Assert.Equal(Unparsed, graph7.nodes.[dirId].documentState)
        Assert.False(setsDocumentState planned.ops)
        Assert.Equal<NodeId>([ fileId; extraId ], planned.push)
        Assert.Equal(Unparsed, graph8.nodes.[extraId].documentState)
        Assert.Equal(ParseState.Unparsed, graph8.nodes.[extraId].parseState)
        Assert.DoesNotContain(dirId, planned.push)
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
