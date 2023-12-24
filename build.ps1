rm -Recurse -Force -ErrorAction Ignore ./src/Fsih/bin/Release
rm -Recurse -Force -ErrorAction Ignore ./src/Fsih/obj/Release
rm -Recurse -Force -ErrorAction Ignore ./src/Fsih.Tests/bin/Release
rm -Recurse -Force -ErrorAction Ignore ./src/Fsih.Tests/obj/Release
rm -Recurse -Force -ErrorAction Ignore ./nupkg
dotnet tool restore
dotnet fantomas .
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release
