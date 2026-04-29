#!/bin/bash

# =============================================================================
# Setup Files — Copies all implementation files to correct locations
# =============================================================================

PROJECT_NAME="MyService"
TEMPLATES="./templates"

echo "============================================="
echo "📋 Setting up implementation files..."
echo "============================================="

if [ ! -d "$TEMPLATES" ]; then
    echo "❌ templates/ folder not found!"
    exit 1
fi

# DOMAIN
echo "📄 Domain..."
mkdir -p ${PROJECT_NAME}.Domain/Entities
cp ${TEMPLATES}/Domain/*.cs ${PROJECT_NAME}.Domain/Entities/
rm -f ${PROJECT_NAME}.Domain/Class1.cs

# APPLICATION
echo "📄 Application..."
mkdir -p ${PROJECT_NAME}.Application/Interfaces
mkdir -p ${PROJECT_NAME}.Application/DTOs
mkdir -p ${PROJECT_NAME}.Application/Services
mkdir -p ${PROJECT_NAME}.Application/Validators
cp ${TEMPLATES}/Application/Interfaces/*.cs  ${PROJECT_NAME}.Application/Interfaces/
cp ${TEMPLATES}/Application/DTOs/*.cs        ${PROJECT_NAME}.Application/DTOs/
cp ${TEMPLATES}/Application/Services/*.cs    ${PROJECT_NAME}.Application/Services/
cp ${TEMPLATES}/Application/Validators/*.cs  ${PROJECT_NAME}.Application/Validators/
rm -f ${PROJECT_NAME}.Application/Class1.cs

# INFRASTRUCTURE
echo "📄 Infrastructure..."
mkdir -p ${PROJECT_NAME}.Infrastructure/Persistence
mkdir -p ${PROJECT_NAME}.Infrastructure/Repositories
mkdir -p ${PROJECT_NAME}.Infrastructure/Services
cp ${TEMPLATES}/Infrastructure/Persistence/*.cs   ${PROJECT_NAME}.Infrastructure/Persistence/
cp ${TEMPLATES}/Infrastructure/Repositories/*.cs  ${PROJECT_NAME}.Infrastructure/Repositories/
cp ${TEMPLATES}/Infrastructure/Services/*.cs      ${PROJECT_NAME}.Infrastructure/Services/
rm -f ${PROJECT_NAME}.Infrastructure/Class1.cs

# API
echo "📄 API..."
mkdir -p ${PROJECT_NAME}.API/Controllers
mkdir -p ${PROJECT_NAME}.API/Middleware
mkdir -p ${PROJECT_NAME}.API/Filters
cp ${TEMPLATES}/API/Controllers/*.cs   ${PROJECT_NAME}.API/Controllers/
cp ${TEMPLATES}/API/Middleware/*.cs    ${PROJECT_NAME}.API/Middleware/
cp ${TEMPLATES}/API/Filters/*.cs      ${PROJECT_NAME}.API/Filters/
cp ${TEMPLATES}/API/Program.cs        ${PROJECT_NAME}.API/Program.cs
cp ${TEMPLATES}/API/appsettings.json  ${PROJECT_NAME}.API/appsettings.json
cp ${TEMPLATES}/API/MyService.API.csproj ${PROJECT_NAME}.API/${PROJECT_NAME}.API.csproj

echo ""
echo "✅ All files set up!"
echo ""
echo "============================================="
echo "🎯 NEXT STEPS:"
echo "============================================="
echo ""
echo "1. Edit appsettings.json:"
echo "   → ConnectionStrings:DefaultConnection"
echo "   → Jwt:SecretKey  ← MUST MATCH AuthService's secret key"
echo ""
echo "2. Run migrations:"
echo "   cd ${PROJECT_NAME}.API"
echo "   dotnet ef migrations add InitialCreate --project ../${PROJECT_NAME}.Infrastructure"
echo "   dotnet ef database update --project ../${PROJECT_NAME}.Infrastructure"
echo ""
echo "3. Run the service:"
echo "   dotnet run --urls \"http://localhost:5001\""
echo "   (AuthService runs on 5000, this on 5001)"
echo ""
echo "4. Open Swagger:"
echo "   http://localhost:5001/swagger"
echo ""
echo "5. Login on AuthService, copy accessToken, paste in Swagger → Authorize"
echo "============================================="
