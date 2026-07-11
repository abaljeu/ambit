namespace Gambol.Shared

open System

/// Pure helpers for the workspace git remote URL / service shape.
module WorkspaceGitRemote =

    [<Literal>]
    let RemoteName = "ambit"

    /// Locked G3 path: `/ambit/git/{label}.git`.
    let repoPath (label: string) : string =
        sprintf "/ambit/git/%s.git" label

    /// Stock smart HTTP service path / ?service= value (fetch / workspace-pull).
    [<Literal>]
    let WorkspacePull = "git-upload-pack"

    /// Stock smart HTTP service path / ?service= value (push / workspace-push).
    [<Literal>]
    let WorkspacePush = "git-receive-pack"

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
