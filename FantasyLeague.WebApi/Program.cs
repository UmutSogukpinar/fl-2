using FantasyLeague.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var dbSettings = builder.Configuration.GetSection("DbSettings");
var connectionString = $"Host={dbSettings["Host"]};" +
                       $"Port={dbSettings["Port"]};" +
                       $"Database={dbSettings["Database"]};" +
                       $"Username={dbSettings["Username"]};" +
                       $"Password={dbSettings["Password"]};";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseRouting();
app.MapGet("/health", () => "Fantasy League API is running!");

app.Run();
