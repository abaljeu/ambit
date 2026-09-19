namespace Gambol.Server

open System
open System.IO
open System.Text
open Gambol.Shared
open Gambol.Shared

module EventEnc = Thoth.Json.Newtonsoft.Encode
module EventDec = Thoth.Json.Newtonsoft.Decode

/// Append-only JSON log for Events and Changes.
[<RequireQualifiedAccess>]
module EventLogFile =

    // ------------------------------------------------------------------
    // Generic append-only log operations
    // ------------------------------------------------------------------

    let private headerLength = 8

    let private entryLine (id: int) (json: string) =
        sprintf "%08d" id + json + Environment.NewLine

    /// Append a single entry to the log with an 8-char zero-padded decimal id.
    let appendEntry (stream: FileStream) (id: int) (json: string) : int64 =
        let offset = stream.Position
        let bytes = Encoding.UTF8.GetBytes(entryLine id json)
        stream.Write(bytes, 0, bytes.Length)
        stream.Flush()
        offset

    /// Append multiple entries to the log.
    let appendEntries (stream: FileStream) (entries: (int * string) list) : int64 list =
        let startOffset = stream.Position
        let lines =
            entries
            |> List.map (fun (id, json) -> entryLine id json)
        let offsets =
            lines
            |> List.mapFold
                (fun offset line ->
                    let nextOffset = offset + int64 (Encoding.UTF8.GetByteCount line)
                    offset, nextOffset)
                startOffset
            |> fst
        let bytes = lines |> String.concat "" |> Encoding.UTF8.GetBytes
        stream.Write(bytes, 0, bytes.Length)
        stream.Flush()
        offsets

    /// Scan the entire log from the beginning, building an offset index.
    let buildIndex (stream: FileStream) : int64 ResizeArray =
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        let index = ResizeArray<int64>()
        let length = int stream.Length
        if length > 0 then
            let bytes = Array.zeroCreate length
            let rec fill totalRead =
                if totalRead < length then
                    let n = stream.Read(bytes, totalRead, length - totalRead)
                    fill (totalRead + n)
            fill 0
            index.Add(0L)
            for i in 0 .. length - 1 do
                if bytes.[i] = byte '\n' && i + 1 < length then
                    index.Add(int64 (i + 1))
        index

    /// Read a single record at the given byte offset. Returns (id, jsonPayload).
    let readEntryAt
        (stream: FileStream)
        (offset: int64)
        : Result<int * string, string> =
        stream.Seek(offset, SeekOrigin.Begin) |> ignore
        let buf = ResizeArray<byte>()
        let rec readLine () =
            match stream.ReadByte() with
            | b when b < 0 || b = int '\n' -> ()
            | b ->
                buf.Add(byte b)
                readLine ()
        readLine ()
        let line = Encoding.UTF8.GetString(buf.ToArray()).TrimEnd('\r')
        if line.Length < headerLength then
            Error "Event log entry shorter than header"
        else
            match Int32.TryParse(line.Substring(0, headerLength)) with
            | true, id -> Ok(id, line.Substring(headerLength))
            | false, _ -> Error "Invalid event log entry header"

    // ------------------------------------------------------------------
    // Ev-specific operations
    // ------------------------------------------------------------------

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

    let encodeEvent (event: Ev) : string =
        EventEnc.toString 0 (EventJson.encode event)

    let decodeEvent (json: string) : Result<Ev, string> =
        EventDec.fromString EventJson.decode json

    let readAllEvents
        (stream: FileStream)
        (offsets: int64 ResizeArray)
        : Ev list =
        [ 0 .. offsets.Count - 1 ]
        |> List.choose (fun i ->
            match readEntryAt stream offsets.[i] with
            | Ok(_, json) ->
                match decodeEvent json with
                | Ok event -> Some event
                | Error _ -> None
            | Error _ -> None)

    let truncate
        (stream: FileStream)
        (offsets: int64 ResizeArray)
        : unit =
        stream.SetLength 0L
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        offsets.Clear()

    let appendEvent
        (stream: FileStream)
        (offsets: int64 ResizeArray)
        (event: Ev)
        : Result<unit, string> =
        let n = EventId.value event.id
        let startLen = stream.Length
        stream.Seek(0L, SeekOrigin.End) |> ignore
        try
            let offset = appendEntry stream n (encodeEvent event)
            offsets.Add offset
            Ok ()
        with ex ->
            stream.SetLength(startLen)
            stream.Seek(0L, SeekOrigin.End) |> ignore
            Error $"Event log error: {ex.Message}"
