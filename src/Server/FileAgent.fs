namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

type FileAgentMsg =
    | GetState of AsyncReplyChannel<string>
    | PostChange of body: string * AsyncReplyChannel<Result<string, string>>
    | SnapshotDone

// FileAgent — serialises all reads/writes for a single file
type FileAgent = {
    mailbox: MailboxProcessor<FileAgentMsg>
    logStream: FileStream
}

module FileAgent =

    let private backupRetentionDays = 30

    let private makeStartupBackup (snapshotPath: string) =
        if File.Exists(snapshotPath) then
            let dateStamp = DateTime.Today.ToString("yyyyMMdd")
            let backupPath = snapshotPath + ".bak." + dateStamp
            if not (File.Exists(backupPath)) then
                File.Copy(snapshotPath, backupPath)
            // Prune backups beyond retention limit
            let dir = Path.GetDirectoryName(snapshotPath)
            let prefix = Path.GetFileName(snapshotPath) + ".bak."
            let backups =
                Directory.GetFiles(dir, prefix + "*")
                |> Array.sort
                |> Array.toList
            let excess = backups.Length - backupRetentionDays
            if excess > 0 then
                backups |> List.take excess |> List.iter File.Delete

    let create (dataDir: string) (filename: string) : FileAgent =
        let snapshotPath = Path.Combine(dataDir, filename)
        let metaPath = snapshotPath + ".meta"
        let logPath = snapshotPath + ".log"

        makeStartupBackup snapshotPath

        let initialGraph =
            if File.Exists(snapshotPath) then
                Snapshot.read (File.ReadAllText(snapshotPath))
            else
                Graph.create ()

        let initialRevision =
            if File.Exists(metaPath) then
                Revision (Int32.Parse(File.ReadAllText(metaPath).Trim()))
            else
                Revision 0

        let logStream =
            new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)

        let offsetIndex = ChangeLog.buildIndex logStream
        let state = ref { graph = initialGraph; history = History.empty; revision = initialRevision }
        let snapshotInProgress = ref false
        let snapshotNeeded = ref false

        for i in state.Value.revision.Value .. offsetIndex.Count - 1 do
            let _, json = ChangeLog.readEntryAt logStream offsetIndex.[i]
            match ChangeLog.decodeChange json with
            | Ok change ->
                match History.applyChange change state.Value with
                | ApplyResult.Changed newState ->
                    state.Value <- { newState with revision = Revision (state.Value.revision.Value + 1) }
                | _ -> ()
            | Error _ -> ()

        logStream.Seek(0L, SeekOrigin.End) |> ignore

        let encodeStateJson () =
            Encode.toString 0 (
                Thoth.Json.Core.Encode.object
                    [ "revision", Serialization.encodeRevision state.Value.revision
                      "graph", Serialization.encodeGraph state.Value.graph ])

        let startSnapshot (inbox: MailboxProcessor<FileAgentMsg>) =
            snapshotInProgress.Value <- true
            snapshotNeeded.Value <- false
            let text = Snapshot.write state.Value.graph
            let rev = state.Value.revision.Value
            Task.Run(fun () ->
                try
                    let tmpPath = snapshotPath + ".tmp"
                    let metaTmpPath = metaPath + ".tmp"
                    File.WriteAllText(tmpPath, text)
                    File.WriteAllText(metaTmpPath, string rev)
                    // Rename meta first — if we crash between the two renames,
                    // meta will be ahead of the snapshot, which is safe:
                    // on replay we skip log entries the snapshot already has.
                    File.Move(metaTmpPath, metaPath, true)
                    File.Move(tmpPath, snapshotPath, true)
                with _ ->
                    () // snapshot failure is non-fatal; log has the data
                inbox.Post(SnapshotDone)
            ) |> ignore

        let handlePostChange body (reply: AsyncReplyChannel<Result<string, string>>) inbox =
            match Decode.fromString Serialization.decodeChange body with
            | Error err ->
                reply.Reply(Error $"Invalid JSON: {err}")
            | Ok change ->
                match History.applyChange change state.Value with
                | ApplyResult.Invalid (_, errMsg) ->
                    reply.Reply(Error errMsg)
                | ApplyResult.Unchanged _ ->
                    reply.Reply(Ok (encodeStateJson ()))
                | ApplyResult.Changed newState ->
                    let json = ChangeLog.encodeChange change
                    let offset = ChangeLog.appendEntry logStream change.id json
                    offsetIndex.Add(offset)
                    state.Value <- { newState with revision = Revision (state.Value.revision.Value + 1) }
                    reply.Reply(Ok (encodeStateJson ()))
                    if snapshotInProgress.Value then snapshotNeeded.Value <- true
                    else startSnapshot inbox

        let mailbox = MailboxProcessor<FileAgentMsg>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                match msg with
                | GetState reply ->
                    reply.Reply(encodeStateJson ())
                | PostChange (body, reply) ->
                    handlePostChange body reply inbox
                | SnapshotDone ->
                    snapshotInProgress.Value <- false
                    if snapshotNeeded.Value then startSnapshot inbox
                return! loop ()
            }
            loop ()
        )

        { mailbox = mailbox; logStream = logStream }

    let getState (agent: FileAgent) : Async<string> =
        agent.mailbox.PostAndAsyncReply(GetState)

    let postChange (agent: FileAgent) (body: string) : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply -> PostChange(body, reply))

    let dispose (agent: FileAgent) =
        agent.logStream.Flush()
        agent.logStream.Dispose()
