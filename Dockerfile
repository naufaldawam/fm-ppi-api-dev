# =========================================================
# AuthService.API — Single-stage Docker image (SDK .NET 10)
# Build & run menggunakan image SDK (berisi runtime + compiler)
# =========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

# Salin semua .csproj sesuai folder asli untuk optimasi cache restore
COPY AuthService.API/AuthService.API.csproj AuthService.API/
COPY AuthService.Application/AuthService.Application.csproj AuthService.Application/
COPY AuthService.Domain/AuthService.Domain.csproj AuthService.Domain/
COPY AuthService.Infrastructure/AuthService.Infrastructure.csproj AuthService.Infrastructure/

# Restore dependensi (cache layer saat .csproj tidak berubah)
RUN dotnet restore AuthService.API/AuthService.API.csproj

# Salin seluruh source code
COPY . .

# Publish aplikasi ke /app/publish
RUN dotnet publish AuthService.API/AuthService.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# =========================================================
# Runtime environment
# =========================================================
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

WORKDIR /app/publish
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthService.API.dll"]
