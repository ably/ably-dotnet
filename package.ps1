cd tools\psake
import-module .\psake.psm1
cd..
cd..

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 -properties @{ configuration = "release" } -framework '4.0' -verbose Package

cd tools\psake
remove-module psake
cd..
cd..