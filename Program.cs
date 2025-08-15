using HarborMaster.Services;
using HarborMaster.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    await dbService.InitializeDatabaseAsync();
    await dbService.SeedDatabaseAsync();
}

// Define API endpoints
app.MapGet("/", () => "Welcome to Harbor Master API!");

app.MapShipEndpoints();
app.MapDockEndpoints();
app.MapHaulerEndpoints();

app.Run();
