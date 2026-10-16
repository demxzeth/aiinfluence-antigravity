$ErrorActionPreference = 'Stop'

$src = "Z:\AIInfluence-Antigravity"
$dest = "Y:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AIInfluenceAntigravity"
$modulesRoot = "Y:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules"

$submodulePath = Join-Path $src 'SubModule.xml'
$version = ([xml](Get-Content -LiteralPath $submodulePath -Raw)).Module.Version.value.TrimStart('v')

Write-Host "Deploying AIInfluenceAntigravity v$version to $dest..."

# Ensure target directories exist
New-Item -ItemType Directory -Force -Path (Join-Path $dest "bin\Win64_Shipping_Client") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $dest "Config") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $dest "new-prompts-detection") | Out-Null

# Copy binary payload
try {
    Copy-Item -LiteralPath (Join-Path $src "bin\Win64_Shipping_Client\AIInfluenceAntigravity.dll") -Destination (Join-Path $dest "bin\Win64_Shipping_Client\AIInfluenceAntigravity.dll") -Force
    if (Test-Path (Join-Path $src "bin\Win64_Shipping_Client\AIInfluenceAntigravity.pdb")) {
        Copy-Item -LiteralPath (Join-Path $src "bin\Win64_Shipping_Client\AIInfluenceAntigravity.pdb") -Destination (Join-Path $dest "bin\Win64_Shipping_Client\AIInfluenceAntigravity.pdb") -Force
    }
} catch [System.IO.IOException] {
    Write-Warning "AIInfluenceAntigravity.dll is currently locked by a running process. Close Bannerlord to overwrite the active DLL."
}

# Copy metadata & configs
Copy-Item -LiteralPath (Join-Path $src "SubModule.xml") -Destination (Join-Path $dest "SubModule.xml") -Force
if (Test-Path (Join-Path $src "CHANGELOG.md")) {
    Copy-Item -LiteralPath (Join-Path $src "CHANGELOG.md") -Destination (Join-Path $dest "CHANGELOG.md") -Force
}
if (Test-Path (Join-Path $src "README.md")) {
    Copy-Item -LiteralPath (Join-Path $src "README.md") -Destination (Join-Path $dest "README.md") -Force
}
if (Test-Path (Join-Path $src "Config\antigravity_settings.json")) {
    Copy-Item -LiteralPath (Join-Path $src "Config\antigravity_settings.json") -Destination (Join-Path $dest "Config\antigravity_settings.json") -Force
}

# Copy prompt detection markers
if (Test-Path (Join-Path $src "new-prompts-detection")) {
    Copy-Item -Path (Join-Path $src "new-prompts-detection\*") -Destination (Join-Path $dest "new-prompts-detection") -Recurse -Force
}

# Sync source code if destination src exists
if (Test-Path (Join-Path $dest "src")) {
    Copy-Item -Path (Join-Path $src "src\*") -Destination (Join-Path $dest "src") -Recurse -Force
}

Write-Host "Deployment completed successfully."

# Distribute archive
$zipSource = Join-Path $src "dist\AIInfluenceAntigravity-$version.zip"
if (-not (Test-Path $zipSource)) {
    $zipSource = Join-Path $src "AIInfluenceAntigravity-$version.zip"
}

if (Test-Path $zipSource) {
    Copy-Item -LiteralPath $zipSource -Destination "Z:\AIInfluenceClide\AIInfluenceAntigravity-$version.zip" -Force
    Copy-Item -LiteralPath $zipSource -Destination (Join-Path $modulesRoot "AIInfluenceAntigravity-$version.zip") -Force
    Write-Host "Copied $version archive to:"
    Write-Host " - Z:\AIInfluenceClide\AIInfluenceAntigravity-$version.zip"
    Write-Host " - $modulesRoot\AIInfluenceAntigravity-$version.zip"
}
