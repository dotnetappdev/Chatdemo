using Microsoft.EntityFrameworkCore;
using ChatDemo.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();  // Built-in .NET 10 OpenAPI

// Database: SQL Server (primary) with SQLite fallback for development
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Server="))
{
    builder.Services.AddDbContext<ChatDbContext>(opt =>
        opt.UseSqlServer(connectionString));
}
else
{
    // SQLite for local dev / demo when no SQL Server is configured
    builder.Services.AddDbContext<ChatDbContext>(opt =>
        opt.UseSqlite(connectionString ?? "Data Source=chatdemo.db"));
}

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── App ────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();   // Exposes /openapi/v1.json
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();


