namespace Gambol.Shared

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Xml.Linq

/// One PROPFIND inventory row (server-filtered).
type DavInventoryEntry =
    { relative: string
      isCollection: bool
      lastModifiedUtc: DateTime option
      contentLength: int64 }

/// HttpClient WebDAV Class 1 against `/ambit/dav/{label}/…`.
[<RequireQualifiedAccess>]
module WorkspaceDavClient =

    /// Client-supplied file mtime on PUT (UTC ISO-8601).
    [<Literal>]
    let SourceMtimeHeaderName = "X-Gambol-Source-Mtime"

    let private davNs = XNamespace.Get "DAV:"

    let encodeResourceToken (label: string) (relative: string) =
        Encoding.UTF8.GetBytes(label + "\u0000" + relative)
        |> Convert.ToBase64String
        |> fun text ->
            text.TrimEnd('=').Replace('+', '-').Replace('/', '_')

    let decodeResourceToken (token: string) : Result<string * string, string> =
        try
            let base64 = token.Replace('-', '+').Replace('_', '/')
            let padded =
                match base64.Length % 4 with
                | 0 -> base64
                | 2 -> base64 + "=="
                | 3 -> base64 + "="
                | _ -> ""
            let text =
                Convert.FromBase64String padded |> Encoding.UTF8.GetString
            let separator = text.IndexOf('\u0000')
            if separator <= 0 then
                Error "invalid_resource_token"
            else
                Ok(
                    text.Substring(0, separator),
                    text.Substring(separator + 1))
        with _ ->
            Error "invalid_resource_token"

    let resourceUrl (ambitBase: string) (label: string) (relative: string) =
        let root = ambitBase.TrimEnd('/')
        root + "/dav-resource/" + encodeResourceToken label relative

    let private controlUrl (ambitBase: string) (label: string) (action: string) =
        ambitBase.TrimEnd('/')
        + "/dav/"
        + Uri.EscapeDataString label
        + "/"
        + action

    let finishCommitUrl (ambitBase: string) (label: string) =
        controlUrl ambitBase label "_finish-commit"

    let preparePushUrl (ambitBase: string) (label: string) =
        controlUrl ambitBase label "_prepare-push"

    let private decodeHref
        (label: string)
        (href: string)
        : Result<string, string> =
        try
            let path =
                let u = href.Trim()
                if u.StartsWith("http", StringComparison.OrdinalIgnoreCase) then
                    Uri(u).AbsolutePath
                else
                    u
            let marker = "/ambit/dav/" + Uri.EscapeDataString label
            let idx =
                path.IndexOf(marker, StringComparison.OrdinalIgnoreCase)
            if idx < 0 then
                Error("unexpected href: " + href)
            else
                let tail = path.Substring(idx + marker.Length).Trim('/')
                if tail = "" then Ok ""
                else
                    tail.Split('/')
                    |> Array.map Uri.UnescapeDataString
                    |> String.concat "/"
                    |> Ok
        with ex ->
            Error ex.Message

    let private tryParseDate (text: string) =
        match DateTime.TryParse(text) with
        | true, dt -> Some(dt.ToUniversalTime())
        | _ -> None

    let private parseResponse (label: string) (el: XElement) =
        let href =
            el.Element(davNs + "href")
            |> Option.ofObj
            |> Option.map (fun e -> e.Value)
            |> Option.defaultValue ""
        let prop =
            el.Descendants(davNs + "prop") |> Seq.tryHead
        let isColl =
            match prop with
            | None -> false
            | Some p ->
                match p.Element(davNs + "resourcetype") with
                | null -> false
                | rt -> rt.Element(davNs + "collection") <> null
        let mtime =
            prop
            |> Option.bind (fun p ->
                p.Element(davNs + "getlastmodified")
                |> Option.ofObj
                |> Option.map (fun e -> e.Value)
                |> Option.bind tryParseDate)
        let length =
            prop
            |> Option.bind (fun p ->
                p.Element(davNs + "getcontentlength")
                |> Option.ofObj
                |> Option.map (fun e -> e.Value)
                |> Option.bind (fun text ->
                    match Int64.TryParse text with
                    | true, n -> Some n
                    | _ -> None))
            |> Option.defaultValue 0L
        match decodeHref label href with
        | Error e -> Error e
        | Ok relative when relative = "_finish-commit" -> Ok None
        | Ok relative when relative = "_prepare-push" -> Ok None
        | Ok relative ->
            Ok(
                Some
                    { relative = relative
                      isCollection = isColl
                      lastModifiedUtc = mtime
                      contentLength = length })

    /// Parse Class 1 multistatus XML into inventory rows.
    let parsePropfindXml
        (label: string)
        (xml: string)
        : Result<DavInventoryEntry list, string> =
        try
            let doc = XDocument.Parse xml
            let responses = doc.Descendants(davNs + "response")

            responses
            |> Seq.fold
                (fun acc el ->
                    match acc with
                    | Error e -> Error e
                    | Ok soFar ->
                        match parseResponse label el with
                        | Error e -> Error e
                        | Ok None -> Ok soFar
                        | Ok(Some entry) -> Ok(soFar @ [ entry ]))
                (Ok [])
        with ex ->
            Error ex.Message

    let private addCookie
        (req: HttpRequestMessage)
        (cookieHeader: string option)
        =
        match cookieHeader with
        | Some c when c <> "" ->
            req.Headers.TryAddWithoutValidation("Cookie", c) |> ignore
        | _ -> ()

    let private addClientHint
        (req: HttpRequestMessage)
        (hint: string option)
        =
        match hint with
        | Some h when h <> "" ->
            req.Headers.TryAddWithoutValidation(
                ClientIdentity.HeaderName,
                ClientIdentity.normalize h)
            |> ignore
        | _ -> ()

    /// Missing remote path (first push) → empty inventory; 207 → parse.
    let interpretPropfindResponse
        (label: string)
        (code: int)
        (body: string)
        : Result<DavInventoryEntry list, string> =
        if code = 404 then Ok []
        elif code = 207 then parsePropfindXml label body
        else Error("PROPFIND HTTP " + string code + ": " + body)

    let propfind
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (relative: string)
        (depth: string)
        (cookieHeader: string option)
        : Result<DavInventoryEntry list, string> =
        try
            let url = resourceUrl ambitBase label relative
            use req = new HttpRequestMessage(HttpMethod("PROPFIND"), url)
            req.Headers.TryAddWithoutValidation("Depth", depth) |> ignore
            addCookie req cookieHeader
            use resp = client.Send(req)
            let body = resp.Content.ReadAsStringAsync().Result
            interpretPropfindResponse label (int resp.StatusCode) body
        with ex ->
            Error ex.Message

    let getBytes
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (relative: string)
        (cookieHeader: string option)
        : Result<byte[], string> =
        try
            let url = resourceUrl ambitBase label relative
            use req = new HttpRequestMessage(HttpMethod.Get, url)
            addCookie req cookieHeader
            use resp = client.Send(req)
            let code = int resp.StatusCode
            if code < 200 || code >= 300 then
                let body = resp.Content.ReadAsStringAsync().Result
                Error("GET HTTP " + string code + ": " + body)
            else
                Ok(resp.Content.ReadAsByteArrayAsync().Result)
        with ex ->
            Error ex.Message

    let putBytes
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (relative: string)
        (bytes: byte[])
        (cookieHeader: string option)
        (clientHint: string option)
        (sourceMtimeUtc: DateTime option)
        : Result<unit, string> =
        try
            let url = resourceUrl ambitBase label relative
            use content = new ByteArrayContent(bytes)
            // POST avoids shared-host Apache rules that reject PUT before proxy.php.
            use req = new HttpRequestMessage(HttpMethod.Post, url)
            req.Content <- content
            addCookie req cookieHeader
            addClientHint req clientHint
            match sourceMtimeUtc with
            | Some utc ->
                req.Headers.TryAddWithoutValidation(
                    SourceMtimeHeaderName,
                    utc.ToString("O"))
                |> ignore
            | None -> ()
            use resp = client.Send(req)
            let code = int resp.StatusCode
            if code = 201 || code = 204 || code = 200 then Ok ()
            else
                let body = resp.Content.ReadAsStringAsync().Result
                Error(
                    "upload HTTP "
                    + string code
                    + ": "
                    + LogText.truncateForLog 200 body)
        with ex ->
            Error ex.Message

    let mkcol
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (relative: string)
        (cookieHeader: string option)
        (clientHint: string option)
        : Result<unit, string> =
        try
            let url = resourceUrl ambitBase label relative
            use req = new HttpRequestMessage(HttpMethod("MKCOL"), url)
            addCookie req cookieHeader
            addClientHint req clientHint
            use resp = client.Send(req)
            let code = int resp.StatusCode
            // 201 created; 405 already exists (ok for idempotent push)
            if code = 201 || code = 405 then Ok ()
            else
                let body = resp.Content.ReadAsStringAsync().Result
                Error("MKCOL HTTP " + string code + ": " + body)
        with ex ->
            Error ex.Message

    let preparePush
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (cookieHeader: string option)
        (clientHint: string option)
        : Result<unit, string> =
        try
            let url = preparePushUrl ambitBase label
            use req = new HttpRequestMessage(HttpMethod.Post, url)
            req.Content <-
                new StringContent("{}", Encoding.UTF8, "application/json")
            addCookie req cookieHeader
            addClientHint req clientHint
            use resp = client.Send(req)
            let body = resp.Content.ReadAsStringAsync().Result
            let code = int resp.StatusCode
            if code < 200 || code >= 300 then
                Error("prepare-push HTTP " + string code + ": " + body)
            else
                Ok ()
        with ex ->
            Error ex.Message

    let finishCommit
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (cookieHeader: string option)
        (clientHint: string option)
        : Result<string, string> =
        try
            let url = finishCommitUrl ambitBase label
            use req = new HttpRequestMessage(HttpMethod.Post, url)
            req.Content <-
                new StringContent("{}", Encoding.UTF8, "application/json")
            addCookie req cookieHeader
            addClientHint req clientHint
            use resp = client.Send(req)
            let body = resp.Content.ReadAsStringAsync().Result
            let code = int resp.StatusCode
            if code < 200 || code >= 300 then
                Error("finish-commit HTTP " + string code + ": " + body)
            else
                Ok body
        with ex ->
            Error ex.Message
