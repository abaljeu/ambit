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

[<RequireQualifiedAccess>]
module UploadCapability =

    type Claim =
        { user: string
          label: string
          relative: string
          size: int64
          sha256: string
          sourceMtimeTicks: int64
          expiresUnix: int64
          nonce: Guid }

    let private base64Url (bytes: byte[]) =
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_')

    let private decodeBase64Url (text: string) =
        let base64 = text.Replace('-', '+').Replace('_', '/')
        let padded =
            match base64.Length % 4 with
            | 0 -> base64
            | 2 -> base64 + "=="
            | 3 -> base64 + "="
            | _ -> ""
        Convert.FromBase64String padded

    let private encodeText (text: string) =
        Encoding.UTF8.GetBytes text |> base64Url

    let private payload (claim: Claim) =
        String.concat
            "|"
            [ "1"
              string claim.expiresUnix
              string claim.size
              string claim.sourceMtimeTicks
              claim.nonce.ToString("N")
              encodeText claim.user
              encodeText claim.label
              encodeText claim.relative
              claim.sha256.ToLowerInvariant() ]

    let private signature (secret: string) (text: string) =
        let key = Encoding.UTF8.GetBytes("gambol-upload:" + secret)
        use hmac = new HMACSHA256(key)
        hmac.ComputeHash(Encoding.UTF8.GetBytes text)

    let issue (secret: string) (claim: Claim) =
        let encodedPayload =
            payload claim |> Encoding.UTF8.GetBytes |> base64Url
        encodedPayload + "." + base64Url (signature secret encodedPayload)

    let private tryParsePayload (encoded: string) =
        try
            match Encoding.UTF8.GetString(decodeBase64Url encoded).Split('|') with
            | [| "1"; expires; size; ticks; nonce; user; label; relative; sha |] ->
                Some
                    { user = Encoding.UTF8.GetString(decodeBase64Url user)
                      label = Encoding.UTF8.GetString(decodeBase64Url label)
                      relative = Encoding.UTF8.GetString(decodeBase64Url relative)
                      size = Int64.Parse size
                      sha256 = sha
                      sourceMtimeTicks = Int64.Parse ticks
                      expiresUnix = Int64.Parse expires
                      nonce = Guid.ParseExact(nonce, "N") }
            | _ -> None
        with _ ->
            None

    let validate
        (secret: string)
        (expectedUser: string)
        (now: DateTimeOffset)
        (token: string)
        =
        try
            match token.Split('.') with
            | [| encoded; supplied |] ->
                let expected = signature secret encoded
                let actual = decodeBase64Url supplied
                if not (CryptographicOperations.FixedTimeEquals(expected, actual)) then
                    Error "invalid_upload_capability"
                else
                    match tryParsePayload encoded with
                    | Some claim when claim.user <> expectedUser ->
                        Error "invalid_upload_capability"
                    | Some claim when now.ToUnixTimeSeconds() >= claim.expiresUnix ->
                        Error "expired_upload_capability"
                    | Some claim -> Ok claim
                    | None -> Error "invalid_upload_capability"
            | _ -> Error "invalid_upload_capability"
        with _ ->
            Error "invalid_upload_capability"
