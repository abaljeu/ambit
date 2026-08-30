[CmdletBinding()]
param(
    [string]$ResourceGroup = "Amble_group",
    [string]$AppName = "Amble",
    [string]$DbHost = "gambol-pg.postgres.database.azure.com",
    [string]$Database = "gambol",
    [string]$Username = "gambol_admin",
    [switch]$UseQualifiedUsername
)

$ErrorActionPreference = "Stop"

function ConvertTo-PlainText([Security.SecureString]$SecureValue) {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        if ($bstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
}

$passwordSecure = Read-Host "PostgreSQL password for $Username" -AsSecureString
$password = ConvertTo-PlainText $passwordSecure

try {
    $effectiveUsername =
        if ($UseQualifiedUsername) {
            "$Username@gambol-pg"
        }
        else {
            $Username
        }

    $connectionString = "Host=$DbHost;Database=$Database;Username=$effectiveUsername;Password=$password;SSL Mode=Require;Trust Server Certificate=true"

    Write-Host "Setting DB_CONNECTION_STRING on App Service '$AppName' in resource group '$ResourceGroup'..."

    az webapp config appsettings set `
        --resource-group $ResourceGroup `
        --name $AppName `
        --settings DB_CONNECTION_STRING="$connectionString"

    Write-Host "DB_CONNECTION_STRING updated."
}
finally {
    if ($null -ne $password) {
        $password = $null
    }
}
