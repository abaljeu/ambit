module WorkspaceDavClientTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``resourceUrl encodes label and path segments`` () =
    let url =
        WorkspaceDavClient.resourceUrl
            "http://localhost:5000/ambit"
            "home"
            "docs/a b.txt"
    Assert.Equal(
        "http://localhost:5000/ambit/dav/home/docs/a%20b.txt",
        url)

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
