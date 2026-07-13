namespace Gambol.Server

open System
open System.Diagnostics
open System.IO
open Gambol.Shared

/// Reject graph disk effects whose destination is ignored by `.gitignore`.
[<RequireQualifiedAccess>]
module IgnoredDestination =

    let private normalizeRel (path: string) =
        path.Replace('\\', '/').TrimStart('/')

    let private isGitignorePath (relativePath: string) =
        let n = normalizeRel(relativePath).TrimEnd('/')
        n = ".gitignore" || n.EndsWith("/.gitignore")

    let private emptyGitDir =
        lazy (
            let dir =
                Path.Combine(Path.GetTempPath(), "gambol-check-ignore-git")
            Directory.CreateDirectory dir |> ignore
            let gitDir = Path.Combine(dir, ".git")

            if not (Directory.Exists gitDir) then
                match GitSave.runGit dir "init -q" with
                | Ok _ -> ()
                | Error _ -> ()

            gitDir)

    let private checkIgnored
        (workTree: string)
        (relativePath: string)
        : Result<bool, string> =
        if String.IsNullOrWhiteSpace relativePath then
            Ok false
        else
            try
                Directory.CreateDirectory workTree |> ignore
                let psi =
                    ProcessStartInfo(
                        FileName = "git",
                        WorkingDirectory = workTree,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false)
                psi.ArgumentList.Add("check-ignore")
                psi.ArgumentList.Add("-q")
                psi.ArgumentList.Add("--no-index")
                psi.ArgumentList.Add("--")
                psi.ArgumentList.Add(relativePath)
                psi.Environment["GIT_DIR"] <- emptyGitDir.Value
                psi.Environment["GIT_WORK_TREE"] <- workTree
                use proc = Process.Start(psi)
                proc.WaitForExit()

                match proc.ExitCode with
                | 0 -> Ok true
                | 1 -> Ok false
                | _ ->
                    let detail =
                        let stderr = proc.StandardError.ReadToEnd().Trim()
                        let stdout = proc.StandardOutput.ReadToEnd().Trim()

                        if String.IsNullOrWhiteSpace stderr then stdout
                        else stderr

                    Error (
                        if String.IsNullOrWhiteSpace detail then
                            "git check-ignore failed"
                        else
                            detail)
            with ex ->
                Error ex.Message

    let private artifactRelative (graph: Graph) (nodeId: NodeId) =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node ->
            match node.kind with
            | Special File ->
                DocumentPartition.artifactFileRelative graph nodeId
            | Special (Workspace | Directory) ->
                DocumentPartition.artifactDirectoryRelative graph nodeId
            | _ -> None

    let private ignoreScope
        (dataDir: string)
        (graph: Graph)
        (nodeId: NodeId)
        (artifactRel: string)
        : (string * string) option =
        let rel = normalizeRel artifactRel
        let dataRoot = DataDir.normalize dataDir

        match GraphQuery.enclosingWorkspace graph nodeId with
        | Some wsId when wsId = Graph.rootId || wsId = Graph.trashId ->
            Some(dataRoot, rel)
        | Some wsId ->
            match Map.tryFind wsId graph.nodes with
            | Some node ->
                match Filename.tryValue node.name with
                | Some name ->
                    let prefix = name + "/"
                    let scoped =
                        if rel = name || rel = name + "/" then ""
                        elif rel.StartsWith(prefix, StringComparison.Ordinal) then
                            rel.Substring(prefix.Length)
                        else
                            rel

                    Some(Path.Combine(dataRoot, name), scoped)
                | None -> None
            | None -> None
        | None -> Some(dataRoot, rel)

    let private destinationEffects
        (preGraph: Graph)
        (postGraph: Graph)
        : (NodeId * string) list =
        postGraph.nodes
        |> Map.toSeq
        |> Seq.choose (fun (nodeId, _) ->
            if not (DocumentPartition.isDocumentRootNode postGraph nodeId) then
                None
            else
                match artifactRelative postGraph nodeId with
                | None -> None
                | Some newPath ->
                    let oldPath = artifactRelative preGraph nodeId

                    match oldPath with
                    | Some old when normalizeRel old = normalizeRel newPath ->
                        None
                    | _ -> Some(nodeId, newPath))
        |> Seq.toList
        |> List.sortBy (fun (nodeId, _) -> nodeId.Value)

    let private validateDestination
        (dataDir: string)
        (graph: Graph)
        (nodeId: NodeId)
        (artifactRel: string)
        : Result<unit, string> =
        if isGitignorePath artifactRel then
            Ok ()
        else
            match ignoreScope dataDir graph nodeId artifactRel with
            | None -> Ok ()
            | Some(_, "") -> Ok ()
            | Some(workTree, relativePath) ->
                match checkIgnored workTree relativePath with
                | Error err -> Error err
                | Ok false -> Ok ()
                | Ok true ->
                    Error (
                        "destination path ignored by .gitignore: "
                        + normalizeRel artifactRel)

    let validateGraphDiskEffects
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        : Result<unit, string> =
        destinationEffects preGraph postGraph
        |> List.fold
            (fun acc (nodeId, artifactRel) ->
                match acc with
                | Error msg -> Error msg
                | Ok () ->
                    validateDestination dataDir postGraph nodeId artifactRel)
            (Ok ())
