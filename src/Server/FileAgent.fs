namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

// ---------------------------------------------------------------------------
// MailboxProcessor messages
// ---------------------------------------------------------------------------

type FileAgentMsg =
    | GetState of AsyncReplyChannel<string>
    | PostChange of body: string * AsyncReplyChannel<Result<string, string>>
    | SnapshotDone

// ---------------------------------------------------------------------------
// FileAgent — serialises all reads/writes for a single file
// ---------------------------------------------------------------------------

type FileAgent(dataDir: string, filename: string) =

    let snapshotPath = Path.Combine(dataDir, filename)
    let metaPath = snapshotPath + ".meta"
    let logPath = snapshotPath + ".log"

    // ---- Load snapshot + meta ----

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

    // ---- Open / create log, build index, replay ----

    let logStream =
        new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)

    let offsetIndex = ChangeLog.buildIndex logStream

    let mutable state: State =
        { graph = initialGraph; history = History.empty }

    let mutable revision = initialRevision

    do
        // Replay log entries not yet reflected in the snapshot
        let startIdx = revision.Value
        for i in startIdx .. offsetIndex.Count - 1 do
            let _changeId, json = ChangeLog.readEntryAt logStream offsetIndex.[i]
            match ChangeLog.decodeChange json with
            | Ok change ->
                match History.applyChange change state with
                | ApplyResult.Changed newState ->
                    state <- newState
                    revision <- Revision (revision.Value + 1)
                | _ -> () // skip invalid / unchanged entries
            | Error _ -> ()

        // Position stream at end for appending
        logStream.Seek(0L, SeekOrigin.End) |> ignore

    // ---- Snapshot coalescing state ----

    let mutable snapshotInProgress = false
    let mutable snapshotNeeded = false

    // ---- Helpers ----

    let encodeStateJson () =
        Encode.toString 0 (
            Thoth.Json.Core.Encode.object
                [ "revision", Serialization.encodeRevision revision
                  "graph", Serialization.encodeGraph state.graph ])

    let startSnapshot (inbox: MailboxProcessor<FileAgentMsg>) =
        snapshotInProgress <- true
        snapshotNeeded <- false
        let text = Snapshot.write state.graph
        let rev = revision.Value
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
            with _ex ->
                () // snapshot failure is non-fatal; log has the data
            inbox.Post(SnapshotDone)
        ) |> ignore

    // ---- MailboxProcessor ----

    let agent = MailboxProcessor<FileAgentMsg>.Start(fun inbox ->
        let rec loop () = async {
            let! msg = inbox.Receive()
            match msg with

            | GetState reply ->
                reply.Reply(encodeStateJson ())
                return! loop ()

            | PostChange (body, reply) ->
                match Decode.fromString Serialization.decodeChange body with
                | Error err ->
                    reply.Reply(Error $"Invalid JSON: {err}")
                | Ok change ->
                    match History.applyChange change state with
                    | ApplyResult.Invalid (_, errMsg) ->
                        reply.Reply(Error errMsg)
                    | ApplyResult.Unchanged _ ->
                        reply.Reply(Ok (encodeStateJson ()))
                    | ApplyResult.Changed newState ->
                        // 1. Append to log (before replying — WAL-ish)
                        let json = ChangeLog.encodeChange change
                        let offset = ChangeLog.appendEntry logStream change.id json
                        offsetIndex.Add(offset)

                        // 2. Update in-memory state
                        state <- newState
                        revision <- Revision (revision.Value + 1)

                        // 3. Reply to client
                        reply.Reply(Ok (encodeStateJson ()))

                        // 4. Trigger coalescing snapshot
                        if snapshotInProgress then
                            snapshotNeeded <- true
                        else
                            startSnapshot inbox

            | SnapshotDone ->
                snapshotInProgress <- false
                if snapshotNeeded then
                    startSnapshot inbox

            return! loop ()
        }
        loop ()
    )

    // ---- Public API ----

    member _.GetState() = agent.PostAndAsyncReply(GetState)

    member _.PostChange(body: string) =
        agent.PostAndAsyncReply(fun reply -> PostChange(body, reply))

    interface IDisposable with
        member _.Dispose() =
            logStream.Flush()
            logStream.Dispose()
