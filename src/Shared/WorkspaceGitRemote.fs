namespace Gambol.Shared

open System

/// Ahead / behind / dirty from `git status -sb` (desktop sync indicator).
type WorkspaceGitStatus =
    { branch: string option
      ahead: int
      behind: int
      dirty: bool }

/// Pure helpers for the workspace git remote URL / service shape.
module WorkspaceGitRemote =

    [<Literal>]
    let RemoteName = "ambit"

    /// Locked G3 path: `/ambit/git/{label}.git`.
    let repoPath (label: string) : string =
        sprintf "/ambit/git/%s.git" label

    /// Full remote URL. `ambitBase` is the app base ending in `/ambit`
    /// (e.g. `https://host/ambit` or `http://localhost:5215/ambit`).
    let remoteUrl (ambitBase: string) (label: string) : string =
        let baseUrl =
            if isNull ambitBase then "" else ambitBase.TrimEnd('/')
        sprintf "%s/git/%s.git" baseUrl label

    /// True when `currentUrl` is the gateway URL for `label` at `ambitBase`.
    let remoteUrlMatches (ambitBase: string) (label: string) (currentUrl: string) : bool =
        if isNull currentUrl then false
        else
            String.Equals(
                remoteUrl ambitBase label,
                currentUrl.Trim(),
                StringComparison.OrdinalIgnoreCase)

    /// Stock smart HTTP service path / ?service= value (fetch / workspace-pull).
    [<Literal>]
    let WorkspacePull = "git-upload-pack"

    /// Stock smart HTTP service path / ?service= value (push / workspace-push).
    [<Literal>]
    let WorkspacePush = "git-receive-pack"

    /// Parse attached `.git/HEAD` content (`ref: refs/heads/<branch>`).
    let parseHeadRef (text: string) : Result<string, string> =
        let trimmed =
            if isNull text then ""
            else text.Trim()
        let prefix = "ref: refs/heads/"
        if trimmed.StartsWith(prefix, StringComparison.Ordinal) then
            let branch = trimmed.Substring(prefix.Length).Trim()
            if branch.Length > 0 then Ok branch
            else Error "HEAD ref does not name a branch."
        else
            Error "Cannot use detached HEAD for workspace git."

    /// Parse route segment `home.git` → `home`.
    let tryLabelFromRepoName (repoName: string) : string option =
        if isNull repoName then
            None
        elif not (repoName.EndsWith(".git", StringComparison.Ordinal)) then
            None
        else
            let label = repoName.Substring(0, repoName.Length - 4)
            match Filename.create label with
            | Filename.Ok s -> Some s
            | _ -> None

    let private tryParseInt (s: string) =
        match Int32.TryParse s with
        | true, n when n >= 0 -> Some n
        | _ -> None

    let private countToken (prefix: string) (part: string) =
        let t = part.Trim()
        if t.StartsWith(prefix, StringComparison.Ordinal) then
            tryParseInt (t.Substring(prefix.Length).Trim())
            |> Option.defaultValue 0
        else
            0

    let private parseAheadBehind (bracket: string) =
        let parts = bracket.Split(',')
        let ahead =
            parts |> Array.sumBy (countToken "ahead ")
        let behind =
            parts |> Array.sumBy (countToken "behind ")
        ahead, behind

    let private parseBranchLine (line: string) =
        let rest = line.Substring(3)
        let bracketAt = rest.IndexOf(" [", StringComparison.Ordinal)
        let head =
            if bracketAt < 0 then rest
            else rest.Substring(0, bracketAt)
        let ahead, behind =
            if bracketAt < 0 then 0, 0
            else
                let close = rest.LastIndexOf(']')
                if close <= bracketAt then 0, 0
                else
                    parseAheadBehind
                        (rest.Substring(bracketAt + 2, close - bracketAt - 2))
        let dots = head.IndexOf("...", StringComparison.Ordinal)
        let name =
            if dots < 0 then head.Trim()
            else head.Substring(0, dots).Trim()
        (if name.Length = 0 then None else Some name), ahead, behind

    /// Parse `git status -sb` into branch / ahead / behind / dirty.
    let parseShortStatus (text: string) : WorkspaceGitStatus =
        let lines =
            if isNull text then [||]
            else
                text.Replace("\r\n", "\n").Split('\n')
                |> Array.map (fun l -> l.TrimEnd())
                |> Array.filter (fun l -> l.Length > 0)

        let branch, ahead, behind =
            match lines |> Array.tryHead with
            | Some line when line.StartsWith("## ", StringComparison.Ordinal) ->
                parseBranchLine line
            | _ -> None, 0, 0

        let bodyStart =
            if lines.Length > 0 && lines.[0].StartsWith("## ") then 1
            else 0

        let dirty = lines.Length > bodyStart

        { branch = branch
          ahead = ahead
          behind = behind
          dirty = dirty }

    /// Compact status for cmd-last-result / logs (e.g. `main ↑2 ↓1 *`).
    let formatStatusLine (status: WorkspaceGitStatus) : string =
        let branch = status.branch |> Option.defaultValue "?"
        let ahead =
            if status.ahead > 0 then sprintf " ↑%d" status.ahead else ""
        let behind =
            if status.behind > 0 then sprintf " ↓%d" status.behind else ""
        let dirty = if status.dirty then " *" else ""
        branch + ahead + behind + dirty
