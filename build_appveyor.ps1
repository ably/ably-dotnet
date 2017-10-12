cd tools\psake
import-module .\psake.psm1
cd..
cd..

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 -properties @{ configuration = "ci_release"; msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" } -framework '4.0' -verbose

cd tools\psake
remove-module psake
cd..
cd..