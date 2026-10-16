<#
.SYNOPSIS
    Builds the distributable archive for AIInfluence - Antigravity.
#>
[CmdletBinding()]
param([switch] $IncludeSource)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'dist'

$submodulePath = Join-Path $root 'SubModule.xml'
$version = ([xml](Get-Content -LiteralPath $submodulePath -Raw)).Module.Version.value.TrimStart('v')
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$stage = Join-Path $env:TEMP ('agy-pkg-' + [guid]::NewGuid().ToString('N'))
$payload = Join-Path $stage 'AIInfluenceAntigravity'
New-Item -ItemType Directory -Force -Path $payload | Out-Null

Copy-Item -LiteralPath (Join-Path $root 'SubModule.xml') $payload
if (Test-Path (Join-Path $root 'README.md')) { Copy-Item -LiteralPath (Join-Path $root 'README.md') $payload }
if (Test-Path (Join-Path $root 'CHANGELOG.md')) { Copy-Item -LiteralPath (Join-Path $root 'CHANGELOG.md') $payload }
if (Test-Path (Join-Path $root 'new-prompts-detection')) {
    Copy-Item -LiteralPath (Join-Path $root 'new-prompts-detection') $payload -Recurse
}

New-Item -ItemType Directory -Force -Path (Join-Path $payload 'bin\Win64_Shipping_Client') | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'bin\Win64_Shipping_Client\AIInfluenceAntigravity.dll') (Join-Path $payload 'bin\Win64_Shipping_Client')
if (Test-Path (Join-Path $root 'bin\Win64_Shipping_Client\AIInfluenceAntigravity.pdb')) {
    Copy-Item -LiteralPath (Join-Path $root 'bin\Win64_Shipping_Client\AIInfluenceAntigravity.pdb') (Join-Path $payload 'bin\Win64_Shipping_Client')
}

New-Item -ItemType Directory -Force -Path (Join-Path $payload 'Config') | Out-Null
if (Test-Path (Join-Path $root 'Config\antigravity_settings.json')) {
    Copy-Item -LiteralPath (Join-Path $root 'Config\antigravity_settings.json') (Join-Path $payload 'Config')
}

$zip = Join-Path $dist "AIInfluenceAntigravity-$version.zip"
Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($payload, $zip)
Write-Host "Release archive: $zip"

# Copy alias to root
$zipAliasRoot = Join-Path $root "AIInfluenceAntigravity-$version.zip"
Copy-Item -LiteralPath $zip -Destination $zipAliasRoot -Force
Write-Host "Created root alias: $zipAliasRoot"

if ($IncludeSource) {
    $srcStage = Join-Path $env:TEMP ('agy-src-' + [guid]::NewGuid().ToString('N'))
    $srcPayload = Join-Path $srcStage 'AIInfluenceAntigravity'
    New-Item -ItemType Directory -Force -Path $srcPayload | Out-Null
    foreach ($item in @('src','tests','build.bat','package.ps1','deploy.ps1','.gitignore','SubModule.xml','Config','README.md','CHANGELOG.md')) {
        $p = Join-Path $root $item
        if (Test-Path -LiteralPath $p) { Copy-Item -LiteralPath $p -Destination $srcPayload -Recurse }
    }
    foreach ($junk in @('obj','bin')) {
        Get-ChildItem -LiteralPath $srcPayload -Recurse -Directory -Filter $junk -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
    $srcZip = Join-Path $dist "AIInfluenceAntigravity-$version-src.zip"
    Remove-Item -LiteralPath $srcZip -Force -ErrorAction SilentlyContinue
    [System.IO.Compression.ZipFile]::CreateFromDirectory($srcPayload, $srcZip)
    Write-Host "Source archive:  $srcZip"
    Remove-Item -LiteralPath $srcStage -Recurse -Force -ErrorAction SilentlyContinue
}

Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Packaging complete."
