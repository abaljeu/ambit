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

    /// Replay log entries from `startEntryIndex` through end of index (inclusive range).
    let replayLogFromIndex
        (logStream: FileStream)
        (offsetIndex: int64 ResizeArray)
        (startEntryIndex: int)
        (initial: State)
        : State =
        if offsetIndex.Count = 0 || startEntryIndex > offsetIndex.Count - 1 then
            initial
        else
            [ startEntryIndex .. offsetIndex.Count - 1 ]
            |> List.fold
                (fun st i ->
                    let _, json = ChangeLog.readEntryAt logStream offsetIndex.[i]

                    match ChangeLog.decodeChange json with
                    | Error _ -> st
                    | Ok change ->
                        match History.applyChange change st with
                        | ApplyResult.Changed newState ->
                            { newState with revision = Revision (st.revision.Value + 1) }
                        | _ -> st)
                initial

    let private readMetaRevision (metaPath: string) : Revision =
        if File.Exists(metaPath) then
            Revision(System.Int32.Parse(File.ReadAllText(metaPath).Trim()))
        else
            Revision 0

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

    let private replayFromMetaAndLog (metaPath: string) (logPath: string) (initialGraph: Graph) : State =
        let initialRevision = readMetaRevision metaPath

        use logStream =
            new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite)

        let offsetIndex = ChangeLog.buildIndex logStream

        let st0 =
            { graph = initialGraph
              history = History.empty
              revision = initialRevision }

        replayLogFromIndex logStream offsetIndex initialRevision.Value st0

    /// Read `.amb` network or legacy snapshot + meta + replay `.log`.
    let tryLoadState (dataDir: string) (filename: string) : Result<State, string> =
        let snapshotPath = Path.Combine(dataDir, filename)
        let metaPath = snapshotPath + ".meta"
        let logPath = snapshotPath + ".log"
        let hadArtifacts = DocumentPersistence.hasArtifactSet dataDir

        if not hadArtifacts && File.Exists(snapshotPath) then
            ensureSnapshotBackup snapshotPath

        match loadGraphFromDisk dataDir snapshotPath with
        | Error msg -> Error msg
        | Ok initialGraph ->
            let afterReplay = replayFromMetaAndLog metaPath logPath initialGraph

            if hadArtifacts then
                Ok afterReplay
            else
                match DocumentPersistence.writeAllDocuments dataDir afterReplay.graph with
                | Error msg -> Error msg
                | Ok _ -> Ok afterReplay

    /// Fail-fast wrapper around `tryLoadState`.
    let loadState (dataDir: string) (filename: string) : State =
        match tryLoadState dataDir filename with
        | Ok state -> state
        | Error msg -> failwith msg

    /// Write a file-format backup from DB state without reading existing document files.
    let writeStateBackup (dataDir: string) (filename: string) (state: State) : unit =
        Directory.CreateDirectory(dataDir) |> ignore
        let snapshotPath = Path.Combine(dataDir, filename)
        let metaPath = snapshotPath + ".meta"
        let logPath = snapshotPath + ".log"

        match DocumentPersistence.writeAllDocuments dataDir state.graph with
        | Error msg -> failwith msg
        | Ok _ -> ()

        File.WriteAllText(metaPath, string state.revision.Value)
        File.WriteAllText(logPath, "")
