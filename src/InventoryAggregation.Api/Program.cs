using InventoryAggregation.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<InventoryApiOptions>()
    .BindConfiguration(InventoryApiOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
