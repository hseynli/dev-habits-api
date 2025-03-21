using API.Database;
using API.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options => 
              options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
              npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application))
                                            .UseSnakeCaseNamingConvention());

builder.Services.AddOpenTelemetry()
                .ConfigureResource(resurce => resurce.AddService(builder.Environment.ApplicationName))
                .WithTracing(tracing => tracing.AddHttpClientInstrumentation()
                                               .AddAspNetCoreInstrumentation())
                .WithMetrics(metrics => metrics.AddHttpClientInstrumentation()
                                               .AddAspNetCoreInstrumentation()
                                               .AddRuntimeInstrumentation())
                .UseOtlpExporter();

builder.Logging.AddOpenTelemetry(options => 
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    //await app.ApplyMigrationAsync();
}

app.UseHttpsRedirection();

app.MapControllers();

await app.RunAsync();