param([Parameter(Mandatory=$true)][string]$GamePath)
$ErrorActionPreference = 'Stop'
$managed = Join-Path $GamePath 'MelonLoader\Managed'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$references = @(
    (Join-Path $GamePath 'MelonLoader\MelonLoader.dll'),
    (Join-Path $managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $managed 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $managed 'UnityEngine.ScreenCaptureModule.dll'),
    (Join-Path $managed 'UnhollowerBaseLib.dll'),
    (Join-Path $managed 'Il2Cppmscorlib.dll'),
    (Join-Path $framework 'mscorlib.dll'),
    (Join-Path $framework 'System.dll'),
    (Join-Path $framework 'System.Core.dll')
)
[System.Reflection.Assembly]::LoadFrom((Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')) | Out-Null
[System.Reflection.Assembly]::LoadFrom((Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')) | Out-Null
$metadata = [System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
foreach ($reference in $references) {
    $metadata.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($reference))
}
$bridgePath = (Join-Path $GamePath 'UserLibs\SignalisVrTracking\SignalisVrRenderBridge.dll').Replace('\', '/')
$source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'SignalisVrTracking.cs')).Replace('"SignalisVrRenderBridge"', ('"' + $bridgePath + '"'))
$tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($source)
$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
# Mono resolves each native import separately. Bind every entry point to the
# same explicit installation path, including shutdown and reconnection.
$nativePath = (Join-Path $GamePath 'UserLibs\SignalisVrTracking\openvr_api.dll').Replace('\', '/')
$bindings = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'vendor\openvr_api.cs')).Replace('"openvr_api"', ('"' + $nativePath + '"'))
$bindingTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($bindings)
$mathTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'HeadingMath.cs')))
$puzzleTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'PuzzleScreen.cs')))
$flowTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'DiagnosticFlow.cs')))
$keyTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'HotkeyEdge.cs')))
$compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('SignalisVrTracking', [Microsoft.CodeAnalysis.SyntaxTree[]]@($tree, $bindingTree, $mathTree, $puzzleTree, $flowTree, $keyTree), $metadata, $options)
$stream = [IO.File]::Create((Join-Path $PSScriptRoot 'SignalisVrTracking.dll'))
try { $result = $compilation.Emit($stream) } finally { $stream.Dispose() }
$result.Diagnostics | ForEach-Object { $_.ToString() }
if (-not $result.Success) { throw 'VR probe compilation failed.' }
Write-Output 'Built SignalisVrTracking.dll'
