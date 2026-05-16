namespace Gambol.Desktop

open System
open System.IO
open System.Windows
open System.Windows.Controls
open System.Windows.Media
open Microsoft.Web.WebView2.Core
open Microsoft.Web.WebView2.Wpf

module Program =
    [<Literal>]
    let private appUrl = "https://collaborative-systems.org/ambit"

    let private createStatusText text =
        TextBlock(
            Text = text,
            Margin = Thickness(8.0, 4.0, 8.0, 4.0),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center)

    let private setStatus (statusText: TextBlock) state =
        statusText.Text <- "Loading state: " + state

    let private userDataFolder =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.LocalApplicationData,
            "Gambol",
            "WebView2")

    let private createMainWindow (localUrl: Uri) =
        let urlText = createStatusText ("URL: " + string localUrl)
        let statusText = createStatusText "Loading state: initializing WebView2"
        let proxyText = createStatusText ("Proxy target: " + appUrl)
        let currentDirectoryText =
            createStatusText ("Current directory: " + Environment.CurrentDirectory)

        let statusPanel =
            StackPanel(
                Background = Brushes.WhiteSmoke,
                Orientation = Orientation.Vertical)

        statusPanel.Children.Add urlText |> ignore
        statusPanel.Children.Add statusText |> ignore
        statusPanel.Children.Add proxyText |> ignore
        statusPanel.Children.Add currentDirectoryText |> ignore

        let webView =
            new WebView2(
                CreationProperties =
                    CoreWebView2CreationProperties(
                        UserDataFolder = userDataFolder))

        webView.CoreWebView2InitializationCompleted.Add(fun args ->
            if args.IsSuccess then
                setStatus statusText "initialized; loading"
            else
                setStatus statusText ("initialization failed: " + args.InitializationException.Message))

        webView.NavigationStarting.Add(fun args ->
            urlText.Text <- "URL: " + args.Uri
            setStatus statusText "loading")

        webView.NavigationCompleted.Add(fun args ->
            if args.IsSuccess then
                setStatus statusText "complete"
            else
                setStatus statusText ("failed: " + string args.WebErrorStatus))

        webView.Source <- localUrl

        let layout = DockPanel()
        DockPanel.SetDock(statusPanel, Dock.Top)
        layout.Children.Add statusPanel |> ignore
        layout.Children.Add webView |> ignore

        new Window(
                Title = "Gambol",
                Width = 1280,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = layout)

    [<EntryPoint; STAThread>]
    let main _ =
        let proxy: LocalProxy =
            LocalProxy.start appUrl
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let app = Application(ShutdownMode = ShutdownMode.OnMainWindowClose)

        let window = createMainWindow proxy.LocalUrl
        app.MainWindow <- window
        let exitCode = app.Run window

        proxy.Stop().GetAwaiter().GetResult()
        exitCode
