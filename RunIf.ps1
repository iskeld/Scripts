# Runs given executable if it is not already running (checks full path).
# Optionally, another executable may be specified to run if the former is not running
Param(
    [parameter(Mandatory=$true, Position = 1)]
    [alias("e")]
    $Executable,
    [parameter(Position = 2)]
    [alias("r")]
    $ExecutableToRun
)

if ($ExecutableToRun -eq $null) {
    $ExecutableToRun = $Executable
}

$processes = Get-Process
$found = $false
foreach ($process in $processes) {
    $procName = $process.ProcessName
    $modulesCount = $process.Modules.Count
    if ($modulesCount -gt 0) {
        $firstModule = $process.Modules[0]
        $fileName = $firstModule.FileName
        if ($fileName -eq $Executable.Trim()) {
            $found = $true
            break
        }
        #Write-Host "proc: $procName, cout: $modulesCount, fileName: $fileName"
    }
}

if (!$found) {
    & $ExecutableToRun
}