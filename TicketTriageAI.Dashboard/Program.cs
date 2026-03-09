using Microsoft.Azure.Cosmos;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Dashboard.Options;
using TicketTriageAI.Dashboard.Repositories;
using TicketTriageAI.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddProblemDetails();

// Options Cosmos (riuso del Core)
builder.Services
    .AddOptions<CosmosOptions>()
    .Bind(builder.Configuration.GetSection("Cosmos"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<IngestApiOptions>()
    .Bind(builder.Configuration.GetSection("IngestApi"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<ITicketIngestClient, TicketIngestClient>();

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
    app.UseHsts();

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var accept = context.Request.Headers.Accept.ToString();

            // Browser -> pagina Razor
            if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/Error");
                return;
            }

            // Client non-HTML -> ProblemDetails
            var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            var ex = feature?.Error;

            await Results.Problem(
                title: "Unhandled exception",
                detail: ex?.Message,
                statusCode: StatusCodes.Status500InternalServerError
            ).ExecuteAsync(context);
        });
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseStatusCodePages(async context =>
{
    var http = context.HttpContext;
    var path = http.Request.Path.Value ?? "";

    if (path.StartsWith("/Error", StringComparison.OrdinalIgnoreCase))
        return;

    var accept = http.Request.Headers.Accept.ToString();
    var statusCode = http.Response.StatusCode;

    if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
    {
        http.Response.Redirect($"/Error?statusCode={statusCode}");
        return;
    }

    await Results.Problem(title: "Request failed", statusCode: statusCode)
        .ExecuteAsync(http);
});

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
