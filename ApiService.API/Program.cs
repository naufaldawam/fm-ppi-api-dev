using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Serilog;

using FluentValidation;
using FluentValidation.AspNetCore;

using ApiService.Infrastructure.Persistence;
using ApiService.Infrastructure.Repositories;
using ApiService.Infrastructure.Services;

using ApiService.Application.Services;
using ApiService.Application.Interfaces;
using ApiService.Application.Configurations;

using ApiService.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// LOGGING
// ===================================

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/log-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ===================================
// DATABASE
// ===================================

builder.Services.AddDbContext<ServiceDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IServiceDbContext>(
    p => p.GetRequiredService<ServiceDbContext>());

// ===================================
// JWT AUTHENTICATION
// ===================================

var jwt = builder.Configuration.GetSection("Jwt");

var key = Encoding.UTF8.GetBytes(
    jwt["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(key),

            ClockSkew = TimeSpan.Zero
        };
});

builder.Services.AddAuthorization();

// ===================================
// CURRENT USER
// ===================================

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ===================================
// REPOSITORIES
// ===================================

builder.Services.AddScoped(
    typeof(IRepository<>),
    typeof(Repository<>));

// ===================================
// STORAGE
// ===================================

builder.Services.Configure<StorageConfig>(
    builder.Configuration.GetSection("Storage"));

// ===================================
// SERVICES
// ===================================

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IJabatanService, JabatanService>();
builder.Services.AddScoped<IPekerjaService, PekerjaService>();


// ===================================
// VALIDATION
// ===================================

builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<ApiService.Application.Validators.CreateProductRequestValidator>();

// ===================================
// CONTROLLERS & ROUTING
// ===================================

builder.Services.AddControllers();

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// ===================================
// SWAGGER SERVICES
// ===================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Service API",
            Version = "v1",
            Description =
                "Validated by JWT tokens issued from AuthService"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Paste your JWT token from AuthService here"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

// ===================================
// CORS
// ===================================

builder.Services.AddCors(options =>
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }));

// ===================================
// BUILD APP
// ===================================

var app = builder.Build();

// ===================================
// FORWARDED HEADERS
// ===================================

app.UseForwardedHeaders(
    new ForwardedHeadersOptions
    {
        ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto
    });

// ===================================
// ERROR HANDLING
// ===================================

app.UseMiddleware<ErrorHandlingMiddleware>();

// ===================================
// SWAGGER
// ===================================

// Dockerfile:
// ENV Swagger__Enabled=true
// ENV Swagger__ServerBasePath=/api

var swaggerEnabled =
    builder.Configuration.GetValue<bool>(
        "Swagger:Enabled");

var swaggerBasePath =
    builder.Configuration.GetValue<string>(
        "Swagger:ServerBasePath") ?? string.Empty;

// DEBUG LOG
Log.Information(
    "Swagger configuration: Enabled={SwaggerEnabled}, ServerBasePath={SwaggerBasePath}, Environment={Environment}",
    swaggerEnabled,
    swaggerBasePath,
    builder.Environment.EnvironmentName);

if (true)
{
    Log.Information("Swagger is ENABLED");

    app.UseSwagger(options =>
    {
        options.RouteTemplate =
            "swagger/{documentName}/swagger.json";

        options.PreSerializeFilters.Add(
            (swagger, httpRequest) =>
            {
                if (!string.IsNullOrWhiteSpace(swaggerBasePath))
                {
                    swagger.Servers =
                        new List<OpenApiServer>
                        {
                            new OpenApiServer
                            {
                                Url = swaggerBasePath
                            }
                        };
                }
            });
    });

    app.UseSwaggerUI(options =>
    {
        // External:
        // /api/swagger/index.html
        //
        // Swagger UI requests:
        // /api/swagger/v1/swagger.json
        //
        // NGINX rewrites it to:
        // /swagger/v1/swagger.json

        options.SwaggerEndpoint(
            "v1/swagger.json",
            "Service API v1");

        options.RoutePrefix = "swagger";

        options.DocumentTitle =
            "Service API - Swagger";

        options.DisplayRequestDuration();
    });
}
else
{
    Log.Warning(
        "Swagger is DISABLED because Swagger:Enabled={SwaggerEnabled}",
        swaggerEnabled);
}

// ===================================
// SERILOG REQUEST LOGGING
// ===================================

app.UseSerilogRequestLogging();

// ===================================
// CORS
// ===================================

app.UseCors("AllowAll");

// ===================================
// AUTHENTICATION
// ===================================

app.UseAuthentication();

app.UseAuthorization();

// ===================================
// CONTROLLERS
// ===================================

app.MapControllers();

// ===================================
// RUN
// ===================================

app.Run();