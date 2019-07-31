Param(
    [Parameter(mandatory=$false)]
    [string]$version,
    [string]$configuration = "package",
    [string]$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
    [switch]$msgpack
 )

import-module .\tools\psake\psake.psm1

if(!$version) {
    $gitversion = Get-ChildItem -Path ".\src\packages\" -Filter GitVersion.exe -Recurse -ErrorAction SilentlyContinue -Force | Select FullName | Select-Object -First 1 | Select -ExpandProperty FullName
    $version = & $gitversion /output json /showvariable NuGetVersionV2
}

if($skiptests) {
    $runtests = $false
} else {
    $runtests = $true
}

$const = "PACKAGE"
if ($msgpack) {
	# Use %3B in place of ; as PS is borking the value - https://msdn.microsoft.com/en-us/library/bb383819.aspx
	$const = "MSGPACK%3BPACKAGE"
}

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 Build -properties @{ msbuild = $msbuild; configuration = $configuration; runtests = $runtests; constants = "$const"  } -framework '4.0' -verbose

remove-module psake

if ($msgpack) {
.\\tools\\NuGet.exe pack .\\nuget\\io.ably_msgpack.nuspec -properties "version=$version;configuration=$configuration"
} else {
.\\tools\\NuGet.exe pack .\\nuget\\io.ably.nuspec -properties "version=$version;configuration=$configuration"
}
