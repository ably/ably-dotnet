Param(
    [Parameter(mandatory=$false)]
    [string]$configuration = "package",
    [string]$version,
    [string]$buildserver = "appveyor",
    [string]$source = "https://www.myget.org/F/ably-dotnet-dev/api/v2/package",
    [string]$apikey = "",
    [switch]$msgpack,
    [switch]$publish

 )

import-module .\tools\psake\psake.psm1

if(!$version) {
    $gitversion = Get-ChildItem -Path ".\src\packages\" -Filter GitVersion.exe -Recurse -ErrorAction SilentlyContinue -Force | Select FullName | Select-Object -First 1 | Select -ExpandProperty FullName
    $version = & $gitversion /output json /showvariable NuGetVersionV2
}

$const = "PACKAGE"
if ($msgpack) {
    # Use %3B in place of ; as PS is borking the value - https://msdn.microsoft.com/en-us/library/bb383819.aspx
    $const = "MSGPACK%3BPACKAGE"
} 

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 Build -properties @{ configuration = $configuration; sln_name = "IO.Ably.sln"; msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"; buildserver = "appveyor"; runtests = $runtests; constants = "$const" } -framework '4.0' -verbose

remove-module psake


if ($msgpack) {
.\\tools\\NuGet.exe pack .\\nuget\\io.ably_msgpack.nuspec -properties "version=$version;configuration=$configuration"
} else {
.\\tools\\NuGet.exe pack .\\nuget\\io.ably.nuspec -properties "version=$version;configuration=$configuration"
}

if($publish) {
.\\tools\\NuGet.exe push ably.io.*.nupkg $apikey -source $source
}