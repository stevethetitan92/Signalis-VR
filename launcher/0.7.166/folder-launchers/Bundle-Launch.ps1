param(
 [ValidateSet('SteamVR','VirtualDesktop')][string]$Runtime='SteamVR',
 [string]$GamePath,[string]$SteamVRPath,[string]$VirtualDesktopPath,
 [ValidateSet(0,72,80,90,120)][int]$RefreshRate=0,
 [switch]$ValidateOnly,[switch]$DiagnoseOnly,[switch]$NoRepair,
 [string]$RestoreSaveBackup
)
$ErrorActionPreference='Stop'
try {
 . (Join-Path $PSScriptRoot 'Install-Discovery.ps1')
 . (Join-Path $PSScriptRoot 'Launcher-Diagnostics.ps1')
 . (Join-Path $PSScriptRoot 'Bundle-Health.ps1')
 if(Get-Process -Name SIGNALIS -ErrorAction SilentlyContinue){throw 'Close SIGNALIS before launcher diagnostics/repair.'}
 $roots=@()
 foreach($key in @('HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam')){
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
 if($RestoreSaveBackup){
  if($ValidateOnly){throw 'ValidateOnly cannot restore saves.'}
  Write-Host "Selected backup: $RestoreSaveBackup"
  Write-Host 'This restores older save files, which can lose progress since that snapshot. Current saves will be backed up first. Steam Cloud may resync them. No save-format repair is performed.' -ForegroundColor Yellow
  if((Read-Host 'Type RESTORE to use this snapshot, or Enter to cancel') -cne 'RESTORE'){throw 'Save restoration cancelled.'}
  $before=Join-Path $PSScriptRoot ('Backups/BeforeRestore-'+[Guid]::NewGuid().ToString('N'))
  Restore-HealthSaves -GameRoot $game -BackupFolder $RestoreSaveBackup -BeforeRestoreFolder $before
 }
 $health=Invoke-BundleHealth -PayloadRoot $PSScriptRoot -GameRoot $game -Repair:(!$ValidateOnly -and !$NoRepair)
 $bridge=Invoke-SignalisLauncherDiagnostics -LauncherRoot $PSScriptRoot -Runtime $Runtime -AttemptedPath $game -SteamVRPath $SteamVRPath -VirtualDesktopPath $VirtualDesktopPath -NoSave:$ValidateOnly
 if($DiagnoseOnly){if($health.Blocking -or $bridge.Failed){exit 1};exit 0}
 if($health.Blocking){throw 'Unresolved file issues remain. Read the errors above. Nothing launched.'}
 if($bridge.Failed){throw 'Launcher, bridge or selected VR connection checks failed. Read the specific errors above. Nothing launched.'}
 & (Join-Path $PSScriptRoot 'Launch VR.ps1') -Runtime $Runtime -GamePath $game -SteamVRPath $SteamVRPath -VirtualDesktopPath $VirtualDesktopPath -RefreshRate $RefreshRate -ValidateOnly:$ValidateOnly
 exit $LASTEXITCODE
}catch{
 Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
 if(!$ValidateOnly){$null=Read-Host 'Press Enter to close'}
 exit 1
}
