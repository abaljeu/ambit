module Gambol.Shared.Tests.ImportDocumentTests

open System
open Xunit
open Gambol.Shared

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private replaceOps (ops: Op list) : Op list =
    ops
    |> List.choose (function
        | Op.Replace _ as op -> Some op
        | _ -> None)

let private applyChange (graph: Graph) (change: Change) : Graph =
    let state = { graph = graph; history = History.empty; revision = Revision.Zero }

    change.ops
    |> List.fold
        (fun acc op ->
            match acc with
            | Error msg -> Error msg
            | Ok state ->
                match Op.apply op state with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> Ok next
                | ApplyResult.Invalid(_, error) -> Error error)
        (Ok state)
    |> function
        | Ok state -> state.graph
        | Error msg -> failwith msg

[<Fact>]
let ``buildFilePackage md heading produces nested replace ops`` () =
    let text =
        "# Agent Instructions"
        + Environment.NewLine
        + Environment.NewLine
        + "## Workspace Purpose"
        + Environment.NewLine
        + "- item"
        + Environment.NewLine

    let package =
        ImportDocument.buildFilePackage "//life/AGENTS.md" text
        |> requireOk "build package"

    Assert.Equal("//life/AGENTS.md", package.sourcePath)
    Assert.False(package.isDirectory)
    Assert.Equal(1, package.topLevelIds.Length)
    Assert.NotEmpty(replaceOps package.ops)

    let h1Id = package.topLevelIds.Head
    let h1Children =
        package.ops
        |> List.tryPick (function
            | Op.Replace(parentId, _, _, children) when parentId = h1Id ->
                Some children
            | _ -> None)

    match h1Children with
    | None -> failwith "expected nested children under h1"
    | Some children ->
        Assert.NotEmpty(children)
        let h2Id = children.Head.id
        let h2Children =
            package.ops
            |> List.tryPick (function
                | Op.Replace(parentId, _, _, grandchildren) when parentId = h2Id ->
                    Some grandchildren
                | _ -> None)

        match h2Children with
        | None -> failwith "expected list item under h2"
        | Some grandchildren -> Assert.NotEmpty(grandchildren)

[<Fact>]
let ``buildFilePackage md differs from paste flat siblings`` () =
    let text = "# one" + Environment.NewLine + "plain" + Environment.NewLine + "## two" + Environment.NewLine

    let documentPackage =
        ImportDocument.buildFilePackage "//life/notes.md" text
        |> requireOk "document package"

    let pastePackage =
        ImportText.buildPackage "//life/notes.md" text
        |> requireOk "paste package"

    Assert.Equal(1, documentPackage.topLevelIds.Length)
    Assert.True(documentPackage.topLevelIds.Length < pastePackage.topLevelIds.Length)
    Assert.True(replaceOps documentPackage.ops |> List.length > 0)

[<Fact>]
let ``buildFilePackage integrates with buildImportChange for md`` () =
    let text = "# section" + Environment.NewLine + "- item" + Environment.NewLine
    let package =
        ImportDocument.buildFilePackage "//life/AGENTS.md" text
        |> requireOk "build package"

    let focusId = NodeId.New()
    let graph0 = Graph.create ()
    let file =
        Node.Create(
            focusId,
            text = "AGENTS.md",
            name = Filename.create "AGENTS.md",
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed)

    let graph =
        graph0.nodes
        |> Map.add focusId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let change =
        ImportText.buildImportChange graph focusId [] package 1 (Guid.NewGuid())

    let after = applyChange graph change
    let sectionId = after.nodes.[focusId].children.Head.id

    Assert.Equal("section", after.nodes.[sectionId].text)
    Assert.Equal(1, after.nodes.[sectionId].children.Length)
    Assert.Equal("item", after.nodes.[after.nodes.[sectionId].children.Head.id].text)

[<Fact>]
let ``buildFilePackage md heading applies md-head and md-list classes`` () =
    let text = "# section" + Environment.NewLine + "- item" + Environment.NewLine
    let package =
        ImportDocument.buildFilePackage "//life/notes.md" text
        |> requireOk "build package"

    let hasMdHead =
        package.ops
        |> List.exists (function
            | Op.SetClasses(_, _, classes) ->
                CssClass.toList classes |> List.contains "md-head"
            | _ -> false)

    let hasMdList =
        package.ops
        |> List.exists (function
            | Op.SetClasses(_, _, classes) ->
                CssClass.toList classes |> List.contains "md-list"
            | _ -> false)

    Assert.True(hasMdHead, "expected SetClasses with md-head")
    Assert.True(hasMdList, "expected SetClasses with md-list")

    let focusId = NodeId.New()
    let graph0 = Graph.create ()
    let file =
        Node.Create(
            focusId,
            text = "notes.md",
            name = Filename.create "notes.md",
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed)

    let graph =
        graph0.nodes
        |> Map.add focusId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let change =
        ImportText.buildImportChange graph focusId [] package 1 (Guid.NewGuid())

    let after = applyChange graph change
    let sectionId = after.nodes.[focusId].children.Head.id
    let itemId = after.nodes.[sectionId].children.Head.id

    Assert.True(
        CssClass.toList after.nodes.[sectionId].cssClasses
        |> List.contains "md-head")
    Assert.True(
        CssClass.toList after.nodes.[itemId].cssClasses
        |> List.contains "md-list")

    let written =
        DocumentFormat.writeArtifact after focusId "notes.md" None
        |> requireOk "writeArtifact"

    Assert.Equal(
        "# section"
        + Environment.NewLine
        + Environment.NewLine
        + "- item"
        + Environment.NewLine,
        written)

[<Fact>]
let ``buildFilePackage rejects blank input`` () =
    match ImportDocument.buildFilePackage "//life/empty.md" "  \n" with
    | Ok _ -> failwith "expected blank import to fail"
    | Error err -> Assert.Equal("cold import parser: text is empty", err)

[<Fact>]
let ``buildFilePackage rejects oversized text before graph materialization`` () =
    let actualCodeUnits = DocumentParseLimits.maxInputCodeUnits + 1
    let text = String('x', actualCodeUnits)

    match ImportDocument.buildFilePackage "//life/large.csv" text with
    | Ok _ -> failwith "expected oversized import to fail"
    | Error err ->
        Assert.Equal(
            DocumentParseLimits.errorForCodeUnits actualCodeUnits,
            err)

[<Fact>]
let ``buildTextPackage Plain indent nesting under paste path`` () =
    let text = "alpha" + Environment.NewLine + "\tbeta" + Environment.NewLine

    let package =
        ImportDocument.buildTextPackage "//paste" text None
        |> requireOk "buildTextPackage"

    Assert.False(package.isDirectory)
    Assert.Equal(1, package.topLevelIds.Length)

    let alphaId = package.topLevelIds.Head
    let betaChildren =
        package.ops
        |> List.tryPick (function
            | Op.Replace(parentId, _, _, children) when parentId = alphaId ->
                Some children
            | _ -> None)

    match betaChildren with
    | None -> failwith "expected nested child under alpha"
    | Some children ->
        Assert.Equal(1, children.Length)

[<Fact>]
let ``planParseFile md reorder updates child order`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let nl = Environment.NewLine
    let orderA = "# Title" + nl + "alpha" + nl + "beta" + nl
    let orderB = "# Title" + nl + "beta" + nl + "alpha" + nl
    let file =
        Node.Create(
            fileId,
            text = "notes.md",
            name = Filename.create "notes.md",
            owner = graph0.root,
            kind = Special File,
            documentState = Current)
    let graph1 =
        graph0.nodes
        |> Map.add fileId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] [ { ref = Ownership.Owner; id = fileId } ] graph1
        |> requireOk "root->file"
    let cold =
        MdDocument.read orderA fileId graph2
        |> requireOk "cold"
    let graph =
        DocumentFormat.mergeReadResult
            true
            graph2
            { documentRootId = fileId; nodes = cold.nodes }
        |> requireOk "merge cold"
    let titleId = graph.nodes.[fileId].children.Head.id
    let alphaId = graph.nodes.[titleId].children.[0].id
    let betaId = graph.nodes.[titleId].children.[1].id

    let ops =
        ImportDocument.planParseFile graph fileId orderB
        |> requireOk "planParseFile"

    Assert.False(List.isEmpty ops, "reorder must produce ops")

    let state0 =
        { graph = graph; history = History.empty; revision = Revision.Zero }
    let after =
        match History.applyChange { id = 0; changeId = Guid.NewGuid(); ops = ops } state0 with
        | ApplyResult.Changed s -> s.graph
        | ApplyResult.Unchanged _ -> failwith "expected Changed"
        | ApplyResult.Invalid(_, err) -> failwith err

    Assert.Equal<string list>(
        [ "beta"; "alpha" ],
        after.nodes.[titleId].children
        |> List.map (fun c -> after.nodes.[c.id].text))
    Assert.Equal(betaId, after.nodes.[titleId].children.[0].id)
    Assert.Equal(alphaId, after.nodes.[titleId].children.[1].id)

[<Fact>]
let ``planParseFile plain keeps id on line text edit`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let aId = NodeId.New()
    let bId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "readme.txt",
            name = Filename.create "readme.txt",
            owner = graph0.root,
            kind = Special File,
            documentState = Current,
            children =
                [ { ref = Ownership.Owner; id = aId }
                  { ref = Ownership.Owner; id = bId } ])
    let aNode = Node.Create(aId, text = "alpha", owner = fileId)
    let bNode = Node.Create(bId, text = "beta", owner = fileId)
    let graph =
        graph0.nodes
        |> Map.add fileId file
        |> Map.add aId aNode
        |> Map.add bId bNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let ops =
        ImportDocument.planParseFile
            graph
            fileId
            ("ALPHA" + Environment.NewLine + "beta" + Environment.NewLine)
        |> requireOk "planParseFile"

    Assert.False(List.isEmpty ops)

    let after = applyChange graph {
        id = 1
        changeId = Guid.NewGuid()
        ops = ops
    }

    Assert.Equal(aId, after.nodes.[fileId].children.Head.id)
    Assert.Equal("ALPHA", after.nodes.[aId].text)
    Assert.Equal(bId, after.nodes.[fileId].children.[1].id)
    Assert.Equal(Current, after.nodes.[fileId].documentState)

[<Fact>]
let ``planParseFile blank input marks Unparsed file Current`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "readme.txt",
            name = Filename.create "readme.txt",
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed)

    let graph =
        graph0.nodes
        |> Map.add fileId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let ops =
        ImportDocument.planParseFile graph fileId "  \n"
        |> requireOk "planParseFile blank"

    Assert.True(
        ops
        |> List.exists (function
            | Op.SetDocumentState(id, Unparsed, Current) when id = fileId ->
                true
            | _ -> false))

    let after = applyChange graph {
        id = 1
        changeId = Guid.NewGuid()
        ops = ops
    }

    Assert.Equal(Current, after.nodes.[fileId].documentState)

[<Fact>]
let ``planParseFile rejects binary image extension`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "cat.jpg",
            name = Filename.create "cat.jpg",
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed)

    let graph =
        graph0.nodes
        |> Map.add fileId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    match ImportDocument.planParseFile graph fileId "not an image" with
    | Ok _ -> failwith "expected binary parse to fail"
    | Error err -> Assert.Equal(DocumentBinary.parseError, err)

[<Fact>]
let ``planParseFile rejects NUL content on unknown extension`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "mystery.bin",
            name = Filename.create "mystery.bin",
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed)

    let graph =
        graph0.nodes
        |> Map.add fileId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let text = "hdr" + string '\000' + "tail"

    match ImportDocument.planParseFile graph fileId text with
    | Ok _ -> failwith "expected binary parse to fail"
    | Error err -> Assert.Equal(DocumentBinary.parseError, err)

[<Fact>]
let ``planParseFile unparsed marks Current`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "readme.txt",
            name = Filename.create "readme.txt",
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed)

    let graph =
        graph0.nodes
        |> Map.add fileId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let ops =
        ImportDocument.planParseFile
            graph
            fileId
            ("alpha" + Environment.NewLine)
        |> requireOk "planParseFile"

    Assert.True(
        ops
        |> List.exists (function
            | Op.SetDocumentState(id, Unparsed, Current) when id = fileId -> true
            | _ -> false))

    let after = applyChange graph {
        id = 1
        changeId = Guid.NewGuid()
        ops = ops
    }

    Assert.Equal(Current, after.nodes.[fileId].documentState)
    Assert.False(List.isEmpty after.nodes.[fileId].children)

[<Fact>]
let ``buildReconcilePackage rejects unparsed file`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "readme.txt",
            name = Filename.create "readme.txt",
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed)

    let graph =
        graph0.nodes
        |> Map.add fileId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    match
        ImportDocument.buildReconcilePackage
            graph
            fileId
            "//life/readme.txt"
            "alpha\n"
    with
    | Ok _ -> failwith "expected unparsed reconcile to fail"
    | Error err ->
        Assert.Equal("file is unparsed; use cold import", err)

/// After upload/M, File is Unparsed but still owns prior children. Parse must
/// warm-reconcile against that owner (not cold-replan Owners).
[<Fact>]
let ``planParseFile Unparsed with prior children warms and keeps line ids`` () =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let aId = NodeId.New()
    let bId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = "readme.txt",
            name = Filename.create "readme.txt",
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed,
            children =
                [ { ref = Ownership.Owner; id = aId }
                  { ref = Ownership.Owner; id = bId } ])
    let aNode = Node.Create(aId, text = "alpha", owner = fileId)
    let bNode = Node.Create(bId, text = "beta", owner = fileId)
    let graph =
        graph0.nodes
        |> Map.add fileId file
        |> Map.add aId aNode
        |> Map.add bId bNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let ops =
        ImportDocument.planParseFile
            graph
            fileId
            ("ALPHA" + Environment.NewLine + "beta" + Environment.NewLine)
        |> requireOk "planParseFile"

    Assert.True(
        ops
        |> List.exists (function
            | Op.SetDocumentState(id, Unparsed, Current) when id = fileId ->
                true
            | _ -> false),
        "Unparsed → Current must lead the batch")

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops = ops }
    let state =
        { graph = graph; history = History.empty; revision = Revision.Zero }

    match History.applyChange change state with
    | ApplyResult.Invalid(_, msg) ->
        Assert.True(false, "Unparsed warm parse must apply; got: " + msg)
    | ApplyResult.Unchanged _ -> failwith "expected Changed"
    | ApplyResult.Changed after ->
        Assert.Equal(Current, after.graph.nodes.[fileId].documentState)
        Assert.Equal(aId, after.graph.nodes.[fileId].children.Head.id)
        Assert.Equal("ALPHA", after.graph.nodes.[aId].text)
        Assert.Equal(bId, after.graph.nodes.[fileId].children.[1].id)
        Assert.Equal("beta", after.graph.nodes.[bId].text)

/// Current File Load + desktop-newer Amb body that carets a node already Owned
/// under a sibling File: reuse that Owner; claim no second edge (Owner or Ref)
/// under the parse File. Warm-update content; do not dual-Own.
/// Symptom without fix: History 400 "expected exactly one owner occurrence".
[<Fact>]
let ``planParseFile Current warm Amb reuses foreign owner without Ref`` () =
    let graph0 = Graph.create ()
    let workspaceId, wsOps = FileNodeOps.planCreateWorkspace graph0 "home"
    let state0 =
        { graph = graph0; history = History.empty; revision = Revision.Zero }
    let withWs =
        wsOps
        |> List.fold
            (fun s op ->
                match Op.apply op s with
                | ApplyResult.Changed n
                | ApplyResult.Unchanged n -> n
                | ApplyResult.Invalid(_, e) -> failwith e)
            state0
    let noteId, noteOps =
        FileNodeOps.planCreateOwnedFile withWs.graph workspaceId "note.txt"
    let withNote =
        noteOps
        |> List.fold
            (fun s op ->
                match Op.apply op s with
                | ApplyResult.Changed n
                | ApplyResult.Unchanged n -> n
                | ApplyResult.Invalid(_, e) -> failwith e)
            withWs
    let otherId, otherOps =
        FileNodeOps.planCreateOwnedFile withNote.graph workspaceId "other.txt"
    let withOther =
        otherOps
        |> List.fold
            (fun s op ->
                match Op.apply op s with
                | ApplyResult.Changed n
                | ApplyResult.Unchanged n -> n
                | ApplyResult.Invalid(_, e) -> failwith e)
            withNote
    let priorId = NodeId.New()
    let foreignId = NodeId.New()
    let prior = Node.Create(priorId, text = "prior", owner = noteId)
    let foreign = Node.Create(foreignId, text = "foreign", owner = otherId)
    let noteNode = withOther.graph.nodes.[noteId]
    let otherNode = withOther.graph.nodes.[otherId]
    let graph =
        withOther.graph.nodes
        |> Map.add priorId prior
        |> Map.add foreignId foreign
        |> Map.add
            noteId
            { noteNode with
                children = [ { ref = Ownership.Owner; id = priorId } ]
                documentState = Current }
        |> Map.add
            otherId
            { otherNode with
                children = [ { ref = Ownership.Owner; id = foreignId } ]
                documentState = Current }
        |> fun nodes -> Graph.fromNodes withOther.graph.root nodes

    let ambBody =
        "^"
        + AmbDocument.formatStableId foreignId
        + " stolen"
        + Environment.NewLine

    let ops =
        ImportDocument.planParseFile graph noteId ambBody
        |> requireOk "planParseFile"

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops = ops }
    let state =
        { graph = graph; history = History.empty; revision = Revision.Zero }

    match History.applyChange change state with
    | ApplyResult.Invalid(_, msg) ->
        Assert.True(
            false,
            "Current warm Amb must reuse existing owner; got: " + msg)
    | ApplyResult.Unchanged _ -> failwith "expected Changed"
    | ApplyResult.Changed after ->
        match History.validateOwnership after.graph with
        | Error msg -> Assert.True(false, "ownership broken after parse: " + msg)
        | Ok () ->
            let owners =
                after.graph.nodes
                |> Map.toList
                |> List.collect (fun (parentId, node) ->
                    node.children
                    |> List.choose (fun c ->
                        if c.ref = Ownership.Owner && c.id = foreignId then
                            Some parentId
                        else
                            None))
            Assert.Equal(1, owners.Length)
            Assert.Equal(otherId, owners.Head)
            Assert.Equal(Ownership.Owner, after.graph.nodes.[otherId].children.Head.ref)
            Assert.Equal("stolen", after.graph.nodes.[foreignId].text)
            let underNote =
                after.graph.nodes.[noteId].children
                |> List.filter (fun c -> c.id = foreignId)
            Assert.True(
                List.isEmpty underNote,
                "parse must not claim Owner or Ref under note for foreign-owned node")

/// Current warm reparent within the File overlay: child drops under old parent
/// but is reclaimed as Owner under another overlay parent. Must not Delete→trash
/// then Replace-claim (dual Owner / History 400).
[<Fact>]
let ``planParseFile Current warm overlay reparent does not dual-Own`` () =
    let graph0 = Graph.create ()
    let workspaceId, wsOps = FileNodeOps.planCreateWorkspace graph0 "life"
    let state0 =
        { graph = graph0; history = History.empty; revision = Revision.Zero }
    let applyOps (s: State) ops =
        ops
        |> List.fold
            (fun st op ->
                match Op.apply op st with
                | ApplyResult.Changed n
                | ApplyResult.Unchanged n -> n
                | ApplyResult.Invalid(_, e) -> failwith e)
            s
    let withWs = applyOps state0 wsOps
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile
            withWs.graph
            workspaceId
            "reparent.txt"
    let withFile = applyOps withWs fileOps
    let seededGraph =
        { withFile.graph.nodes.[fileId] with documentState = Unparsed }
        |> fun n ->
            withFile.graph.nodes
            |> Map.add fileId n
            |> fun nodes -> Graph.fromNodes withFile.graph.root nodes
    let seedBody =
        "Section"
        + Environment.NewLine
        + "\tItem"
        + Environment.NewLine
        + "Other"
        + Environment.NewLine
    let seedOps =
        ImportDocument.planParseFile seededGraph fileId seedBody
        |> requireOk "seed planParseFile"
    let seeded =
        match
            History.applyChange
                { id = 0; changeId = Guid.NewGuid(); ops = seedOps }
                { graph = seededGraph
                  history = History.empty
                  revision = Revision.Zero }
        with
        | ApplyResult.Changed s -> s.graph
        | ApplyResult.Unchanged s -> s.graph
        | ApplyResult.Invalid(_, msg) -> failwith ("seed: " + msg)

    let sectionId = seeded.nodes.[fileId].children.[0].id
    let otherId = seeded.nodes.[fileId].children.[1].id
    let itemId = seeded.nodes.[sectionId].children.Head.id
    // Move Item under Other (same texts → warm Keep ids; overlay reparent).
    let newBody =
        "Section"
        + Environment.NewLine
        + "Other"
        + Environment.NewLine
        + "\tItem"
        + Environment.NewLine
    let ops =
        ImportDocument.planParseFile seeded fileId newBody
        |> requireOk "planParseFile"

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops = ops }
    let state =
        { graph = seeded; history = History.empty; revision = Revision.Zero }

    match History.applyChange change state with
    | ApplyResult.Invalid(_, msg) ->
        Assert.True(
            false,
            "Current warm overlay reparent must apply; got: " + msg)
    | ApplyResult.Unchanged _ -> failwith "expected Changed"
    | ApplyResult.Changed after ->
        match History.validateOwnership after.graph with
        | Error msg ->
            Assert.True(false, "ownership broken after warm reparent: " + msg)
        | Ok () ->
            Assert.Equal(otherId, after.graph.ownerParentByChild.[itemId])
            Assert.True(
                List.isEmpty after.graph.nodes.[sectionId].children,
                "Section must no longer own Item")
            let underTrash =
                after.graph.nodes.[Graph.trashId].children
                |> List.exists (fun c -> c.id = itemId)
            Assert.False(
                underTrash,
                "reclaimed overlay child must not MoveToTrash")

/// Current warm reparse: prior owned child unmatched by the new artifact must
/// use Delete → TRASH (same as the Delete command), never silent Owner drop.
/// HITL class: Load 400 "owner chain does not reach root" after unmoored Owner.
[<Fact>]
let ``planParseFile Current warm unmatched owned child Deletes to trash`` () =
    let graph0 = Graph.create ()
    let workspaceId, wsOps = FileNodeOps.planCreateWorkspace graph0 "life"
    let state0 =
        { graph = graph0; history = History.empty; revision = Revision.Zero }
    let applyOps (s: State) ops =
        ops
        |> List.fold
            (fun st op ->
                match Op.apply op st with
                | ApplyResult.Changed n
                | ApplyResult.Unchanged n -> n
                | ApplyResult.Invalid(_, e) -> failwith e)
            s
    let withWs = applyOps state0 wsOps
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile
            withWs.graph
            workspaceId
            "honda-civic-sale.txt"
    let withFile = applyOps withWs fileOps
    // Seed via parse so warm previousText matches outline ids.
    let seededGraph =
        { withFile.graph.nodes.[fileId] with documentState = Unparsed }
        |> fun n ->
            withFile.graph.nodes
            |> Map.add fileId n
            |> fun nodes -> Graph.fromNodes withFile.graph.root nodes
    let seedBody =
        "Listing Draft"
        + Environment.NewLine
        + "Marketplace Description Draft"
        + Environment.NewLine
    let seedOps =
        ImportDocument.planParseFile seededGraph fileId seedBody
        |> requireOk "seed planParseFile"
    let seeded =
        match
            History.applyChange
                { id = 0; changeId = Guid.NewGuid(); ops = seedOps }
                { graph = seededGraph
                  history = History.empty
                  revision = Revision.Zero }
        with
        | ApplyResult.Changed s -> s.graph
        | ApplyResult.Unchanged s -> s.graph
        | ApplyResult.Invalid(_, msg) -> failwith ("seed: " + msg)

    let listingId = seeded.nodes.[fileId].children.[0].id
    let midId = seeded.nodes.[fileId].children.[1].id
    // Drop Marketplace sibling; rename Listing. Unmatched mid → Delete→trash.
    let newBody =
        "Listing Draft (ready to post)" + Environment.NewLine
    let ops =
        ImportDocument.planParseFile seeded fileId newBody
        |> requireOk "planParseFile"

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops = ops }
    let state =
        { graph = seeded; history = History.empty; revision = Revision.Zero }

    match History.applyChange change state with
    | ApplyResult.Invalid(_, msg) ->
        Assert.True(
            false,
            "Current warm unmatched must apply; got: " + msg)
    | ApplyResult.Unchanged _ -> failwith "expected Changed"
    | ApplyResult.Changed after ->
        match History.validateOwnership after.graph with
        | Error msg ->
            Assert.True(false, "ownership broken after warm parse: " + msg)
        | Ok () ->
            let underFile = after.graph.nodes.[fileId].children
            Assert.Equal(1, underFile.Length)
            Assert.Equal(
                "Listing Draft (ready to post)",
                after.graph.nodes.[underFile.Head.id].text)
            let rec ownerUnderTrash nodeId =
                match Map.tryFind nodeId after.graph.ownerParentByChild with
                | Some p when p = Graph.trashId -> true
                | Some p -> ownerUnderTrash p
                | None -> false

            // LCS may Keep either sibling in place; the unmatched one must
            // hit Delete → TRASH (never a silent Owner-edge drop).
            Assert.True(
                ownerUnderTrash listingId || ownerUnderTrash midId,
                "unmatched prior owned sibling must Delete to TRASH")
            Assert.True(
                underFile.Head.id = listingId
                || underFile.Head.id = midId,
                "File child should be a rematched prior line id")

/// Baseline: empty Unparsed (first parse) still cold-applies via History.
[<Fact>]
let ``planParseFile Unparsed plain upload body applies via History`` () =
    let graph0 = Graph.create ()
    let workspaceId, wsOps = FileNodeOps.planCreateWorkspace graph0 "home"
    let state0 =
        { graph = graph0; history = History.empty; revision = Revision.Zero }
    let withWs =
        wsOps
        |> List.fold
            (fun s op ->
                match Op.apply op s with
                | ApplyResult.Changed n
                | ApplyResult.Unchanged n -> n
                | ApplyResult.Invalid(_, e) -> failwith e)
            state0
    let noteId, noteOps =
        FileNodeOps.planCreateOwnedFile withWs.graph workspaceId "note.txt"
    let withNote =
        noteOps
        |> List.fold
            (fun s op ->
                match Op.apply op s with
                | ApplyResult.Changed n
                | ApplyResult.Unchanged n -> n
                | ApplyResult.Invalid(_, e) -> failwith e)
            withWs
    let graph =
        withNote.graph.nodes
        |> Map.add
            noteId
            { withNote.graph.nodes.[noteId] with documentState = Unparsed }
        |> fun nodes -> Graph.fromNodes withNote.graph.root nodes

    let ops =
        ImportDocument.planParseFile graph noteId ("NEW-EDIT" + Environment.NewLine)
        |> requireOk "planParseFile"

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops = ops }
    let state =
        { graph = graph; history = History.empty; revision = Revision.Zero }

    match History.applyChange change state with
    | ApplyResult.Invalid(_, msg) ->
        Assert.True(false, "plain Unparsed parse must apply; got: " + msg)
    | ApplyResult.Unchanged _
    | ApplyResult.Changed _ -> ()

/// TEMP probe: OpenDrive ownership. Remove after diagnosis.
[<Fact>]
let ``PROBE OpenDrive cold then warm ownership`` () =
    let body = System.IO.File.ReadAllText @"d:\downloads\OpenDrive"
    let applyOps (s: State) ops =
        ops
        |> List.fold
            (fun st op ->
                match Op.apply op st with
                | ApplyResult.Changed n
                | ApplyResult.Unchanged n -> n
                | ApplyResult.Invalid(_, e) -> failwith e)
            s

    let graph0 = Graph.create ()
    let workspaceId, wsOps = FileNodeOps.planCreateWorkspace graph0 "home"
    let withWs =
        applyOps
            { graph = graph0; history = History.empty; revision = Revision.Zero }
            wsOps
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile withWs.graph workspaceId "OpenDrive"
    let withFile = applyOps withWs fileOps
    let unparsed =
        { withFile.graph.nodes.[fileId] with documentState = Unparsed }
        |> fun n ->
            withFile.graph.nodes
            |> Map.add fileId n
            |> fun nodes -> Graph.fromNodes withFile.graph.root nodes

    let coldOps =
        ImportDocument.planParseFile unparsed fileId body
        |> requireOk "cold planParseFile"

    let afterCold =
        match
            History.applyChange
                { id = 0; changeId = Guid.NewGuid(); ops = coldOps }
                { graph = unparsed
                  history = History.empty
                  revision = Revision.Zero }
        with
        | ApplyResult.Changed s -> s
        | ApplyResult.Unchanged s -> s
        | ApplyResult.Invalid(_, msg) ->
            failwith ("COLD apply failed: " + msg)

    match History.validateOwnership afterCold.graph with
    | Error msg -> failwith ("COLD ownership: " + msg)
    | Ok () -> ()

    let rel =
        DocumentPartition.artifactFileRelative afterCold.graph fileId
        |> Option.defaultValue "OpenDrive"

    let exported =
        match DocumentFormat.writeArtifact afterCold.graph fileId rel None with
        | Ok t -> t
        | Error e -> failwith ("export: " + e)

    let warmOps =
        ImportDocument.planParseFile afterCold.graph fileId body
        |> requireOk "warm planParseFile"

    let warmMsg =
        match
            History.applyChange
                { id = 1; changeId = Guid.NewGuid(); ops = warmOps }
                afterCold
        with
        | ApplyResult.Invalid(_, msg) -> "WARM apply: " + msg
        | ApplyResult.Unchanged s
        | ApplyResult.Changed s ->
            match History.validateOwnership s.graph with
            | Error msg -> "WARM ownership: " + msg
            | Ok () -> "WARM ok"

    let package =
        ImportDocument.buildFilePackage "//home/OpenDrive" body
        |> requireOk "buildFilePackage"

    let g0 = Graph.create ()
    let wsId, wsOps2 = FileNodeOps.planCreateWorkspace g0 "home"
    let withWs2 =
        applyOps
            { graph = g0; history = History.empty; revision = Revision.Zero }
            wsOps2
    let focusId, focusOps =
        FileNodeOps.planCreateOwnedFile withWs2.graph wsId "OpenDrive"
    let withFocus = applyOps withWs2 focusOps
    let focusGraph =
        { withFocus.graph.nodes.[focusId] with documentState = Unparsed }
        |> fun n ->
            withFocus.graph.nodes
            |> Map.add focusId n
            |> fun nodes -> Graph.fromNodes withFocus.graph.root nodes

    let existing = focusGraph.nodes.[focusId].children
    let change =
        ImportText.buildImportChange
            focusGraph focusId existing package 1 (Guid.NewGuid())

    let importMsg =
        match
            History.applyChange
                change
                { graph = focusGraph
                  history = History.empty
                  revision = Revision.Zero }
        with
        | ApplyResult.Invalid(_, msg) -> "IMPORT apply: " + msg
        | ApplyResult.Unchanged s
        | ApplyResult.Changed s ->
            match History.validateOwnership s.graph with
            | Error msg -> "IMPORT ownership: " + msg
            | Ok () -> "IMPORT ok"

    let dual msgGraph =
        msgGraph.nodes
        |> Map.toList
        |> List.collect (fun (pid, n) ->
            n.children
            |> List.choose (fun c ->
                if c.ref = Ownership.Owner then Some(c.id, pid) else None))
        |> List.groupBy fst
        |> List.filter (fun (_, pairs) -> List.length pairs > 1)
        |> List.length

    Assert.True(
        false,
        sprintf
            "children=%d exportEq=%b exportLen=%d bodyLen=%d warm=%s import=%s dualCold=%d pkgTops=%d pkgOps=%d"
            afterCold.graph.nodes.[fileId].children.Length
            (exported = body)
            exported.Length
            body.Length
            warmMsg
            importMsg
            (dual afterCold.graph)
            package.topLevelIds.Length
            package.ops.Length)
