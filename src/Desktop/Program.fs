namespace Gambol.Desktop

open System
open System.Windows
open Microsoft.Web.WebView2.Wpf

module Program =
    [<Literal>]
    let private appUrl = "https://collaborative-systems.org/ambit"

    let private createMainWindow () =
        let webView =
            new WebView2(
                Source = Uri appUrl)

        new Window(
                Title = "Gambol",
                Width = 1280,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = webView)

    [<EntryPoint; STAThread>]
    let main _ =
        let app = Application()
        let window = createMainWindow ()
        app.Run window
