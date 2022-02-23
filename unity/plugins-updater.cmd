@echo off
if "%~1"=="" (echo "Provide latest version number like plugins-updater.cmd 1.2.8") else (
	dotnet fake run ..\build.fsx -t Build.NetStandard
	copy ..\src\IO.Ably.NETStandard20\bin\Release\netstandard2.0\IO.Ably.dll Assets\Ably\Plugins
	copy ..\src\IO.Ably.NETStandard20\bin\Release\netstandard2.0\IO.Ably.pdb Assets\Ably\Plugins 
	copy ..\src\IO.Ably.NETStandard20\bin\Release\netstandard2.0\IO.Ably.DeltaCodec.dll Assets\Ably\Plugins
	copy ..\src\IO.Ably.NETStandard20\bin\Release\netstandard2.0\IO.Ably.DeltaCodec.pdb Assets\Ably\Plugins
	echo %~1 > Assets\Ably\version.txt
)