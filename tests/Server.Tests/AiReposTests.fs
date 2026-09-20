module Gambol.Server.Tests.AiReposTests

open Microsoft.Extensions.Configuration
open Xunit
open Gambol.CloudAgents
open Gambol.Server
open Gambol.Server.Tests.AskCancelHarness

let private cursorKey = { Name = "cursor"; ApiKey = "first-secret" }
let private workKey = { Name = "work"; ApiKey = "work-secret" }
let private lifeRepo =
    { Name = "life"
      Url = "https://origin.cursor.com/alanbaljeu/life.git"
      StartingRef = "master" }
let private notesRepo =
    { Name = "notes"
      Url = "https://example.com/notes.git"
      StartingRef = "" }

let private configFrom pairs =
    ConfigurationBuilder()
        .AddInMemoryCollection(dict pairs)
        .Build()

let private lifeConfig =
    { RepoConfig.Url = lifeRepo.Url
      StartingRef = Some "master" }

let private notesConfig =
    { RepoConfig.Url = notesRepo.Url
      StartingRef = None }

[<Fact>]
let ``fromConfig binds Name Url and StartingRef`` () =
    let config =
        configFrom
            [ "AiRepos:0:Name", "life"
              "AiRepos:0:Url", lifeRepo.Url
              "AiRepos:0:StartingRef", "master"
              "AiRepos:1:Name", "notes"
              "AiRepos:1:Url", notesRepo.Url ]
    Assert.Equal<AiRepo list>(
        [ lifeRepo; notesRepo ],
        AiRepos.fromConfig config)

[<Fact>]
let ``fromConfig is empty when AiRepos is missing`` () =
    Assert.Equal<AiRepo list>([], AiRepos.fromConfig (configFrom []))

[<Fact>]
let ``resolve uses the named entry`` () =
    Assert.Equal(
        Some [ lifeConfig ],
        AiRepos.resolve [ lifeRepo; notesRepo ] (Some "life"))

[<Fact>]
let ``resolve named entry is case-insensitive`` () =
    Assert.Equal(
        Some [ lifeConfig ],
        AiRepos.resolve [ lifeRepo; notesRepo ] (Some "LIFE"))

[<Fact>]
let ``resolve default is no repos`` () =
    Assert.Equal(None, AiRepos.resolve [ lifeRepo; notesRepo ] None)

[<Fact>]
let ``resolve missing name is no repos`` () =
    Assert.Equal(None, AiRepos.resolve [ lifeRepo ] (Some "missing"))

[<Fact>]
let ``resolve empty StartingRef is None`` () =
    Assert.Equal(
        Some [ notesConfig ],
        AiRepos.resolve [ notesRepo ] (Some "notes"))

[<Fact>]
let ``fromText with no token is default key and no repo`` () =
    Assert.Equal(
        { Keyname = None; Reponame = None },
        AiAskArgs.fromText [ cursorKey ] [ lifeRepo ] "?ai")

[<Fact>]
let ``fromText first token is keyname`` () =
    Assert.Equal(
        { Keyname = Some "cursor"; Reponame = None },
        AiAskArgs.fromText [ cursorKey ] [ lifeRepo ] "?ai cursor")

[<Fact>]
let ``fromText second token is reponame`` () =
    Assert.Equal(
        { Keyname = Some "cursor"; Reponame = Some "life" },
        AiAskArgs.fromText
            [ cursorKey ]
            [ lifeRepo ]
            "?ai cursor life extra")

[<Fact>]
let ``fromText one token matching only a repo is reponame`` () =
    Assert.Equal(
        { Keyname = None; Reponame = Some "life" },
        AiAskArgs.fromText [ cursorKey ] [ lifeRepo ] "?ai life")

[<Fact>]
let ``fromText one token matching a key is keyname`` () =
    let lifeKey = { Name = "life"; ApiKey = "life-secret" }
    Assert.Equal(
        { Keyname = Some "life"; Reponame = None },
        AiAskArgs.fromText [ lifeKey ] [ lifeRepo ] "?ai life")

[<Fact>]
let ``fromText unknown single token is keyname`` () =
    Assert.Equal(
        { Keyname = Some "missing"; Reponame = None },
        AiAskArgs.fromText [ cursorKey ] [ lifeRepo ] "?ai missing")

[<Fact>]
let ``fromText two tokens stay key then repo even if first is a repo`` () =
    Assert.Equal(
        { Keyname = Some "life"; Reponame = Some "cursor" },
        AiAskArgs.fromText
            [ cursorKey ]
            [ lifeRepo ]
            "?ai life cursor")

[<Collection("Agent ask runner")>]
type AiReposActorTests() =

    [<Fact>]
    member _.``AI without reponame sends no repos``() =
        withFake
            (fun args ->
                Assert.Equal(None, args.Repos)
                Assert.Equal("first-secret", args.Config.ApiKey)
                fakeReply "from-default")
            (fun () ->
                withHostKeysRepos
                    [ cursorKey ]
                    [ lifeRepo ]
                    (fun host pool -> task {
                        let! seeded = seedAskTree host "?ai"
                        let! request = startAsk host seeded
                        do! expectActorSucceeded
                                host pool request.focusId
                    }))

    [<Fact>]
    member _.``AI keyname and reponame sends that repo``() =
        withFake
            (fun args ->
                Assert.Equal(Some [ lifeConfig ], args.Repos)
                Assert.Equal("work-secret", args.Config.ApiKey)
                fakeReply "from-named-repo")
            (fun () ->
                withHostKeysRepos
                    [ cursorKey; workKey ]
                    [ lifeRepo ]
                    (fun host pool -> task {
                        let! seeded =
                            seedAskTree host "?ai work life"
                        let! request = startAsk host seeded
                        do! expectActorSucceeded
                                host pool request.focusId
                    }))

    [<Fact>]
    member _.``AI repo-only token sends default key and that repo``() =
        withFake
            (fun args ->
                Assert.Equal(Some [ lifeConfig ], args.Repos)
                Assert.Equal("first-secret", args.Config.ApiKey)
                fakeReply "from-repo-only")
            (fun () ->
                withHostKeysRepos
                    [ cursorKey ]
                    [ lifeRepo ]
                    (fun host pool -> task {
                        let! seeded = seedAskTree host "?ai life"
                        let! request = startAsk host seeded
                        do! expectActorSucceeded
                                host pool request.focusId
                    }))
