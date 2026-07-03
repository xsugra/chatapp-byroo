using ChatApp.Data;
using ChatApp.Server.Hubs;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Načítaj .env zo záhlavia repozitára (ak existuje) - hodnoty sa nastavia ako
// skutočné environment premenné, ktoré nižšie prečíta builder.Configuration.
var envDir = Directory.GetCurrentDirectory();
while (envDir is not null && !File.Exists(Path.Combine(envDir, ".env")))
    envDir = Directory.GetParent(envDir)?.FullName;
if (envDir is not null)
    Env.Load(Path.Combine(envDir, ".env"));

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// 2. DbContext - Use only DbContextFactory (not AddDbContext)
// IDbContextFactory is better for SignalR hubs which have transient lifecycle
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContextFactory<ChatDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 3. SignalR
builder.Services.AddSignalR();

// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true));
});

// 5. Controllers
builder.Services.AddControllers();

// 6. Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    });
});

app.UseCors("AllowAll");
app.MapControllers();
app.MapHub<ChatHub>("/chat");

app.Run();