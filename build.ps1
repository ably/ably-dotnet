cd tools\psake
import-module .\psake.psm1
cd..
cd..

$build_constants
if ($msgpack) {
	$build_constants = 'MSGPACK'
} 

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 -properties @{ configuration = "release"; constants = "$build_constants"; } -framework '4.0' -verbose

cd tools\psake
remove-module psake
cd..
cd..