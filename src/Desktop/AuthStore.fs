namespace Gambol.Desktop

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Gambol.Shared

module AuthStore =
    type StoredCredentials = LoginForm.Credentials

    let private storagePath =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.LocalApplicationData,
            "Gambol",
            "auth.dat")

    let private protect (plain: byte[]) =
        ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser)

    let private unprotect (protectedBytes: byte[]) =
        ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser)

    let load () : StoredCredentials option =
        if not (File.Exists storagePath) then
            None
        else
            try
                let plain = unprotect (File.ReadAllBytes storagePath)
                let json = Encoding.UTF8.GetString plain
                use doc = JsonDocument.Parse json
                let user = doc.RootElement.GetProperty("username").GetString()
                let pass = doc.RootElement.GetProperty("password").GetString()

                match user, pass with
                | null, _
                | _, null -> None
                | u, _ when u = "" -> None
                | u, p -> Some { Username = u; Password = p }
            with _ ->
                None

    let save (creds: StoredCredentials) =
        Directory.CreateDirectory(Path.GetDirectoryName storagePath) |> ignore

        let json =
            JsonSerializer.Serialize(
                {| username = creds.Username; password = creds.Password |})

        let plain = Encoding.UTF8.GetBytes json
        File.WriteAllBytes(storagePath, protect plain)

    let clear () =
        if File.Exists storagePath then
            File.Delete storagePath
