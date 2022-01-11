#! /bin/bash

dotnet tool restore

dotnet fake run build.fsx -t PushPackage -v $1