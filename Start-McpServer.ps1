#Requires -Version 5.1
<#
.SYNOPSIS
    Start unitypilot MCP server for UnityUIFlow (HTTP transport).

.DESCRIPTION
    After starting, agents can call tools via http://127.0.0.1:8011/mcp,
    and Unity Editor connects via WebSocket ws://127.0.0.1:8765.

.PARAMETER HttpPort
    MCP HTTP endpoint port. Default: 8011.

.PARAMETER WsPort
    Unity Editor WebSocket port. Default: 8765.

.PARAMETER LogLevel
    Log level: DEBUG | INFO | WARNING | ERROR | CRITICAL. Default: INFO.

.EXAMPLE
    .\Start-McpServer.ps1
    .\Start-McpServer.ps1 -HttpPort 8012 -WsPort 8766 -LogLevel DEBUG
#>
[CmdletBinding()]
param(
    [int] $HttpPort = 8011,
    [int] $WsPort = 8765,
    [string] $LogLevel = "INFO"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$LogDir = Join-Path $ProjectRoot "log"
$LogFile = Join-Path $LogDir "mcp-server.log"
$EntryScript = "D:\unitypilot\run_unitypilot_mcp.py"

if (-not (Test-Path $EntryScript)) {
    Write-Host "[ERROR] MCP entry script not found: $EntryScript" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

function Test-PortOccupied($HostName, $Port) {
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $client.Connect($HostName, $Port)
        $client.Close()
        return $true
    } catch {
        return $false
    }
}

$httpOccupied = Test-PortOccupied "127.0.0.1" $HttpPort
$wsOccupied = Test-PortOccupied "127.0.0.1" $WsPort

if ($httpOccupied -or $wsOccupied) {
    Write-Host "[WARN] Port already occupied, MCP server may already be running." -ForegroundColor Yellow
    if ($httpOccupied) { Write-Host "       HTTP  $HttpPort occupied" }
    if ($wsOccupied)   { Write-Host "       WS    $WsPort occupied" }
    Write-Host "       To restart, kill the occupying process first." -ForegroundColor Yellow
    exit 0
}

Write-Host "[INFO] Starting unitypilot MCP server..." -ForegroundColor Cyan
Write-Host "       HTTP  : http://127.0.0.1:$HttpPort/mcp"
Write-Host "       WS    : ws://127.0.0.1:$WsPort"
Write-Host "       Log   : $LogFile"
Write-Host "       Level : $LogLevel"

$arguments = @(
    $EntryScript
    "--transport", "http"
    "--http-port", "$HttpPort"
    "--port", "$WsPort"
    "--log-file", "$LogFile"
    "--log-level", "$LogLevel"
)

Start-Process -FilePath "python" -ArgumentList $arguments -WorkingDirectory $ProjectRoot

Start-Sleep -Seconds 2

$httpOk = Test-PortOccupied "127.0.0.1" $HttpPort
if ($httpOk) {
    Write-Host "[INFO] MCP server started successfully!" -ForegroundColor Green
} else {
    Write-Host "[WARN] Port not yet listening, server may still be starting. Check log later." -ForegroundColor Yellow
}