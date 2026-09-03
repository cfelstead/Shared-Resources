$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

$model = if ($env:AI_MODEL) { $env:AI_MODEL } else { "llama3.1:8b" }

# Start Ollama and wait for its healthcheck to pass.
docker compose up --build -d --wait ollama

Write-Host "Pulling model: $model"
$pulled = $false
for ($attempt = 1; $attempt -le 5; $attempt++) {
    docker compose exec ollama ollama pull $model
    if ($?) {
        $pulled = $true
        break
    }
    Write-Host "Model pull failed, retrying..."
    Start-Sleep -Seconds 10
}

if (-not $pulled) {
    throw "Failed to pull model '$model' after 5 attempts."
}

Write-Host ""
Write-Host "Ollama is ready. Run any step natively, e.g.:"
Write-Host "  dotnet run --project src\Step01Echo"
