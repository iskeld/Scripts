# Runs NuGet pack for all .csproj files which has corresponding (residing in the same directory) .nuspec file.
# Search starts from the $RootPath
Param(
    [parameter(Mandatory=$true, Position = 1)]
    [alias("Root")]
    $RootPath,
    [parameter(Position = 2)]
    [alias("OutputDir")]
    $OutputDirectory
)

if ($OutputDirectory -eq $null) {
    $NuGetOuputCmd = ""
} else {
    if ((Test-Path $OutputDirectory -PathType Container) -eq $False) {
        throw "Output directory $OutputDirectory does not exist"
    }
    $NuGetOuputCmd = "-OutputDirectory `"$OutputDirectory`""
}

if ((Test-Path $RootPath -PathType Leaf) -eq $False) {
    if ((Test-Path $RootPath -PathType Container) -eq $False) {
        throw "Path $RootPath does not exist"
    } else {
        $rootDirectory = $RootPath
    }
}
else {
    $rootDirectory = [System.IO.Path]::GetDirectoryName($RootPath);
}

$nuspecDirs = Get-ChildItem -Path $rootDirectory -Filter *.nuspec -Recurse | Select-Object -ExpandProperty Directory 

foreach ($dir in $nuspecDirs) {
    $csprojFiles = Get-ChildItem -Path $dir.FullName -Filter *.csproj
    if ($csprojFiles.count -gt 0) {
        $csprojPath = $csprojFiles[0].FullName
        Invoke-Expression "nuget pack $csprojPath -NonInteractive $NuGetOuputCmd"
    }
}