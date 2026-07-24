module WorkspaceDavClientTests

open Gambol.Shared
open Xunit

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
