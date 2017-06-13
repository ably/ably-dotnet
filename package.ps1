import-module .\tools\psake\psake.psm1

$psake.use_exit_on_error = $true
invoke-psake ./default.ps1 Build -properties @{ configuration = "package"; sln_name = "IO.Ably.Package.sln" } -framework '4.0' 

remove-module psake

.\\tools\\NuGet.exe pack .\\nuget\\io.ably.nuspec