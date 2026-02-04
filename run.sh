#!/bin/bash
cd SmartGridApp/SmartGridApp
echo "Restoring packages..."
dotnet restore SmartGridApp.Manual.csproj
echo "Running SmartGridApp (Manual)..."
dotnet run --project SmartGridApp.Manual.csproj
