#!/bin/bash
echo "======================================================"
echo "   Cake Build - Creating ably.io.*.nupkg            "
echo "======================================================"
echo " "
if [ $# -eq 0 ]; then
    echo "Provide version number like: package-cake.sh 1.2.8"
else
    dotnet tool restore
    dotnet cake cake-build/build.cake -- --target=Package --version="$1"
fi
