module Gambol.Shared.LogText

open System

/// Truncate `text` for log lines; longer values end with "...".
let truncateForLog (maxLen: int) (text: string) : string =
    if text.Length <= maxLen then
        text
    else
        text.Substring(0, maxLen) + "..."

/// Truncate `text` for compact UI labels; longer values are capped at `maxLen`.
let truncateForDisplay (maxLen: int) (text: string) : string =
    if text.Length <= maxLen then
        text
    else
        text.Substring(0, maxLen)

let private htmlStartIndex (text: string) =
    let doc =
        text.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
    let html =
        text.IndexOf("<html", StringComparison.OrdinalIgnoreCase)
    match doc >= 0, html >= 0 with
    | true, true -> min doc html
    | true, false -> doc
    | false, true -> html
    | false, false -> -1

/// Collapse platform HTML bodies (e.g. Azure unavailable) for UI / logs.
let summarizeHttpBody (maxLen: int) (text: string) : string =
    if String.IsNullOrWhiteSpace text then
        ""
    else
        let unavailable =
            text.IndexOf(
                "Web App - Unavailable",
                StringComparison.OrdinalIgnoreCase)
            >= 0
        let htmlAt = htmlStartIndex text
        if unavailable || htmlAt >= 0 then
            let summary =
                if unavailable then "Azure web app unavailable"
                else "HTML error page"
            if htmlAt > 0 then
                text.Substring(0, htmlAt).TrimEnd() + " " + summary
            else
                summary
        else
            truncateForLog maxLen text

/// True when `text` begins with gzip or zlib deflate magic — compressed bytes that
/// were not transparently decompressed (missing or wrong Content-Encoding).
let looksCompressed (text: string) : bool =
    if text.Length < 2 then
        false
    else
        let b0 = int text.[0]
        let b1 = int text.[1]
        b0 = 0x1F && b1 = 0x8B
        || b0 = 0x78 && (b1 = 0x01 || b1 = 0x5E || b1 = 0x9C || b1 = 0xDA)
