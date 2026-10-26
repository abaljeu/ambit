namespace Gambol.Shared

open System

type DesktopImportPackage =
    { sourcePath: string
      isDirectory: bool
      topLevelIds: NodeId list
      ops: Op list }

[<RequireQualifiedAccess>]
module ImportText =
    let private hasImportContent (entries: (string * int) list) : bool =
        entries
        |> List.exists (fun (text, _) -> text.Trim().Length > 0)

    let buildPackage (sourcePath: string) (text: string) : Result<DesktopImportPackage, string> =
        let entries = Paste.parsePasteText text

        if hasImportContent entries then
            let topLevelIds, ops = Paste.buildPasteOps entries

            Ok
                { sourcePath = sourcePath
                  isDirectory = false
                  topLevelIds = topLevelIds
                  ops = ops }
        else
            Error "directory import parser: text is empty"

    let parseFirstFileReference (text: string) : FileReference =
        FileReference.parseFirst text

    let tryFindFirstFileReference (text: string) : Result<string, string> =
        match parseFirstFileReference text with
        | NoFileReference -> Error "file reference not found"
        | InvalidFileReference -> Error "file reference is invalid"
        | FileReference path -> Ok path

    let private ownedChildren = ChildNode.owners

    let private importMarkPrefix = "Import:"

    let importCommand (focusId: NodeId) : string =
        importMarkPrefix + focusId.Value.ToString("N")

    let tryImportFocus (commandName: string) : NodeId option =
        if commandName.StartsWith(importMarkPrefix, StringComparison.Ordinal) then
            let tail = commandName.Substring(importMarkPrefix.Length)
            match Guid.TryParse tail with
            | true, guid -> Some(NodeId guid)
            | _ -> None
        else
            None

    /// Graph the Change ops see. An import command applies NodeParsed here.
    /// The Notice commits only after that Change is accepted.
    let stateForImportOps (event: Ev) (state: State) : State =
        match tryImportFocus event.commandName with
        | None -> state
        | Some nodeId ->
            let marked =
                { event with
                    body = EventBody.Notice(Notice.NodeParsed nodeId) }
            match Ev.apply marked state with
            | ApplyResult.Changed next
            | ApplyResult.Unchanged next -> next
            | ApplyResult.Invalid _ -> state

    let private documentEnum (state: State) (nodeId: NodeId) =
        Map.tryFind nodeId state.graph.nodes
        |> Option.map Node.documentStateEnum

    /// Apply on the import graph. An enum change with unchanged ops still commits.
    let commitImportApply
        (apply: State -> ApplyResult * bool * Op list)
        (event: Ev)
        (state: State)
        : ApplyResult * bool * Op list =
        let seen = stateForImportOps event state
        let result, amended, ops = apply seen
        match result, tryImportFocus event.commandName with
        | ApplyResult.Unchanged next, Some nodeId
            when documentEnum state nodeId <> documentEnum next nodeId ->
            ApplyResult.Changed next, amended, ops
        | other, _ -> other, amended, ops

    let private commandForUnparsedDocument (graph: Graph) (focusId: NodeId) : string =
        match Map.tryFind focusId graph.nodes with
        | Some node
            when DocumentPartition.isDocumentRootNode graph focusId
                 && Node.documentStateEnum node = Unparsed ->
            importCommand focusId
        | _ -> ""

    let private normalizeEntryName (path: string) = path.TrimEnd('/')

    let private entryNameFromText (text: string) =
        match FileReference.parseFirst text with
        | FileReference path -> Some (normalizeEntryName path)
        | _ -> None

    let private existingChildName (graph: Graph) (child: ChildNode) =
        match Map.tryFind child.id graph.nodes with
        | None -> None
        | Some node ->
            match node.kind with
            | Special (File | Directory) -> Filename.tryValue node.name
            | Normal ->
                match FileReference.parseFirst node.text with
                | FileReference path -> Some (normalizeEntryName path)
                | _ -> None
            | _ -> None

    /// One EventBody.Change: package paste ops plus replace-all-children.
    let buildImportChange
        (graph: Graph)
        (focusId: NodeId)
        (existingChildren: ChildNode list)
        (package: DesktopImportPackage)
        (submissionId: System.Guid)
        : Ev =
        let attach =
            ChildListWire.replace focusId existingChildren (ownedChildren package.topLevelIds)

        { id = EventId.zero
          submissionId = submissionId
          authority = Authority "Browser"
          commandName = commandForUnparsedDocument graph focusId
          body = EventBody.Change(package.ops @ [ attach ]) }

    /// Directory import: add only top-level entries whose names are not already children.
    let buildDirectoryMergeChange
        (graph: Graph)
        (focusId: NodeId)
        (existingChildren: ChildNode list)
        (package: DesktopImportPackage)
        (submissionId: System.Guid)
        : Ev =
        let existingNames =
            existingChildren
            |> List.choose (existingChildName graph)
            |> Set.ofList
        let commandName = commandForUnparsedDocument graph focusId

        let idToText =
            package.ops
            |> List.choose (function
                | Op.NewNode(id, text) -> Some(id, text)
                | _ -> None)
            |> Map.ofList

        let filteredIds =
            package.topLevelIds
            |> List.filter (fun id ->
                match Map.tryFind id idToText with
                | None -> true
                | Some text ->
                    match entryNameFromText text with
                    | None -> true
                    | Some name -> not (Set.contains name existingNames))

        if filteredIds.IsEmpty then
            { id = EventId.zero
              submissionId = submissionId
              authority = Authority "Browser"
              commandName = commandName
              body = EventBody.Change [] }
        else
            let filteredIdSet = Set.ofList filteredIds

            let filteredOps =
                package.ops
                |> List.filter (function
                    | Op.NewNode(id, _) -> Set.contains id filteredIdSet
                    | Op.Replace(parentId, _, _) -> Set.contains parentId filteredIdSet
                    | _ -> true)

            let attach =
                ChildListWire.append focusId existingChildren (ownedChildren filteredIds)

            { id = EventId.zero
              submissionId = submissionId
              authority = Authority "Browser"
              commandName = commandName
              body = EventBody.Change(filteredOps @ [ attach ]) }
