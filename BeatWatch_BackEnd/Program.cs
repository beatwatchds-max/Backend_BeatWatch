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
    });

// --- CONFIGURACIÓN AGRUPADA DE RATE LIMITING ---
builder.Services.AddRateLimiter(options =>
{
    // 1. Respuesta por defecto cuando alguien excede un límite
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 2. Política existente: Login Web
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    // 3. Política existente: Recuperación de contraseña
    options.AddPolicy("password-recovery", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));

    // 4. NUEVA Política: Login Móvil (HU4.4)
    options.AddPolicy("LoginMovilPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// --- INYECCIÓN DE DEPENDENCIAS ---
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient<ICaptchaVerifier, RecaptchaVerifier>(client => client.BaseAddress = new Uri("https://www.google.com/"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ILicenciaService, LicenciaService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<AutenticacionService>();
builder.Services.AddScoped<PacienteService>();
builder.Services.AddHostedService<MongoDbInitializer>();

builder.Services.AddControllers();

// --- CONFIGURACIÓN DE SWAGGER ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BeatWatch API", Version = "v1" });

    // Configuración para el botón Authorize
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

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();