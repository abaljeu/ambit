namespace Gambol.Desktop

open System
open System.Windows
open Microsoft.Win32

[<RequireQualifiedAccess>]
module FolderPicker =

    /// Show the native folder picker on the WPF UI thread. Returns None if cancelled.
    let pickFolder () : string option =
        if isNull Application.Current then
            None
        else
            Application.Current.Dispatcher.Invoke(fun () ->
                let dlg = OpenFolderDialog()
                dlg.Title <- "Select workspace folder"

                let result = dlg.ShowDialog()

                if result.HasValue && result.Value then
                    Some dlg.FolderName
                else
                    None)
