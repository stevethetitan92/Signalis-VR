function Test-BundleSources {
 param([string]$PayloadRoot)
 try {
  $base=[IO.Path]::GetFullPath($PayloadRoot).TrimEnd('\','/')
  $entries=Get-Content -LiteralPath (Join-Path $base 'Bundle-Integrity.json') -Raw -ErrorAction Stop|ConvertFrom-Json
  if(!$entries){return $false}
  $seen=@{}
  foreach($entry in $entries){
   $relative=[string]$entry.file
   if(!$relative -or [IO.Path]::IsPathRooted($relative) -or $seen.ContainsKey($relative.ToLowerInvariant())){return $false}
   $seen[$relative.ToLowerInvariant()]=$true
   $file=[IO.Path]::GetFullPath((Join-Path $base $relative))
   if(!$file.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){return $false}
   $cursor=$file
   while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){return $false}};if($cursor -eq $base){break};$cursor=Split-Path -Parent $cursor}
   if(!(Test-Path -LiteralPath $file -PathType Leaf) -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.sha256){return $false}
  }
  foreach($required in @('Bundle-Launch.ps1','Bundle-Download.ps1','Bundle-Health.ps1','Install-Discovery.ps1','Launcher-Diagnostics.ps1','Launch VR.ps1','Repair-Manifest.json','Bundle-Version.json')){if(!$seen.ContainsKey($required.ToLowerInvariant())){return $false}}
  return $true
 }catch{return $false}
}

function Expand-VerifiedBundle {
 param([string]$Zip,[string]$Destination,[string]$Sha256)
 if((Get-FileHash -LiteralPath $Zip -Algorithm SHA256).Hash -ne $Sha256){throw 'Download SHA-256 does not match GitHub. Nothing will be executed or installed.'}
 if(Test-Path -LiteralPath $Destination){throw 'Extraction destination must be new.'}
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 $archive=[IO.Compression.ZipFile]::OpenRead($Zip)
 try {
  $base=[IO.Path]::GetFullPath($Destination).TrimEnd('\','/')
  $seen=@{};$size=[long]0
  if($archive.Entries.Count -gt 10000){throw 'Unexpectedly large package file count.'}
  foreach($entry in $archive.Entries){
   $relative=$entry.FullName.Replace('\','/')
   if($relative -notmatch '^(Launch SteamVR\.cmd|Launch VirtualDesktop\.cmd|Mod Files/.*)$' -or [IO.Path]::IsPathRooted($relative) -or $relative.Contains(':')){throw "Unexpected ZIP path: $relative"}
   $path=[IO.Path]::GetFullPath((Join-Path $base $relative))
   if(!$path.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $seen.ContainsKey($path.ToLowerInvariant())){throw 'Duplicate or escaping ZIP path.'}
   $seen[$path.ToLowerInvariant()]=$true
   if((($entry.ExternalAttributes -shr 16) -band 61440) -eq 40960){throw 'Linked ZIP entry rejected.'}
   $size+=$entry.Length;if($size -gt 2GB){throw 'Package expanded size exceeds limit.'}
  }
  $null=New-Item -ItemType Directory -Path $base
  foreach($entry in $archive.Entries){
   $relative=$entry.FullName.Replace('\','/');$path=[IO.Path]::GetFullPath((Join-Path $base $relative))
   if($relative.EndsWith('/')){$null=New-Item -ItemType Directory -Force -Path $path;continue}
   $null=New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path)
   [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$path,$false)
  }
 }finally{$archive.Dispose()}
 if(!(Test-BundleSources (Join-Path $Destination 'Mod Files'))){throw 'Downloaded package contents failed integrity checks. Nothing launched.'}
 return (Join-Path $Destination 'Mod Files/Bundle-Launch.ps1')
}

function Resolve-BundleDownload {
 param([string]$PayloadRoot,[string]$CacheRoot=(Join-Path $env:LOCALAPPDATA 'SignalisVR/Packages'),[switch]$NoDownload,
       [scriptblock]$FetchRelease={Invoke-RestMethod -Uri 'https://api.github.com/repos/stevethetitan92/Signalis-VR/releases?per_page=10' -Headers @{'User-Agent'='SIGNALIS-VR-Launcher'} -TimeoutSec 15},
       [scriptblock]$DownloadFile={param($url,$path) Invoke-WebRequest -Uri $url -OutFile $path -UseBasicParsing -TimeoutSec 180 -Headers @{'User-Agent'='SIGNALIS-VR-Launcher'}})
 $healthy=Test-BundleSources $PayloadRoot
 if($NoDownload){if(!$healthy){throw 'Bundled files are missing/damaged and downloads are disabled.'};return $null}
 try {
  [Net.ServicePointManager]::SecurityProtocol=[Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
  $releases=& $FetchRelease
  $asset=$null
  foreach($release in @($releases|Where-Object {!$_.draft}|Sort-Object published_at -Descending)){
   $matches=@($release.assets|Where-Object {$_.name -match '^SIGNALIS-VR-[0-9A-Za-z.\-]+-All-In-One(?:-v[0-9]+)?\.zip$' -and $_.state -eq 'uploaded'})
   if($matches.Count -gt 1){throw 'Several all-in-one downloads found in one release.'}
   if($matches.Count -eq 1){$asset=$matches[0];break}
  }
  if(!$asset){throw 'No all-in-one release package is currently available.'}
  $uri=[Uri]$asset.browser_download_url
  if($uri.Scheme -ne 'https' -or $uri.Host -ne 'github.com' -or $uri.AbsolutePath -notlike '/stevethetitan92/Signalis-VR/releases/download/*' -or $asset.digest -notmatch '^sha256:([0-9a-fA-F]{64})$'){throw 'Release download URL or checksum is invalid.'}
  $hash=$Matches[1]
  $version=Get-Content -LiteralPath (Join-Path $PayloadRoot 'Bundle-Version.json') -Raw -ErrorAction SilentlyContinue|ConvertFrom-Json
  if($healthy -and $version.asset -eq $asset.name){Write-Host "Package is current: $($asset.name)";return $null}
  if(!$asset.size -or $asset.size -gt 512MB){throw 'Unexpected package download size.'}
  $cache=[IO.Path]::GetFullPath($CacheRoot)
  $cursor=$cache
  while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked download cache rejected.'}};$cursor=Split-Path -Parent $cursor}
  $null=New-Item -ItemType Directory -Force -Path $cache
  $zip=Join-Path $cache ($hash+'.zip')
  if(!(Test-Path -LiteralPath $zip) -or (Get-FileHash -LiteralPath $zip).Hash -ne $hash){
   $partial=Join-Path $cache ([Guid]::NewGuid().ToString('N')+'.download')
   Write-Host "Downloading $($asset.name) from stevethetitan92/Signalis-VR..."
   & $DownloadFile $asset.browser_download_url $partial
   if((Get-Item -LiteralPath $partial).Length -ne $asset.size -or (Get-FileHash -LiteralPath $partial).Hash -ne $hash){throw 'Downloaded package size/checksum mismatch.'}
   if(Test-Path -LiteralPath $zip){$zip=Join-Path $cache ($hash+'-'+[Guid]::NewGuid().ToString('N')+'.zip')}
   Move-Item -LiteralPath $partial -Destination $zip
  }
  $destination=Join-Path $cache $hash
  if(Test-Path -LiteralPath $destination){
   $cachedPayload=Join-Path $destination 'Mod Files'
   Add-Type -AssemblyName System.IO.Compression.FileSystem
   $trustedZip=[IO.Compression.ZipFile]::OpenRead($zip)
   $digest=[Security.Cryptography.SHA256]::Create()
   try{
    $manifestEntries=@($trustedZip.Entries|Where-Object {$_.FullName.Replace('\','/') -eq 'Mod Files/Bundle-Integrity.json'})
    $sameManifest=$false
    if($manifestEntries.Count -eq 1 -and (Test-Path -LiteralPath (Join-Path $cachedPayload 'Bundle-Integrity.json'))){$stream=$manifestEntries[0].Open();try{$manifestHash=[BitConverter]::ToString($digest.ComputeHash($stream)).Replace('-','')}finally{$stream.Dispose()};$sameManifest=(Get-FileHash -LiteralPath (Join-Path $cachedPayload 'Bundle-Integrity.json')).Hash -eq $manifestHash}
    if($sameManifest -and (Test-BundleSources $cachedPayload)){Write-Host 'Using verified cached package.';return (Join-Path $cachedPayload 'Bundle-Launch.ps1')}
   }finally{$trustedZip.Dispose();$digest.Dispose()}
   $destination=Join-Path $cache ($hash+'-'+[Guid]::NewGuid().ToString('N'))
  }
  $script=Expand-VerifiedBundle -Zip $zip -Destination $destination -Sha256 $hash
  Write-Host "Verified update ready: $destination"
  return $script
 }catch{
  if($healthy){Write-Host "Download check unavailable: $($_.Exception.Message) Using intact bundled version." -ForegroundColor Yellow;return $null}
  throw "Bundled files are missing/damaged and recovery failed: $($_.Exception.Message)"
 }
}
