[CmdletBinding()]
param(
    [Parameter()]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$WebsiteName,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$SelfContained,

    [string]$ZipPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList
    )

    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "'$FilePath' exited with code $LASTEXITCODE."
    }
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$webProject = Join-Path $repositoryRoot 'BlazorBlogs\BlazorBlogs.csproj'
$artifactRoot = Join-Path $repositoryRoot 'artifacts'
$publishRoot = Join-Path $artifactRoot 'publish'

if ([string]::IsNullOrWhiteSpace($ZipPath)) {
    Remove-Item $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

    $publishArguments = @(
        'publish', $webProject,
        '-c', $Configuration,
        '-o', $publishRoot,
        '--nologo'
    )
    if ($SelfContained) {
        $publishArguments += @('--self-contained', 'true', '-r', 'win-x64')
    }

    Invoke-External -FilePath 'dotnet' -ArgumentList $publishArguments

    $protectedPaths = @(
        'wwwroot\uploads',
        'wwwroot\blogs',
        'wwwroot\files',
        'PrivateFiles'
    )

    $unsafeFiles = @(
        foreach ($protectedPath in $protectedPaths) {
            $fullPath = Join-Path $publishRoot $protectedPath
            if (Test-Path $fullPath) {
                Get-ChildItem $fullPath -Recurse -File -Force
            }
        }
    )
    if ($unsafeFiles.Count -gt 0) {
        $relativeFiles = $unsafeFiles | ForEach-Object {
            [System.IO.Path]::GetRelativePath($publishRoot, $_.FullName)
        }
        throw "Publish package contains protected user content: $($relativeFiles -join ', ')"
    }

    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    $ZipPath = Join-Path $artifactRoot ("site-{0}.zip" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
    Compress-Archive -Path (Join-Path $publishRoot '*') -DestinationPath $ZipPath -Force
}
else {
    $ZipPath = (Resolve-Path $ZipPath).Path
}

if (-not (Test-Path $ZipPath -PathType Leaf)) {
    throw "Deployment package '$ZipPath' does not exist."
}

if ([string]::IsNullOrWhiteSpace($ResourceGroup)) {
    $resourceGroupJson = & az webapp list --query "[?name=='$WebsiteName'].resourceGroup" -o tsv
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI could not resolve the resource group for '$WebsiteName'."
    }
    $ResourceGroup = ($resourceGroupJson | Select-Object -First 1).Trim()
    if ([string]::IsNullOrWhiteSpace($ResourceGroup)) {
        throw "No Azure Web App named '$WebsiteName' is visible to the current Azure account."
    }
}

Invoke-External -FilePath 'az' -ArgumentList @(
    'webapp', 'deploy',
    '--resource-group', $ResourceGroup,
    '--name', $WebsiteName,
    '--src-path', $ZipPath,
    '--type', 'zip',
    '--clean', 'false'
)

Write-Host "Deployed $ZipPath to $WebsiteName in $ResourceGroup."
