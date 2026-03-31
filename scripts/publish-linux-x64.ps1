param(
    [string]$Configuration = "Release",
    [string]$Runtime = "linux-x64",
    [string]$Output = ".codex-publish/backend/linux-x64"
)

$ErrorActionPreference = "Stop"

$project = "src/KH2.ManagementSystem.Api/KH2.ManagementSystem.Api.csproj"

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    /p:UseAppHost=false `
    -o $Output
