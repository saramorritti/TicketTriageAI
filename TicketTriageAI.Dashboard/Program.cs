using Microsoft.Azure.Cosmos;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Dashboard.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Options Cosmos (riuso del Core)
builder.Services
    .AddOptions<CosmosOptions>()
    .Bind(builder.Configuration.GetSection("Cosmos"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// CosmosClient con env/user-secrets key "CosmosDbConnection"
builder.Services.AddSingleton(_ =>
{
    var cs = builder.Configuration["CosmosDbConnection"];
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Missing CosmosDbConnection (set in User Secrets or env vars).");

    return new CosmosClient(cs);
});

builder.Services.AddScoped<ITicketReadRepository, CosmosTicketReadRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        service = "TicketTriageAI.Dashboard",
        time = DateTime.UtcNow
    });
});

app.Run();
