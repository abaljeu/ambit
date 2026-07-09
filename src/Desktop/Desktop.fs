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
    let private cloudAppUrl = "https://collaborative-systems.org/ambit"

    [<Literal>]
    let private localAppUrl = "http://localhost:5115/ambit"

    let private targetFromEnv () =
        match Environment.GetEnvironmentVariable "GAMBOL_TARGET_URL" with
        | null
        | "" -> None
        | url -> Some url

    let private targetFromArgs (args: string array) =
        let rec loop i =
            if i >= args.Length then None
            elif args.[i] = "--target" && i + 1 < args.Length then Some args.[i + 1]
            elif args.[i] = "--local" then Some localAppUrl
            elif args.[i] = "--cloud" then Some cloudAppUrl
            elif args.[i].StartsWith "--" then loop (i + 1)
            else Some args.[i]
        loop 0

    let resolveTargetUrl (args: string array) =
        targetFromArgs args
        |> Option.orElseWith targetFromEnv
        |> Option.defaultValue cloudAppUrl

    let private createStatusText text =
        TextBlock(
            Text = text,
            Margin = Thickness(8.0, 4.0, 8.0, 4.0),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center)

    let private formatStatusLine url loadingState proxyTarget currentDirectory =
        sprintf
            "URL: %s  |  Loading state: %s  |  Proxy target: %s  |  Current directory: %s"
            url
            loadingState
            proxyTarget
            currentDirectory

    let private userDataFolder =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.LocalApplicationData,
            "Gambol",
            "WebView2")

    let private createMainWindow (proxyTargetUrl: string) (localUrl: Uri) =
        let mutable currentUrl = string localUrl
        let mutable loadingState = "initializing WebView2"
        let currentDirectory = Environment.CurrentDirectory

        let statusText = createStatusText ""

        let refreshStatus () =
            statusText.Text <-
                formatStatusLine currentUrl loadingState proxyTargetUrl currentDirectory

        refreshStatus ()

        let webView =
            new WebView2(
                CreationProperties =
                    CoreWebView2CreationProperties(
                        UserDataFolder = userDataFolder))

        webView.CoreWebView2InitializationCompleted.Add(fun args ->
            loadingState <-
                if args.IsSuccess then
                    "initialized; loading"
                else
                    "initialization failed: " + args.InitializationException.Message

            refreshStatus ())

        webView.NavigationStarting.Add(fun args ->
            currentUrl <- args.Uri
            loadingState <- "loading"
            refreshStatus ())

        webView.NavigationCompleted.Add(fun args ->
            loadingState <-
                if args.IsSuccess then
                    "complete"
                else
                    "failed: " + string args.WebErrorStatus

            refreshStatus ())

        webView.Source <- localUrl

        statusText.Background <- Brushes.WhiteSmoke

        let layout = DockPanel()
        DockPanel.SetDock(statusText, Dock.Top)
        layout.Children.Add statusText |> ignore
        layout.Children.Add webView |> ignore

        new Window(
                Title = "Gambol",
                Width = 1280,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = layout)

    [<EntryPoint; STAThread>]
    let main argv =
        let proxyTargetUrl = resolveTargetUrl argv

        let proxy: LocalProxy =
            LocalProxy.start proxyTargetUrl
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let app = Application(ShutdownMode = ShutdownMode.OnMainWindowClose)

        let window = createMainWindow proxyTargetUrl proxy.LocalUrl
        app.MainWindow <- window
        let exitCode = app.Run window

        proxy.Stop().GetAwaiter().GetResult()
        exitCode
