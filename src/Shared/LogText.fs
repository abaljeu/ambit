module Gambol.Shared.LogText

/// Truncate `text` for log lines; longer values end with "...".
let truncateForLog (maxLen: int) (text: string) : string =
    if text.Length <= maxLen then
        text
    else
        text.Substring(0, maxLen) + "..."

/// Truncate `text` for compact UI labels; longer values end with U+2026 ellipsis.
let truncateForDisplay (maxLen: int) (text: string) : string =
    if text.Length <= maxLen then
        text
    else
        text.Substring(0, maxLen - 1)
