using System.Text;
using ChristianJewelry.Domain.Interfaces;
using ChristianJewelry.Infrastructure.Persistence;
using ChristianJewelry.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? $"Host={Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost"};" +
       $"Port={Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432"};" +
       $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "christian_jewelry"};" +
       $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "cj_user"};" +
       $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "cj_pass"};";

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connectionString)
       .UseSnakeCaseNamingConvention());

// ── Repositories / Unit of Work ───────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

// ── JWT ───────────────────────────────────────────────────────
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
            ClockSkew                = TimeSpan.Zero,
            RoleClaimType            = System.Security.Claims.ClaimTypes.Role
        };
    });

// ── Authorization — підтримка і "admin" і "Admin" ─────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("admin") || ctx.User.IsInRole("Admin")));
});

// ── CORS (один раз!) ─────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader()));

// ── Session ───────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout        = TimeSpan.FromDays(7);
    opt.Cookie.HttpOnly    = true;
    opt.Cookie.IsEssential = true;
});

// ── Controllers + JSON snake_case ─────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy   = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ── Swagger ───────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Christian Jewelry API",
        Version     = "v1.0",
        Description = "REST API для інтернет-магазину християнських прикрас"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        Description  = "Введіть JWT токен"
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

// ─────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Перевірка з'єднання з БД ─────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.CanConnectAsync();
        app.Logger.LogInformation("✅ Database connection successful");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("⚠️ Database not ready: {Message}", ex.Message);
    }
}

// ── Middleware (порядок ВАЖЛИВИЙ!) ────────────────────────────
app.UseCors("AllowAll");      // 1. CORS — завжди першим

app.UseSwagger();             // 2. Swagger JSON
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Christian Jewelry API v1");
    c.RoutePrefix   = "swagger";
    c.DocumentTitle = "Christian Jewelry API";
    c.EnableDeepLinking();
    c.DisplayRequestDuration();
});

app.UseDefaultFiles();        // 3. index.html з wwwroot
app.UseStaticFiles();         // 4. статичні файли (css, js, img)

app.UseSession();             // 5. сесії (кошик гостей)
app.UseAuthentication();      // 6. JWT перевірка токену
app.UseAuthorization();       // 7. перевірка ролей

// ── Endpoints ─────────────────────────────────────────────────
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }))
   .AllowAnonymous();

app.MapControllers();

app.Logger.LogInformation("🚀 Сайт: http://localhost:5000  |  API: http://localhost:5000/swagger");

app.Run();