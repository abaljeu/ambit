namespace Gambol.Shared

/// Desktop `git` capability probe.
/// Shared by Desktop host and .NET tests (not Fable / Client).
[<RequireQualifiedAccess>]
module DesktopGit =

    let isAvailable () : bool = GitRun.isAvailable ()
