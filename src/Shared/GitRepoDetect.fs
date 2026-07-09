namespace Gambol.Shared

#if !FABLE_COMPILER
open System
open System.IO
#endif

[<RequireQualifiedAccess>]
module GitRepoDetect =

#if !FABLE_COMPILER
    let private isGitDirectory (path: string) =
        Directory.Exists(Path.Combine(path, ".git"))

    /// Resolve a git repository work-tree root from a selected path.
    /// Returns the directory containing `.git`, or an error message.
    let detectRoot (selectedPath: string) : Result<string, string> =
        if isNull selectedPath || selectedPath.Trim() = "" then
            Error "path is required"
        else
            try
                let full =
                    if Path.IsPathFullyQualified selectedPath then selectedPath
                    else Path.GetFullPath selectedPath

                let name = Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))

                if name = ".git" then
                    Ok(Path.GetDirectoryName full)
                elif isGitDirectory full then
                    Ok full
                else
                    Error "Select a git repository root (folder containing .git)"
            with
            | :? IOException -> Error "invalid path"
            | :? ArgumentException -> Error "invalid path"
#endif
