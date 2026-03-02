# Getting Debugging working

## The problem
jubale — 8:28 AM
“Can i get help trying to run dotnet debugger in vscode, never done this and launch.json config is not working.  I get a problem report "type":"coreclr" debug type is not recognized.!”   Answer linked below, but why does my editor say coreclr is a  bad value?  I think i need to fix a thing. 

Squiggles in yellow.  Popup message:
> The debug type is not recognized. Make sure that you have a corresponding debug extension installed and that it is enabled.

## Things checked

[x] The only f# plugin installed is Ionide.  The framework is .NET 10
[x] C# extension from MS is installed.
[x] This is VSCode, not Codium

[x] doing dotnet build in the directory works fine? - YES


[x] Manually create the .vscode/launch.json file (could not get this to create the file thru the dialogs)

### Selecting a launch config
[ ] Then I was able to select a launch config
* Hm.  I don't get a launch config selection.  It just "starts" but that does nothing, and never reaches my breakpoint.  The server isn't actually started.


[ ] Choose .net Core launch (console) and provde the path the dll
The launch project option doesn't work because the C# plugin doesn't care about fsproj files.
 
{
    "configurations": [
        {
            "name": ".NET Core Launch (console)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/bin\\Debug\\net10.0\\debugger-support.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "console": "internalConsole"
        }
     
    ]
}
[x] Copied above config in place.
TheAngryByrd

 — 12:01 PM
Image
Image
jubale — 12:51 PM
I'm going to stream attempts to fix.
