using MatchMovie.Controllers;
using MatchMovie.Hubs;
using MatchMovie.Infrastructure.Persistence.Redis;
using MatchMovie.Interfaces;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCorsConfiguration();
builder.Services.AddRedisConfiguration(builder.Configuration);
builder.Services.AddSignalRConfiguration();
builder.Services.AddServices();

builder.Services.AddCustomHealthChecks(builder.Configuration);

builder.Services.AddSingleton<IConnectionMapping, RedisConnectionMapping>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMetricServer();
app.UseHttpMetrics();

app.UseCors();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.UseCustomHealthChecks();

app.MapHub<MatchHub>("/matchhub");

app.Run();
