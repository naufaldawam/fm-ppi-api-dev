# Extract
tar -xzf AuthBoilerplate.tar.gz
cd AuthBoilerplate

# Generate + setup
chmod +x CREATE_MY_SERVICE.sh SETUP_FILES.sh
./CREATE_MY_SERVICE.sh
./SETUP_FILES.sh

# Configure
nano ApiService.API/appsettings.json
# → Update: DefaultConnection (your database)
# → Update: SecretKey (PASTE_SAME_SECRET_KEY_AS_AUTHSERVICE)

# Migrate + run
cd ApiService.API

dotnet remove package Microsoft.AspNetCore.OpenApi
dotnet remove package Microsoft.OpenApi

# Clean and restore
dotnet restore --no-cache

# Then migrate
dotnet ef migrations add InitialCreate --project ../ApiService.Infrastructure

dotnet ef database update --project ../ApiService.Infrastructure
dotnet run --launch-profile https
