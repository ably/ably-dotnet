#! /bin/bash
echo "===================================================================================="
echo "       Script for building ably.io.push.android.*.nupkg and ably.io.push.ios.*.nupkg"
echo "===================================================================================="
echo " "
echo "Warning : you should run this script on mac since it also needs to build package for iOS"
echo " "
if [ $# -eq 0 ]
then
	echo "Provide latest version number like package-push.sh 1.2.8"
else
	dotnet tool restore
	dotnet fake run build.fsx -t PushPackage -v $1
fi
