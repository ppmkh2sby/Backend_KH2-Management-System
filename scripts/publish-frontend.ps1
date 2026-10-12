param(
    [string]$Output = ".codex-publish/frontend"
)

$ErrorActionPreference = "Stop"

Push-Location "frontend"
try {
    npm ci
    npm run build
}
finally {
    Pop-Location
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null
Copy-Item -Path "frontend/dist/*" -Destination $Output -Recurse -Force
