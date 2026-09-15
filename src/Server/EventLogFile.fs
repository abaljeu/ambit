namespace Gambol.Server

open System.IO
open Gambol.Shared.Events

module EventEnc = Thoth.Json.Newtonsoft.Encode
module EventDec = Thoth.Json.Newtonsoft.Decode

/// Append-only Event JSON beside ChangeLog (`SYSTEM/gambol.events`).
[<RequireQualifiedAccess>]
module EventLogFile =

    let eventsPath (dataDir: string) =
        Path.Combine(Bookkeeping.systemDir dataDir, "gambol.events")

    let openStream (dataDir: string) : FileStream =
        Directory.CreateDirectory(Bookkeeping.systemDir dataDir)
        |> ignore
        new FileStream(
            eventsPath dataDir,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite)

    let encode (event: Event) : string =
        EventEnc.toString 0 (EventJson.encode event)

    let decode (json: string) : Result<Event, string> =
        EventDec.fromString EventJson.decode json

    let readAll
        (stream: FileStream)
        (offsets: int64 ResizeArray)
        : Event list =
        [ 0 .. offsets.Count - 1 ]
        |> List.choose (fun i ->
            let _, json = ChangeLog.readEntryAt stream offsets.[i]
            match decode json with
            | Ok event -> Some event
            | Error _ -> None)

    let append
        (stream: FileStream)
        (offsets: int64 ResizeArray)
        (event: Event)
        : Result<unit, string> =
        let (EventId n) = event.id
        let startLen = stream.Length
        stream.Seek(0L, SeekOrigin.End) |> ignore
        try
            let offset =
                ChangeLog.appendEntry stream n (encode event)
            offsets.Add offset
            Ok ()
        with ex ->
            stream.SetLength(startLen)
            stream.Seek(0L, SeekOrigin.End) |> ignore
            Error $"Event log error: {ex.Message}"
