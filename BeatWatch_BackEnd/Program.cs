using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.infrescture;
using BeatWatch_BackEnd.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using MongoDB.Driver;
using System.IO;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// --- CONFIGURACIÓN DE OPCIONES ---
builder.Services.AddOptions<MongoDbSettings>()
    .Bind(builder.Configuration.GetSection("MongoDbSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("JwtSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RecaptchaSettings>()
    .Bind(builder.Configuration.GetSection("RecaptchaSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection("EmailSettings"))
    .ValidateDataAnnotations();

// --- 1. CONFIGURACIÓN DE CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", // React / Vite (puerto por defecto)
                "http://localhost:5174", // React / Vite (el puerto que usa tu compañero)
                "http://localhost:3000",  // React clásico / Next.js
                "https://frontend-beatwatch-857q.onrender.com",
                "https://beatwatch-frontend.onrender.com"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Permite envío de cookies/encabezados de autenticación si los usan
    });
});

// --- CONFIGURACIÓN DE JWT ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("La configuracion JWT es obligatoria.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey ?? string.Empty));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                // Only mobile tokens participate in the one-device session policy.
                if (context.Principal?.FindFirst("tipoSesion")?.Value != "movil") return;

                var usuarioId = context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.Principal.FindFirst("sub")?.Value;
                var sesionId = context.Principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(sesionId))
                {
                    context.Fail("El token no contiene una sesión móvil válida.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<MongoDbContext>();
                var usuario = await db.Usuarios.Find(u => u.Id == usuarioId && u.SesionActiva && u.UltimaSesionId == sesionId).FirstOrDefaultAsync();
                if (usuario is null) context.Fail("La sesión ha sido cerrada o revocada.");
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// --- CONFIGURACIÓN AGRUPADA DE RATE LIMITING ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    options.AddPolicy("password-recovery", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));

    options.AddPolicy("LoginMovilPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
             _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    options.AddPolicy("device-pairing", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
             _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    options.AddPolicy("license-activation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
             _ => new FixedWindowRateLimiterOptions { PermitLimit = 2, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));

    options.AddPolicy("notification-test", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// --- INYECCIÓN DE DEPENDENCIAS ---
builder.Services.AddSingleton<MongoDbContext>();
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("BeatWatch");
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient<ICaptchaVerifier, RecaptchaVerifier>(client => client.BaseAddress = new Uri("https://www.google.com/"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ILicenciaService, LicenciaService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IDispositivoService, DispositivoService>();
builder.Services.AddScoped<AutenticacionService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IPacienteAccessService, PacienteAccessService>();
builder.Services.AddScoped<ISaludService, SaludService>();
builder.Services.AddHostedService<MongoDbInitializer>();
builder.Services.AddScoped<IEstadisticaService, EstadisticaService>();
builder.Services.AddScoped<IMedicionService, MedicionService>();
builder.Services.AddSingleton<IFcmNotificationService, FcmNotificationService>();
builder.Services.AddScoped<IAlertaService, AlertaService>();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// --- CONFIGURACIÓN DE SWAGGER ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BeatWatch API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autorización JWT usando el esquema Bearer. \r\n\r\n Ingresa 'Bearer' [espacio] y luego tu token en el campo de texto.\r\n\r\nEjemplo: \"Bearer eyJhbGciOiJIUzI1Ni...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// ==========================================
// CONSTRUCCIÓN DE LA APLICACIÓN
// ==========================================
var app = builder.Build();

// --- CONFIGURACIÓN DEL PIPELINE HTTP ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BeatWatch API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none';";
    await next();
});

// --- 2. USAR CORS (DEBE IR ANTES DE AUTHENTICATION Y AUTHORIZATION) ---
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program;
