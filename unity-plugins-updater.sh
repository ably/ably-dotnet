if [ $# -eq 0 ]
then
  echo "Provide latest version number like unity-plugins-updater.sh 1.2.8"
else
	dotnet tool restore
	dotnet cake cake-build/build.cake -- --target=Build.NetStandard --define=UNITY_PACKAGE
	cp src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.dll unity/Assets/Ably/Plugins
	cp src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.pdb unity/Assets/Ably/Plugins
	cp src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.DeltaCodec.dll unity/Assets/Ably/Plugins
	cp src/IO.Ably.NETStandard20/bin/Release/netstandard2.0/IO.Ably.DeltaCodec.pdb unity/Assets/Ably/Plugins
	echo $1 > unity/Assets/Ably/version.txt
fi
