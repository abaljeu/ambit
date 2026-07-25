namespace Gambol.Server

open System.IO
open Gambol.Shared

/// Load canonical document state from disk (same semantics as `FileAgent` startup).
[<RequireQualifiedAccess>]
module DocumentLoader =

    /// Daily `.bak.yyyyMMdd` copy and prune (same policy as `FileAgent`).
    let ensureSnapshotBackup (snapshotPath: string) =
        if File.Exists(snapshotPath) then
            let dateStamp = System.DateTime.Today.ToString("yyyyMMdd")
            let backupPath = snapshotPath + ".bak." + dateStamp
            if not (File.Exists(backupPath)) then
                File.Copy(snapshotPath, backupPath)
            let dir = Path.GetDirectoryName(snapshotPath)
            let prefix = Path.GetFileName(snapshotPath) + ".bak."

            let backups =
                Directory.GetFiles(dir, prefix + "*")
                |> Array.sort
                |> Array.toList

            let excess = backups.Length - 30
            if excess > 0 then
                backups |> List.take excess |> List.iter File.Delete

    let private loadGraphFromDisk (dataDir: string) (snapshotPath: string) : Result<Graph, string> =
        if DocumentPersistence.hasArtifactSet dataDir then
            DocumentPersistence.readAllDocuments dataDir
        else
            let graph =
                if File.Exists(snapshotPath) then
                    Snapshot.read (File.ReadAllText(snapshotPath))
                else
                    Graph.create ()
            Ok graph

    let private stateFromGraph (graph: Graph) : State =
        { graph = graph
          history = History.empty
          revision = Revision 0 }

    /// Read `.amb` network or legacy snapshot (materializing `.amb` when needed).
    let tryLoadState (dataDir: string) (filename: string) : Result<State, string> =
        let snapshotPath = Path.Combine(dataDir, filename)

        match loadGraphFromDisk dataDir snapshotPath with
        | Error msg -> Error msg
        | Ok initialGraph -> Ok (stateFromGraph initialGraph)

    /// Fail-fast wrapper around `tryLoadState`.
    let loadState (dataDir: string) (filename: string) : State =
        match tryLoadState dataDir filename with
        | Ok state -> state
        | Error msg -> failwith msg
