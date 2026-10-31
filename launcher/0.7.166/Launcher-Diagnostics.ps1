param([switch]$Run,[ValidateSet('SteamVR','VirtualDesktop','Both')][string]$CheckRuntime='Both',[string]$CheckGamePath)

function Get-LauncherNativeExports {
    param([string]$Path,[string[]]$Exports)
    if(!('SignalisLauncher.NativeProbe' -as [type])){
        Add-Type -TypeDefinition @'
using System;using System.Runtime.InteropServices;
namespace SignalisLauncher {public static class NativeProbe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]public static extern IntPtr LoadLibraryExW(string path,IntPtr file,uint flags);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi,ExactSpelling=true)]public static extern IntPtr GetProcAddress(IntPtr module,string name);
 [DllImport("kernel32.dll")]public static extern bool FreeLibrary(IntPtr module);
}}
'@
    }
    $module=[SignalisLauncher.NativeProbe]::LoadLibraryExW($Path,[IntPtr]::Zero,0x1100)
    if($module -eq [IntPtr]::Zero){$code=[Runtime.InteropServices.Marshal]::GetLastWin32Error();throw "Windows could not load the DLL (error $code): $([ComponentModel.Win32Exception]::new($code).Message). A missing x64 native dependency, blocked DLL or incomplete download may be the cause."}
    try{foreach($entry in $Exports){if([SignalisLauncher.NativeProbe]::GetProcAddress($module,$entry) -eq [IntPtr]::Zero){throw "Required export missing: $entry"}}}
    finally{$null=[SignalisLauncher.NativeProbe]::FreeLibrary($module)}
}

function Invoke-SignalisLauncherDiagnostics {
    param([string]$LauncherRoot,[ValidateSet('SteamVR','VirtualDesktop','Both')][string]$Runtime='Both',
          [string]$AttemptedPath,[string[]]$SteamLibraries,[string]$DiscoveryError,[string]$SteamVRPath,[string]$VirtualDesktopPath,[switch]$NoSave)
    $checks=New-Object System.Collections.Generic.List[object]
    $recovered=New-Object System.Collections.Generic.List[string]
    $manualResolved=$null
    function Record($state,$area,$detail){$checks.Add([pscustomobject]@{state=$state;area=$area;detail=$detail});Write-Host "[$state] $area : $detail"}
    Write-Host ''
    Write-Host 'QUICK CHECK 1 - Finding SIGNALIS' -ForegroundColor Cyan
    if($DiscoveryError){Record 'INFO' 'Search result' $DiscoveryError}
    $normalized=$AttemptedPath
    if($normalized){
        $normalized=$normalized.Trim()
        if($normalized.Length -ge 2 -and (($normalized[0] -eq '"' -and $normalized[$normalized.Length-1] -eq '"') -or ($normalized[0] -eq "'" -and $normalized[$normalized.Length-1] -eq "'"))){$normalized=$normalized.Substring(1,$normalized.Length-2)}
        try{
            if(!(Test-Path -LiteralPath $normalized)){Record 'FAIL' 'Pasted path' "Path does not exist or is inaccessible: $normalized. Check the drive is connected; use Steam > Manage > Browse local files > SIGNALIS.exe > Copy as path."}
            elseif(Test-Path -LiteralPath $normalized -PathType Leaf){
                if([IO.Path]::GetFileName($normalized) -ieq 'SIGNALIS.exe'){$manualResolved=Split-Path -Parent (Resolve-Path -LiteralPath $normalized).ProviderPath;$recovered.Add($manualResolved);Record 'PASS' 'Pasted path' 'SIGNALIS.exe found.'}
                elseif([IO.Path]::GetExtension($normalized) -eq '.zip'){Record 'FAIL' 'Pasted path' 'This is a ZIP download. Extract it first. Paste the installed game SIGNALIS.exe path, not the ZIP.'}
                else{Record 'FAIL' 'Pasted path' "Wrong file: $([IO.Path]::GetFileName($normalized)). Select SIGNALIS.exe itself, not a shortcut, DLL or Steam URL."}
            }else{
                $exe=Join-Path $normalized 'SIGNALIS.exe'
                if(Test-Path -LiteralPath $exe -PathType Leaf){$manualResolved=(Resolve-Path -LiteralPath $normalized).ProviderPath;$recovered.Add($manualResolved);Record 'PASS' 'Pasted folder' 'SIGNALIS.exe is in this folder.'}
                else{
                    Record 'FAIL' 'Pasted folder' "This folder does not contain SIGNALIS.exe: $normalized. Choose the game folder, not SIGNALIS_Data or the extracted mod folder."
                    $parent=Split-Path -Parent $normalized
                    if($parent -and (Test-Path -LiteralPath (Join-Path $parent 'SIGNALIS.exe') -PathType Leaf)){$manualResolved=(Resolve-Path -LiteralPath $parent).ProviderPath;$recovered.Add($manualResolved);Record 'PASS' 'Nearby game' "Game found in parent folder: $parent"}
                }
            }
        }catch{Record 'FAIL' 'Pasted path access' $_.Exception.Message}
    }else{Record 'INFO' 'Pasted path' 'No manual path supplied yet.'}
    if(!$SteamLibraries){
        $roots=@()
        foreach($key in @('HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam')){
            $entry=Get-ItemProperty -LiteralPath $key -ErrorAction SilentlyContinue
            if($entry){if($entry.SteamPath){$roots+=$entry.SteamPath};if($entry.InstallPath){$roots+=$entry.InstallPath}}
        }
        if(${env:ProgramFiles(x86)}){$roots+=Join-Path ${env:ProgramFiles(x86)} 'Steam'}
        if(Get-Command Get-SteamLibraries -ErrorAction SilentlyContinue){$SteamLibraries=@(Get-SteamLibraries -Roots $roots)}else{$SteamLibraries=$roots}
    }
    foreach($library in @($SteamLibraries|Sort-Object -Unique)){
        $apps=Join-Path $library 'steamapps'
        if(!(Test-Path -LiteralPath $apps -PathType Container)){Record 'WARN' 'Steam library' "Not available: $apps";continue}
        $manifest=Join-Path $apps 'appmanifest_1262350.acf'
        if(!(Test-Path -LiteralPath $manifest -PathType Leaf)){Record 'INFO' 'Steam library' "No SIGNALIS install manifest in $library";continue}
        try{
            $text=Get-Content -LiteralPath $manifest -Raw -ErrorAction Stop
            $install=[regex]::Match($text,'"installdir"\s+"([^"]+)"').Groups[1].Value
            if(!$install){Record 'FAIL' 'Steam manifest' "Missing installdir in $manifest";continue}
            $common=[IO.Path]::GetFullPath((Join-Path $apps 'common'))
            $folder=[IO.Path]::GetFullPath((Join-Path $common $install))
            if(!$folder.StartsWith($common+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){Record 'FAIL' 'Steam manifest' 'Install directory escapes the Steam library; entry ignored.';continue}
            if(Test-Path -LiteralPath (Join-Path $folder 'SIGNALIS.exe') -PathType Leaf){$recovered.Add($folder);Record 'PASS' 'Steam game' "Found SIGNALIS.exe: $folder"}
            else{Record 'FAIL' 'Steam game' "Steam records $folder, but SIGNALIS.exe is missing or inaccessible. Check the library drive or Steam > Properties > Installed Files > Verify integrity."}
        }catch{Record 'FAIL' 'Steam manifest access' $_.Exception.Message}
    }
    $recoveredPaths=@($recovered|Sort-Object -Unique)
    if($manualResolved){$recoveredPaths=@($manualResolved)}
    if($recoveredPaths.Count -eq 0){Record 'FAIL' 'Game discovery' 'No verified game location found. The launcher will not guess or launch a different file. Paste SIGNALIS.exe from Steam Browse local files.'}
    elseif($recoveredPaths.Count -gt 1){Record 'WARN' 'Game discovery' 'Several installations found. Paste the path to the one you want.'}
    Write-Host ''
    Write-Host 'QUICK CHECK 2 - Launcher and VR bridge health' -ForegroundColor Cyan
    Record 'INFO' 'Computer' "PowerShell $($PSVersionTable.PSVersion); launcher process $([IntPtr]::Size*8)-bit; selected connection $Runtime"
    if([IntPtr]::Size -ne 8){Record 'FAIL' 'Architecture' 'Use 64-bit Windows PowerShell. The game and VR bridge are 64-bit.'}
    foreach($name in @('Launch VR.ps1','Install-Discovery.ps1','Launcher-Diagnostics.ps1','Launch SteamVR.cmd','Launch VirtualDesktop.cmd','Diagnose Launcher.cmd','Launch-Dependencies.json','Launch-SelfCheck.json')){
        $path=Join-Path $LauncherRoot $name
        if(!(Test-Path -LiteralPath $path -PathType Leaf)){Record 'FAIL' 'Launcher file' "Missing $name. Re-extract the complete ZIP; keep launcher files together.";continue}
        if($name.EndsWith('.ps1')){
            $tokens=$null;$parseErrors=$null;$null=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$parseErrors)
            if($parseErrors.Count){Record 'FAIL' 'Script syntax' "$name : $($parseErrors[0].Message)"}else{Record 'PASS' 'Script syntax' $name}
        }
    }
    $self=Join-Path $LauncherRoot 'Launch-SelfCheck.json'
    if(Test-Path -LiteralPath $self -PathType Leaf){
        try{$selfEntries=Get-Content -LiteralPath $self -Raw|ConvertFrom-Json;foreach($entry in $selfEntries){
            if([IO.Path]::GetFileName($entry.file) -ne $entry.file){Record 'FAIL' 'Self-check manifest' 'Unexpected non-local file entry; ignored.';continue}
            $path=Join-Path $LauncherRoot $entry.file
            if(!(Test-Path -LiteralPath $path -PathType Leaf) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256){Record 'FAIL' 'Launcher checksum' "$($entry.file) is missing or different from this package. Re-extract the matching launcher update."}
            else{Record 'PASS' 'Launcher checksum' $entry.file}
        }}catch{Record 'FAIL' 'Self-check manifest' $_.Exception.Message}
    }
    $inspect=if($recoveredPaths.Count -eq 1){$recoveredPaths[0]}else{$LauncherRoot}
    Record 'INFO' 'Mod inspection folder' $inspect
    if($inspect -eq $LauncherRoot -and $recoveredPaths.Count -ne 1){Record 'INFO' 'Inspection limit' 'No unique installed game located. Checks below cover package files only; they cannot establish the installed game is healthy.'}
    $pinsFile=Join-Path $LauncherRoot 'Launch-Dependencies.json'
    if(Test-Path -LiteralPath $pinsFile -PathType Leaf){
        try{$dependencyEntries=Get-Content -LiteralPath $pinsFile -Raw|ConvertFrom-Json;foreach($pin in $dependencyEntries){
            if($Runtime -ne 'Both' -and $pin.runtime -and $pin.runtime -ne $Runtime){continue}
            $relative=$pin.file.Replace('/',[IO.Path]::DirectorySeparatorChar)
            $path=[IO.Path]::GetFullPath((Join-Path $inspect $relative))
            if(!$path.StartsWith([IO.Path]::GetFullPath($inspect).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){Record 'FAIL' 'Dependency manifest' 'Unexpected path outside mod folder; ignored.';continue}
            if(!(Test-Path -LiteralPath $path -PathType Leaf)){Record 'FAIL' 'Mod dependency' "Missing $($pin.file). Copy the full mod package into the folder containing SIGNALIS.exe.";continue}
            if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $pin.sha256){Record 'FAIL' 'Mod dependency checksum' "Different/mixed version: $($pin.file). Restore matching package files; your saves/settings are not changed.";continue}
            Record 'PASS' 'Mod dependency checksum' $pin.file
            if($pin.file -like 'UserLibs/*' -and [IntPtr]::Size -eq 8){
                try{
                    $exports=if($pin.file -like '*SignalisVrRenderBridge.dll'){@('ConfigureBridge','GetRenderEventFunc','QueuePoseWithFrame','QueueFrameWithFrame','TryDetach')}else{@('VR_InitInternal','VR_GetGenericInterface','VR_ShutdownInternal')}
                    Get-LauncherNativeExports -Path $path -Exports $exports
                    Record 'PASS' 'Native DLL load/exports' "$($pin.file): loaded and required functions present. No VR session or game initialized."
                }catch{Record 'FAIL' 'Native DLL load/exports' "$($pin.file): $($_.Exception.Message)"}
            }
        }}catch{Record 'FAIL' 'Dependency inspection' $_.Exception.Message}
    }
    foreach($relative in @('version.dll','MelonLoader/MelonLoader.dll')){
        if(Test-Path -LiteralPath (Join-Path $inspect $relative) -PathType Leaf){Record 'PASS' 'Mod loader file' $relative}
        else{Record 'FAIL' 'Mod loader file' "Missing $relative. A launcher update alone cannot install the mod; extract the FULL package into the game folder first."}
    }
    Write-Host ''
    Write-Host 'QUICK CHECK 3 - Selected connection software' -ForegroundColor Cyan
    if($Runtime -in @('SteamVR','Both')){
        $vr=@()
        if($SteamVRPath -and (Test-Path -LiteralPath (Join-Path $SteamVRPath 'bin/win64/vrstartup.exe') -PathType Leaf)){$vr+=$SteamVRPath}
        elseif(!$SteamVRPath -and (Get-Command Find-SteamInstall -ErrorAction SilentlyContinue)){$vr=@(Find-SteamInstall -Libraries $SteamLibraries -Name '^SteamVR$' -RequiredFile 'bin/win64/vrstartup.exe')}
        if($vr.Count -eq 1){Record 'PASS' 'SteamVR software' $vr[0]}
        else{$state=if($Runtime -eq 'Both'){'WARN'}else{'FAIL'};Record $state 'SteamVR software' 'SteamVR not uniquely located. Install SteamVR from Steam, or specify -SteamVRPath. Virtual Desktop is not needed for this launcher.'}
    }
    if($Runtime -in @('VirtualDesktop','Both')){
        $vd=@()
        if($VirtualDesktopPath){$vd+=$VirtualDesktopPath.Trim().Trim('"')}
        else{
            foreach($key in @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall','HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall','HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall')){
                foreach($child in @(Get-ChildItem -LiteralPath $key -ErrorAction SilentlyContinue)){$app=Get-ItemProperty -LiteralPath $child.PSPath -ErrorAction SilentlyContinue;if($app.DisplayName -like '*Virtual Desktop Streamer*' -and $app.InstallLocation){$vd+=$app.InstallLocation}}
            }
            if($env:ProgramFiles){$vd+=Join-Path $env:ProgramFiles 'Virtual Desktop Streamer'}
        }
        $streamers=@($vd|ForEach-Object {if(Test-Path -LiteralPath $_ -PathType Leaf){$_}else{Join-Path $_ 'VirtualDesktop.Streamer.exe'}}|Where-Object {Test-Path -LiteralPath $_ -PathType Leaf}|Sort-Object -Unique)
        if($streamers.Count -eq 1){
            Record 'PASS' 'Virtual Desktop Streamer' $streamers[0]
            $manifest=Join-Path (Split-Path -Parent $streamers[0]) 'OpenXR/virtualdesktop-openxr.json'
            try{
                $json=Get-Content -LiteralPath $manifest -Raw -ErrorAction Stop|ConvertFrom-Json
                $lib=$json.runtime.library_path;if(!$lib){throw 'runtime.library_path is missing'}
                if(![IO.Path]::IsPathRooted($lib)){$lib=Join-Path (Split-Path -Parent $manifest) $lib}
                if(!(Test-Path -LiteralPath $lib -PathType Leaf)){throw "Runtime DLL missing: $lib"}
                Record 'PASS' 'VDXR runtime' $manifest
            }catch{Record 'FAIL' 'VDXR runtime' "Missing/incomplete Virtual Desktop OpenXR runtime: $($_.Exception.Message). Update Virtual Desktop Streamer. SteamVR is not required."}
        }else{$state=if($Runtime -eq 'Both'){'WARN'}else{'FAIL'};Record $state 'Virtual Desktop software' 'Virtual Desktop Streamer not uniquely located. Install it or specify -VirtualDesktopPath. SteamVR is not needed for this launcher.'}
    }
    Write-Host ''
    $failureCount=@($checks|Where-Object state -eq 'FAIL').Count
    if($failureCount){Write-Host "Found $failureCount issue(s). Follow the messages above. No files were installed or repaired automatically." -ForegroundColor Yellow}
    else{Write-Host 'Offline launcher/bridge checks passed. Headset rendering and game startup have not been tested.' -ForegroundColor Green}
    $reportPath=$null
    if(!$NoSave){
        try{
            $logs=Join-Path $LauncherRoot 'Logs';$null=New-Item -ItemType Directory -Path $logs -Force
            $reportPath=Join-Path $logs ('SIGNALIS-diagnostic-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')+'-'+$PID+'.txt')
            @("SIGNALIS launcher diagnostic - UTC $([DateTime]::UtcNow.ToString('o'))","Connection: $Runtime","No game/VR session launched by diagnostics.",'')+@($checks|ForEach-Object {"[$($_.state)] $($_.area): $($_.detail)"}) | Set-Content -LiteralPath $reportPath -Encoding UTF8
            Write-Host "Diagnostic report: $reportPath"
        }catch{Write-Host "Could not save diagnostic report: $($_.Exception.Message). Copy this command window's output." -ForegroundColor Yellow}
    }
    return [pscustomobject]@{Failed=$failureCount;Checks=$checks.ToArray();RecoveredGamePaths=$recoveredPaths;ReportPath=$reportPath}
}

if($Run){
    $ErrorActionPreference='Stop'
    $helper=Join-Path $PSScriptRoot 'Install-Discovery.ps1'
    if(Test-Path -LiteralPath $helper -PathType Leaf){try{. $helper}catch{Write-Host "Discovery helper could not load: $($_.Exception.Message)"}}
    $result=Invoke-SignalisLauncherDiagnostics -LauncherRoot $PSScriptRoot -Runtime $CheckRuntime -AttemptedPath $CheckGamePath
    $null=Read-Host 'Press Enter to close'
    if($result.Failed){exit 1}else{exit 0}
}

