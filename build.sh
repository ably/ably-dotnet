#! /bin/bash
dotnet tool restore

if [ $# -eq 0 ]
then
    dotnet run --project ./build-script/build-script.fsproj
else
    dotnet run --project ./build-script/build-script.fsproj -- -t $@
fi
