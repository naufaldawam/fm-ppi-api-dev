FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ApiService.slnx ./
COPY ApiService.API/ApiService.API.csproj ApiService.API/
COPY ApiService.Application/ApiService.Application.csproj ApiService.Application/
COPY ApiService.Domain/ApiService.Domain.csproj ApiService.Domain/
COPY ApiService.Infrastructure/ApiService.Infrastructure.csproj ApiService.Infrastructure/

RUN dotnet restore ApiService.API/ApiService.API.csproj

COPY . .

RUN dotnet publish ApiService.API/ApiService.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ApiService.API.dll"]