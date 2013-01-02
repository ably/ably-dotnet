#
# ==============================================================
# @ID       $Id: GetChildItemExtension.ps1 1204 2012-04-12 18:19:21Z ww $
# @created  2011-07-01
# @project  http://cleancode.sourceforge.net/
# ==============================================================
#
# The official license for this file is shown next.
# Unofficially, consider this e-postcardware as well:
# if you find this module useful, let us know via e-mail, along with
# where in the world you are and (if applicable) your website address.
#
#
# ***** BEGIN LICENSE BLOCK *****
# Version: MPL 1.1
#
# The contents of this file are subject to the Mozilla Public License Version
# 1.1 (the "License"); you may not use this file except in compliance with
# the License. You may obtain a copy of the License at
# http://www.mozilla.org/MPL/
#
# Software distributed under the License is distributed on an "AS IS" basis,
# WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
# for the specific language governing rights and limitations under the
# License.
#
# The Original Code is part of the CleanCode toolbox.
#
# The Initial Developer of the Original Code is Michael Sorens.
# Portions created by the Initial Developer are Copyright (C) 2011
# the Initial Developer. All Rights Reserved.
#
# Contributor(s):
#
# ***** END LICENSE BLOCK *****
#

# Adapted from http://get-powershell.com/post/2009/01/05/Using-Proxy-Commands-in-PowerShell.aspx

# Reference to requested feature for -ContainersOnly
# http://connect.microsoft.com/PowerShell/feedback/details/308796/add-enumeration-parameter-to-get-childitem-cmdlet-to-specify-container-non-container-both

Set-StrictMode -Version Latest

$introducedParameters = `
	"ExcludeTree", `
	"Svn", `
	"ContainersOnly", `
	"NoContainersOnly", `
	"FullName"


<#

.SYNOPSIS
A variation of Get-ChildItem notably providing the ability to prune trees,
isolate Subversion-aware items, and flatten the output for brevity.

.DESCRIPTION
Get-EnhancedChildItem is a variation of Get-ChildItem,
supporting some but not all parameters of its progenitor,
while adding some unique ones that enrich its capabilities.

Enhancements include:
o exclude an entire subtree (-ExcludeTree).
o isolate Subversion-aware files and folders (-Svn);
o restrict output to just files (-NoContainersOnly)
	or just folders (-ContainersOnly);
o generate a concise list of full names (-FullName) similar to the list
	of simple names provided by Get-ChildItem -Names.

.PARAMETER Exclude
See Get-ChildItem.

.PARAMETER Filter
See Get-ChildItem.

.PARAMETER Force
See Get-ChildItem.

.PARAMETER Include
See Get-ChildItem.

.PARAMETER LiteralPath
See Get-ChildItem.

.PARAMETER Name
See Get-ChildItem.

.PARAMETER Path
See Get-ChildItem.

.PARAMETER Recurse
See Get-ChildItem.

.PARAMETER UseTransaction
See Get-ChildItem.

.PARAMETER ExcludeTree
Excludes not just a matching item but also all its children as well.
Wildcards are permitted.

.PARAMETER Svn
Ignores files and folders that are not Subversion-aware.

.PARAMETER NoContainersOnly
Returns only non-containers (files).
Mutually exclusive with -ContainersOnly.

.PARAMETER ContainersOnly
Returns only containers (directories).
Mutually exclusive with -NoContainersOnly.

.PARAMETER FullName
Retrieves only the full names of the items in the locations. If you pipe
the output of this command to another command, only the item full names
are sent.  Mutually exclusive with -Name.

.INPUTS
System.String. You can pipe one or more path strings to Get-EnhancedChildItem.

.OUTPUTS
Object. The type of object returned is determined by the provider with
which it is used.

.EXAMPLE
PS> Get-EnhancedChildItem

	Directory: W:\powershell

Mode                LastWriteTime     Length Name
----                -------------     ------ ----
-a---          6/6/2011   1:12 PM       4833 Get-EnhancedChildItem.ps1
-a---          6/6/2011   9:45 AM       9340 keywordCheck.ps1
-a---          6/6/2011   9:40 AM        488 Parse-IniFile.ps1
-a---          6/6/2011  11:07 AM        327 temp.ps1

This command gets the child items in the current location. 
If the location is a file system directory, it gets the files and
sub-directories in the current directory. If the item does not have
child items, this command returns to the command prompt without
displaying anything.

.EXAMPLE
PS> Get-EnhancedChildItem . -FullName

W:\powershell\Get-EnhancedChildItem.ps1
W:\powershell\keywordCheck.ps1
W:\powershell\Parse-IniFile.ps1
W:\powershell\temp.ps1

Same results as above, but outputs only full names in a single list.
For a single directory, there is little difference but for many directories,
all items appear in a single list, making the output more concise.

.EXAMPLE
PS> Get-EnhancedChildItem . -Recurse -ExcludeTree *-tmp,doc -Svn

For a large subtree rooted at the current location, this enumerates the
entire subtree, pruning any directories named "doc" or those ending
in "-tmp", and further restricting the output to Subversion-aware files
and folders.

.NOTES
You can also refer to Get-ChildItem by its built-in alias "gciplus".
For more information, see about_Aliases.

Get-EnhancedChildItem does not get hidden items by default.

This function is part of the CleanCode toolbox
from http://cleancode.sourceforge.net/.

Since CleanCode 1.1.01.

.LINK
Get-ChildItem

#>

Function Get-EnhancedChildItem {
[CmdletBinding(DefaultParameterSetName='Items', SupportsTransactions=$true)]
param(
	[Parameter(ParameterSetName='Items', Position=0, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
	[System.String[]]
	${Path},

	[Parameter(ParameterSetName='LiteralItems', Mandatory=$true, Position=0, ValueFromPipelineByPropertyName=$true)]
	[Alias('PSPath')]
	[System.String[]]
	${LiteralPath},

	[Parameter(Position=1)]
	[System.String]
	${Filter},

	[System.String[]]
	${Include},

	[System.String[]]
	${Exclude},

	[Switch]
	${Recurse},

	[Switch]
	${Force},

	[Switch]
	${Name},

	[System.String[]]
	${ExcludeTree} = @(),

	[Switch]
	${Svn},

	[Switch]
	${ContainersOnly},

	[Switch]
	${NoContainersOnly},

	[Switch]
	${FullName}
	)

begin
{
	try {
		$outBuffer = $null
		if ($PSBoundParameters.TryGetValue('OutBuffer', [ref]$outBuffer) -and $outBuffer -gt 1024)
		{
			$PSBoundParameters['OutBuffer'] = 1024
		}
		# 2011.08.04 msorens bug fix: no parameters threw null exception
		if ($PSBoundParameters.Count -eq 0) { $PSBoundParameters = @{} }

		$wrappedCmd = $ExecutionContext.InvokeCommand.GetCommand( `
			'Get-ChildItem', [System.Management.Automation.CommandTypes]::Cmdlet)
		$filters = @("$wrappedCmd @PSBoundParameters")

		#region Connecting Filters
		if ($ExcludeTree)          { $filters += "FilterExcludeTree"
		                             $script:excludeList = $ExcludeTree }

		if ($Svn)                  { $filters += "FilterSvn" }

		if ($ContainersOnly)       { $filters += "FilterContainersOnly" }
		elseif ($NoContainersOnly) { $filters += "FilterNoContainersOnly" }

		# These two must be last.
		# Thus, must process the -Name parameter here rather than
		# pass it through to base cmdlet.
		if ($FullName)             { $filters += "FilterFullName" }
		elseif ($Name)             { $filters += "FilterName"
		                             [Void]$PSBoundParameters.Remove("Name") }
		#endregion Connecting Filters

		$code = [string]::join(" | ", $filters)

		RemoveIntroducedParameters $PSBoundParameters

		#region verbose
		Write-Verbose "[[ $code ]]"
		Write-Verbose "Parameters to Get-ChildItem = [["
		$PSBoundParameters.GetEnumerator() | 
		% { Write-Verbose ("    {0} = {1}" -f $_.key, [string]::join(',',$_.value)) }
		Write-Verbose "]]"
		#endregion verbose

		$scriptCmd = $ExecutionContext.InvokeCommand.NewScriptBlock($code)
		$steppablePipeline = $scriptCmd.GetSteppablePipeline()
		$steppablePipeline.Begin($PSCmdlet)
	} catch {
		throw
	}
}

process
{
	try {
		$steppablePipeline.Process($_)
	} catch {
		throw
	}
}

end
{
	try {
		$steppablePipeline.End()
	} catch {
		throw
	}
}

}

# Must remove all introduced parameters
# in order to call the base cmdlet, which has no notion of them.
function RemoveIntroducedParameters($boundParams)
{
	# 2011.08.04 msorens bug fix: have to remove *all* introduced params
	$introducedParameters | % { [Void]$boundParams.Remove($_) }
}

# From http://solutionizing.net/2008/12/20/powershell-coalesce-and-powershellasp-query-string-parameters/
# (Could use Invoke-NullCoalescing from PSCX but I wanted to minimize dependencies.)
function Coalesce-Args {
  (@($args | ?{$_}) + $null)[0]
}

filter FilterExcludeTree()
{
	$target = $_
	Coalesce-Args $Path "." | % {
		$canonicalPath = (Get-Item $_).FullName
		if ($target.FullName.StartsWith($canonicalPath)) {
			$pathParts = $target.FullName.substring($canonicalPath.Length + 1).split("\");
			if ( ! ($excludeList | where { $pathParts -like $_ } ) ) { $target }
		}
	}
}

filter FilterSvn()
{
	# Check just the current item (depth => empty);
	# force it to report even if up-to-date (verbose => true); and
	# wrap stderr into stdout (2>&1) for the next step.
	$svnStatus = (svn status --verbose --depth empty $_.fullname 2>&1)

	# Item is non-Svn with status of "?" (unversioned) or "I" (ignored).
	# Descendants, which are still traversed, cause "svn status" to fail
	# with a warning (second part of regex), and they of course are also non-Svn.
	$svnFilter = ($svnStatus -notmatch "^[?I]|is not a working copy")

	if ($svnFilter) { $_ }
}

filter FilterContainersOnly()
{
	if ($_.PSIsContainer) { $_ }
}

filter FilterNoContainersOnly()
{
	if (! $_.PSIsContainer) { $_ }
}

filter FilterFullName()
{
	$_.FullName
}

filter FilterName()
{
	$_.Name
}

Export-ModuleMember Get-EnhancedChildItem
Set-Alias gciplus Get-EnhancedChildItem
Export-ModuleMember -Alias gciplus

# Test
# cls
# Import-Module CleanCode/EnhancedChildItem -Force
# Get-EnhancedChildItem .
