using dotBento.EntityFramework.Context;
using dotBento.WebApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Just use environment variables
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Get connection string from environment variables
var connectionString = builder.Configuration["DatabaseConnectionString"];
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("DatabaseConnectionString is not configured.");
}

builder.Services.AddDbContextFactory<BotDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();