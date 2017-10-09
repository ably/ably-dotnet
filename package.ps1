Param(
    [switch]$msgpack
 )

import-module .\tools\psake\psake.psm1

$const = "PACKAGE"
if ($msgpack) {
	$const = 'PACKAGE;MSGPACK'
} 

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 Build -properties @{ configuration = "package"; sln_name = "IO.Ably.Package.sln"; constants = "$const" } -framework '4.0' 

remove-module psake

if ($msgpack) {
.\\tools\\NuGet.exe pack .\\nuget\\io.ably_msgpack.nuspec
} else {
.\\tools\\NuGet.exe pack .\\nuget\\io.ably.nuspec
}

