#!/bin/bash
echo "===================================================================================="
echo "   Cake Build - Creating ably.io.push.android.*.nupkg and ably.io.push.ios.*.nupkg"
echo "===================================================================================="
echo " "
echo "Warning: Run this script on macOS for iOS package support"
echo " "

if [ $# -eq 0 ]; then
    echo "Provide version number like: package-push-cake.sh 1.2.8"
else
    ./build-cake.sh --target=PushPackage --version=$1
fi
