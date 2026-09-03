$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

docker compose down --remove-orphans
