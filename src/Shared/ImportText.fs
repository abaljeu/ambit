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

