function Resolve-HealthPath {
 param([string]$Root,[string]$Relative)
 if(!$Relative -or [IO.Path]::IsPathRooted($Relative)){throw 'Expected a relative package path.'}
 $base=[IO.Path]::GetFullPath($Root).TrimEnd('\','/')
 $path=[IO.Path]::GetFullPath((Join-Path $base $Relative))
 if(!$path.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Path escapes its allowed folder.'}
 $cursor=$path
 while($cursor){
  if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Linked folders/files are not repaired: $cursor"}}
  if($cursor -eq $base){break};$cursor=Split-Path -Parent $cursor
 }
 return $path
}

function Backup-HealthSaves {
 param([string]$GameRoot,[string]$BackupRoot)
 $save=Resolve-HealthPath $GameRoot 'SIGNALIS_Data/save'
 if(!(Test-Path -LiteralPath $save -PathType Container)){return $null}
 foreach($item in Get-ChildItem -LiteralPath $save -Recurse -Force){if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked save entries cannot be backed up safely; repair stopped.'}}
 $dest=Join-Path $BackupRoot 'Saves'
 $null=New-Item -ItemType Directory -Path $dest -Force
 $entries=@(foreach($file in Get-ChildItem -LiteralPath $save -Recurse -File -Force){
  $relative=$file.FullName.Substring($save.Length+1)
  $source=Resolve-HealthPath $save $relative
  $target=Resolve-HealthPath $dest $relative
  $null=New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force
  $hash=(Get-FileHash -LiteralPath $source).Hash
  Copy-Item -LiteralPath $source -Destination $target -ErrorAction Stop
  if((Get-FileHash -LiteralPath $target).Hash -ne $hash -or (Get-FileHash -LiteralPath $source).Hash -ne $hash){throw 'Save changed or backup verification failed; no repairs will proceed.'}
  [pscustomobject]@{file=$relative.Replace('\','/');sha256=$hash;bytes=$file.Length}
 })
 $record=[pscustomobject]@{format=1;gameRoot=[IO.Path]::GetFullPath($GameRoot);files=$entries;gameValidated=$false;createdUtc=[DateTime]::UtcNow.ToString('o')}
 $record|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $BackupRoot 'Save-Backup.json') -Encoding UTF8
 return (Join-Path $BackupRoot 'Save-Backup.json')
}

function Restore-HealthSaves {
 param([string]$GameRoot,[string]$BackupFolder,[string]$BeforeRestoreFolder)
 if(Get-Process -Name SIGNALIS -ErrorAction SilentlyContinue){throw 'Close SIGNALIS before restoring saves.'}
 $record=Get-Content -LiteralPath (Join-Path $BackupFolder 'Save-Backup.json') -Raw|ConvertFrom-Json
 if($record.format -ne 1 -or !$record.files -or $record.gameRoot -ne [IO.Path]::GetFullPath($GameRoot)){throw 'Backup does not belong to this game location or is empty/invalid.'}
 $save=Resolve-HealthPath $GameRoot 'SIGNALIS_Data/save'
 $validated=@(foreach($entry in $record.files){
  $source=Resolve-HealthPath (Join-Path $BackupFolder 'Saves') $entry.file
  $target=Resolve-HealthPath $save $entry.file
  if((Get-FileHash -LiteralPath $source).Hash -ne $entry.sha256){throw 'Backup checksum mismatch; restoration blocked.'}
  [pscustomobject]@{source=$source;target=$target;hash=$entry.sha256;file=$entry.file;existed=(Test-Path -LiteralPath $target -PathType Leaf)}
 })
 $null=Backup-HealthSaves -GameRoot $GameRoot -BackupRoot $BeforeRestoreFolder
 # Never delete current slots; restore only the selected snapshot's files.
 $touched=New-Object System.Collections.Generic.List[object]
 try{
  foreach($entry in $validated){$null=Resolve-HealthPath $save $entry.file;$null=New-Item -ItemType Directory -Force -Path (Split-Path -Parent $entry.target);$touched.Add($entry);Copy-Item -LiteralPath $entry.source -Destination $entry.target -ErrorAction Stop;if((Get-FileHash -LiteralPath $entry.target).Hash -ne $entry.hash){throw 'Restore verification failed.'}}
 }catch{
  foreach($entry in $touched){if($entry.existed){Copy-Item -LiteralPath (Resolve-HealthPath (Join-Path $BeforeRestoreFolder 'Saves') $entry.file) -Destination $entry.target -ErrorAction Stop}else{Remove-Item -LiteralPath $entry.target -ErrorAction Stop}}
  throw "Save restore failed; original snapshot files restored. Current-save backup retained: $BeforeRestoreFolder"
 }
 Write-Host 'RESTORED selected backup files. Other slots retained. This does not prove the save loads; Steam Cloud can resync saves.'
}

function Invoke-BundleHealth {
 param([string]$PayloadRoot,[string]$GameRoot,[switch]$Repair,[string]$SaveBackupRoot)
 $ErrorActionPreference='Stop'
 $checks=New-Object System.Collections.Generic.List[object]
 function HealthRecord($state,$area,$detail){$checks.Add([pscustomobject]@{state=$state;area=$area;detail=$detail});Write-Host "[$state] $area : $detail"}
 $game=[IO.Path]::GetFullPath($GameRoot)
 if(!(Test-Path -LiteralPath (Join-Path $game 'SIGNALIS.exe') -PathType Leaf)){throw 'Select the folder containing SIGNALIS.exe.'}
 $stamp=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')+'-'+$PID+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6)
 $backup=if($SaveBackupRoot){Resolve-HealthPath $SaveBackupRoot $stamp}else{Resolve-HealthPath $PayloadRoot ('Backups/'+$stamp)}
 $running=@(Get-Process -Name SIGNALIS -ErrorAction SilentlyContinue).Count -gt 0
 if($Repair -and $running){throw 'SIGNALIS is running. Close it before repair or save backup.'}
 foreach($relative in @('SIGNALIS.exe','GameAssembly.dll','UnityPlayer.dll','SIGNALIS_Data/il2cpp_data/Metadata/global-metadata.dat')){
  $path=Resolve-HealthPath $game $relative
  if(!(Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0){HealthRecord 'FAIL' 'Base game' "Missing/empty $relative. Use Steam > Properties > Installed Files > Verify integrity. Mod repair cannot replace proprietary game files."}
 }
 HealthRecord 'LIMIT' 'Base game integrity' 'Presence checks are not full game verification. Steam verifies original game data; this launcher has no official game checksums.'
 $manifest=Get-Content -LiteralPath (Join-Path $PayloadRoot 'Repair-Manifest.json') -Raw|ConvertFrom-Json
 if(!$manifest){throw 'Repair manifest is empty.'}
 $pending=@();$seen=@{}
 foreach($entry in $manifest){
  $relative=[string]$entry.file
  if($relative -notmatch '^(version\.dll|(Mods|Plugins|MelonLoader|BepInEx|UserLibs)/.+)$' -or $relative -match '(^|/)(Logs?|Backups?|save|Saves|UserData)(/|$)' -or $entry.mode -notin @($null,'replace','missingOnly')){throw "Non-repairable manifest entry: $relative"}
  if($seen.ContainsKey($relative.ToLowerInvariant())){throw 'Duplicate repair manifest entry.'};$seen[$relative.ToLowerInvariant()]=$true
  $source=Resolve-HealthPath $PayloadRoot $relative
  $target=Resolve-HealthPath $game $relative
  if(!(Test-Path -LiteralPath $source -PathType Leaf) -or (Get-FileHash -LiteralPath $source).Hash -ne $entry.sha256){HealthRecord 'FAIL' 'Download/package' "Missing or changed repair source: $relative. Re-extract a fresh download. This file will not be copied.";continue}
  $exists=Test-Path -LiteralPath $target -PathType Leaf
  if($exists -and $entry.mode -eq 'missingOnly'){HealthRecord 'PRESERVED' 'Configuration' $relative;continue}
  if(!$exists -or (Get-FileHash -LiteralPath $target).Hash -ne $entry.sha256){
   HealthRecord 'FAIL' 'Mod files' "Missing or different from this build: $relative. A different checksum may also mean another mod version."
   if(![StringComparer]::OrdinalIgnoreCase.Equals($source,$target)){$pending+=[pscustomobject]@{file=$relative;source=$source;target=$target;sha256=$entry.sha256;existed=$exists;originalHash=if($exists){(Get-FileHash -LiteralPath $target).Hash}else{$null}}}
  }
 }
 $packageFaults=@($checks|Where-Object {$_.state -eq 'FAIL' -and $_.area -in @('Base game','Download/package')}).Count
 if($Repair -and $packageFaults){HealthRecord 'WARN' 'Installation/update' 'Base-game or bundled-source errors prevent installation/update. Resolve the errors above first.'}
 if($Repair -and !$packageFaults -and $pending.Count -gt 0){
  $null=New-Item -ItemType Directory -Force -Path $backup
  $saveRecord=Backup-HealthSaves -GameRoot $game -BackupRoot $backup
  if($saveRecord){HealthRecord 'BACKUP' 'Saves' "Copied and checksum-verified existing save files: $saveRecord. Backup integrity is not proof that the game can load them."}
  foreach($entry in $pending){if($entry.existed){$copy=Resolve-HealthPath (Join-Path $backup 'OriginalFiles') $entry.file;$null=New-Item -ItemType Directory -Force -Path (Split-Path -Parent $copy);Copy-Item -LiteralPath $entry.target -Destination $copy;if((Get-FileHash -LiteralPath $copy).Hash -ne $entry.originalHash){throw 'Original mod backup failed; no repairs started.'}}}
  $pending|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $backup 'Repair-Plan.json')
  $touched=New-Object System.Collections.Generic.List[object]
  try{
   foreach($entry in $pending){
    # Recheck source and destination after backup, before any replacement.
    if((Get-FileHash -LiteralPath $entry.source).Hash -ne $entry.sha256){throw 'Repair source changed during operation.'}
    $null=Resolve-HealthPath $game $entry.file
    $null=New-Item -ItemType Directory -Force -Path (Split-Path -Parent $entry.target)
    $touched.Add($entry)
    Copy-Item -LiteralPath $entry.source -Destination $entry.target -ErrorAction Stop
    if((Get-FileHash -LiteralPath $entry.target).Hash -ne $entry.sha256){throw 'Repaired file checksum mismatch.'}
    HealthRecord 'FIXED' 'Mod files' $entry.file
   }
  }catch{
   $repairError=$_.Exception.Message
   foreach($entry in $touched){try{if($entry.existed){Copy-Item -LiteralPath (Resolve-HealthPath (Join-Path $backup 'OriginalFiles') $entry.file) -Destination $entry.target -ErrorAction Stop;if((Get-FileHash -LiteralPath $entry.target).Hash -ne $entry.originalHash){throw 'Rollback checksum mismatch'}}else{Remove-Item -LiteralPath $entry.target -ErrorAction Stop}}catch{HealthRecord 'FAIL' 'Repair rollback' "$($entry.file): $($_.Exception.Message). Original backup: $backup"}}
   HealthRecord 'FAIL' 'Repair' "$repairError. Rollback attempted; inspect rollback results. Backup: $backup"
  }
 }
 $save=Resolve-HealthPath $game 'SIGNALIS_Data/save'
 if(Test-Path -LiteralPath $save -PathType Container){
  $empty=@(Get-ChildItem -LiteralPath $save -Recurse -File | Where-Object Length -eq 0)
  if($empty.Count){HealthRecord 'WARN' 'Saves' "$($empty.Count) empty file(s). This alone does not prove corruption; no save contents changed."}
 }
 $logs=@((Join-Path $game 'MelonLoader/Latest.log'),(Join-Path $env:USERPROFILE 'AppData/LocalLow/rose-engine/SIGNALIS/Player.log'))
 foreach($log in $logs){
  if(!(Test-Path -LiteralPath $log -PathType Leaf)){continue}
  $lines=@(Get-Content -LiteralPath $log -Tail 4000 -ErrorAction Stop)
  $errorLines=@($lines|Where-Object {$_ -match '(?i)exception|\[error\]|\bfatal\b|failed to|could not load'})
  $contexts=@(for($i=0;$i -lt $lines.Count;$i++){if($lines[$i] -match '(?i)exception|\[error\]|\bfatal\b|failed to|could not load'){$end=$i;for($j=$i+1;$j -le [Math]::Min($i+5,$lines.Count-1);$j++){if($lines[$j] -match '(?i)exception|\[error\]|\bfatal\b|failed to|could not load'){break};$end=$j};($lines[$i..$end] -join ' | ')}})
  foreach($category in @(@('Medicine','heal|medicine|injector|repair.?spray|repair.?patch|UseItem'),@('Graphics','shader|texture|render|graphics|d3d|compositor|OpenXR|OpenVR'),@('Saves','save|loadsaved|deserialize|SProgress|SaveManager'))){
   $matches=@($contexts|Where-Object {$_ -match $category[1]}|Select-Object -Last 5)
   foreach($line in $matches){HealthRecord 'EVIDENCE' $category[0] ($line.Substring(0,[Math]::Min(600,$line.Length))+' [log evidence; may be from an earlier session]')}
  }
  foreach($line in @($errorLines|Select-Object -Last 3)){HealthRecord 'EVIDENCE' 'Runtime error' $line.Substring(0,[Math]::Min(600,$line.Length))}
  if($errorLines.Count){HealthRecord 'WARN' 'Runtime log' "$($errorLines.Count) recent error-like lines in $log. Review full stack traces; errors are not automatically attributed to a particular symptom."}
 }
 $saveErrors=@($checks|Where-Object {$_.area -eq 'Saves' -and $_.state -eq 'EVIDENCE'})
 if($Repair -and $saveErrors.Count -gt 0 -and !(Test-Path -LiteralPath (Join-Path $backup 'Save-Backup.json'))){
  $null=New-Item -ItemType Directory -Force -Path $backup
  $saveRecord=Backup-HealthSaves -GameRoot $game -BackupRoot $backup
  if($saveRecord){HealthRecord 'BACKUP' 'Save error evidence' "Saves preserved at $saveRecord; no save content rewritten."}
 }
 $blocking=@($checks|Where-Object {$_.state -eq 'FAIL' -and $_.area -in @('Base game','Download/package','Repair','Repair rollback')}).Count
 foreach($entry in $manifest){$target=Resolve-HealthPath $game $entry.file;$exists=Test-Path -LiteralPath $target -PathType Leaf;if(!$exists -or ($entry.mode -ne 'missingOnly' -and (Get-FileHash -LiteralPath $target).Hash -ne $entry.sha256)){$blocking++}}
 HealthRecord 'SUMMARY' 'File health' "$blocking unresolved blocking file issue(s). Earlier FAIL entries describe the original findings; FIXED entries record successful repairs."
 HealthRecord 'LIMIT' 'Medicine / graphics / saves' 'Offline checks cannot reproduce healing, headset visuals or save loading. No speculative health/settings/save edits. Use the in-game diagnostic report plus Latest.log if symptoms remain. A prior backup can be restored explicitly; no invented save repair.'
 $logsRoot=Join-Path $PayloadRoot 'Logs';$null=New-Item -ItemType Directory -Force -Path $logsRoot
 $report=Join-Path $logsRoot ('Health-'+$stamp+'.json')
 [pscustomobject]@{checks=$checks.ToArray();backup=$backup;repairRequested=[bool]$Repair;gameLaunched=$false}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $report
 Write-Host "Health report: $report"
 return [pscustomobject]@{Checks=$checks.ToArray();Pending=$pending.Count;Blocking=$blocking;Backup=$backup;Report=$report}
}
