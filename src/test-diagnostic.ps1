$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'DiagnosticFlow.cs')
function Assert-Diagnostic($condition, $message) { if (-not $condition) { throw $message } }
$f = [SignalisVrTracking.DiagnosticFlow]::new()
Assert-Diagnostic (-not $f.CanSubmit) 'Startup can submit'
Assert-Diagnostic (-not $f.BeginSubmit(0)) 'Skipped both prerequisites'
$f.RenderReady()
Assert-Diagnostic ($f.Stage -eq 0) 'Skipped connection stage'
$f.Connected()
Assert-Diagnostic ($f.Stage -eq 1 -and -not $f.CanSubmit) 'Connection submits'
Assert-Diagnostic (-not $f.BeginSubmit(1)) 'Skipped local rendering'
$f.RenderReady()
Assert-Diagnostic ($f.Stage -eq 2 -and -not $f.CanSubmit) 'Local rendering submits'
Assert-Diagnostic ($f.BeginSubmit(30)) 'Submission did not arm'
Assert-Diagnostic ($f.CanSubmit -and -not $f.Expired(329.99)) 'Window ended early'
Assert-Diagnostic (-not $f.BeginSubmit(329)) 'Repeated F10 extended the window'
Assert-Diagnostic ($f.Expired(330)) 'Window did not expire'
$f.Stop()
Assert-Diagnostic ($f.Stage -eq 0 -and -not $f.CanSubmit) 'Stop leaves submission enabled'
foreach ($stage in @(1,2,3)) {
    $f.Connected()
    if ($stage -ge 2) { $f.RenderReady() }
    if ($stage -ge 3) { [void]$f.BeginSubmit(0) }
    $f.Stop()
    Assert-Diagnostic (-not $f.CanSubmit -and $f.Stage -eq 0) 'Stage cancellation failed'
}
'PASS: prerequisite gating, no submission in stages A/B, bounded stage C, repeated-key protection, cancellation at every stage.'
