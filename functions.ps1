function global:run_msbuild($solutionPath, $configuration)
{
	clear-obj-artifacts $solutionPath

	Write-Output "Configuration: $configuration"

	try {
		switch($configuration)
		{
			"test" { exec { msbuild $solutionPath "/t:clean;build" "/v:Quiet"
			"/p:Configuration=$configuration;Platform=Any CPU" } }
			"release" { exec { msbuild $solutionPath "/t:clean;build" "/p:Configuration=$configuration;Platform=Any CPU" } }
			default { exec { msbuild $solutionPath "/t:clean;build" "/p:Configuration=$configuration;Platform=Any CPU" } }
		}
	}
	catch {
		Write-Output "##teamcity[buildStatus text='MSBuild Compiler Error - see build log for details' status='ERROR']"
		Write-Host $_
		Write-Host ("************************ BUILD FAILED ***************************")
		exit 1
	}
}

function global:run_tests($test_result_file, $xunit_console_dir, $solution_dir, $configuration, $testType)
{
	$configType = $configuration.ToString().ToLower()
	$testAssemblies = @()
	$testDlls = (get-childitem "$solution_dir\" -r -i "*$testType.dll" -exclude "*.config")
	foreach($testDll in $testDlls) {
		if($testDll.ToString().ToLower().Contains("bin\" + $configTYpe))
		{
			$testAssemblies += $testDll
		}
	}

	try {
		if(Test-Path $test_result_file) {
			Remove-Item $test_result_file -Force
		}

		Write-Output "TestAssemblies: $testAssemblies"

		$xunit_console = "$xunit_console_dir\xunit.console.clr4.exe"
		& "$xunit_console" $testAssemblies /noshadow /nunit "$test_result_file"

		Write-Output "##teamcity[importData type='nunit' path='$test_result_file']"
	}
	catch {
		Write-Output "##teamcity[buildStatus text='Error running unit tests' status='ERROR']"
		Write-Host $_
		Write-Host ("************************ Unit Test failure ***************************")
		exit 1
	}
}


function global:clear-obj-artifacts($directory)
{
	try {
		Get-ChildItem $directory -include obj -Recurse | foreach ($_) { remove-item $_.fullname -Force -Recurse }
	}
	catch {
		Write-Output "##teamcity[buildStatus text='Clearing the obj folder generated an error - try again' status='ERROR']"
		Write-Host $_
		Write-Host ("************************ Build Failed ***************************")
		exit 1
	}
}

function global:publish_build($source_dir, $artifacts_dir)
{
	try
	{
		Write-Output "Publishing from $source_dir to $artifacts_dir"

		setup_folder $artifacts_dir

		clean_directory $artifacts_dir

		$itemsToCopy = Get-EnhancedChildItem $source_dir -Recurse -Force
		Write-Output 'Items to copy: ' $itemsToCopy.Count
		foreach ($item in $itemsToCopy)
		{
			$dest = Join-Path $artifacts_dir $item.FullName.Substring($source_dir.length)
			Write-Host "Copying item from " $item.FullName " to $dest"
			Copy-Item $item.FullName $dest
			
		}
		Write-Host "All copied...."
		remove_empty_directories $artifacts_dir
	}
	catch {
		Write-Output "##teamcity[buildStatus text='Pushing to artifacts fails' status='ERROR']"
		Write-Host $_
		Write-Host ("******************** Artifact Push Failure ********************")
		exit 1
	}
}

function global:publish_tools($root, $build_afrtifacts_tools_dir)
{
	try
	{
		setup_folder $build_afrtifacts_tools_dir
		
		clean_directory $build_afrtifacts_tools_dir
		Write-Host 'Getting children from: ' $root
		$itemsToCopy = Get-ChildItem "$root\*.ps1"
		Write-Host 'Items to copy: ' $itemsToCopy.Count
		foreach ($item in $itemsToCopy)
		{
			$dest = Join-Path $build_afrtifacts_tools_dir $item.FullName.Substring($root.length)
			Write-Host "Copying item from $item to $dest"
			Copy-Item $item $dest
		}
		
		Copy-Item "$root/tools" "$build_afrtifacts_tools_dir/tools" -Recurse
	}
	catch {
		Write-Output "##teamcity[buildStatus text='Pushing to artifacts fails' status='ERROR']"
		Write-Host $_
		Write-Host ("******************** Artifact Push Failure ********************")
		exit 1
	}
}

function global:remove_empty_directories($directory)
{
	Write-Host "******************** Removing empty directories in $directory ********************"
	$items = Get-ChildItem $directory -Recurse
	foreach($item in $items)
	{
		  if( $item.PSIsContainer )
		  {
				$subitems = Get-ChildItem -Recurse -Path $item.FullName
				if($subitems -eq $null)
				{
					  "Remove item: " + $item.FullName
					  Remove-Item $item.FullName
				}
				$subitems = $null
		  }
	}
}

function global:clean_directory($directory)
{
	if(Test-Path $directory) {
		Get-ChildItem "$directory\*" | foreach ($_) { remove-item $_.fullname -Force -Recurse }
	}
}

function global:setup_folder($directory)
{
	if(!(Test-Path $directory))
	{
		New-Item $directory -type directory
	}
}

function global:generate_assembly_info
{
param(
	[string]$clsCompliant = "true",
	[string]$company, 
	[string]$product, 
	[string]$copyright,
	[string]$version,
	[string]$file = $(throw "file is a required parameter.")
)
$asmInfo = "using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: CLSCompliantAttribute($clsCompliant )]
[assembly: ComVisibleAttribute(false)]
[assembly: AssemblyCompanyAttribute(""$company"")]
[assembly: AssemblyProductAttribute(""$product"")]
[assembly: AssemblyCopyrightAttribute(""$copyright"")]
[assembly: AssemblyVersionAttribute(""$version"")]
[assembly: AssemblyInformationalVersionAttribute(""$version"")]
[assembly: AssemblyFileVersionAttribute(""$version"")]
"

	$dir = [System.IO.Path]::GetDirectoryName($file)
	if ([System.IO.Directory]::Exists($dir) -eq $false)
	{
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}
	Write-Host "Generating assembly info file: $file"
	Write-Output $asmInfo > $file
}