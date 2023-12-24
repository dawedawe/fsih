#!/bin/sh

rm -rf ./src/Fsih/bin/Release ./src/Fsih/obj/Release ./src/Fsih.Tests/bin/Release ./src/Fsih.Tests/obj/Release ./nupkg
dotnet tool restore
dotnet fantomas .
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release
