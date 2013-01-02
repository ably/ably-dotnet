#
# ==============================================================
# @ID       $Id: FileTree.ps1 1206 2012-04-12 21:16:29Z ww $
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

Set-StrictMode -Version Latest

<#

.SYNOPSIS
Generates a file tree skeleton (i.e. real directories and files,
but the files are empty) from a list of paths.

.DESCRIPTION
New-FileTree generates a set of directories and empty files
from a list of paths you provide.
Any intermediate directories are created automatically;
you do not need to explicitly create them.
To explicitly create a directory, a path must end in a trailing slash.
Either virgules (/) or backslashes (\) may be used in paths.

.PARAMETER Path
Specifies a path to one or more locations.
This may be provided as an array of path strings
or as a 'here string', with one path per line. (See the examples.)

.INPUTS
System.String. You can pipe one or more path strings to New-FileTree.

.OUTPUTS
Array of System.IO.FileInfo or System.IO.DirectoryInfo objects.

.EXAMPLE
PS> $result = New-FileTree $testdir\foo1.txt,$testdir\foo2.txt,$testdir\temp1.zip

Creates the path specified in $testdir if it does not yet exist
plus the three files specified as a list on one line.

.EXAMPLE
PS> 
$result = New-FileTree @"
$testdir\subdir1\s1a1.txt
$testdir\subdir1\s1a2.txt
$testdir\subdir2\s2b1.txt
$testdir\subdir2\s2b2.txt
"@

Creates the files specified in the here list (as well as any
intermediate paths needed).

.EXAMPLE
PS> 
$result = New-FileTree @"
foo\bar\abc.txt
foo\bar\def.txt
foo\gollum\stuff1.txt

foo\other dir\
foo/other2/
#foo/commented-out/
"@

Creates the files and directories specified in the here list, ignoring those commented out (#)
and ignoring blank lines. Directories are differentiated by the trailing
slash.

.NOTES
This function is part of the CleanCode toolbox
from http://cleancode.sourceforge.net/.

Since CleanCode 1.1.03.

#>

function New-FileTree ([String[]]$Path)
{
	# if 'here-string' split into list
	if ($Path.count -eq 1 -and $Path -like "*`r`n*") { $Path = $Path -split "`r`n" }
	
	$result = @()
	$Path | % {
		if ($_ -notmatch "^\s*#" -and $_ -notmatch "^\s*$")
		{
			$itemType = ?: {$_ -match "/$|\\$"} {"directory"} {"file"}
			$result += New-Item $_ -ItemType $itemType -Force
			Write-Verbose $_
		}
	}
	$result
}

Export-ModuleMember New-FileTree
