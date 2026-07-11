namespace Gambol.Server

open System
open System.Security.Cryptography
open System.Text

module AuthToken =
    [<Literal>]
    let cookieName = "gambol_auth"

    [<Literal>]
    let gitBasicRealm = "Gambol Git"

    /// HMAC-SHA256 of username keyed by password, hex-encoded (matches server cookie value).
    let deriveToken (username: string) (password: string) =
        use hmac = new HMACSHA256(Encoding.UTF8.GetBytes(password))
        let hash: byte array = hmac.ComputeHash(Encoding.UTF8.GetBytes(username))
        Convert.ToHexString(hash).ToLowerInvariant()

    /// Git-scoped PAT: distinct from the browser cookie token (prefix "git:").
    let deriveGitToken (username: string) (password: string) =
        use hmac = new HMACSHA256(Encoding.UTF8.GetBytes(password))
        let payload = "git:" + username
        let hash: byte array = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))
        Convert.ToHexString(hash).ToLowerInvariant()

    let cookieHeaderValue (username: string) (password: string) =
        sprintf "%s=%s" cookieName (deriveToken username password)

    let basicAuthHeaderValue (username: string) (password: string) =
        let raw = sprintf "%s:%s" username password
        let b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
        "Basic " + b64

    let tryParseBasicAuth (header: string) : (string * string) option =
        if isNull header then
            None
        else
            let trimmed = header.Trim()
            if not (trimmed.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) then
                None
            else
                try
                    let b64 = trimmed.Substring(6).Trim()
                    let bytes = Convert.FromBase64String(b64)
                    let decoded = Encoding.UTF8.GetString(bytes)
                    let idx = decoded.IndexOf(':')
                    if idx < 0 then
                        None
                    else
                        Some(
                            decoded.Substring(0, idx),
                            decoded.Substring(idx + 1))
                with _ ->
                    None
