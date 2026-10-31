function Get-SteamLibraries {
    param([string[]]$Roots)
    $libraries = @()
    foreach ($root in $Roots) {
        if (!$root) { continue }
        $libraries += $root
        $vdf = Join-Path $root 'steamapps/libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf -PathType Leaf) {
            $text = Get-Content -LiteralPath $vdf -Raw -Encoding UTF8
            # Current Steam path fields and legacy numbered library entries.
            foreach ($match in [regex]::Matches($text, '"(?:path|\d+)"\s+"((?:[^"\\]|\\.)*)"')) {
                $value = $match.Groups[1].Value.Replace('\\','\').Replace('\"','"')
                if ([IO.Path]::IsPathRooted($value)) { $libraries += $value }
            }
        }
    }
    @($libraries | Where-Object { Test-Path -LiteralPath $_ -PathType Container } | Sort-Object -Unique)
}
function Find-SteamInstall {
    param([string[]]$Libraries,[string]$Name,[string]$RequiredFile)
    $found = @()
    foreach ($library in $Libraries) {
        $apps = Join-Path $library 'steamapps'
        foreach ($manifest in @(Get-ChildItem -LiteralPath $apps -Filter 'appmanifest_*.acf' -File -ErrorAction SilentlyContinue)) {
            $text = Get-Content -LiteralPath $manifest.FullName -Raw -Encoding UTF8
            $title = [regex]::Match($text,'"name"\s+"([^"]+)"').Groups[1].Value
            $install = [regex]::Match($text,'"installdir"\s+"([^"]+)"').Groups[1].Value
            if ($title -notmatch $Name -or !$install) { continue }
            $common = [IO.Path]::GetFullPath((Join-Path $apps 'common'))
            $folder = [IO.Path]::GetFullPath((Join-Path $common $install))
            if (!$folder.StartsWith($common + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { continue }
            if (Test-Path -LiteralPath (Join-Path $folder $RequiredFile) -PathType Leaf) { $found += $folder }
        }
    }
    @($found | Sort-Object -Unique)
}
function Resolve-Install {
    param([string]$Explicit,[string[]]$Candidates,[string]$RequiredFile,[string]$Option)
    if ($Explicit) {
        $resolved = (Resolve-Path -LiteralPath $Explicit -ErrorAction Stop).ProviderPath
        if (Test-Path -LiteralPath $resolved -PathType Leaf) {
            if ([IO.Path]::GetFileName($resolved) -ne $RequiredFile) { throw "Expected $RequiredFile for $Option" }
            $resolved = Split-Path -Parent $resolved
        }
        if (!(Test-Path -LiteralPath (Join-Path $resolved $RequiredFile) -PathType Leaf)) { throw "$Option does not contain $RequiredFile" }
        return $resolved
    }
    $unique = @($Candidates | Sort-Object -Unique)
    if ($unique.Count -eq 1) { return $unique[0] }
    if ($unique.Count -gt 1) { throw "Multiple installations found. Specify $Option. Candidates: $($unique -join '; ')" }
    throw "Installation not found. Specify $Option; no files will be moved."
}

function Resolve-GameInstall {
    param([string]$Explicit,[string[]]$Candidates,[switch]$NonInteractive,[scriptblock]$Diagnostics,
          [scriptblock]$Prompt = { Read-Host 'Paste the full path to SIGNALIS.exe (or its folder), then press Enter. Leave blank to cancel' })
    while ($true) {
        if ($Explicit) {
            $Explicit=$Explicit.Trim()
            # Windows Explorer's Copy as path wraps the path in quotes.
            if ($Explicit.Length -ge 2 -and (($Explicit.StartsWith('"') -and $Explicit.EndsWith('"')) -or ($Explicit.StartsWith("'") -and $Explicit.EndsWith("'")))) {
                $Explicit=$Explicit.Substring(1,$Explicit.Length-2)
            }
        }
        try { return Resolve-Install -Explicit $Explicit -Candidates $Candidates -RequiredFile 'SIGNALIS.exe' -Option '-GamePath' }
        catch {
            if ($NonInteractive) { throw }
            Write-Host "Could not locate a usable SIGNALIS installation: $($_.Exception.Message)" -ForegroundColor Yellow
            if($Explicit -and $Diagnostics){
                $reason=$_.Exception.Message
                try{
                    $report=& $Diagnostics $Explicit $reason
                    $recovered=@($report.RecoveredGamePaths)
                    if($recovered.Count -eq 1){return Resolve-Install -Candidates $recovered -RequiredFile 'SIGNALIS.exe' -Option '-GamePath'}
                }catch{Write-Host "Diagnostic check could not finish: $($_.Exception.Message). Try Diagnose Launcher.cmd or re-extract the ZIP." -ForegroundColor Yellow}
            }
            Write-Host 'In Steam: right-click SIGNALIS > Manage > Browse local files. Right-click SIGNALIS.exe and choose Copy as path. Paste it here.'
            $Explicit = & $Prompt
            if ([string]::IsNullOrWhiteSpace($Explicit)) { throw 'Launch cancelled. No game was started.' }
            $Candidates=@()
        }
    }
}


