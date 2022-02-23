if [ $# -eq 0 ]
then
  echo -n "Provide latest version number like plugins-updator.cmd 1.2.8"
else
	dotnet fake run ../build.fsx -t Build.NetStandard
	cp ../src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.dll Assets/Ably/Plugins
	cp ../src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.pdb Assets/Ably/Plugins 
	cp ../src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.DeltaCodec.dll Assets/Ably/Plugins
	cp ../src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.DeltaCodec.pdb Assets/Ably/Plugins
	echo $1 > unity/Assets/Ably/version.txt
fi