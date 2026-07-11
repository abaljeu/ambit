namespace Gambol.Desktop

open System
open System.IO
open System.Text.Json
open System.Threading.Tasks
open Gambol.Shared
open Microsoft.AspNetCore.Http

/// `/_desktop/workspace-mappings`, `/_desktop/pick-folder`, `/_desktop/detect-git`.
[<RequireQualifiedAccess>]
module WorkspaceMappingEndpoints =

    let private quoteJson (text: string) =
        JsonSerializer.Serialize text

    let private writeJson (context: HttpContext) (json: string) = task {
        context.Response.StatusCode <- StatusCodes.Status200OK
        context.Response.ContentType <- "application/json; charset=utf-8"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private writeBadRequest (context: HttpContext) (message: string) = task {
        context.Response.StatusCode <- StatusCodes.Status400BadRequest
        context.Response.ContentType <- "application/json; charset=utf-8"
        let json = "{\"error\":" + quoteJson message + "}"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private readBody (context: HttpContext) = task {
        use reader = new StreamReader(context.Request.Body)
        return! reader.ReadToEndAsync(context.RequestAborted)
    }

    let private tryGetString (root: JsonElement) (name: string) =
        let mutable value = Unchecked.defaultof<JsonElement>
        if not (root.TryGetProperty(name, &value)) then
            None
        elif value.ValueKind <> JsonValueKind.String then
            None
        else
            match value.GetString() with
            | null -> None
            | s when s.Trim().Length = 0 -> None
            | s -> Some (s.Trim())

    let private tryGetBool (root: JsonElement) (name: string) =
        let mutable value = Unchecked.defaultof<JsonElement>
        if not (root.TryGetProperty(name, &value)) then
            None
        elif value.ValueKind = JsonValueKind.True then
            Some true
        elif value.ValueKind = JsonValueKind.False then
            Some false
        else
            None

    let private currentMappings
        (workspaceMap: Map<string, WorkspaceMapping> ref)
        : WorkspaceMappings =
        { entries =
            workspaceMap.Value
            |> Map.toList
            |> List.map snd }

    let private persist
        (configPath: string)
        (workspaceMap: Map<string, WorkspaceMapping> ref)
        (mappings: WorkspaceMappings)
        : Result<unit, string> =
        match WorkspaceLocalMapping.saveToFile configPath mappings with
        | Error e -> Error e
        | Ok () ->
            workspaceMap.Value <- WorkspaceLocalMapping.toMap mappings
            Ok ()

    let private handleGet
        (workspaceMap: Map<string, WorkspaceMapping> ref)
        (context: HttpContext)
        = task {
        let json = WorkspaceLocalMapping.encode (currentMappings workspaceMap)
        do! writeJson context json
    }

    let private decodePutBody
        (body: string)
        : Result<WorkspaceMappings -> Result<WorkspaceMappings, string>, string> =
        try
            use document = JsonDocument.Parse body
            let root = document.RootElement
            let mutable mappingsValue = Unchecked.defaultof<JsonElement>

            if root.TryGetProperty("workspaceMappings", &mappingsValue) then
                match WorkspaceLocalMapping.decode body with
                | Error e -> Error e
                | Ok next -> Ok(fun _ -> Ok next)
            else
                match tryGetString root "label", tryGetString root "path" with
                | Some label, Some path ->
                    Ok(fun current -> WorkspaceLocalMapping.upsert current label path)
                | None, _ -> Error "label is required"
                | _, None -> Error "path is required"
        with
        | :? JsonException -> Error "invalid JSON"

    let private handlePut
        (configPath: string)
        (workspaceMap: Map<string, WorkspaceMapping> ref)
        (context: HttpContext)
        = task {
        let! body = readBody context
        match decodePutBody body with
        | Error message -> do! writeBadRequest context message
        | Ok apply ->
            match apply (currentMappings workspaceMap) with
            | Error message -> do! writeBadRequest context message
            | Ok next ->
                match persist configPath workspaceMap next with
                | Error message -> do! writeBadRequest context message
                | Ok () ->
                    do! writeJson context (WorkspaceLocalMapping.encode next)
    }

    let private handlePickFolder (context: HttpContext) = task {
        let! body = readBody context
        let requireGit =
            if String.IsNullOrWhiteSpace body then
                false
            else
                try
                    use document = JsonDocument.Parse body
                    tryGetBool document.RootElement "requireGit"
                    |> Option.defaultValue false
                with
                | :? JsonException -> false

        match FolderPicker.pickFolder () with
        | None -> do! writeJson context "{\"cancelled\":true}"
        | Some path ->
            match requireGit, WorkspaceLocalMapping.tryGitRoot path with
            | true, Error err -> do! writeBadRequest context err
            | true, Ok gitRoot ->
                let json =
                    "{\"cancelled\":false,\"path\":"
                    + quoteJson path
                    + ",\"gitRoot\":"
                    + quoteJson gitRoot
                    + "}"
                do! writeJson context json
            | false, Ok gitRoot ->
                let json =
                    "{\"cancelled\":false,\"path\":"
                    + quoteJson path
                    + ",\"gitRoot\":"
                    + quoteJson gitRoot
                    + "}"
                do! writeJson context json
            | false, Error _ ->
                let json =
                    "{\"cancelled\":false,\"path\":"
                    + quoteJson path
                    + ",\"gitRoot\":null}"
                do! writeJson context json
    }

    let private handleDetectGit (context: HttpContext) = task {
        let! body = readBody context
        try
            use document = JsonDocument.Parse body
            match tryGetString document.RootElement "path" with
            | None -> do! writeBadRequest context "path is required"
            | Some path ->
                match WorkspaceLocalMapping.tryGitRoot path with
                | Error err -> do! writeBadRequest context err
                | Ok gitRoot ->
                    let json = "{\"gitRoot\":" + quoteJson gitRoot + "}"
                    do! writeJson context json
        with
        | :? JsonException -> do! writeBadRequest context "invalid JSON"
    }

    let tryHandle
        (configPath: string)
        (workspaceMap: Map<string, WorkspaceMapping> ref)
        (context: HttpContext)
        : Task<bool> =
        task {
            let path = context.Request.Path
            if
                HttpMethods.IsGet context.Request.Method
                && path.Equals(PathString "/_desktop/workspace-mappings")
            then
                do! handleGet workspaceMap context
                return true
            elif
                HttpMethods.IsPut context.Request.Method
                && path.Equals(PathString "/_desktop/workspace-mappings")
            then
                do! handlePut configPath workspaceMap context
                return true
            elif
                HttpMethods.IsPost context.Request.Method
                && path.Equals(PathString "/_desktop/pick-folder")
            then
                do! handlePickFolder context
                return true
            elif
                HttpMethods.IsPost context.Request.Method
                && path.Equals(PathString "/_desktop/detect-git")
            then
                do! handleDetectGit context
                return true
            else
                return false
        }
