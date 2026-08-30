module ClientIdentityTests

open System
open Gambol.Shared
open Xunit

[<Fact>]
let ``normalize strips CR LF and trims`` () =
    Assert.Equal("Win32; Mozilla", ClientIdentity.normalize "  Win32; Mozilla\r\n  ")

[<Fact>]
let ``normalize truncates to MaxLength`` () =
    let raw = String.replicate (ClientIdentity.MaxLength + 20) "a"
    let n = ClientIdentity.normalize raw
    Assert.Equal(ClientIdentity.MaxLength, n.Length)

[<Fact>]
let ``tryFromValues skips empty and returns first usable`` () =
    let got =
        ClientIdentity.tryFromValues [ ""; "  "; "MacIntel; Mozilla/5.0" ]
    Assert.Equal(Some "MacIntel; Mozilla/5.0", got)

[<Fact>]
let ``tryFromValues returns None when all blank`` () =
    Assert.Equal(None, ClientIdentity.tryFromValues [ ""; " \n " ])

[<Fact>]
let ``formatCommitMessage appends client hint`` () =
    let msg =
        ClientIdentity.formatCommitMessage
            "rev 42"
            (Some "Win32; Mozilla/5.0")
    Assert.Equal("rev 42 | client: Win32; Mozilla/5.0", msg)

[<Fact>]
let ``formatCommitMessage omits blank hint`` () =
    Assert.Equal(
        "rev 7",
        ClientIdentity.formatCommitMessage "rev 7" None)
    Assert.Equal(
        "rev 7",
        ClientIdentity.formatCommitMessage "rev 7" (Some "  "))

[<Fact>]
let ``formatCommitMessage scrubs double quotes`` () =
    let msg =
        ClientIdentity.formatCommitMessage
            "rev 1"
            (Some "Win32; \"quoted\"")
    Assert.Equal("rev 1 | client: Win32; 'quoted'", msg)
