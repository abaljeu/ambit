namespace Gambol.Shared

open System
open System.Diagnostics
open System.IO

/// Pure-ish wrappers around `git check-ignore --no-index` for a work tree
/// that need not be a client clone (shared empty GIT_DIR + GIT_WORK_TREE).
[<RequireQualifiedAccess>]
module GitCheckIgnore =

    let normalizeRel (path: string) =
        if isNull path then ""
        else path.Replace('\\', '/').TrimStart('/')

    /// `.gitignore` files themselves remain transferable.
    let isGitignorePath (relativePath: string) =
        let n = normalizeRel(relativePath).TrimEnd('/')
        n = ".gitignore" || n.EndsWith("/.gitignore", StringComparison.Ordinal)

    let private emptyGitDir =
        lazy (
            let dir =
                Path.Combine(Path.GetTempPath(), "gambol-check-ignore-git")
            Directory.CreateDirectory dir |> ignore
            let gitDir = Path.Combine(dir, ".git")

            if not (Directory.Exists gitDir) then
                match GitRun.gitExec dir "init -q" with
                | Ok _ -> ()
                | Error _ -> ()

            gitDir)

    let private failureDetail (stdout: string) (stderr: string) =
        if String.IsNullOrWhiteSpace stderr then stdout
        else stderr

    let private baseConfigure
        (workTree: string)
        (redirectStdin: bool)
        (psi: ProcessStartInfo)
        =
        psi.WorkingDirectory <- workTree
        if redirectStdin then
            psi.RedirectStandardInput <- true
        psi.Environment["GIT_DIR"] <- emptyGitDir.Value
        psi.Environment["GIT_WORK_TREE"] <- workTree

    let private ignoreError (stdout: string) (stderr: string) =
        let detail = failureDetail stdout stderr
        if String.IsNullOrWhiteSpace detail then
            "git check-ignore failed"
        else
            detail

    /// Ok true = ignored; Ok false = not ignored; Error = git missing/failed.
    let isIgnored
        (workTree: string)
        (relativePath: string)
        : Result<bool, string> =
        let rel = normalizeRel relativePath

        if String.IsNullOrWhiteSpace rel then
            Ok false
        else
            Directory.CreateDirectory workTree |> ignore
            let configure (psi: ProcessStartInfo) =
                baseConfigure workTree false psi
                psi.ArgumentList.Add("check-ignore")
                psi.ArgumentList.Add("-q")
                psi.ArgumentList.Add("--no-index")
                psi.ArgumentList.Add("--")
                psi.ArgumentList.Add(rel)

            match GitRun.gitCapture configure None with
            | Error e -> Error e
            | Ok(0, _, _) -> Ok true
            | Ok(1, _, _) -> Ok false
            | Ok(_, stdout, stderr) -> Error(ignoreError stdout stderr)

    /// Like `isIgnored`, but `.gitignore` paths are never treated as ignored.
    let isEffectivelyIgnored
        (workTree: string)
        (relativePath: string)
        : Result<bool, string> =
        if isGitignorePath relativePath then
            Ok false
        else
            isIgnored workTree relativePath

    let private ignoredSetFromStdout (stdout: string) =
        stdout.Split(
            [| '\u0000'; '\n'; '\r' |],
            StringSplitOptions.RemoveEmptyEntries)
        |> Array.map normalizeRel
        |> Set.ofArray

    /// Classify many relatives via `--stdin`. Ok (path, ignored) list.
    let classify
        (workTree: string)
        (relativePaths: string list)
        : Result<(string * bool) list, string> =
        let paths =
            relativePaths
            |> List.map normalizeRel
            |> List.filter (fun p -> not (String.IsNullOrWhiteSpace p))

        if paths.IsEmpty then
            Ok []
        else
            Directory.CreateDirectory workTree |> ignore
            let configure (psi: ProcessStartInfo) =
                baseConfigure workTree true psi
                psi.ArgumentList.Add("check-ignore")
                psi.ArgumentList.Add("--no-index")
                psi.ArgumentList.Add("--stdin")
                psi.ArgumentList.Add("-z")

            let write (stdin: StreamWriter) =
                for path in paths do
                    stdin.Write(path)
                    stdin.Write('\u0000')

            match GitRun.gitCapture configure (Some write) with
            | Error e -> Error e
            | Ok(code, stdout, stderr) when code = 0 || code = 1 ->
                let ignored = ignoredSetFromStdout stdout
                paths
                |> List.map (fun p -> p, Set.contains p ignored)
                |> Ok
            | Ok(_, stdout, stderr) -> Error(ignoreError stdout stderr)
