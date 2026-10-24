@echo off
cd /d "%~dp0"
echo Building Ashenbound... > build_log.txt
dotnet build TBRPG.csproj -nologo -clp:NoSummary;ErrorsOnly >> build_log.txt 2>&1
echo EXIT %ERRORLEVEL% >> build_log.txt
