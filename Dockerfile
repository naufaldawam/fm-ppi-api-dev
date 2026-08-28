FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# COPY ApiService.slnx ./
# COPY ApiService.API/ApiService.API.csproj ApiService.API/
# COPY ApiService.Application/ApiService.Application.csproj ApiService.Application/
# COPY ApiService.Domain/ApiService.Domain.csproj ApiService.Domain/
# COPY ApiService.Infrastructure/ApiService.Infrastructure.csproj ApiService.Infrastructure/

RUN dotnet restore ApiService.API/ApiService.API.csproj

COPY . .

RUN dotnet publish ApiService.API/ApiService.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore
    
# ==========================
# Runtime Stage
# ==========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

# Dev/staging server: expose Swagger UI without flipping ASPNETCORE_ENVIRONMENT
# to Development (which would re-open CORS/TLS hardening). Strip this before any
# real production promotion.
ENV Swagger__Enabled=true
# The ingress serves the API under /api (and strips it before the pod). Advertise
# that prefix in the OpenAPI doc so Swagger "Try it out" hits /api/... not the host
# root (which is the frontend). Local runs leave this empty.
ENV Swagger__ServerBasePath=/api

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ApiService.API.dll"]