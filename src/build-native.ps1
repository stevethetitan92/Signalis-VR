$ErrorActionPreference = 'Stop'
$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
$vs = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vs) { throw 'Visual C++ build tools are required.' }
$vc = (Get-ChildItem -LiteralPath (Join-Path $vs 'VC\Tools\MSVC') -Directory | Sort-Object Name -Descending | Select-Object -First 1).FullName
$sdkRoot = 'C:\Program Files (x86)\Windows Kits\10'
$sdk = (Get-ChildItem -LiteralPath (Join-Path $sdkRoot 'Include') -Directory | Sort-Object Name -Descending | Select-Object -First 1).Name
$compiler = Join-Path $vc 'bin\Hostx64\x64\cl.exe'
$scratch = Join-Path $PSScriptRoot '..\work\stereo-build'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$includes = @('/I' + (Join-Path $vc 'include'))
foreach ($part in @('ucrt','shared','um')) { $includes += '/I' + (Join-Path $sdkRoot "Include\$sdk\$part") }
$libraries = @(
    ('/LIBPATH:' + (Join-Path $vc 'lib\x64')),
    ('/LIBPATH:' + (Join-Path $sdkRoot "Lib\$sdk\ucrt\x64")),
    ('/LIBPATH:' + (Join-Path $sdkRoot "Lib\$sdk\um\x64"))
)
Push-Location $scratch
try {
    & $compiler /nologo /std:c++17 /EHsc /MT /O2 /W4 /WX @includes /LD (Join-Path $PSScriptRoot 'native\Bridge.cpp') /link @libraries ('/OUT:' + (Join-Path $PSScriptRoot 'SignalisVrRenderBridge.dll'))
    if ($LASTEXITCODE -ne 0) { throw 'Native bridge build failed.' }
    & $compiler /nologo /std:c++17 /EHsc /MT /O2 /W4 /WX @includes (Join-Path $PSScriptRoot 'native\Bridge.cpp') (Join-Path $PSScriptRoot 'native\BridgeTests.cpp') /link @libraries d3d11.lib /OUT:BridgeTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Native test build failed.' }
    & '.\BridgeTests.exe'
    if ($LASTEXITCODE -ne 0) { throw 'Native bridge tests failed.' }
    & '.\BridgeTests.exe' hardware
    if ($LASTEXITCODE -ne 0) { throw 'Hardware bridge tests failed.' }
} finally { Pop-Location }
