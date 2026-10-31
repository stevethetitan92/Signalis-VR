param(
 [ValidateSet('VirtualDesktop','SteamVR')][string]$Runtime='SteamVR',
 [string]$GamePath,[string]$SteamVRPath,[string]$VirtualDesktopPath,
 [ValidateSet(0,72,80,90,120)][int]$RefreshRate=0,
 [switch]$ValidateOnly
)
$ErrorActionPreference='Stop'
$transcribing=$false
$previous=@{}
foreach($key in @('SIGNALIS_VR_RUNTIME','SIGNALIS_VR_REFRESH_HZ','XR_RUNTIME_JSON','VR_OVERRIDE')){$previous[$key]=[Environment]::GetEnvironmentVariable($key,'Process')}
try {
 . (Join-Path $PSScriptRoot 'Install-Discovery.ps1')
 . (Join-Path $PSScriptRoot 'Launcher-Diagnostics.ps1')
 if (!$ValidateOnly) {
  $logs=Join-Path $PSScriptRoot 'Logs'
  $null=New-Item -ItemType Directory -Force -Path $logs
  $log=Join-Path $logs ('SIGNALIS-launch-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')+'-'+$PID+'.log')
  $null=Start-Transcript -LiteralPath $log
  $transcribing=$true
  Write-Host "Launcher log: $log"
 }
 $roots=@()
 foreach($key in @('HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam')) {
  $entry=Get-ItemProperty -LiteralPath $key -ErrorAction SilentlyContinue
  if($entry){if($entry.SteamPath){$roots+=$entry.SteamPath};if($entry.InstallPath){$roots+=$entry.InstallPath}}
 }
 if(${env:ProgramFiles(x86)}){$roots+=Join-Path ${env:ProgramFiles(x86)} 'Steam'}
 $libraries=@(Get-SteamLibraries -Roots $roots)
 $candidates=@(Find-SteamInstall -Libraries $libraries -Name '^SIGNALIS$' -RequiredFile 'SIGNALIS.exe')
 if(!$GamePath -and (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'SIGNALIS.exe') -PathType Leaf)){$GamePath=$PSScriptRoot}
 $game=Resolve-GameInstall -Explicit $GamePath -Candidates $candidates -NonInteractive:$ValidateOnly -Diagnostics {
  param($failedPath,$reason)
  Invoke-SignalisLauncherDiagnostics -LauncherRoot $PSScriptRoot -Runtime $Runtime -AttemptedPath $failedPath -SteamLibraries $libraries -DiscoveryError $reason -SteamVRPath $SteamVRPath -VirtualDesktopPath $VirtualDesktopPath
 }
 $steamVR=$null
 if($Runtime -eq 'SteamVR'){$steamVR=Resolve-Install -Explicit $SteamVRPath -Candidates @(Find-SteamInstall -Libraries $libraries -Name '^SteamVR$' -RequiredFile 'bin/win64/vrstartup.exe') -RequiredFile 'bin/win64/vrstartup.exe' -Option '-SteamVRPath'}
 $exe=Join-Path $game 'SIGNALIS.exe'
 foreach($relative in @('version.dll','MelonLoader/MelonLoader.dll','Mods/SignalisVrTracking.dll')) {
  if(!(Test-Path -LiteralPath (Join-Path $game $relative) -PathType Leaf)){throw "Missing $relative. Extract the full SIGNALIS VR package into the folder containing SIGNALIS.exe first."}
 }
 $pins=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Launch-Dependencies.json') -Raw | ConvertFrom-Json
 foreach($pin in $pins){
  if($pin.runtime -and $pin.runtime -ne $Runtime){continue}
  $path=Join-Path $game $pin.file
  if(!(Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing required mod dependency: $path"}
  if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $pin.sha256){throw "Mismatched mod dependency: $path. Restore the matching package files."}
 }
 $streamer=$null
 if($Runtime -eq 'VirtualDesktop') {
  $vd=@()
  if($VirtualDesktopPath){$vd+= $VirtualDesktopPath.Trim().Trim('"')}
  else {
   foreach($root in @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall','HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall','HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall')) {
    foreach($key in @(Get-ChildItem -LiteralPath $root -ErrorAction SilentlyContinue)){
     $item=Get-ItemProperty -LiteralPath $key.PSPath -ErrorAction SilentlyContinue
     if($item.DisplayName -like '*Virtual Desktop Streamer*' -and $item.InstallLocation){$vd+=$item.InstallLocation}
    }
   }
   if($env:ProgramFiles){$vd+=Join-Path $env:ProgramFiles 'Virtual Desktop Streamer'}
  }
  $vd=@($vd | ForEach-Object {if(Test-Path -LiteralPath $_ -PathType Leaf){$_}else{Join-Path $_ 'VirtualDesktop.Streamer.exe'}} | Where-Object {Test-Path -LiteralPath $_ -PathType Leaf} | Sort-Object -Unique)
  if($vd.Count -ne 1){throw 'Virtual Desktop Streamer was not uniquely located. Specify -VirtualDesktopPath with its installation folder or VirtualDesktop.Streamer.exe.'}
  $streamer=$vd[0]
  $manifest=Join-Path (Split-Path -Parent $streamer) 'OpenXR/virtualdesktop-openxr.json'
  if(!(Test-Path -LiteralPath $manifest -PathType Leaf)){throw "VDXR runtime manifest missing: $manifest. Install or update Virtual Desktop Streamer."}
  $manifest=(Resolve-Path -LiteralPath $manifest).ProviderPath
  $json=Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
  if(!$json.runtime.library_path){throw 'Invalid VDXR manifest: runtime.library_path is missing.'}
  $runtimeLibrary=$json.runtime.library_path
  if(![IO.Path]::IsPathRooted($runtimeLibrary)){$runtimeLibrary=Join-Path (Split-Path -Parent $manifest) $runtimeLibrary}
  if(!(Test-Path -LiteralPath $runtimeLibrary -PathType Leaf)){throw "VDXR runtime DLL missing: $runtimeLibrary"}
 }
 Write-Host "Game: $exe"
 Write-Host "Connection: $Runtime"
 if($steamVR){Write-Host "Runtime: SteamVR / OpenVR ($steamVR). Virtual Desktop is not required."}
 if($streamer){Write-Host "Runtime: Virtual Desktop / VDXR through OpenComposite. SteamVR is not required.";Write-Host "VDXR manifest: $manifest"}
 if($ValidateOnly){Write-Host 'PASS: paths and pinned dependencies validated. Nothing launched or installed.';exit 0}
 if(Get-Process -Name SIGNALIS -ErrorAction SilentlyContinue){throw 'Close SIGNALIS before launching another session.'}
 if(!(Get-Process -Name steam -ErrorAction SilentlyContinue)){throw 'Open Steam and sign in before launching SIGNALIS VR.'}
 if($streamer -and !(Get-Process -Name VirtualDesktop.Streamer -ErrorAction SilentlyContinue)){
  Start-Process -FilePath $streamer -WorkingDirectory (Split-Path -Parent $streamer) -WindowStyle Hidden
 }
 if($streamer){Write-Host 'Connect your headset to Virtual Desktop Streamer before starting the game.'}
 if($steamVR -and !(Get-Process -Name vrserver -ErrorAction SilentlyContinue)){
  Start-Process -FilePath (Join-Path $steamVR 'bin/win64/vrstartup.exe') -WorkingDirectory $steamVR -WindowStyle Hidden
  Write-Host 'SteamVR started. Waiting for its server...'
  $deadline=[DateTime]::UtcNow.AddSeconds(45)
  while(!(Get-Process -Name vrserver -ErrorAction SilentlyContinue)){
   if([DateTime]::UtcNow -ge $deadline){throw 'SteamVR did not start within 45 seconds. Open SteamVR, connect the headset, then retry.'}
   Start-Sleep -Milliseconds 250
  }
 }
 $env:SIGNALIS_VR_RUNTIME=$Runtime
 $env:SIGNALIS_VR_REFRESH_HZ=if($RefreshRate){[string]$RefreshRate}else{$null}
 if($Runtime -eq 'VirtualDesktop'){$env:XR_RUNTIME_JSON=$manifest;$env:VR_OVERRIDE=$null}
 else{$env:XR_RUNTIME_JSON=$null;$env:VR_OVERRIDE=$steamVR}
 $process=Start-Process -FilePath $exe -WorkingDirectory $game -PassThru
 Write-Host "SIGNALIS process started: $($process.Id). This does not confirm headset readiness."
 Write-Host "Game/mod log: $(Join-Path $game 'MelonLoader/Latest.log')"
 Write-Host 'First-time MelonLoader assembly generation may take longer. F10 retries VR; F7 recenters; F12 stops VR.'
}catch{
 Write-Host $_ -ForegroundColor Red
 if(!$ValidateOnly -and $_.Exception.Message -notlike '*cancelled*' -and (Get-Command Invoke-SignalisLauncherDiagnostics -ErrorAction SilentlyContinue)){
  try{$null=Invoke-SignalisLauncherDiagnostics -LauncherRoot $PSScriptRoot -Runtime $Runtime -AttemptedPath $GamePath -SteamLibraries $libraries -DiscoveryError $_.Exception.Message -SteamVRPath $SteamVRPath -VirtualDesktopPath $VirtualDesktopPath}catch{Write-Host "Diagnostics could not finish: $($_.Exception.Message). Re-extract the complete package and try Diagnose Launcher.cmd." -ForegroundColor Yellow}
 }
 if(!$ValidateOnly){$null=Read-Host 'Press Enter to close'}
 exit 1
}finally{
 foreach($key in $previous.Keys){[Environment]::SetEnvironmentVariable($key,$previous[$key],'Process')}
 if($transcribing){$null=Stop-Transcript}
}

