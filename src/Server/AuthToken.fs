namespace Gambol.Server

open System
open System.Security.Cryptography
open System.Text

module AuthToken =
    [<Literal>]
    let cookieName = "gambol_auth"

    /// HMAC-SHA256 of username keyed by password, hex-encoded (matches server cookie value).
    let deriveToken (username: string) (password: string) =
        use hmac = new HMACSHA256(Encoding.UTF8.GetBytes(password))
        let hash: byte array = hmac.ComputeHash(Encoding.UTF8.GetBytes(username))
        Convert.ToHexString(hash).ToLowerInvariant()

    let cookieHeaderValue (username: string) (password: string) =
        sprintf "%s=%s" cookieName (deriveToken username password)
