namespace Gambol.Shared

open System

type DesktopImportPackage =
    { sourcePath: string
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

    /// One change: package paste ops plus replace-all-children on the focus node.
    let buildImportChange
        (focusId: NodeId)
        (existingChildren: ChildNode list)
        (package: DesktopImportPackage)
        (revision: int)
        (changeId: System.Guid)
        : Change =
        let attach =
            Op.Replace(focusId, 0, existingChildren, ownedChildren package.topLevelIds)

        { id = revision
          changeId = changeId
          ops = package.ops @ [ attach ] }

