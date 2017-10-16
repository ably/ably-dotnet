Param(
    [Parameter(mandatory=$false)]
    [string]$version,
    [switch]$msgpack
 )

import-module .\tools\psake\psake.psm1

if(!$version) {
    $version = & gitversion /output json /showvariable NuGetVersionV2
}

$const = "PACKAGE"
if ($msgpack) {
	# Use %3B in place of ; and PS is borking the value - https://msdn.microsoft.com/en-us/library/bb383819.aspx
	$const = "MSGPACK%3BPACKAGE"
} 

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 Build -properties @{ configuration = "package"; sln_name = "IO.Ably.Package.sln"; constants = "$const" } -framework '4.0' 

remove-module psake



if ($msgpack) {
.\\tools\\NuGet.exe pack .\\nuget\\io.ably_msgpack.nuspec -properties version=$version
} else {
.\\tools\\NuGet.exe pack .\\nuget\\io.ably.nuspec -properties version=$version
}