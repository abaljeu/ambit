module Gambol.Server.Tests.AiCommandArgsTests

open Xunit
open Gambol.Server

let private keys names =
    { DefaultAiKey = ""
      Keys = names |> List.map (fun name -> name, "") |> Map.ofList }

let private cursorKeys = keys [ "cursor" ]
let private lifeRepo =
    { Name = "life"
      Url = "https://origin.cursor.com/alanbaljeu/life.git"
      StartingRef = "master" }

[<Fact>]
let ``fromText with no token is default key and no repo`` () =
    Assert.Equal(
        { Keyname = None; Reponame = None },
        AiCommandArgs.fromText cursorKeys [ lifeRepo ] "?ai")

[<Fact>]
let ``fromText first token is keyname`` () =
    Assert.Equal(
        { Keyname = Some "cursor"; Reponame = None },
        AiCommandArgs.fromText cursorKeys [ lifeRepo ] "?ai cursor")

[<Fact>]
let ``fromText second token is reponame`` () =
    Assert.Equal(
        { Keyname = Some "cursor"; Reponame = Some "life" },
        AiCommandArgs.fromText
            cursorKeys
            [ lifeRepo ]
            "?ai cursor life extra")

[<Fact>]
let ``fromText one token matching only a repo is reponame`` () =
    Assert.Equal(
        { Keyname = None; Reponame = Some "life" },
        AiCommandArgs.fromText cursorKeys [ lifeRepo ] "?ai life")

[<Fact>]
let ``fromText one token matching a key is keyname`` () =
    Assert.Equal(
        { Keyname = Some "life"; Reponame = None },
        AiCommandArgs.fromText (keys [ "life" ]) [ lifeRepo ] "?ai life")

[<Fact>]
let ``fromText unknown single token is keyname`` () =
    Assert.Equal(
        { Keyname = Some "missing"; Reponame = None },
        AiCommandArgs.fromText cursorKeys [ lifeRepo ] "?ai missing")

[<Fact>]
let ``fromText two tokens stay key then repo even if first is a repo`` () =
    Assert.Equal(
        { Keyname = Some "life"; Reponame = Some "cursor" },
        AiCommandArgs.fromText
            cursorKeys
            [ lifeRepo ]
            "?ai life cursor")
