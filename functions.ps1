function global:run_msbuild($msbuild, $solutionPath, $configuration, $signKeyPath)
{
	clear-obj-artifacts $solutionPath

	Write-Output "Configuration: $configuration"

	try {
		switch($configuration)
		{
			"package" { exec { & $msbuild $solutionPath "/t:clean;build" "/p:Configuration=release;Platform=Any CPU" "/p:AssemblyOriginatorKeyFile=$signKeyPath" "/p:SignAssembly=true" "/p:DefineConstants=PACKAGE" "/p:UseSharedCompilation=false" } }
			"release" { exec { & $msbuild $solutionPath "/t:clean;build" "/p:Configuration=$configuration;Platform=Any CPU" "/p:UseSharedCompilation=false"} }
			"ci_release" { exec { & $msbuild $solutionPath "/t:clean;build" "/p:Configuration=$configuration;Platform=Any CPU" "/p:UseSharedCompilation=false"} }
			default { exec { & $msbuild $solutionPath "/t:clean;build" "/p:Configuration=$configuration;Platform=Any CPU" "/p:UseSharedCompilation=false" } }
		}
	}
	catch {
		Write-Output "##teamcity[buildStatus text='MSBuild Compiler Error - see build log for details' status='ERROR']"
		Write-Host $_
		Write-Host ("************************ BUILD FAILED ***************************")
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
	[string]$company, 
	[string]$product, 
	[string]$copyright,
	[string]$full_version,
	[string]$assembly_version,
	[string]$file = $(throw "file is a required parameter.")
)

$fileVersion = $full_version -replace "-\w+$", ""

$asmInfo = "using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: ComVisibleAttribute(false)]
[assembly: AssemblyCompanyAttribute(""$company"")]
[assembly: AssemblyDescription(""Client for ably.io realtime service"")]
[assembly: AssemblyProductAttribute(""$product"")]
[assembly: AssemblyCopyrightAttribute(""$copyright"")]
[assembly: AssemblyVersionAttribute(""$assembly_version"")]
[assembly: AssemblyInformationalVersionAttribute(""$full_version"")]
[assembly: AssemblyFileVersionAttribute(""$fileVersion"")]
"

	$dir = [System.IO.Path]::GetDirectoryName($file)
	if ([System.IO.Directory]::Exists($dir) -eq $false)
	{
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}
	Write-Host "Generating assembly info file: $file. Version: $full_version"
	Write-Output $asmInfo > $file
}