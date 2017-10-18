Param(
  [switch]$skiptests,
  [string]$configuration = "ci_release"
)

import-module .\tools\psake\psake.psm1

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 -properties @{ configuration = $configuration; msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"; buildserver = "appveyor"; runtests = !$skiptests } -framework '4.0' -verbose

cd tools\psake
remove-module psake
cd..
cd..