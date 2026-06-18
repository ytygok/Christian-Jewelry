using System.Text;
using ChristianJewelry.Domain.Interfaces;
using ChristianJewelry.Infrastructure.Persistence;
using ChristianJewelry.Infrastructure.Persistence.Repositories;
using ChristianJewelry.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── Database
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? $"Host={Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost"};" +
       $"Port={Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432"};" +
       $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "christian_jewelry"};" +
       $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "cj_user"};" +
       $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "cj_pass"};";

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connectionString));

// ── Repositories / Unit of Work 
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Services 
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

// ── JWT Auth
var jwtSecret   = builder.Configuration["Jwt:Secret"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? "christian-jewelry-super-secret-key-change-in-production-2024!";
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "christian-jewelry";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "christian-jewelry-client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = true,
            ValidIssuer              = jwtIssuer,
            ValidateAudience         = true,
            ValidAudience            = jwtAudience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── CORS 
builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader()));

// ── Session (cart для гостей
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout      = TimeSpan.FromDays(7);
    opt.Cookie.HttpOnly  = true;
    opt.Cookie.IsEssential = true;
});

// ── Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ── Swagger 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Christian Jewelry API",
        Version     = "v1.0",
        Description = "REST API для інтернет-магазину християнських прикрас",
        Contact     = new OpenApiContact { Name = "Christian Jewelry", Email = "admin@christian-jewelry.ua" }
    });

    // JWT у Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        Description  = "Введіть JWT токен (без Bearer prefix)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Auto-migrate on startup 
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // EF не використовується для міграцій (є SQL-скрипти),
        // але перевіряємо з'єднання
        await db.Database.CanConnectAsync();
        app.Logger.LogInformation("✅ Database connection successful");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("⚠️ Database not ready yet: {Message}", ex.Message);
    }
}

// ── Middleware pipeline 
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Christian Jewelry API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Christian Jewelry API";
        c.EnableDeepLinking();
        c.DisplayRequestDuration();
    });
}

app.UseSession();
app.UseAuthentication();
app.UseCors("AllowAll");
app.UseAuthorization();

// ── Health check 
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }))
   .WithTags("Health")
   .AllowAnonymous();

app.MapControllers();

// ── Static files (фронтенд)
app.UseDefaultFiles();
app.UseStaticFiles();

app.Logger.LogInformation("🚀 Christian Jewelry API запущено на {Url}", 
    string.Join(", ", app.Urls.DefaultIfEmpty("http://localhost:5000")));

app.Run();
