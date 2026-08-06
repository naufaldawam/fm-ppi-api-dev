# FM (Fleet Management)

## 1. Ringkasan Proyek

### 1.1. Nama Proyek
FM (Fleet Management)

### 1.2. Latar Belakang

Dalam era digitalisasi dan meningkatnya tuntutan kepatuhan terhadap regulasi, perusahaan dituntut untuk mampu menyediakan data yang akurat, transparan, dan real-time, khususnya terkait pelaporan pajak natura. Namun, dalam praktiknya, proses pengumpulan dan pengolahan data tersebut masih sering dilakukan secara manual, tersebar, dan kurang terintegrasi. Hal ini menyebabkan tingginya potensi kesalahan, keterlambatan pelaporan, serta kesulitan dalam melakukan monitoring penggunaan sumber daya perusahaan, terutama pada operasional kendaraan pekerja.

Kebutuhan akan sistem yang terintegrasi menjadi semakin penting, terutama untuk mengelola data operasional kendaraan yang melibatkan banyak pihak, seperti driver dan admin. Saat ini, data penggunaan bahan bakar minyak (BBM) seringkali tidak terdokumentasi dengan baik, baik dari sisi jumlah, waktu pengisian, maupun bukti transaksi. Selain itu, proses validasi data yang masih manual juga menambah beban kerja serta berisiko terhadap ketidaksesuaian data yang berdampak pada pelaporan pajak.

Oleh karena itu, diperlukan sebuah aplikasi atau sistem Fleet Management (FM) yang mampu mengakomodasi kebutuhan tersebut secara menyeluruh. Sistem ini memungkinkan driver untuk melakukan input data BBM secara langsung melalui aplikasi mobile, kapanpun dan dimanapun, lengkap dengan detail kendaraan serta bukti nota pengisian. Selanjutnya, data tersebut akan melalui proses validasi oleh admin untuk memastikan kesesuaian antara input dan bukti transaksi sebelum disetujui.

Dengan data yang telah tervalidasi, sistem dapat secara otomatis mengolah dan menghasilkan laporan terkait pajak natura, termasuk komponen pendukung seperti penggunaan e-toll dan biaya lembur driver. Melalui sistem ini, perusahaan diharapkan dapat memperoleh data yang cepat, akurat, dan terintegrasi, sehingga mendukung pengambilan keputusan yang lebih baik serta memastikan kepatuhan terhadap regulasi yang berlaku.

### 1.3. Tujuan Bisnis & Manfaat

- Membangun sistem Fleet Management (FM) yang terintegrasi untuk pengelolaan data operasional kendaraan secara digital dan terpusat.
- Mempermudah proses pencatatan penggunaan BBM oleh driver melalui aplikasi mobile secara real-time, lengkap dengan bukti transaksi.
- Menyediakan mekanisme validasi data yang akurat melalui proses verifikasi oleh admin untuk memastikan kesesuaian data input dan dokumen pendukung.
- Mengotomatisasi penyajian laporan pajak natura berdasarkan data operasional yang telah tervalidasi.
- Meningkatkan akurasi, transparansi, dan kecepatan dalam penyediaan data untuk kebutuhan pelaporan dan pengambilan keputusan.
- Mendukung kepatuhan perusahaan terhadap regulasi terkait pajak natura.

---

## 2. Setup & Instalasi

> Catatan: langkah di bawah ini mengikuti struktur boilerplate. Ganti setiap kemunculan `ApiService` dengan nama service yang sesuai (misalnya `FleetService`) sebelum dijalankan, agar konsisten dengan penamaan proyek FM.

```bash
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
```

### Project File Reference (`ApiService.API.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- NOTE: No Microsoft.AspNetCore.OpenApi - conflicts with Swashbuckle -->
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.1" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
    <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ApiService.Application\ApiService.Application.csproj" />
    <ProjectReference Include="..\ApiService.Infrastructure\ApiService.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```