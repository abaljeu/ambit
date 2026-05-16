namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module RedirectRewrite =
    let private sameOrigin (left: Uri) (right: Uri) =
        String.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
        && String.Equals(left.IdnHost, right.IdnHost, StringComparison.OrdinalIgnoreCase)
        && left.Port = right.Port

    let private normalizeBasePath (path: string) =
        if String.IsNullOrWhiteSpace path || path = "/" then
            "/"
        else
            path.TrimEnd '/'

    let private isInAppPath (cloudAppUrl: Uri) (redirectUri: Uri) =
        let basePath = normalizeBasePath cloudAppUrl.AbsolutePath

        basePath = "/"
        || redirectUri.AbsolutePath.Equals(basePath, StringComparison.OrdinalIgnoreCase)
        || redirectUri.AbsolutePath.StartsWith(
            basePath + "/",
            StringComparison.OrdinalIgnoreCase)

    let rewriteLocation (cloudAppUrl: Uri) (localUrl: Uri) (location: string) =
        match Uri.TryCreate(location, UriKind.Absolute) with
        | true, redirectUri when sameOrigin cloudAppUrl redirectUri ->
            if isInAppPath cloudAppUrl redirectUri then
                let builder = UriBuilder redirectUri
                builder.Scheme <- localUrl.Scheme
                builder.Host <- localUrl.Host
                builder.Port <- localUrl.Port
                string builder.Uri
            else
                location
        | _ -> location
