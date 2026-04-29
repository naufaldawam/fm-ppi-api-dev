#!/bin/bash

# =============================================================================
# Service API Generator
# Pairs with AuthService — validates JWT tokens issued by AuthService.
# =============================================================================

PROJECT_NAME="MyService"

echo "============================================="
echo "🚀 Service API Generator"
echo "============================================="

# Create solution
dotnet new sln -n $PROJECT_NAME

# Create projects
echo "📁 Creating projects..."
dotnet new classlib -n ${PROJECT_NAME}.Domain       -o ${PROJECT_NAME}.Domain       -f net10.0
dotnet new classlib -n ${PROJECT_NAME}.Application  -o ${PROJECT_NAME}.Application  -f net10.0
dotnet new classlib -n ${PROJECT_NAME}.Infrastructure -o ${PROJECT_NAME}.Infrastructure -f net10.0
dotnet new webapi   -n ${PROJECT_NAME}.API          -o ${PROJECT_NAME}.API          -f net10.0

# Add to solution
dotnet sln add ${PROJECT_NAME}.Domain/${PROJECT_NAME}.Domain.csproj
dotnet sln add ${PROJECT_NAME}.Application/${PROJECT_NAME}.Application.csproj
dotnet sln add ${PROJECT_NAME}.Infrastructure/${PROJECT_NAME}.Infrastructure.csproj
dotnet sln add ${PROJECT_NAME}.API/${PROJECT_NAME}.API.csproj

# Project references
echo "🔗 Setting up references..."
cd ${PROJECT_NAME}.Application
dotnet add reference ../${PROJECT_NAME}.Domain/${PROJECT_NAME}.Domain.csproj
cd ..

cd ${PROJECT_NAME}.Infrastructure
dotnet add reference ../${PROJECT_NAME}.Domain/${PROJECT_NAME}.Domain.csproj
dotnet add reference ../${PROJECT_NAME}.Application/${PROJECT_NAME}.Application.csproj
cd ..

cd ${PROJECT_NAME}.API
dotnet add reference ../${PROJECT_NAME}.Application/${PROJECT_NAME}.Application.csproj
dotnet add reference ../${PROJECT_NAME}.Infrastructure/${PROJECT_NAME}.Infrastructure.csproj

# FIX: Remove conflicting package auto-added by dotnet new webapi
dotnet remove package Microsoft.AspNetCore.OpenApi 2>/dev/null || true
cd ..

# Install packages
echo "📦 Installing packages..."

cd ${PROJECT_NAME}.Application
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
cd ..

cd ${PROJECT_NAME}.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.AspNetCore.Http.Abstractions
cd ..

cd ${PROJECT_NAME}.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.OpenApi --version 1.2.3
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package FluentValidation.AspNetCore
cd ..

echo ""
echo "✅ Project structure created!"
echo "📋 Now run: ./SETUP_FILES.sh"
echo "============================================="
