using Microsoft.EntityFrameworkCore;
using MISOQueryingApp.Client;
using MISOQueryingApp.Repository;
using MISOQueryingApp.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Database");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddHttpClient<IMISOFuelMixClient, MISOFuelMixClient>(
    client =>
    {
        client.BaseAddress = new Uri("https://public-api.misoenergy.org");
        client.Timeout = TimeSpan.FromSeconds(30);
    }
);

builder.Services.AddScoped<IMISOFuelMixIngestionService, MISOFuelMixIngestionService>();

builder.Services.AddHostedService<MISOApiPollingService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();