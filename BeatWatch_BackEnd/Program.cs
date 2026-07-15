using BeatWatch_BackEnd.Configuration;
using BeatWatch_BackEnd.Data;
using BeatWatch_BackEnd.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddOptions<MongoDbSettings>()
    .Bind(builder.Configuration.GetSection("MongoDbSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
    
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ILicenciaService, LicenciaService>();
builder.Services.AddHostedService<MongoDbInitializer>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BeatWatch API v1");
        options.RoutePrefix = "swagger"; // La URL será http://localhost:XXXX/swagger
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
