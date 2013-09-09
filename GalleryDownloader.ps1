<#
    Edit GallerySource.ps1 to specify parameters
#>

function InstallPackage ([string]$name, [string]$pattern) {
    if ((Test-Path $pattern) -eq $False) {
        Write-Host "Downloading $name from NuGet"
        & nuget install $name -OutputDirectory lib
    }

    if ((Test-Path $pattern) -eq $False) {
        throw "Cannot download $name"
    }

    $dllPath = Get-ChildItem $pattern | Select-Object -First 1

    Add-Type -Path $dllPath
}

. .\GallerySource.ps1

$url = $sourceParams.Get_Item("url")
$selector = $sourceParams.Get_Item("selector")

if ($sourceParams.ContainsKey("outputDir")) {
    $outputDir = $sourceParams.Get_Item("outputDir")
} else {
    $outputDir = "output"
}

$agilityPackPathPattern = "lib\HtmlAgilityPack.[0-9].[0-9].[0-9]\lib\Net40\HtmlAgilityPack.dll"
$scrapySharpPathPattern = "lib\ScrapySharp.*\lib\Net40\ScrapySharp.dll"

InstallPackage "HtmlAgilityPack" $agilityPackPathPattern
InstallPackage "ScrapySharp" $scrapySharpPathPattern

Write-Host "Packages installed"

$wc = New-Object System.Net.WebClient
$html = $wc.DownloadString($url)

$htmlDoc = New-Object HtmlAgilityPack.HtmlDocument
$htmlDoc.LoadHtml($html)

$rootNode = $htmlDoc.DocumentNode

$nodes = [ScrapySharp.Extensions.CssQueryExtensions]::CssSelect($rootNode, $selector)

$rootOutput = Join-Path -Path $PSScriptRoot $outputDir

if ((Test-Path $rootOutput -PathType Container) -eq $False) {
    New-Item -ItemType directory -Path $rootOutput | Out-Null
}

foreach ($node in $nodes) {
    $imgSrc = GetImgSource $node
    [string] $fileName = [System.IO.Path]::GetFileName($imgSrc)
    $queryStringIndex = $fileName.IndexOf('?')
    if ($queryStringIndex -gt 0) {
        $fileName = $fileName.Substring(0, $queryStringIndex)
    }
    if ($fileName.EndsWith("-jpg") -eq $True) {
        $fileName = $fileName.Replace("-jpg", ".jpg")
    }
    $outputPath = Join-Path $rootOutput $fileName

    Write-Host "Downloading from $imgSrc to $outputPath"

    $wc.DownloadFile($imgSrc, $outputPath)
}
