namespace Gambol.Shared

/// Weak browser client hint for mutating requests (platform + UA snippet).
module ClientIdentity =

    /// HTTP header name. Client attaches via F# post wrappers.
    [<Literal>]
    let HeaderName = "X-Gambol-Client"

    let MaxLength = 120

    /// Strip CR/LF and truncate for safe header / log use.
    let normalize (raw: string) : string =
        if isNull raw then
            ""
        else
            let cleaned =
                raw.Replace("\r", " ").Replace("\n", " ").Trim()
            if cleaned.Length <= MaxLength then
                cleaned
            else
                cleaned.Substring(0, MaxLength)

    /// First non-empty normalized value from a header value list.
    let tryFromValues (values: string seq) : string option =
        values
        |> Seq.map normalize
        |> Seq.tryFind (fun s -> s.Length > 0)

    /// Commit subject with optional client hint.
    /// Example: `rev 42 | client: Win32; Mozilla/5.0…`
    /// Scrubs `"` so the string is safe for `git commit -m "…"`.
    let formatCommitMessage
        (baseMsg: string)
        (clientHint: string option)
        : string =
        let scrub (s: string) = s.Replace("\"", "'")
        let body =
            match clientHint |> Option.map normalize with
            | Some hint when hint.Length > 0 ->
                sprintf "%s | client: %s" baseMsg hint
            | _ -> baseMsg
        scrub body
