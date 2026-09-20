module Gambol.Server.Tests.AiCommandArgsTests

open Xunit
open Gambol.Server

let private cursorKey = { Name = "cursor"; ApiKey = "first-secret" }
let private lifeRepo =
    { Name = "life"
      Url = "https://origin.cursor.com/alanbaljeu/life.git"
      StartingRef = "master" }

[<Fact>]
let ``fromText with no token is default key and no repo`` () =
    Assert.Equal(
        { Keyname = None; Reponame = None },
        AiCommandArgs.fromText [ cursorKey ] [ lifeRepo ] "?ai")

[<Fact>]
let ``fromText first token is keyname`` () =
    Assert.Equal(
        { Keyname = Some "cursor"; Reponame = None },
        AiCommandArgs.fromText [ cursorKey ] [ lifeRepo ] "?ai cursor")

[<Fact>]
let ``fromText second token is reponame`` () =
    Assert.Equal(
        { Keyname = Some "cursor"; Reponame = Some "life" },
        AiCommandArgs.fromText
            [ cursorKey ]
            [ lifeRepo ]
            "?ai cursor life extra")

[<Fact>]
let ``fromText one token matching only a repo is reponame`` () =
    Assert.Equal(
        { Keyname = None; Reponame = Some "life" },
        AiCommandArgs.fromText [ cursorKey ] [ lifeRepo ] "?ai life")

[<Fact>]
let ``fromText one token matching a key is keyname`` () =
    let lifeKey = { Name = "life"; ApiKey = "life-secret" }
    Assert.Equal(
        { Keyname = Some "life"; Reponame = None },
        AiCommandArgs.fromText [ lifeKey ] [ lifeRepo ] "?ai life")

[<Fact>]
let ``fromText unknown single token is keyname`` () =
    Assert.Equal(
        { Keyname = Some "missing"; Reponame = None },
        AiCommandArgs.fromText [ cursorKey ] [ lifeRepo ] "?ai missing")

[<Fact>]
let ``fromText two tokens stay key then repo even if first is a repo`` () =
    Assert.Equal(
        { Keyname = Some "life"; Reponame = Some "cursor" },
        AiCommandArgs.fromText
            [ cursorKey ]
            [ lifeRepo ]
            "?ai life cursor")
