namespace Gambol.Shared.Tests

open System.IO
open Gambol.Shared
open Xunit

module GitRepoDetectTests =

    let private withTempGitRepo (name: string) (f: string -> unit) =
        let root = Path.Combine(Path.GetTempPath(), "gambol-git-detect-" + name + "-" + System.Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(root) |> ignore
        Directory.CreateDirectory(Path.Combine(root, ".git")) |> ignore

        try
            f root
        finally
            try
                Directory.Delete(root, true)
            with
            | :? IOException -> ()

    [<Fact>]
    let ``detectRoot returns folder containing dot git`` () =
        withTempGitRepo "root" (fun root ->
            match GitRepoDetect.detectRoot root with
            | Ok detected -> Assert.Equal(root, detected)
            | Error err -> Assert.Fail err)

    [<Fact>]
    let ``detectRoot when path is dot git returns parent`` () =
        withTempGitRepo "parent" (fun root ->
            let gitDir = Path.Combine(root, ".git")

            match GitRepoDetect.detectRoot gitDir with
            | Ok detected -> Assert.Equal(root, detected)
            | Error err -> Assert.Fail err)

    [<Fact>]
    let ``detectRoot rejects folder without git`` () =
        let root =
            Path.Combine(Path.GetTempPath(), "gambol-git-detect-none-" + System.Guid.NewGuid().ToString("N"))

        Directory.CreateDirectory root |> ignore

        try
            match GitRepoDetect.detectRoot root with
            | Ok _ -> Assert.Fail "expected error"
            | Error err -> Assert.Contains(".git", err)
        finally
            try
                Directory.Delete(root, true)
            with
            | :? IOException -> ()

    [<Fact>]
    let ``detectRoot rejects empty path`` () =
        match GitRepoDetect.detectRoot "" with
        | Ok _ -> Assert.Fail "expected error"
        | Error err -> Assert.Equal("path is required", err)
