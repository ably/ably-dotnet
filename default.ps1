Framework "4.5.1"
$ErrorActionPreference = 'Stop'

properties { 
	#Folder Settings
	$build_script_dir  = resolve-path .
	$tools_dir = "$build_script_dir\tools"
	$configuration = 'debug'
	$sln_dir = "src"
	$sln_name = "IO.Ably.sln"
	
	$project_name = "IO.Ably"
	$main_version = "0.8"
	$build_number = "0.8.5"
	$solution_dir = "$build_script_dir\$sln_dir"
	$signKeyPath = "$build_script_dir\IO.Ably.snk"
	
	$build_artifacts_dir_base = "$build_script_dir\build\artifacts"
	$build_artifacts_dir = "$build_artifacts_dir_base\library"
	$build_artifacts_tools_dir = "$build_artifacts_dir_base\tools"
	$build_output_dir = "$build_script_dir\build\output"
}

$base_dir = ""

.\functions.ps1

task default -depends Init, Build, Unit_Tests

task Init {
	Import-Module $tools_dir\powershell\FileSupport -Force
}

task Assembly_Info {
	$base_dir = "$build_script_dir\$sln_dir"
	generate_assembly_info `
		-file  "$base_dir\CommonAssemblyInfo.cs" `
		-company "Ably" `
		-product "Ably .Net Library" `
		-assembly_version $main_version `
		-full_version $build_number `
		-copyright "Ably" . [DateTime]::Now`
} 

task Build -depends Assembly_Info, Init {
	clean_directory $build_artifacts_dir_base
	
	$base_dir = "$build_script_dir\$sln_dir"

	run_msbuild "$base_dir\$sln_name" $configuration $signKeyPath

	$package_dir = "$base_dir\$project_name\bin\$configuration"
}

task Package -depends Build {
	& "$build_script_dir\tools\NuGet.exe" pack ".\nuget\io.ably.nuspec"
}

task Unit_Tests {

	$base_dir = "$build_script_dir\$sln_dir\IO.Ably.Tests\bin\$configuration"

	$xunit_runner = "$build_script_dir\tools\xunit-runners\tools"

	setup_folder $build_output_dir

	run_tests "$build_output_dir\TestResults.xml" $xunit_runner $base_dir $configuration ".Tests"
	run_nunit_tests "$build_output_dir\AcceptanceTestResults.xml" "$build_script_dir\tools\nunit-runners" $base_dir $configuration ".AcceptanceTests"
	Write-Host "Running unit tests"
}