#!/bin/bash
dotnet tool restore

# Pass all arguments directly to Cake
dotnet cake cake-build/build.cake -- "$@"
