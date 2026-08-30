namespace Gambol.Shared

open System
open System.Text.RegularExpressions

/// Locate an http(s), mailto, www…, or scheme-less hostname URL in plain text (after HTML is
/// stripped elsewhere). Hostnames without a scheme are opened as https.
[<RequireQualifiedAccess>]
module OpenTarget =

    let private reHttp =
        Regex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase)

    let private reMailto =
        Regex(@"mailto:[^\s<>""']+", RegexOptions.IgnoreCase)

    let private reWww =
        Regex(@"\bwww\.[^\s<>""']+", RegexOptions.IgnoreCase)

    /// Hostname with at least one dot and a letter-only TLD (2+ chars), optional port and path.
    /// Excludes `www.` (handled by `reWww`). Prepended with https:// when matched.
    let private reBareHost =
        Regex(
            @"\b(?!www\.)(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}"
            + @"(?::[0-9]{1,5})?(?:/[^\s<>""']*)?",
            RegexOptions.IgnoreCase
        )

    let private trimTrailingGlue (s: string) : string =
        let glue = Set.ofList [ ')'; ']'; '.'; ','; ';' ]

        let rec go (t: string) =
            if t.Length = 0 then
                t
            elif Set.contains t.[t.Length - 1] glue then
                go (t.Substring(0, t.Length - 1))
            else
                t

        go s

    let private dangerousScheme (s: string) : bool =
        let u = s.TrimStart().ToLowerInvariant()

        u.StartsWith("javascript:")
        || u.StartsWith("data:")
        || u.StartsWith("vbscript:")

    let private firstMatch (re: Regex) (s: string) : (int * string) option =
        let m = re.Match(s)
        if m.Success then Some (m.Index, m.Value) else None

    let private bestMatchRaw (s: string) : string option =
        let pairs =
            [ firstMatch reHttp s
              firstMatch reMailto s
              firstMatch reWww s
              firstMatch reBareHost s ]
            |> List.choose id

        match pairs with
        | [] -> None
        | ps -> ps |> List.minBy fst |> snd |> Some

    /// `mailto:` and explicit `://` unchanged; otherwise prepend `https://` (www or bare host).
    let private normalizeCandidate (trimmed: string) : string =
        let t = trimmed.Trim()
        if t.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) then t
        elif t.Contains("://") then t
        else "https://" + t

    let private acceptOpenable (candidate: string) : string option =
        if dangerousScheme candidate then
            None
        else
            let mutable parsed: Uri = Unchecked.defaultof<_>

            if Uri.TryCreate(candidate, UriKind.Absolute, &parsed) then
                match parsed.Scheme.ToLowerInvariant() with
                | "http"
                | "https" -> Some parsed.AbsoluteUri
                | "mailto" -> Some parsed.OriginalString
                | _ -> None
            else
                None

    /// First openable URI in `text`, or None. Does not walk the graph; use
    /// `tryFindOpenableUriWithFirstChildFallback` for parent-then-first-child.
    let tryFindOpenableUri (text: string) : string option =
        if String.IsNullOrWhiteSpace text then
            None
        else
            match bestMatchRaw text with
            | None -> None
            | Some raw ->
                let trimmed = trimTrailingGlue raw
                let normalized = normalizeCandidate trimmed
                acceptOpenable normalized

    /// Try primary plain text, then optional first child plain text (one hop only).
    let tryFindOpenableUriWithFirstChildFallback
        (primaryPlain: string)
        (firstChildPlainOpt: string option)
        : string option =
        match tryFindOpenableUri primaryPlain with
        | Some u -> Some u
        | None -> firstChildPlainOpt |> Option.bind tryFindOpenableUri
