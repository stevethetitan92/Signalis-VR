$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'HeadingMath.cs')
$count = 0
foreach ($yaw in @(-170,-90,0,45,170)) {
    foreach ($pitch in @(-80,-30,0,30,80)) {
        foreach ($roll in @(-45,0,45)) {
            $q = [System.Numerics.Quaternion]::CreateFromYawPitchRoll($yaw * [Math]::PI / 180, $pitch * [Math]::PI / 180, $roll * [Math]::PI / 180)
            $heading = [SignalisVrTracking.HeadingMath]::Yaw($q.X,$q.Y,$q.Z,$q.W)
            if ([Math]::Abs($heading - $yaw * [Math]::PI / 180) -gt 0.00001) { throw 'Heading changed with pitch or roll' }
            $neutral = [System.Numerics.Quaternion]::CreateFromAxisAngle([System.Numerics.Vector3]::UnitY, $heading)
            $up = [System.Numerics.Vector3]::Transform([System.Numerics.Vector3]::UnitY, [System.Numerics.Quaternion]::Inverse($neutral))
            if ([System.Numerics.Vector3]::Distance($up,[System.Numerics.Vector3]::UnitY) -gt 0.00001) { throw 'Recenter tilted the vertical axis' }
            $count++
        }
    }
}
Write-Output "PASS: $count pitched/rolled heading combinations preserve yaw and the vertical axis."
