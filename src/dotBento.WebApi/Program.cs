using dotBento.EntityFramework.Context;
using dotBento.WebApi;
using Microsoft.EntityFrameworkCore;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("configs/config.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var configuration = configBuilder.Build();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ApplicationName = typeof(Program).Assembly.FullName
});

// Override default config with your own
builder.Configuration.AddConfiguration(configuration);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration["DatabaseConnectionString"];
builder.Services.AddDbContextFactory<BotDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();