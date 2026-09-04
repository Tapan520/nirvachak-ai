param(
    [Parameter(Mandatory = $true)]
    [string]$MySqlPassword,

    [string]$Server = "127.0.0.1",
    [int]$Port = 3306,
    [string]$User = "root",
    [string]$Database = "nirvachak_ai"
)

$ErrorActionPreference = "Stop"
$mysql = "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
if (-not (Test-Path $mysql)) {
    throw "mysql.exe not found at $mysql"
}

Write-Host "Creating database '$Database' if it does not exist..." -ForegroundColor Cyan
& $mysql -h $Server -P $Port -u $User "--password=$MySqlPassword" -e "CREATE DATABASE IF NOT EXISTS `$Database CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

$cs = "Server=$Server;Port=$Port;Database=$Database;User=$User;Password=$MySqlPassword;CharSet=utf8mb4;"
$env:MYSQL_CONNECTION_STRING = $cs

Write-Host "Saving connection string to .NET user-secrets..." -ForegroundColor Cyan
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $cs --project Nirvachak_AI.csproj | Out-Null

Write-Host "Applying EF Core migrations..." -ForegroundColor Cyan
dotnet ef database update --project Nirvachak_AI.csproj

Write-Host "Local MySQL setup complete." -ForegroundColor Green
Write-Host "Run the app with: dotnet run --project Nirvachak_AI.csproj" -ForegroundColor Green
