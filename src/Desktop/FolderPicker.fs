namespace Gambol.Desktop

open System
open System.Windows
open Microsoft.Win32

/// Native folder browse dialog (WPF). Must run on the UI thread.
[<RequireQualifiedAccess>]
module FolderPicker =

    let private showDialog () : string option =
        let dialog = OpenFolderDialog()
        dialog.Title <- "Select folder"
        let accepted = dialog.ShowDialog()
        if accepted.HasValue && accepted.Value then
            match dialog.FolderName with
            | null
            | "" -> None
            | path -> Some path
        else
            None

    /// Open the OS folder picker. Returns None if cancelled.
    let pickFolder () : string option =
        match Application.Current with
        | null -> showDialog ()
        | app ->
            app.Dispatcher.Invoke(Func<string option>(showDialog))
