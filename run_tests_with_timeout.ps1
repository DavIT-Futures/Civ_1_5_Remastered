param(
    [string]$Filter = "PlayedTurnsMatchBaseline",
    [int]$TimeoutSeconds = 300
)

$argsList = @("test", "--no-build", "--filter", $Filter)

Write-Host "Uruchamianie dotnet test (timeout: $TimeoutSeconds s, filtr: $Filter)..."

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "test --filter `"$Filter`""
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true

$proc = [System.Diagnostics.Process]::Start($psi)

$stdoutTask = $proc.StandardOutput.ReadToEndAsync()
$stderrTask = $proc.StandardError.ReadToEndAsync()

if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
    try {
        $proc.Kill($true)
    } catch {}
    Write-Error "PRZEKROCZONO LIMIT CZASU ($TimeoutSeconds s)! Proces testowy zostat zabity ze wzgledow bezpieczenstwa."
    exit 124
}

[System.Threading.Tasks.Task]::WaitAll($stdoutTask, $stderrTask)

$out = $stdoutTask.Result
$err = $stderrTask.Result

if ($out) { Write-Host $out }
if ($err) { Write-Error $err }

exit $proc.ExitCode
