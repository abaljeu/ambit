module Gambol.Server.Tests.TestDbConfigTests

open System
open System.IO
open Xunit

module TestDbConfig =

    let testConnEnv = "TEST_DB_CONNECTION_STRING"

    let private repoConfigRelativePath =
        Path.Combine("src", "Server", "appsettings.Development.json")

    let private tryFindUpwards (startDir: string) (relativePath: string) =
        let rec loop (dir: string) =
            let candidate = Path.Combine(dir, relativePath)
            if File.Exists(candidate) then Some candidate
            else
                match Directory.GetParent(dir) with
                | null -> None
                | parent -> loop parent.FullName
        loop startDir

    let tryReadConnStrFromSettingsFile (path: string) =
        try
            let json = File.ReadAllText(path)
            let key = "\"DB_CONNECTION_STRING\""
            let keyIndex = json.IndexOf(key, StringComparison.Ordinal)

            if keyIndex < 0 then
                None
            else
                let colonIndex = json.IndexOf(':', keyIndex + key.Length)
                let firstQuote = json.IndexOf('"', colonIndex + 1)
                let secondQuote = json.IndexOf('"', firstQuote + 1)

                if colonIndex < 0 || firstQuote < 0 || secondQuote < 0 then
                    None
                else
                    json.Substring(firstQuote + 1, secondQuote - firstQuote - 1)
                    |> fun value ->
                        if String.IsNullOrWhiteSpace(value) then None else Some value
        with _ ->
            None

    let resolveFrom (getEnv: unit -> string option) (startDir: string) =
        match getEnv () with
        | Some value when not (String.IsNullOrWhiteSpace(value)) -> Some value
        | _ ->
            tryFindUpwards startDir repoConfigRelativePath
            |> Option.bind tryReadConnStrFromSettingsFile

[<Fact>]
let ``resolveFrom prefers TEST_DB_CONNECTION_STRING when set`` () =
    let resolved =
        TestDbConfig.resolveFrom
            (fun () -> Some "Host=env;Database=test")
            (Path.GetTempPath())

    Assert.Equal(Some "Host=env;Database=test", resolved)

[<Fact>]
let ``resolveFrom falls back to appsettings development connection string`` () =
    let tempRoot =
        Path.Combine(Path.GetTempPath(), "gambol-test-config-" + Guid.NewGuid().ToString("N"))

    try
        let serverDir = Path.Combine(tempRoot, "src", "Server")
        Directory.CreateDirectory(serverDir) |> ignore

        let configPath = Path.Combine(serverDir, "appsettings.Development.json")
        let configJson =
            "{\r\n  \"DB_CONNECTION_STRING\": \"Host=file;Database=gambol\"\r\n}\r\n"

        File.WriteAllText(configPath, configJson)

        let startDir = Path.Combine(tempRoot, "tests", "Server.Tests", "bin")
        Directory.CreateDirectory(startDir) |> ignore

        let resolved = TestDbConfig.resolveFrom (fun () -> None) startDir
        Assert.Equal(Some "Host=file;Database=gambol", resolved)
    finally
        if Directory.Exists(tempRoot) then
            Directory.Delete(tempRoot, true)
