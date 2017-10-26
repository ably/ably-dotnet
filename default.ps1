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
	
	$solution_dir = "$build_script_dir\$sln_dir"
	$signKeyPath = "$build_script_dir\IO.Ably.snk"
	
	$build_artifacts_dir_base = "$build_script_dir\build\artifacts"
	$build_artifacts_dir = "$build_artifacts_dir_base\library"
	$build_artifacts_tools_dir = "$build_artifacts_dir_base\tools"
	$build_output_dir = "$build_script_dir\build\output"
	$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
}

$base_dir = ""

.\functions.ps1

task default -depends Init, Build, Unit_Tests

task Init {
	Import-Module $tools_dir\powershell\FileSupport -Force
}

task Assembly_Info {

	$gitversion = Get-ChildItem -Path ".\src\packages\" -Filter GitVersion.exe -Recurse -ErrorAction SilentlyContinue -Force | Select FullName | Select-Object -First 1 | Select -ExpandProperty FullName

    $major_version = & "$gitversion" /output json /showvariable Major
    $minor_version = & "$gitversion" /output json /showvariable Minor
    $patch_version = & "$gitversion" /output json /showvariable Patch   

    $main_version = "$major_version.$minor_version"
	$build_number = "$major_version.$minor_version.$patch_version"


    Write-Host "Build Number: $build_number" 

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

	run_msbuild $msbuild "$base_dir\$sln_name" $configuration $signKeyPath
}

task Package -depends Build {
	& "$build_script_dir\tools\NuGet.exe" pack ".\nuget\io.ably.nuspec"
}

task Unit_Tests {

	$base_dir = "$build_script_dir\$sln_dir\IO.Ably.Tests\bin\$configuration"
	$packages_dir = "$build_script_dir\$sln_dir\packages\"

	setup_folder $build_output_dir

	$unit_test_results= "$build_output_dir\TestResults.xml"
	$sandbox_test_results= "$build_output_dir\SandboxTestResults.xml"

	$testDll = "$base_dir\IO.Ably.Tests.dll"
	
	try {
		if(Test-Path $unit_test_results) {
			Remove-Item $unit_test_results -Force
		}
		if(Test-Path $sandbox_test_results) {
			Remove-Item $sandbox_test_results -Force
		}

		$xunit_console = Get-ChildItem -Path ".\src\packages\" -Filter xunit.console.exe -Recurse -ErrorAction SilentlyContinue -Force | Select FullName | Select-Object -First 1 | Select -ExpandProperty FullName
		if($xunit_console -eq "") {
			$xunit_console = "$tools_dir\xunit\xunit.console.exe"
		}
		#Run all the unit tests first
		& "$xunit_console" $testDll -noappdomain -nunit "$unit_test_results" -notrait "requires=sandbox"
		#& "$xunit_console" $testDll -diagnostics -nunit "$sandbox_test_results" -trait "requires=sandbox"
	}
	catch {
		Write-Host $_
		Write-Host ("************************ Unit Test failure ***************************")
		exit 1
	}
}