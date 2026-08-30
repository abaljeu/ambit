namespace Gambol.Shared

open System

/// Refuse outline parse for binary / non-text artifacts.
[<RequireQualifiedAccess>]
module DocumentBinary =

    let parseError = "cannot parse binary file"

    /// First N chars scanned for NUL (unknown-extension heuristic).
    let private contentProbeLength = 8192

    let private binaryExtensions =
        [| ".jpg"
           ".jpeg"
           ".png"
           ".gif"
           ".webp"
           ".bmp"
           ".ico"
           ".pdf"
           ".zip"
           ".exe"
           ".dll"
           ".obj"
           ".pdb"
           ".lib"
           ".ilk" |]

    let private normalizeRelative (relativePath: string) =
        relativePath.Replace('\\', '/').TrimStart('/')

    let extensionOf (relativePath: string) =
        let name =
            normalizeRelative relativePath
            |> fun path -> path.Split('/')
            |> Array.last

        let i = name.LastIndexOf('.')

        if i < 0 then
            ""
        else
            name.Substring(i).ToLowerInvariant()

    let isBinaryExtension (relativePath: string) : bool =
        let ext = extensionOf relativePath
        binaryExtensions |> Array.exists ((=) ext)

    /// True when a NUL appears in the leading content probe window.
    let looksLikeBinaryContent (text: string) : bool =
        if String.IsNullOrEmpty text then
            false
        else
            let n = min text.Length contentProbeLength

            let rec loop i =
                if i >= n then false
                elif text.[i] = '\000' then true
                else loop (i + 1)

            loop 0

    let refuseParse (relativePath: string) (text: string) : Result<unit, string> =
        if isBinaryExtension relativePath then
            Error parseError
        elif looksLikeBinaryContent text then
            Error parseError
        else
            Ok ()
