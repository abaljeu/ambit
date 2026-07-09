namespace Gambol.Shared

open System
open System.IO

type WorkspaceLinkMode =
    | CreateNew
    | LinkExisting

type InitialSyncDirection =
    | Download
    | Upload
    | Skip

[<RequireQualifiedAccess>]
module WorkspaceConnect =

    let private invalidLabelChars =
        [| '/'; '\\'; ':'; '*'; '?'; '"'; '<'; '>'; '|' |]

    let defaultLabelFromRoot (gitRoot: string) : string =
        if isNull gitRoot || gitRoot.Trim() = "" then
            "workspace"
        else
            let trimmed =
                gitRoot.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)

            let name = Path.GetFileName trimmed

            if isNull name || name = "" then
                "workspace"
            else
                name.ToLowerInvariant()

    let validateLabel (label: string) : Result<string, string> =
        let trimmed = if isNull label then "" else label.Trim()

        if trimmed = "" then
            Error "Label is required"
        elif trimmed.IndexOfAny invalidLabelChars >= 0 then
            Error "Invalid label characters"
        else
            Ok trimmed

    let appBaseUrl (origin: string) (pathname: string) : string =
        let path =
            if isNull pathname || pathname = "" then ""
            elif pathname.StartsWith "/" then pathname
            else "/" + pathname

        (if isNull origin then "" else origin.TrimEnd('/')) + path

    let gatewayUrlForLabel (origin: string) (pathname: string) (label: string) : string =
        WorkspaceGitRemote.gatewayUrl (appBaseUrl origin pathname) label

    let resolveEffectiveLabel (linkMode: WorkspaceLinkMode) (labelInput: string) : Result<string, string> =
        match linkMode with
        | CreateNew -> validateLabel labelInput
        | LinkExisting -> validateLabel labelInput

    let shouldCreateWorkspace (graph: Graph) (linkMode: WorkspaceLinkMode) (label: string) =
        match linkMode with
        | CreateNew -> not (WorkspaceGitRemote.findWorkspaceNodeId graph label).IsSome
        | LinkExisting -> false

    let validateLinkTarget (graph: Graph) (linkMode: WorkspaceLinkMode) (label: string) : Result<unit, string> =
        match linkMode with
        | CreateNew -> Ok()
        | LinkExisting ->
            match WorkspaceGitRemote.findWorkspaceNodeId graph label with
            | Some _ -> Ok()
            | None -> Error "Workspace not found on server"
