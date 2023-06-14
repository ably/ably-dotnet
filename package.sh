#! /bin/bash
echo "======================================================"
echo "       Script for building ably.io.*.nupkg            "
echo "======================================================"
echo " "
if [ $# -eq 0 ]
then
    echo "Provide latest version number like package.sh 1.2.8"
else
    ./build.sh Package -v $1
fi
