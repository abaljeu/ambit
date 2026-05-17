namespace Gambol.Shared

open System

module LoginRedirect =
    let private locationPath (location: string) =
        let withoutFragment = location.Split('#').[0]
        let pathOnly = withoutFragment.Split('?').[0]

        if pathOnly.StartsWith("http", StringComparison.OrdinalIgnoreCase) then
            Uri(pathOnly).AbsolutePath
        else
            pathOnly

    /// True when the server accepted credentials (redirect to /ambit, not back to login).
    let isSuccess (statusCode: int) (locations: seq<string>) =
        let isRedirect = statusCode >= 300 && statusCode < 400

        if not isRedirect then
            false
        else
            locations
            |> Seq.exists (fun loc ->
                let path = locationPath loc
                (path = "/ambit" || path.EndsWith("/ambit", StringComparison.Ordinal))
                && path.IndexOf("/login") < 0)
