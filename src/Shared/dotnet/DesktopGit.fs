namespace Gambol.Shared

/// Desktop `git` capability probe.
/// Shared by Desktop host and .NET tests (not Fable / Client).
[<RequireQualifiedAccess>]
module DesktopGit =

    let private available = lazy (GitRun.isAvailable ())

    let isAvailable () : bool = available.Value
