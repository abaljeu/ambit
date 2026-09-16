namespace Gambol.Server

open System
open System.IO
open System.Text
open Gambol.Shared
open Gambol.Shared.Events

module EventEnc = Thoth.Json.Newtonsoft.Encode
module EventDec = Thoth.Json.Newtonsoft.Decode
module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

/// Append-only JSON log for Events and Changes.
[<RequireQualifiedAccess>]
module EventLogFile =

    // ------------------------------------------------------------------
    // Generic append-only log operations
    // ------------------------------------------------------------------

    let private headerLength = 8

    /// Append a single entry to the log with an 8-char zero-padded decimal id.
    let appendEntry (stream: FileStream) (id: int) (json: string) : int64 =
        let offset = stream.Position
        let header = sprintf "%08d" id
        let line = header + json + Environment.NewLine
        let bytes = Encoding.UTF8.GetBytes(line)
        stream.Write(bytes, 0, bytes.Length)
        stream.Flush()
        offset

    /// Append multiple entries to the log.
    let appendEntries (stream: FileStream) (entries: (int * string) list) : int64 list =
        let startOffset = stream.Position
        let lines =
            entries
            |> List.map (fun (id, json) ->
                let header = sprintf "%08d" id
                header + json + Environment.NewLine)
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
            let mutable totalRead = 0
            while totalRead < length do
                let n = stream.Read(bytes, totalRead, length - totalRead)
                totalRead <- totalRead + n
            index.Add(0L)
            for i in 0 .. length - 1 do
                if bytes.[i] = byte '\n' && i + 1 < length then
                    index.Add(int64 (i + 1))
        index

    /// Read a single record at the given byte offset. Returns (id, jsonPayload).
    let readEntryAt (stream: FileStream) (offset: int64) : int * string =
        stream.Seek(offset, SeekOrigin.Begin) |> ignore
        let buf = ResizeArray<byte>()
        let mutable b = stream.ReadByte()
        while b >= 0 && b <> int '\n' do
            buf.Add(byte b)
            b <- stream.ReadByte()
        let line = Encoding.UTF8.GetString(buf.ToArray()).TrimEnd('\r')
        let id = Int32.Parse(line.Substring(0, headerLength))
        let json = line.Substring(headerLength)
        id, json

    // ------------------------------------------------------------------
    // Event-specific operations
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

    let encodeEvent (event: Event) : string =
        EventEnc.toString 0 (EventJson.encode event)

    let decodeEvent (json: string) : Result<Event, string> =
        EventDec.fromString EventJson.decode json

    let readAllEvents
        (stream: FileStream)
        (offsets: int64 ResizeArray)
        : Event list =
        [ 0 .. offsets.Count - 1 ]
        |> List.choose (fun i ->
            let _, json = readEntryAt stream offsets.[i]
            match decodeEvent json with
            | Ok event -> Some event
            | Error _ -> None)

    let appendEvent
        (stream: FileStream)
        (offsets: int64 ResizeArray)
        (event: Event)
        : Result<unit, string> =
        let (EventId n) = event.id
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

