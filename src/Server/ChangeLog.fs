namespace Gambol.Server

open System
open System.IO
open System.Text
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

/// Append-only change log with O(1) lookup by change number.
///
/// File format: each record is one line:
///   [8-char zero-padded decimal change id][compact JSON Change][newline]
///
/// On startup, scan the file to build an in-memory offset index
/// (ResizeArray where index.[i] is the byte offset of entry i).
/// Then readEntryAt seeks directly to any record.
[<RequireQualifiedAccess>]
module ChangeLog =

    let private headerLength = 8

    // ------------------------------------------------------------------
    // Write
    // ------------------------------------------------------------------

    /// Append a change record to the log. Caller must provide the
    /// compact JSON representation of the Change.
    let appendEntry (stream: FileStream) (changeId: int) (json: string) : int64 =
        let offset = stream.Position
        let header = sprintf "%08d" changeId
        let line = header + json + "\n"
        let bytes = Encoding.UTF8.GetBytes(line)
        stream.Write(bytes, 0, bytes.Length)
        stream.Flush()
        offset

    // ------------------------------------------------------------------
    // Read / index
    // ------------------------------------------------------------------

    /// Scan the entire log from the beginning, building the offset index.
    /// Returns a ResizeArray where index.[i] is the byte offset of entry i.
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

    /// Read a single record at the given byte offset.
    /// Returns (changeId, jsonPayload).
    let readEntryAt (stream: FileStream) (offset: int64) : int * string =
        stream.Seek(offset, SeekOrigin.Begin) |> ignore
        let buf = ResizeArray<byte>()
        let mutable b = stream.ReadByte()
        while b >= 0 && b <> int '\n' do
            buf.Add(byte b)
            b <- stream.ReadByte()
        let line = Encoding.UTF8.GetString(buf.ToArray())
        let changeId = Int32.Parse(line.Substring(0, headerLength))
        let json = line.Substring(headerLength)
        changeId, json

    // ------------------------------------------------------------------
    // JSON helpers (use Thoth.Json.Newtonsoft via Server aliases)
    // ------------------------------------------------------------------

    /// Encode a Change to compact JSON.
    let encodeChange (change: Change) : string =
        Encode.toString 0 (Serialization.encodeChange change)

    /// Decode a Change from JSON.
    let decodeChange (json: string) : Result<Change, string> =
        Decode.fromString Serialization.decodeChange json
