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
            Error "import text is empty"

    let parseFirstFileReference (text: string) : FileReference =
        FileReference.parseFirst text

    let tryFindFirstFileReference (text: string) : Result<string, string> =
        match parseFirstFileReference text with
        | NoFileReference -> Error "file reference not found"
        | InvalidFileReference -> Error "file reference is invalid"
        | FileReference path -> Ok path

    let private ownedChildren (ids: NodeId list) : ChildNode list =
        ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

    let private markDocumentCurrentBeforeParse (graph: Graph) (focusId: NodeId) : Op list =
        match Map.tryFind focusId graph.nodes with
        | Some node
            when DocumentPartition.isDocumentRootNode graph focusId
                 && node.documentState = Unparsed ->
            [ Op.SetDocumentState(focusId, Unparsed, Current) ]
        | _ -> []

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

    /// One change: package paste ops plus replace-all-children on the focus node.
    let buildImportChange
        (graph: Graph)
        (focusId: NodeId)
        (existingChildren: ChildNode list)
        (package: DesktopImportPackage)
        (revision: int)
        (changeId: System.Guid)
        : Change =
        let attach =
            Op.Replace(focusId, 0, existingChildren, ownedChildren package.topLevelIds)
        let markCurrent = markDocumentCurrentBeforeParse graph focusId

        { id = revision
          changeId = changeId
          ops = markCurrent @ package.ops @ [ attach ] }

    /// Directory import: add only top-level entries whose names are not already children.
    let buildDirectoryMergeChange
        (graph: Graph)
        (focusId: NodeId)
        (existingChildren: ChildNode list)
        (package: DesktopImportPackage)
        (revision: int)
        (changeId: System.Guid)
        : Change =
        let existingNames =
            existingChildren
            |> List.choose (existingChildName graph)
            |> Set.ofList
        let markCurrent = markDocumentCurrentBeforeParse graph focusId

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
            { id = revision; changeId = changeId; ops = markCurrent }
        else
            let filteredIdSet = Set.ofList filteredIds

            let filteredOps =
                package.ops
                |> List.filter (function
                    | Op.NewNode(id, _) -> Set.contains id filteredIdSet
                    | Op.Replace(parentId, _, _, _) -> Set.contains parentId filteredIdSet
                    | _ -> true)

            let attach =
                Op.Replace(focusId, existingChildren.Length, [], ownedChildren filteredIds)

            { id = revision
              changeId = changeId
              ops = markCurrent @ filteredOps @ [ attach ] }
