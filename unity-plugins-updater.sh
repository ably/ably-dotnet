if [ $# -eq 0 ]
then
  echo -n "Provide latest version number like plugins-updater.sh 1.2.8"
else
	dotnet fake run build.fsx -t Build.NetStandard
	cp src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.dll unity/Assets/Ably/Plugins
	cp src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.pdb unity/Assets/Ably/Plugins 
	cp src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.DeltaCodec.dll unity/Assets/Ably/Plugins
	cp src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.DeltaCodec.pdb unity/Assets/Ably/Plugins
	echo $1 > unity/Assets/Ably/version.txt
fi