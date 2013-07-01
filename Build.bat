@echo off

if DEFINED  vcvarsallCalled goto START
if exist "C:\Program Files\Microsoft Visual Studio 11.0\VC\vcvarsall.bat" call "C:\Program Files\Microsoft Visual Studio 11.0\VC\vcvarsall.bat"
if exist "C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC\vcvarsall.bat" call "C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC\vcvarsall.bat"
set vcvarsallCalled=true

:START

msbuild /p:Configuration=Release .\Prejector\Prejector.csproj

.\.nuget\NuGet.exe pack prejector.nuspec -OutputDirectory .\