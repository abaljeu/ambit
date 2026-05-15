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

    let tryFindFirstFileReference (text: string) : Result<string, string> =
        if isNull text then
            Error "file reference not found"
        else
            let startIndex = text.IndexOf("[[", StringComparison.Ordinal)

            if startIndex < 0 then
                Error "file reference not found"
            else
                let pathStart = startIndex + 2
                let endIndex = text.IndexOf("]]", pathStart, StringComparison.Ordinal)

                if endIndex < 0 then
                    Error "file reference not found"
                else
                    let path = text.Substring(pathStart, endIndex - pathStart).Trim()

                    if path.Length = 0 then
                        Error "file reference is empty"
                    else
                        Ok path
