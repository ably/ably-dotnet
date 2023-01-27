#! /bin/bash
dotnet tool restore

if [ $# -eq 0 ]
then
    dotnet fake run build.fsx
else
    dotnet fake run build.fsx -t $@
fi
