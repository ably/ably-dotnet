cd tools\psake
import-module .\psake.psm1
cd..
cd..

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 -properties @{ configuration = "release"; msbuild = "msbuild" } -framework '4.0' -verbose

cd tools\psake
remove-module psake
cd..
cd..