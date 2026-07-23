namespace Gambol.Shared

#if !FABLE_COMPILER
open System
open System.IO
open System.Text.Json

type WorkspaceMapping =
    { label: string
      rootPath: string }

type WorkspaceMappings =
    { entries: WorkspaceMapping list }

[<RequireQualifiedAccess>]
module WorkspaceLocalMapping =
    let private hasInvalidLabelChar (label: string) =
        label.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
        || label.Contains("/")
        || label.Contains("\\")

    let private validateLabel (label: string) =
        let trimmed = if isNull label then "" else label.Trim()

        if trimmed = "" then
            Error "invalid_workspace"
        elif hasInvalidLabelChar trimmed then
            Error "invalid_workspace"
        else
            Ok trimmed

    let private validateRootPath (path: string) =
        let trimmed = if isNull path then "" else path.Trim()

        if trimmed = "" then
            Error "invalid_path"
        elif not (Path.IsPathFullyQualified(trimmed)) then
            Error "invalid_path"
        else
            Ok trimmed

    let private parseEntries (root: JsonElement) : Result<WorkspaceMapping list, string> =
        let mutable mappingsValue = Unchecked.defaultof<JsonElement>
        let hasMappings = root.TryGetProperty("workspaceMappings", &mappingsValue)

        if not hasMappings then
            Ok []
        else
            let value = mappingsValue

            if value.ValueKind <> JsonValueKind.Array then
                Error "workspaceMappings must be an array"
            else
                value.EnumerateArray()
                |> Seq.toList
                |> List.mapi (fun i item ->
                    if item.ValueKind <> JsonValueKind.Object then
                        Error $"entry {i} must be an object"
                    else
                        let hasLabel, labelValue = item.TryGetProperty("label")
                        let hasPath, pathValue = item.TryGetProperty("path")

                        if not hasLabel then
                            Error $"entry {i} missing label"
                        elif not hasPath then
                            Error $"entry {i} missing path"
                        elif labelValue.ValueKind <> JsonValueKind.String then
                            Error $"entry {i} label must be a string"
                        elif pathValue.ValueKind <> JsonValueKind.String then
                            Error $"entry {i} path must be a string"
                        else
                            let label = labelValue.GetString()
                            let path = pathValue.GetString()

                            match validateLabel label, validateRootPath path with
                            | Ok validLabel, Ok validPath ->
                                Ok
                                    { label = validLabel
                                      rootPath = validPath }
                            | Error e, _ -> Error e
                            | _, Error e -> Error e)
                |> List.fold
                    (fun acc next ->
                        match acc, next with
                        | Ok xs, Ok x -> Ok (xs @ [ x ])
                        | Error e, _ -> Error e
                        | _, Error e -> Error e)
                    (Ok [])

    let private ensureNoDuplicates (entries: WorkspaceMapping list) =
        let mutable seen = Set.empty
        let mutable dup = None

        for entry in entries do
            let key = entry.label.ToLowerInvariant()

            if Set.contains key seen && dup.IsNone then
                dup <- Some entry.label
            else
                seen <- Set.add key seen

        match dup with
        | Some _ -> Error "duplicate_workspace"
        | None -> Ok entries

    let decode (json: string) : Result<WorkspaceMappings, string> =
        try
            use doc = JsonDocument.Parse(json)
            parseEntries doc.RootElement
            |> Result.bind ensureNoDuplicates
            |> Result.map (fun entries -> { entries = entries })
        with
        | :? JsonException -> Error "malformed_json"

    let encode (mappings: WorkspaceMappings) : string =
        let entries =
            mappings.entries
            |> List.map (fun e -> {| label = e.label; path = e.rootPath |})
            |> List.toArray
        JsonSerializer.Serialize({| workspaceMappings = entries |})

    let loadFromFile (path: string) : Result<WorkspaceMappings, string> =
        if not (File.Exists(path)) then
            Ok { entries = [] }
        else
            try
                let json = File.ReadAllText(path)
                decode json
            with
            | :? IOException -> Error "mapping_read_failed"
            | :? UnauthorizedAccessException -> Error "mapping_read_failed"

    let saveToFile (path: string) (mappings: WorkspaceMappings) : Result<unit, string> =
        try
            let dir = Path.GetDirectoryName(path)
            if not (String.IsNullOrEmpty dir) then
                Directory.CreateDirectory(dir) |> ignore
            File.WriteAllText(path, encode mappings)
            Ok ()
        with
        | :? IOException -> Error "mapping_write_failed"
        | :? UnauthorizedAccessException -> Error "mapping_write_failed"

    /// Insert or replace a label → root path entry (case-insensitive label key).
    let upsert
        (mappings: WorkspaceMappings)
        (label: string)
        (rootPath: string)
        : Result<WorkspaceMappings, string> =
        match validateLabel label, validateRootPath rootPath with
        | Error e, _ -> Error e
        | _, Error e -> Error e
        | Ok validLabel, Ok validPath ->
            let key = validLabel.ToLowerInvariant()
            let rest =
                mappings.entries
                |> List.filter (fun e -> e.label.ToLowerInvariant() <> key)
            Ok
                { entries =
                    rest
                    @ [ { label = validLabel; rootPath = validPath } ] }

    let toMap (mappings: WorkspaceMappings) =
        mappings.entries
        |> List.map (fun entry -> entry.label.ToLowerInvariant(), entry)
        |> Map.ofList

    /// Resolve a selected path to a git work-tree root (`.git` dir or parent of `.git`).
    let tryGitRoot (selectedPath: string) : Result<string, string> =
        let trimmed = if isNull selectedPath then "" else selectedPath.Trim()

        if trimmed = "" then
            Error "invalid_path"
        elif not (Path.IsPathFullyQualified(trimmed)) then
            Error "invalid_path"
        else
            try
                let full = Path.GetFullPath(trimmed)
                let name = Path.GetFileName(full)

                if String.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) then
                    match Path.GetDirectoryName(full) with
                    | null
                    | "" -> Error "not_a_git_repo"
                    | parent -> Ok parent
                elif Directory.Exists(Path.Combine(full, ".git")) then
                    Ok full
                else
                    Error "not_a_git_repo"
            with
            | :? ArgumentException -> Error "invalid_path"
            | :? NotSupportedException -> Error "invalid_path"
            | :? PathTooLongException -> Error "invalid_path"

    let private invalidFileNameCharSet = Path.GetInvalidFileNameChars() |> Set.ofArray
    let private appReservedCharSet = Set.ofList [ '#'; '^' ]

    let private isValidRelativeSegment (segment: string) =
        segment <> "" && segment <> ".."
        && segment |> Seq.forall (fun c ->
            not (Set.contains c invalidFileNameCharSet)
            && not (Set.contains c appReservedCharSet))

    let private validateRelativePath (rel: string) : Result<string, string> =
        if rel.StartsWith('/') || rel.EndsWith('/') then
            Error "invalid_path"
        else
            let segments = rel.Split('/')

            if segments |> Array.forall isValidRelativeSegment then
                Ok rel
            else
                Error "invalid_path"

    let resolvePath
        (workspaceToRoot: Map<string, WorkspaceMapping>)
        (workspaceLabel: string)
        (relativePath: string)
        : Result<string, string>
        =
        let normalizedLabel = if isNull workspaceLabel then "" else workspaceLabel.Trim().ToLowerInvariant()

        match Map.tryFind normalizedLabel workspaceToRoot with
        | None -> Error "invalid_workspace"
        | Some mapping ->
            // Directory desktop paths parse as "doc/"; trim trailing slash only.
            let rel =
                if isNull relativePath then ""
                else relativePath.Trim().TrimEnd('/')

            if rel = "" then
                Ok mapping.rootPath
            else
                match validateRelativePath rel with
                | Error e -> Error e
                | Ok _ ->
                    try
                        let combined = Path.Combine(mapping.rootPath, rel)
                        let resolved = Path.GetFullPath(combined)
                        let root = Path.GetFullPath(mapping.rootPath)

                        let prefix =
                            if root.EndsWith(Path.DirectorySeparatorChar) then root
                            else root + string Path.DirectorySeparatorChar

                        if resolved = root || resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
                            Ok resolved
                        else
                            Error "path_escape"
                    with
                    | :? ArgumentException -> Error "invalid_path"
                    | :? NotSupportedException -> Error "invalid_path"
                    | :? PathTooLongException -> Error "invalid_path"

    /// User-facing error when label has no desktop mapping.
    let missingMappingMessage (label: string) : string =
        sprintf "no local mapping for workspace '%s'" label
#endif
