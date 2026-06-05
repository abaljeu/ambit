# Database migration notes

Status: historical Azure/PostgreSQL setup notes (superseded). Current persistence design:
[[doc/current/persistence-model.md]]. Operational rollout: [[doc/reference/postgres-environments.md]].

The older `operations_log` / `documents` schema described below is superseded. Current Gambol
PostgreSQL persistence uses `changes`, `graph`, `nodes`, and `node_children`. The current planned
mode split is:

- `db` is the default strict PostgreSQL authority. It does not import files at startup.
- `file` is explicit file-authority mode. It may seed an empty DB from files and mirror writes to DB.

Below is a partial description of a persistence mechanism I want to implement on my Azure server using Postgres. The problem I need you to help me solve: I don't have Postgres. I only have the most basic Azure node with a shared space for client data and server code. I am the only client, so this much works but how do i add a Postgres database and set up that program to achieve the below.



Thought Process
expand
Adding PostgreSQL to Your Azure Node
First, a clarifying question matters here: what OS is your Azure VM running? I'll assume Ubuntu/Debian (most common), but I'll note where things differ.

Step 1: Install PostgreSQL on the VM
SSH into your server, then:

bash
# Update package list
sudo apt update

# Install PostgreSQL (this gets you the server, client, and contrib extensions)
sudo apt install -y postgresql postgresql-contrib

# Verify it's running
sudo systemctl status postgresql

# Enable it to start on reboot
sudo systemctl enable postgresql
PostgreSQL creates a system user called postgres automatically. You'll use that to bootstrap everything.

Step 2: Create Your Database and User
bash
# Switch to the postgres system user
sudo -i -u postgres

# Open the PostgreSQL prompt
psql
Inside psql:

sql
-- Create a dedicated user for your app (don't use the superuser in production)
CREATE USER myapp_user WITH PASSWORD 'choose_a_strong_password_here';

-- Create the database
CREATE DATABASE myapp_db OWNER myapp_user;

-- Give the user full rights on the database
GRANT ALL PRIVILEGES ON DATABASE myapp_db TO myapp_user;

-- Exit
\q
Then exit back to your normal shell:

bash
exit
Step 3: Create Your Schema
Connect as your new user:

bash
psql -U myapp_user -d myapp_db -h localhost
You'll be prompted for the password. Then paste in your schema:

sql
-- The write side: append-only, global truth
CREATE TABLE operations_log (
    global_seq_id BIGSERIAL PRIMARY KEY,
    operation_type VARCHAR(50) NOT NULL,
    payload JSONB NOT NULL,
    timestamp TIMESTAMPTZ DEFAULT NOW()
);

-- Index for efficient "give me everything after seq X" queries
-- This is the core query your projection worker will run constantly
CREATE INDEX idx_ops_log_seq ON operations_log (global_seq_id);

-- The read side: projected document snapshots
CREATE TABLE documents (
    id VARCHAR PRIMARY KEY,
    content JSONB NOT NULL,
    adjacent_doc_ids JSONB DEFAULT '[]'::jsonb,
    last_updated_seq_id BIGINT DEFAULT 0,
    search_vector TSVECTOR
);

-- Full-text search index (GIN is the right index type for tsvector)
CREATE INDEX idx_documents_search ON documents USING GIN (search_vector);

-- Index for loading multiple documents by ID in one query
-- (your SELECT content FROM documents WHERE id IN (...) query)
CREATE INDEX idx_documents_id ON documents (id);

\q
Step 4: Configure PostgreSQL to Accept Local Connections
By default Postgres only allows connections from localhost using peer auth for system users. For your app user with a password, you need to confirm the pg_hba.conf allows md5 or scram-sha-256:

bash
# Find the config file location
sudo -u postgres psql -c "SHOW hba_file;"
Open it (path will look like /etc/postgresql/15/main/pg_hba.conf):

bash
sudo nano /etc/postgresql/15/main/pg_hba.conf
Make sure this line exists (or add it above the default lines):

text
# TYPE  DATABASE        USER            ADDRESS         METHOD
local   myapp_db        myapp_user                      scram-sha-256
host    myapp_db        myapp_user      127.0.0.1/32    scram-sha-256
Then reload:

bash
sudo systemctl reload postgresql
Step 5: Connect Your Server Code
This depends on what language your server is written in. Since you mentioned F#, I'll assume a .NET server (likely Giraffe, Saturn, or similar).

Add the NuGet package:

bash
dotnet add package Npgsql
# Or if using Dapper for a lighter query layer:
dotnet add package Dapper
dotnet add package Npgsql
Connection string (put this in your config/environment, not hardcoded):

text
Host=localhost;Database=myapp_db;Username=myapp_user;Password=your_password;
Basic connection module in F#:

fsharp
module Database

open Npgsql
open System

let connectionString = 
    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    // Fallback for local dev only - never hardcode in production:
    |> Option.ofObj
    |> Option.defaultValue "Host=localhost;Database=myapp_db;Username=myapp_user;Password=your_password;"

let getConnection () =
    new NpgsqlConnection(connectionString)
Appending an operation (write side):

fsharp
open Dapper
open System.Text.Json

let appendOperation (operationType: string) (payload: obj) =
    task {
        use conn = Database.getConnection()
        do! conn.OpenAsync()
        
        let sql = """
            INSERT INTO operations_log (operation_type, payload)
            VALUES (@OperationType, @Payload::jsonb)
            RETURNING global_seq_id
        """
        
        let payloadJson = JsonSerializer.Serialize(payload)
        
        let! seqId = conn.ExecuteScalarAsync<int64>(sql, {|
            OperationType = operationType
            Payload = payloadJson
        |})
        
        return seqId  // You'll broadcast this to clients
    }
Loading documents (read side):

fsharp
let loadDocuments (docIds: string list) =
    task {
        use conn = Database.getConnection()
        do! conn.OpenAsync()
        
        // Dapper handles the IN clause via array parameter
        let sql = """
            SELECT id, content, adjacent_doc_ids, last_updated_seq_id
            FROM documents
            WHERE id = ANY(@Ids)
        """
        
        let! rows = conn.QueryAsync(sql, {| Ids = docIds |> Array.ofList |})
        return rows
    }
Step 6: The Projection Worker
This is the part that reads operations_log and updates documents. The simplest version is a loop that runs on your server:

fsharp
module ProjectionWorker

open Dapper

// Tracks where we are in the log
let mutable private lastProcessedSeqId = 0L

let private applyOperation (conn: NpgsqlConnection) (opType: string) (payload: JsonElement) (seqId: int64) =
    task {
        match opType with
        | "UpdateNode" ->
            let nodeId = payload.GetProperty("nodeId").GetString()
            // nodeId format: "docA.nodeX" - split to get document ID
            let docId = nodeId.Split('.')[0]
            let newText = payload.GetProperty("newText").GetString()
            
            // JSON patch: update the specific node inside documents.content
            let sql = """
                UPDATE documents
                SET 
                    content = jsonb_set(
                        content,
                        '{nodes}',
                        (
                            SELECT jsonb_agg(
                                CASE WHEN node->>'nodeId' = @NodeId 
                                THEN jsonb_set(node, '{text}', to_jsonb(@NewText::text))
                                ELSE node END
                            )
                            FROM jsonb_array_elements(content->'nodes') AS node
                        )
                    ),
                    last_updated_seq_id = @SeqId
                WHERE id = @DocId
            """
            do! conn.ExecuteAsync(sql, {|
                NodeId = nodeId
                NewText = newText
                SeqId = seqId
                DocId = docId
            |}) |> Task.ignore
            
        | "DeleteNode" ->
            // Handle node deletion + cascading link removals from payload
            // Similar JSON manipulation pattern
            ()
            
        | "CreateLink" ->
            // Update source doc's content to add the link
            // Update adjacent_doc_ids for both documents
            ()
            
        | "SplitDocument" ->
            // Create new document row, trim content of original
            ()
            
        | _ -> 
            printfn "Unknown operation type: %s" opType
    }

let runProjectionLoop () =
    task {
        // Initialize: find where we left off
        use initConn = Database.getConnection()
        do! initConn.OpenAsync()
        let! maxSeq = initConn.ExecuteScalarAsync<int64>(
            "SELECT COALESCE(MAX(last_updated_seq_id), 0) FROM documents"
        )
        lastProcessedSeqId <- maxSeq
        
        while true do
            use conn = Database.getConnection()
            do! conn.OpenAsync()
            
            let sql = """
                SELECT global_seq_id, operation_type, payload
                FROM operations_log
                WHERE global_seq_id > @LastSeqId
                ORDER BY global_seq_id ASC
                LIMIT 100  -- Process in batches
            """
            
            let! ops = conn.QueryAsync(sql, {| LastSeqId = lastProcessedSeqId |})
            
            for op in ops do
                do! applyOperation conn op.operation_type op.payload op.global_seq_id
                lastProcessedSeqId <- op.global_seq_id
            
            // If no new ops, wait a bit before polling again
            if Seq.isEmpty ops then
                do! Task.Delay(100)  // 100ms poll interval
    }
Start this in your server startup:

fsharp
// In your Program.fs or startup code
Task.Run(fun () -> ProjectionWorker.runProjectionLoop()) |> ignore
Step 7: Store the Connection String Safely
Don't put the password in your code. On your Azure VM:

bash
# Add to your shell profile or systemd service environment
echo 'export DB_CONNECTION_STRING="Host=localhost;Database=myapp_db;Username=myapp_user;Password=your_password;"' >> ~/.profile
source ~/.profile
Or if you run your server as a systemd service (recommended), add it to the service file:

ini
[Service]
Environment="DB_CONNECTION_STRING=Host=localhost;Database=myapp_db;Username=myapp_user;Password=your_password;"
Quick Sanity Check
bash
# Verify you can connect and see your tables
psql -U myapp_user -d myapp_db -h localhost -c "\dt"

# Should show:
#  Schema |     Name       | Type  |    Owner    
# --------+----------------+-------+-------------
#  public | documents      | table | myapp_user
#  public | operations_log | table | myapp_user
What You Have After This
text
Azure VM
├── Your server code (F#)          ← connects to localhost:5432
├── PostgreSQL (same machine)
│   ├── operations_log             ← append-only write side
│   └── documents                  ← projected read side
└── Projection worker (F# task)    ← loops, reads ops, updates documents
The entire stack runs on one machine. No external services, no Azure Database for PostgreSQL pricing. When you outgrow the single VM you can migrate Postgres to Azure Database for PostgreSQL Flexible Server and just change the connection string.

The biggest thing to implement carefully is the JSON patching in applyOperation — PostgreSQL's jsonb_set is powerful but verbose. If that becomes unwieldy, consider pulling the document into your F# code, mutating it as an in-memory object, then writing it back as a single UPDATE.

Claude Sonnet 4.6
clarifying answer: It's a .net service running windows.


Thought Process
expand
PostgreSQL on Windows Azure VM
The schema and F# code from the previous answer stays exactly the same. Only the installation and service management changes.

Step 1: Install PostgreSQL on Windows
Download the installer:

Go to https://www.enterprisedb.com/downloads/postgres-postgresql-downloads and grab the Windows x86-64 installer for the latest stable version (16 or 17).

Or download directly from PowerShell:

powershell
# Run PowerShell as Administrator
$url = "https://get.enterprisedb.com/postgresql/postgresql-16-1-windows-x64.exe"
$output = "C:\Temp\pg_installer.exe"
Invoke-WebRequest -Uri $url -OutFile $output
Start-Process -FilePath $output -Wait
During the installer wizard:

Setting	Value
Installation Directory	C:\Program Files\PostgreSQL\16 (default is fine)
Data Directory	C:\Program Files\PostgreSQL\16\data
Password	Set a strong password for the postgres superuser
Port	5432 (default, keep it)
Locale	Default
Stack Builder	Uncheck — you don't need it
PostgreSQL installs itself as a Windows Service called postgresql-x64-16 and starts automatically.

Step 2: Verify It's Running
powershell
# Check the service status
Get-Service -Name "postgresql*"

# Should show:
# Status   Name               DisplayName
# ------   ----               -----------
# Running  postgresql-x64-16  postgresql-x64-16
If it's not running:

powershell
Start-Service -Name "postgresql-x64-16"

# Make sure it starts automatically on reboot
Set-Service -Name "postgresql-x64-16" -StartupType Automatic
Step 3: Add psql to Your PATH
The installer doesn't always add the bin folder to PATH. Fix that:

powershell
# Run as Administrator
$pgBin = "C:\Program Files\PostgreSQL\16\bin"
$currentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")

if ($currentPath -notlike "*$pgBin*") {
    [Environment]::SetEnvironmentVariable(
        "Path", 
        "$currentPath;$pgBin", 
        "Machine"
    )
}

# Reload PATH in current session
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
Verify:

powershell
psql --version
# postgresql 16.x
Step 4: Create Your Database and User
powershell
# Connect as the postgres superuser
# It will prompt for the password you set during installation
psql -U postgres -h localhost
Inside psql:

sql
CREATE USER myapp_user WITH PASSWORD 'choose_a_strong_password_here';

CREATE DATABASE myapp_db OWNER myapp_user;

GRANT ALL PRIVILEGES ON DATABASE myapp_db TO myapp_user;

\q
Step 5: Create Your Schema
powershell
psql -U myapp_user -d myapp_db -h localhost
sql
CREATE TABLE operations_log (
    global_seq_id BIGSERIAL PRIMARY KEY,
    operation_type VARCHAR(50) NOT NULL,
    payload JSONB NOT NULL,
    timestamp TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_ops_log_seq ON operations_log (global_seq_id);

CREATE TABLE documents (
    id VARCHAR PRIMARY KEY,
    content JSONB NOT NULL,
    adjacent_doc_ids JSONB DEFAULT '[]'::jsonb,
    last_updated_seq_id BIGINT DEFAULT 0,
    search_vector TSVECTOR
);

CREATE INDEX idx_documents_search ON documents USING GIN (search_vector);

\q
Step 6: Configure Authentication
On Windows, PostgreSQL uses pg_hba.conf for authentication. Find it:

powershell
psql -U postgres -h localhost -c "SHOW hba_file;"
# Typically: C:/Program Files/PostgreSQL/16/data/pg_hba.conf
Open it in Notepad (as Administrator) and confirm or add these lines near the top of the connection rules, before the default entries:

text
# TYPE  DATABASE    USER          ADDRESS         METHOD
host    myapp_db    myapp_user    127.0.0.1/32    scram-sha-256
host    myapp_db    myapp_user    ::1/128         scram-sha-256
After saving, reload the config:

powershell
# Either restart the service
Restart-Service -Name "postgresql-x64-16"

# Or reload config without dropping connections (connects as superuser)
psql -U postgres -h localhost -c "SELECT pg_reload_conf();"
Step 7: Store the Connection String Safely
Do not hardcode it. On Windows the right place is a System Environment Variable:

powershell
# Run as Administrator
[Environment]::SetEnvironmentVariable(
    "DB_CONNECTION_STRING",
    "Host=localhost;Database=myapp_db;Username=myapp_user;Password=your_password;",
    "Machine"   # Machine = system-wide, available to all processes including your .NET service
)
Verify it's set:

powershell
[Environment]::GetEnvironmentVariable("DB_CONNECTION_STRING", "Machine")
Your F# code reads it exactly as shown before:

fsharp
let connectionString = 
    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    |> Option.ofObj
    |> Option.defaultValue (failwith "DB_CONNECTION_STRING not set")
Step 8: Running Your .NET Service on Windows
If your server is currently just run manually (e.g. dotnet run), you should register it as a Windows Service so it survives reboots and runs in the background alongside PostgreSQL.

Add the hosting package:

bash
dotnet add package Microsoft.Extensions.Hosting.WindowsServices
In your Program.fs:

fsharp
open Microsoft.Extensions.Hosting

[<EntryPoint>]
let main args =
    Host.CreateDefaultBuilder(args)
        .UseWindowsService()          // <-- This is the key line
        .ConfigureServices(fun services ->
            // Register your services, projection worker, etc.
            services.AddHostedService<ProjectionWorker>() |> ignore
        )
        .Build()
        .Run()
    0
Register it as a Windows Service:

powershell
# Build your project first
dotnet publish -c Release -r win-x64 --self-contained false -o C:\Services\MyApp

# Register as a service (run as Administrator)
New-Service `
    -Name "MyAppServer" `
    -BinaryPathName "C:\Services\MyApp\MyApp.exe" `
    -DisplayName "My App Server" `
    -StartupType Automatic `
    -Description "My F# application server"

# Start it
Start-Service -Name "MyAppServer"
Check it's running:

powershell
Get-Service -Name "MyAppServer"
Now both PostgreSQL and your app start automatically when the VM reboots, in the background, with no one needing to be logged in.

Step 9: Firewall — Keep Postgres Off the Internet
PostgreSQL should only be reachable from localhost. Verify the Windows Firewall is not exposing port 5432 externally:

powershell
# Check for any rules exposing 5432
Get-NetFirewallRule | Where-Object { $_.Enabled -eq "True" } | 
    Get-NetFirewallPortFilter | 
    Where-Object { $_.LocalPort -eq "5432" }

# If you see any results, remove them:
Remove-NetFirewallRule -DisplayName "PostgreSQL"  # adjust name to match what you see
Your .NET service connects to localhost:5432 internally — no firewall rule needed for that.

Full Picture on Your VM
text
Azure Windows VM
├── Windows Service: MyAppServer (your F# .NET app)
│   ├── HTTP/WebSocket endpoint for clients
│   ├── Projection worker (background Task)
│   └── Connects to localhost:5432
│
├── Windows Service: postgresql-x64-16
│   ├── operations_log   (write side)
│   └── documents        (read side / projections)
│
└── Windows Firewall
    ├── Port 443 open     (your app, HTTPS)
    └── Port 5432 CLOSED  (Postgres stays internal only)
Both services start on boot, Postgres is never exposed to the internet, and your connection string lives in a system environment variable rather than in source code.

Claude Sonnet 4.6
what happens to my existing Server.fs main if i use your Service registering main?


Thought Process
expand
Good question to ask before just copy-pasting. The answer depends on what your current main looks like, but here's the general picture:

What Likely Happens: A Conflict
Your current main probably looks something like one of these:

Option A: Manual Kestrel/Giraffe setup

fsharp
[<EntryPoint>]
let main args =
    let app = WebApplication.Create(args)
    // ... route setup ...
    app.Run()
    0
Option B: WebApplication builder

fsharp
[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    // ... service registration ...
    let app = builder.Build()
    // ... middleware pipeline ...
    app.Run()
    0
The problem: You can only have one [<EntryPoint>]. The two mains can't coexist as written.

The Fix: They Merge, Not Replace
UseWindowsService() is just one extra line added to whatever host builder you already have. You don't throw away your existing main:

fsharp
[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    // --- YOUR EXISTING SERVICE REGISTRATIONS STAY ---
    builder.Services.AddSingleton<SomeService>() |> ignore
    // ... etc ...

    // --- ADD THIS ONE LINE ---
    builder.Host.UseWindowsService() |> ignore
    
    let app = builder.Build()
    
    // --- YOUR EXISTING MIDDLEWARE PIPELINE STAYS ---
    app.UseHttpsRedirection() |> ignore
    app.MapGet("/", ...) |> ignore
    // ... etc ...

    // --- ADD YOUR PROJECTION WORKER HERE ---
    // Either as a registered IHostedService above, or just:
    Task.Run(fun () -> ProjectionWorker.runProjectionLoop()) |> ignore

    app.Run()
    0
UseWindowsService() just tells the host:

"If you were started by the Windows Service Control Manager, behave as a service (no console, handle start/stop signals)"
"If you were started from a terminal normally, behave normally"
It's a no-op when you run with dotnet run during development.

The IHostedService Option (Cleaner but Optional)
In the previous answer I showed registering the projection worker as an IHostedService. That's the idiomatic .NET way but it's not required. The Task.Run approach works fine for a single-user server.

If you do want the cleaner approach, the worker becomes a type:

fsharp
type ProjectionWorker() =
    interface IHostedService with
        member _.StartAsync(ct) =
            Task.Run(fun () -> ProjectionWorker.runProjectionLoop()) |> ignore
            Task.CompletedTask
        member _.StopAsync(ct) =
            // Signal your loop to stop if needed
            Task.CompletedTask

// Then in main:
builder.Services.AddHostedService<ProjectionWorker>() |> ignore
But again — only bother with this if you want clean shutdown handling. Task.Run is fine otherwise.

Summary
What you have	What changes
[<EntryPoint>]	Stays, only one entry point
Route definitions	Unchanged
Service registrations	Unchanged
app.Run()	Unchanged
New: builder.Host.UseWindowsService()	One added line
New: Projection worker task	Added before app.Run()
If you paste your actual main here I can show you exactly where each addition goes.

Claude Sonnet 4.6
so builder.Host.UserWindowsService() is added and I don't need all of

Host.CreateDefaultBuilder(args)
or
.ConfigureServices( )


Thought Process
expand
Correct. That pattern I showed:

fsharp
Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices(fun services ->
        services.AddHostedService<ProjectionWorker>() |> ignore
    )
    .Build()
    .Run()
Is the standalone/generic host style — used when you're building a background service with no web server at all.

You already have a WebApplication builder which is a superset of that. So:

Host.CreateDefaultBuilder → already covered by WebApplication.CreateBuilder
.ConfigureServices(...) → already covered by builder.Services.Add...()
UseWindowsService() → the one thing it adds that you need
Just the one line:

fsharp
builder.Host.UseWindowsService() |> ignore
Everything else in your existing main stays untouched.

Claude Sonnet 4.6
When would i need to have a separate machine for the database?


Thought Process
expand
Good thing to understand so you know when your current setup stops being appropriate. There are a few distinct reasons:

Resource Contention
The most common practical reason. PostgreSQL and your .NET service are competing for the same RAM, CPU, and disk I/O on one machine.

This becomes a problem when:

Your documents table grows large enough that Postgres wants to cache significant data in shared_buffers
Your projection worker is doing heavy JSON processing continuously
You're getting request latency spikes because Postgres is saturating the disk exactly when your app needs to respond
On a small single-user server this probably never happens. If you had hundreds of concurrent users it likely would.

Scaling Independently
Your app and your database rarely need more resources at exactly the same rate. You might need:

More app servers to handle concurrent WebSocket connections
But the database load stays the same
Once you need more than one app server instance you are forced to separate the database, because both instances need to talk to the same Postgres. You can't have two machines each running their own copy.

Backup and Recovery Strategy
When they're on the same machine:

A disk failure loses both your app and your data simultaneously
You can't snapshot just the database independently
Restoring means bringing everything back together
Separate machines let you:

Take Postgres-specific point-in-time backups
Restore the database to a different point than the app
Lose the app server without losing data
Security Posture
On one machine, if your app is compromised the attacker is already on the same machine as the database. Separation means:

The database machine can have no public network access at all
Compromise of the app server requires a second step to reach data
You can apply different hardening rules to each machine
Compliance / Audit Requirements
Some regulatory environments (HIPAA, SOC2, PCI-DSS etc.) require demonstrable separation between application tier and data tier. Even if technically unnecessary, auditors expect to see it.

Practical Threshold for Your Situation
For a single-user personal server running this CQRS design, you would likely only be forced to separate when:

You add more users and need multiple app instances, or
Your VM's disk becomes the bottleneck (Postgres I/O starving your app), or
You need stronger backup guarantees than a single-disk snapshot provides
None of those are near-term concerns for what you've described. The current single-machine setup is entirely appropriate and is how most small-to-medium applications run in production.

Claude Sonnet 4.6
what's a basic backup strategy for my database


Thought Process
expand
For your setup — single Windows VM, single user, Azure — here's a practical strategy without overcomplicating it.

The Two Tools You Need
pg_dump — dumps a snapshot of your database to a file at a point in time. This is your primary tool.

Windows Task Scheduler — runs pg_dump automatically on a schedule.

That's it for your scale.

Step 1: Create the Backup Script
Create C:\Scripts\backup-postgres.ps1:

powershell
# Settings
$pgDumpPath = "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe"
$backupDir  = "C:\Backups\Postgres"
$dbName     = "myapp_db"
$dbUser     = "myapp_user"
$dbHost     = "localhost"
$keepDays   = 7  # How many days of backups to retain

# Create backup directory if it doesn't exist
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
}

# Filename with timestamp: myapp_db_2024-01-15_02-00-00.dump
$timestamp  = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$backupFile = "$backupDir\${dbName}_${timestamp}.dump"

# Set password so pg_dump doesn't prompt interactively
$env:PGPASSWORD = [Environment]::GetEnvironmentVariable("DB_PASSWORD", "Machine")

# Run the dump
# -Fc = custom format (compressed, most flexible for restore)
& $pgDumpPath `
    -h $dbHost `
    -U $dbUser `
    -Fc `
    -f $backupFile `
    $dbName

if ($LASTEXITCODE -eq 0) {
    Write-EventLog -LogName Application -Source "PostgresBackup" `
        -EntryType Information -EventId 1 `
        -Message "Backup succeeded: $backupFile"
} else {
    Write-EventLog -LogName Application -Source "PostgresBackup" `
        -EntryType Error -EventId 2 `
        -Message "Backup FAILED for $dbName at $timestamp"
}

# Clean up backups older than $keepDays
Get-ChildItem -Path $backupDir -Filter "*.dump" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$keepDays) } |
    Remove-Item -Force
Store the password as a system environment variable (separate from your connection string so the script can use it directly):

powershell
[Environment]::SetEnvironmentVariable("DB_PASSWORD", "your_password", "Machine")
Register the event log source so the script can write to it (run once):

powershell
New-EventLog -LogName Application -Source "PostgresBackup"
Step 2: Schedule It
Open Task Scheduler and create a new task, or do it from PowerShell:

powershell
$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NonInteractive -File C:\Scripts\backup-postgres.ps1"

# Run at 2am every day
$trigger = New-ScheduledTaskTrigger -Daily -At "02:00"

# Run as SYSTEM so it has access to Machine environment variables
$principal = New-ScheduledTaskPrincipal `
    -UserId "SYSTEM" `
    -LogonType ServiceAccount `
    -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit (New-TimeSpan -Hours 1) `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 5)

Register-ScheduledTask `
    -TaskName "PostgresNightlyBackup" `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Description "Nightly pg_dump backup of myapp_db"
Test it runs correctly before relying on it:

powershell
Start-ScheduledTask -TaskName "PostgresNightlyBackup"
# Wait a few seconds then check:
Get-ChildItem C:\Backups\Postgres
Step 3: Copy Backups Off the Machine
A backup on the same disk as the database is not really a backup — one disk failure loses both. Since you're on Azure the obvious destination is Azure Blob Storage.

Create a storage account in the Azure Portal (cheapest tier: LRS, Cool access tier is fine for backups).

Install the Azure CLI on your VM:

powershell
winget install Microsoft.AzureCLI
Add an upload step to your backup script:

powershell
# Add these settings at the top of backup-postgres.ps1
$storageAccount = "yourstorageaccount"
$containerName  = "postgres-backups"
$sasToken       = [Environment]::GetEnvironmentVariable("AZURE_BACKUP_SAS", "Machine")

# Add this after the successful backup check
if ($LASTEXITCODE -eq 0) {
    # Upload to Azure Blob Storage
    az storage blob upload `
        --account-name $storageAccount `
        --container-name $containerName `
        --name "backups/${dbName}_${timestamp}.dump" `
        --file $backupFile `
        --sas-token $sasToken `
        --overwrite
}
Generate a SAS token in the Azure Portal scoped to just that container with write-only permission, and store it:

powershell
[Environment]::SetEnvironmentVariable("AZURE_BACKUP_SAS", "?sv=2023...", "Machine")
What You End Up With
text
Every night at 2am:
  pg_dump runs
    → writes compressed .dump to C:\Backups\Postgres\
    → uploads copy to Azure Blob Storage
    → deletes local dumps older than 7 days
    → writes success/failure to Windows Event Log
Local copies give you fast restore. Azure Blob copies survive a VM failure. Event Log gives you an audit trail you can check if something seems wrong.

Restoring From a Backup
If you ever need it:

powershell
$pgRestorePath = "C:\Program Files\PostgreSQL\16\bin\pg_restore.exe"
$backupFile    = "C:\Backups\Postgres\myapp_db_2024-01-15_02-00-00.dump"
$env:PGPASSWORD = [Environment]::GetEnvironmentVariable("DB_PASSWORD", "Machine")

# Drop and recreate the database first
psql -U postgres -h localhost -c "DROP DATABASE myapp_db;"
psql -U postgres -h localhost -c "CREATE DATABASE myapp_db OWNER myapp_user;"

# Restore
& $pgRestorePath `
    -h localhost `
    -U myapp_user `
    -d myapp_db `
    $backupFile
One Thing to Add Later
Once your data matters enough to care about losing even a few hours of work, look into WAL archiving (Write-Ahead Log). That's PostgreSQL's continuous logging which lets you restore to any point in time, not just last night's snapshot. It's significantly more complex to set up but that's the next level when nightly dumps aren't enough.

For now, nightly pg_dump plus Azure Blob upload is entirely appropriate.

Claude Sonnet 4.6
how can i extend the backup lifetime. i'm thinking 7 days + 4 weeks + 12 months.


Thought Process
expand
This is called a Grandfather-Father-Son (GFS) rotation scheme. It's a classic and well-proven approach.

The logic is: daily backups are cheap to keep for a week, but instead of deleting everything older than 7 days, you promote certain backups to longer-lived tiers.

The Logic
Tier	Kept	Taken When	Total Files
Daily	7	Every night	7
Weekly	4	Sunday night	4
Monthly	12	1st of the month	12
Total			23 files
A Sunday backup gets saved as both a daily and a weekly. A 1st-of-month backup gets saved as a daily, potentially a weekly, and a monthly. You're not running extra dumps — just copying the same file into different folders with different retention rules.

Revised Backup Script
Replace your backup-postgres.ps1 with this:

powershell
# ── Settings ────────────────────────────────────────────────────────────────
$pgDumpPath     = "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe"
$backupRoot     = "C:\Backups\Postgres"
$dbName         = "myapp_db"
$dbUser         = "myapp_user"
$dbHost         = "localhost"
$storageAccount = "yourstorageaccount"
$containerName  = "postgres-backups"

# Retention counts
$keepDaily      = 7
$keepWeekly     = 4
$keepMonthly    = 12

# ── Directories ──────────────────────────────────────────────────────────────
$dailyDir   = "$backupRoot\daily"
$weeklyDir  = "$backupRoot\weekly"
$monthlyDir = "$backupRoot\monthly"

foreach ($dir in @($dailyDir, $weeklyDir, $monthlyDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
    }
}

# ── Credentials ──────────────────────────────────────────────────────────────
$env:PGPASSWORD = [Environment]::GetEnvironmentVariable("DB_PASSWORD", "Machine")
$sasToken       = [Environment]::GetEnvironmentVariable("AZURE_BACKUP_SAS", "Machine")

# ── Run the dump ─────────────────────────────────────────────────────────────
$now        = Get-Date
$timestamp  = $now.ToString("yyyy-MM-dd_HH-mm-ss")
$backupFile = "$dailyDir\${dbName}_${timestamp}.dump"

& $pgDumpPath -h $dbHost -U $dbUser -Fc -f $backupFile $dbName

if ($LASTEXITCODE -ne 0) {
    Write-EventLog -LogName Application -Source "PostgresBackup" `
        -EntryType Error -EventId 2 `
        -Message "Backup FAILED for $dbName at $timestamp"
    exit 1
}

Write-EventLog -LogName Application -Source "PostgresBackup" `
    -EntryType Information -EventId 1 `
    -Message "Daily backup succeeded: $backupFile"

# ── Promote to weekly (every Sunday) ─────────────────────────────────────────
if ($now.DayOfWeek -eq [DayOfWeek]::Sunday) {
    $weeklyFile = "$weeklyDir\${dbName}_weekly_${timestamp}.dump"
    Copy-Item $backupFile $weeklyFile
    
    Write-EventLog -LogName Application -Source "PostgresBackup" `
        -EntryType Information -EventId 3 `
        -Message "Weekly backup promoted: $weeklyFile"
}

# ── Promote to monthly (1st of each month) ───────────────────────────────────
if ($now.Day -eq 1) {
    $monthlyFile = "$monthlyDir\${dbName}_monthly_${timestamp}.dump"
    Copy-Item $backupFile $monthlyFile
    
    Write-EventLog -LogName Application -Source "PostgresBackup" `
        -EntryType Information -EventId 4 `
        -Message "Monthly backup promoted: $monthlyFile"
}

# ── Upload all three tiers to Azure ──────────────────────────────────────────
function Upload-ToAzure {
    param($localFile, $blobFolder)
    
    $blobName = "$blobFolder/$(Split-Path $localFile -Leaf)"
    
    az storage blob upload `
        --account-name $storageAccount `
        --container-name $containerName `
        --name $blobName `
        --file $localFile `
        --sas-token $sasToken `
        --overwrite

    if ($LASTEXITCODE -ne 0) {
        Write-EventLog -LogName Application -Source "PostgresBackup" `
            -EntryType Warning -EventId 5 `
            -Message "Azure upload failed for $localFile"
    }
}

Upload-ToAzure $backupFile "daily"

if ($now.DayOfWeek -eq [DayOfWeek]::Sunday) {
    Upload-ToAzure "$weeklyDir\${dbName}_weekly_${timestamp}.dump" "weekly"
}

if ($now.Day -eq 1) {
    Upload-ToAzure "$monthlyDir\${dbName}_monthly_${timestamp}.dump" "monthly"
}

# ── Prune old local backups ───────────────────────────────────────────────────
function Prune-Backups {
    param($dir, $keepCount)
    
    Get-ChildItem -Path $dir -Filter "*.dump" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -Skip $keepCount |
        Remove-Item -Force
}

Prune-Backups $dailyDir   $keepDaily
Prune-Backups $weeklyDir  $keepWeekly
Prune-Backups $monthlyDir $keepMonthly

# ── Prune old Azure blobs ─────────────────────────────────────────────────────
function Prune-AzureBlobs {
    param($blobFolder, $keepCount)
    
    # List blobs in the folder, sorted by name (which starts with date so sorts correctly)
    $blobs = az storage blob list `
        --account-name $storageAccount `
        --container-name $containerName `
        --prefix "$blobFolder/" `
        --sas-token $sasToken `
        --query "[].name" `
        --output tsv |
        Sort-Object -Descending |
        Select-Object -Skip $keepCount

    foreach ($blob in $blobs) {
        az storage blob delete `
            --account-name $storageAccount `
            --container-name $containerName `
            --name $blob `
            --sas-token $sasToken
    }
}

Prune-AzureBlobs "daily"   $keepDaily
Prune-AzureBlobs "weekly"  $keepWeekly
Prune-AzureBlobs "monthly" $keepMonthly
What Your Backup Directories Look Like
text
C:\Backups\Postgres\
├── daily\
│   ├── myapp_db_2024-01-15_02-00-00.dump   ← 7 files, oldest deleted
│   ├── myapp_db_2024-01-14_02-00-00.dump
│   └── ...
├── weekly\
│   ├── myapp_db_weekly_2024-01-14_02-00-00.dump  ← 4 files, Sunday dumps
│   └── ...
└── monthly\
    ├── myapp_db_monthly_2024-01-01_02-00-00.dump  ← 12 files, 1st of month
    └── ...
And mirrored in Azure Blob Storage:

text
postgres-backups/
├── daily/
├── weekly/
└── monthly/
One Edge Case to Know About
If the 1st of the month falls on a Sunday, that backup gets promoted to both weekly and monthly — two copies are made from the same dump. That's intentional and correct. It means that file is independently retained under both weekly (4-week) and monthly (12-month) rules.

Verifying It Works Over Time
After the first Sunday you should see a file appear in weekly\. After the 1st of next month one should appear in monthly\. Don't wait to discover a problem — test a restore from a daily backup now while it's low stakes:

powershell
# Pick any .dump file and restore to a test database
psql -U postgres -h localhost -c "CREATE DATABASE myapp_db_test OWNER myapp_user;"

& "C:\Program Files\PostgreSQL\16\bin\pg_restore.exe" `
    -h localhost -U myapp_user -d myapp_db_test `
    "C:\Backups\Postgres\daily\myapp_db_2024-01-15_02-00-00.dump"

# Inspect it
psql -U myapp_user -d myapp_db_test -h localhost -c "SELECT COUNT(*) FROM operations_log;"

# Clean up
psql -U postgres -h localhost -c "DROP DATABASE myapp_db_test;"
If that works, your backup strategy is real. If you've never tested a restore, you don't actually have backups — you have files that might be backups.

Claude Sonnet 4.6