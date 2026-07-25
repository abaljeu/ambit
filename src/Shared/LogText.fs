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
