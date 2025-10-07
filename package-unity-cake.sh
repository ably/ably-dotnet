#!/bin/bash
echo "======================================================"
echo "   Cake Build - Creating ably.io.*.unitypackage     "
echo "======================================================"
echo " "
if [ $# -eq 0 ]; then
    echo "Provide version number like: package-unity-cake.sh 1.2.8"
else
    ./build-cake.sh --target=UnityPackage --version=$1
fi
