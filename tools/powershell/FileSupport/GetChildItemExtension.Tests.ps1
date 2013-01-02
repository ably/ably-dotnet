#
# ==============================================================
# @ID       $Id: GetChildItemExtension.Tests.ps1 1202 2012-03-27 02:40:19Z ww $
# @created  2011-09-01
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

cls

Set-StrictMode -Version Latest
Import-Module CleanCode\Assertion
Import-Module CleanCode\FileSupport -Force

$global:testdir = "test_dir"
Set-MaxExpressionDisplayLength 75

function Cleanup-Test()
{
	Remove-Item $testdir -Recurse
}

function Go-Home()
{
	$homeDir = Join-Path $env:TEMP "gcip-test-dir"
	if (! (Test-Path $homeDir)) { 
		new-item -path $env:TEMP -name "gcip-test-dir" -type directory -Force | Out-Null
	}
	cd $homeDir
}

function Out-Section ($title)
{
	Write-Host -Foreground Blue "=============="
	Write-Host -Foreground Blue $title
	Write-Host -Foreground Blue "=============="
}


#######################################
Out-Section "Basic"
Go-Home
$result = New-FileTree $testdir\foo1.txt,$testdir\foo2.txt,$testdir\temp1.zip
ls $testdir -Recurse | sort -Property FullName | % { $_.fullname }

# All files, current dir
Assert-Expression (gciplus -Name $testdir) foo1.txt,foo2.txt,temp1.zip

# Filtered list
Assert-Expression (gciplus $testdir -Name -Filter foo*.txt) foo1.txt,foo2.txt
Assert-Expression ($testdir | gciplus -Name  -Filter foo*.txt) foo1.txt,foo2.txt
Assert-Expression (gciplus $testdir -Name  -Filter bar*.txt) $null
cd $testdir
Assert-Expression (gciplus . -Name -Filter foo*.txt) foo1.txt,foo2.txt
Assert-Expression (gciplus -Name -Filter foo*.txt) foo1.txt,foo2.txt

#######################################
Out-Section "With subdirectories"

Go-Home
$result = New-FileTree @"
$testdir\subdir1\s1a1.txt
$testdir\subdir1\s1a2.txt
$testdir\subdir1\s1b1.txt
$testdir\subdir1\s1b2.txt
"@
cd $testdir
ls -Recurse | sort -Property FullName | % { $_.fullname }

# Include needs -Recurse to work!
Assert-Expression (gciplus -Name -Include  foo*.txt) $null

# Exclude does not need -Recurse to work
Assert-Expression (gciplus -Name -Exclude  subdir1,temp1.zip) `
	foo1.txt, foo2.txt

# recurse and INclude
Assert-Expression (gciplus -Name -Recurse -Include foo*.txt,s1a*.txt) `
	s1a1.txt, s1a2.txt, foo1.txt, foo2.txt
	
# recurse and EXclude
Assert-Expression (gciplus -Name -Recurse -Exclude s1b*.txt,temp1.zip) `
	subdir1, s1a1.txt, s1a2.txt, foo1.txt, foo2.txt

# recurse and EXclude and skip files (with -ContainersOnly)
Assert-Expression (gciplus -Name -ContainersOnly -Recurse -Exclude s1b*.txt,temp1.zip) `
	subdir1

# recurse and EXclude and skip directories (with -NoContainersOnly)
Assert-Expression (gciplus -Name -NoContainersOnly -Recurse -Exclude s1b*.txt,temp1.zip) `
	s1a1.txt, s1a2.txt, foo1.txt, foo2.txt

# recurse and EXclude and skip directories (with -Exclude)
Assert-Expression (gciplus -Name -Recurse -Exclude s1b*.txt,temp1.zip,subdir1) `
	s1a1.txt, s1a2.txt, foo1.txt, foo2.txt


#######################################
Out-Section "With multiple subdirectories"
Go-Home
$result = New-FileTree $testdir\subdir2\s2a1.txt,$testdir\subdir2\s2a2.txt
cd $testdir
ls -Recurse | sort -Property FullName | % { $_.fullname }

# recurse and EXclude and skip files (with -ContainersOnly)
Assert-Expression (gciplus -Name -ContainersOnly -Recurse -Exclude s1b*.txt,temp1.zip) `
	"subdir1","subdir2"

# Exclude vs. ExcludeTree
Assert-Expression (gciplus . -Name -Recurse | Sort) `
	foo1.txt,foo2.txt,s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,s2a1.txt,s2a2.txt,subdir1,subdir2,temp1.zip
# Note that Exclude changes search trail so use Sort to normalize
Assert-Expression (gciplus . -Name -Recurse -Exclude subdir1 | Sort) `
	foo1.txt,foo2.txt,s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,s2a1.txt,s2a2.txt,subdir2,temp1.zip
Assert-Expression (gciplus . -Name -Recurse -ExcludeTree subdir1 | Sort) `
	foo1.txt,foo2.txt,s2a1.txt,s2a2.txt,subdir2,temp1.zip
Assert-Expression (gciplus . -Name -Recurse -Exclude subdir* | Sort) `
	foo1.txt,foo2.txt,s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,s2a1.txt,s2a2.txt,temp1.zip
Assert-Expression (gciplus . -Name -Recurse -ExcludeTree subdir* | Sort) `
	foo1.txt,foo2.txt,temp1.zip

# Multiple pipeline inputs
Assert-Expression ("subdir1","subdir2" | gciplus -Name) `
	s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,s2a1.txt,s2a2.txt
Assert-Expression (gciplus "subdir1","subdir2" -Name) `
	s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,s2a1.txt,s2a2.txt
Assert-Expression (gciplus -Path "subdir1","subdir2" -Name) `
	s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,s2a1.txt,s2a2.txt


#######################################
Out-Section "With same-named subdirectories"
Go-Home

$result = New-FileTree @"
$testdir\subdir2\subdir2-child\s2-child-a1.txt
$testdir\subdir2\subdir2-child\s2-child-a2.txt
$testdir\subdir2\subdir2-child\subdir2\x-a1.txt
$testdir\subdir2\subdir2-child\subdir2\x-a2.txt
$testdir\subdir2\subdir2-child\subdir2\x-a3.txt
"@
ls -Recurse | sort -Property FullName | % { $_.fullname }

#######################################

Assert-Expression (gciplus -Name -Recurse -ExcludeTree subdir2 |Sort) `
	foo1.txt,foo2.txt,s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,subdir1,temp1.zip,test_dir
Assert-Expression (gciplus "$testdir\subdir2" -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child
Assert-Expression (gciplus "test_dir\subdir1","test_dir\subdir2" -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child
Assert-Expression (gciplus -Name -Recurse -ExcludeTree subdir* |Sort) `
	foo1.txt,foo2.txt,temp1.zip,test_dir

cd $testdir
Assert-Expression (gciplus -Name -Recurse -ExcludeTree subdir2 |Sort) `
	foo1.txt,foo2.txt,s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,subdir1,temp1.zip
Assert-Expression (gciplus "subdir2" -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child

cd subdir1
Assert-Expression (gciplus -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt
Assert-Expression (gciplus .. -Name -Recurse -ExcludeTree subdir2 |Sort) `
	foo1.txt,foo2.txt,s1a1.txt,s1a2.txt,s1b1.txt,s1b2.txt,subdir1,temp1.zip
Assert-Expression (gciplus "../subdir2" -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child
	
cd ..\subdir2
Assert-Expression (gciplus -Name -Recurse -ExcludeTree subdir2 | Sort) `
	s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child
Assert-Expression (gciplus . -Name -Recurse -ExcludeTree subdir2 | Sort) `
	s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child
Assert-Expression (gciplus (Get-Item .).FullName -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child
Assert-Expression (gciplus "..\..\test_dir\subdir2" -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child
Assert-Expression (gciplus -Name -Recurse -ExcludeTree subdir* |Sort) `
	s2a1.txt,s2a2.txt

cd subdir2-child
Assert-Expression (gciplus -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s2-child-a1.txt,s2-child-a2.txt
Assert-Expression (gciplus .. -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child
Assert-Expression (gciplus "../../subdir2" -Name -Recurse -ExcludeTree subdir2 |Sort) `
	s2a1.txt,s2a2.txt,s2-child-a1.txt,s2-child-a2.txt,subdir2-child

Get-AssertCounts # 39 and 0


#######################################
Go-Home
Cleanup-Test
#######################################

#Write-Host ". . ."
#write-host "Svn test on live data:"
#cd C:\usr\ms\devel\powershell
#gciplus . -FullName -Recurse -Svn
#