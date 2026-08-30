@echo off
title Gambol Server
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5215
cd /d "D:\dev\amble\gambol"
echo Starting Gambol.Server on http://localhost:5215 ...
dotnet run --project src\Server -c Debug --no-launch-profile
if errorlevel 1 pause
