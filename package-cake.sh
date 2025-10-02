#!/bin/bash
echo "======================================================"
echo "   Cake Build - Creating ably.io.*.nupkg            "
echo "======================================================"
echo " "
if [ $# -eq 0 ]; then
    echo "Provide version number like: package-cake.sh 1.2.8"
else
    ./build-cake.sh --target=Package --version=$1
fi
