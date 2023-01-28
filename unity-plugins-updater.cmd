@echo off
if "%~1"=="" (echo "Provide latest version number like unity-plugins-updater.cmd 1.2.8") else (
	dotnet fake run build.fsx -t Build.NetStandard
	copy src\IO.Ably.NETStandard20\bin\Release\netstandard2.0\IO.Ably.dll unity\Assets\Ably\Plugins
	copy src\IO.Ably.NETStandard20\bin\Release\netstandard2.0\IO.Ably.pdb unity\Assets\Ably\Plugins 
	copy src\IO.Ably.NETStandard20\bin\Release\netstandard2.0\IO.Ably.DeltaCodec.dll unity\Assets\Ably\Plugins
	copy src\IO.Ably.NETStandard20\bin\Release\netstandard2.0\IO.Ably.DeltaCodec.pdb unity\Assets\Ably\Plugins
	echo %~1 > unity\Assets\Ably\version.txt
)
