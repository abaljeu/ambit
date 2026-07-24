module WorkspaceDavClientTests

open System
open System.Net
open System.Net.Http
open System.Text
open System.Threading
open System.Threading.Tasks
open Gambol.Shared
open Xunit

type private DirectUploadHandler() as this =
    inherit HttpMessageHandler()

    let requests = ResizeArray<string * string * byte[]>()

    member _.Requests = requests |> Seq.toList

    member private _.Handle(req: HttpRequestMessage) =
        let text = req.Content.ReadAsStringAsync().Result
        let bytes = req.Content.ReadAsByteArrayAsync().Result
        requests.Add(string req.RequestUri, text, bytes)
        if requests.Count = 1 then
            new HttpResponseMessage(
                HttpStatusCode.OK,
                Content =
                    new StringContent(
                        """{"uploadUrl":"https://direct.azure/ambit/direct-upload","capability":"secret-cap"}"""))
        else
            Assert.Equal("https://direct.azure/ambit/direct-upload", string req.RequestUri)
            Assert.Equal("GambolUpload secret-cap", string req.Headers.Authorization)
            new HttpResponseMessage(HttpStatusCode.Created)

    override _.Send(req, _cancellationToken: CancellationToken) =
        this.Handle req

    override _.SendAsync(req, _cancellationToken: CancellationToken) =
        Task.FromResult(this.Handle req)

[<Fact>]
let ``resourceUrl keeps exact workspace path opaque`` () =
    let relative =
        "employment/research/companies/upwork/Rule-Based-Kitchen-Layout/"
        + "kitchen-layout-engine-posting.md"
    let url =
        WorkspaceDavClient.resourceUrl
            "http://localhost:5000/ambit"
            "home"
            relative
    Assert.StartsWith(
        "http://localhost:5000/ambit/dav-resource/",
        url)
    Assert.DoesNotContain("employment", url)
    Assert.DoesNotContain("Rule-Based-Kitchen-Layout", url)
    let token = url.Substring(url.LastIndexOf('/') + 1)
    Assert.Matches("^[A-Za-z0-9_-]+$", token)
    Assert.Equal(
        Ok("home", relative),
        WorkspaceDavClient.decodeResourceToken token)

[<Fact>]
let ``resource token round trips URL-significant and Unicode characters`` () =
    let relative = "docs/a b#100%-café.md"
    let token = WorkspaceDavClient.encodeResourceToken "my workspace" relative
    Assert.Matches("^[A-Za-z0-9_-]+$", token)
    Assert.Equal(
        Ok("my workspace", relative),
        WorkspaceDavClient.decodeResourceToken token)

[<Fact>]
let ``putBytes grants through proxy then uploads exact bytes directly`` () =
    let handler = new DirectUploadHandler()
    use client = new HttpClient(handler)
    let relative = "employment/research/targets/priorities.md"
    let payload =
        Array.concat
            [ Encoding.UTF8.GetBytes("<script>ModSecurity</script> café")
              [| 0uy; 255uy |] ]
    let result =
        WorkspaceDavClient.putBytes
            client
            "https://proxy.example/ambit"
            "home"
            relative
            payload
            (Some "gambol_auth=cookie")
            None
            None
    Assert.Equal(Ok (), result)
    let grantUrl, grantBody, _ = handler.Requests.[0]
    Assert.Equal(
        "https://proxy.example/ambit/upload-capability",
        grantUrl)
    Assert.DoesNotContain("employment", grantBody)
    Assert.Equal<byte>(payload, handler.Requests.[1] |> fun (_, _, bytes) -> bytes)

[<Fact>]
let ``finishCommitUrl targets _finish-commit`` () =
    let url =
        WorkspaceDavClient.finishCommitUrl "http://host/ambit" "ws"
    Assert.Equal(
        "http://host/ambit/dav/ws/_finish-commit",
        url)

[<Fact>]
let ``preparePushUrl targets _prepare-push`` () =
    let url =
        WorkspaceDavClient.preparePushUrl "http://host/ambit" "ws"
    Assert.Equal(
        "http://host/ambit/dav/ws/_prepare-push",
        url)

[<Fact>]
let ``interpretPropfindResponse treats 404 as empty inventory`` () =
    match WorkspaceDavClient.interpretPropfindResponse "ws" 404 "" with
    | Ok [] -> ()
    | Ok entries ->
        Assert.Fail("expected empty, got " + string entries.Length)
    | Error e -> Assert.Fail(e)

[<Fact>]
let ``interpretPropfindResponse still errors on non-404 failures`` () =
    match
        WorkspaceDavClient.interpretPropfindResponse "ws" 500 "boom"
    with
    | Error msg ->
        Assert.Contains("PROPFIND HTTP 500", msg)
    | Ok _ -> Assert.Fail("expected error")

[<Fact>]
let ``parsePropfindXml reads href collection and mtime`` () =
    let xml =
        """<?xml version="1.0" encoding="utf-8"?>
<D:multistatus xmlns:D="DAV:">
  <D:response>
    <D:href>/ambit/dav/home/docs/</D:href>
    <D:propstat>
      <D:prop>
        <D:resourcetype><D:collection/></D:resourcetype>
        <D:getlastmodified>Tue, 21 Jul 2026 12:00:00 GMT</D:getlastmodified>
        <D:getcontentlength>0</D:getcontentlength>
      </D:prop>
      <D:status>HTTP/1.1 200 OK</D:status>
    </D:propstat>
  </D:response>
  <D:response>
    <D:href>/ambit/dav/home/docs/a.txt</D:href>
    <D:propstat>
      <D:prop>
        <D:resourcetype/>
        <D:getlastmodified>Tue, 21 Jul 2026 13:00:00 GMT</D:getlastmodified>
        <D:getcontentlength>2</D:getcontentlength>
      </D:prop>
      <D:status>HTTP/1.1 200 OK</D:status>
    </D:propstat>
  </D:response>
</D:multistatus>"""

    match WorkspaceDavClient.parsePropfindXml "home" xml with
    | Error e -> Assert.Fail(e)
    | Ok entries ->
        Assert.Equal(2, entries.Length)
        let docs = entries |> List.find (fun e -> e.relative = "docs")
        Assert.True(docs.isCollection)
        Assert.True(docs.lastModifiedUtc.IsSome)
        let file =
            entries |> List.find (fun e -> e.relative = "docs/a.txt")
        Assert.False(file.isCollection)
        Assert.True(file.lastModifiedUtc.IsSome)
        Assert.Equal(2L, file.contentLength)
