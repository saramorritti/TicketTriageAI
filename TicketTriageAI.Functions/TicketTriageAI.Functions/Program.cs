using Azure.Messaging.ServiceBus;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;
using TicketTriageAI.Core.Services.Ingest;
using TicketTriageAI.Core.Services.Messaging;
using TicketTriageAI.Core.Services.Processing;
using TicketTriageAI.Core.Services.Processing.AI;
using TicketTriageAI.Core.Validators;



var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddOptions<ServiceBusOptions>()
    .Bind(builder.Configuration.GetSection("ServiceBus"))
    .ValidateOnStart();

builder.Services
    .AddOptions<CosmosOptions>()
    .Bind(builder.Configuration.GetSection("Cosmos"))
    .ValidateOnStart();


builder.Services.AddSingleton(_ =>
    new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnection")));
builder.Services.AddSingleton(_ =>
    new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbConnection")));

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServiceBusOptions>>().Value;
    return client.CreateSender(opt.QueueName);
});

builder.Services.AddSingleton<ChatClient>(_ =>
{
    var endpoint = Environment.GetEnvironmentVariable("AzureOpenAIEndpoint");
    var key = Environment.GetEnvironmentVariable("AzureOpenAIKey");
    var deployment = Environment.GetEnvironmentVariable("AzureOpenAIDeployment");

    if (string.IsNullOrWhiteSpace(endpoint))
        throw new InvalidOperationException("Missing AzureOpenAIEndpoint in environment/local.settings.json.");
    if (string.IsNullOrWhiteSpace(key))
        throw new InvalidOperationException("Missing AzureOpenAIKey in environment/local.settings.json.");
    if (string.IsNullOrWhiteSpace(deployment))
        throw new InvalidOperationException("Missing AzureOpenAIDeployment in environment/local.settings.json.");

    // IMPORTANT: per Azure OpenAI con SDK OpenAI, l'endpoint deve puntare a /openai/v1/
    var baseUri = new Uri($"{endpoint.TrimEnd('/')}/openai/v1/");

    return new ChatClient(
        model: deployment,
        credential: new ApiKeyCredential(key),
        options: new OpenAIClientOptions { Endpoint = baseUri });
});


builder.Services.AddSingleton<ITicketIngestedFactory, TicketIngestedFactory>();
builder.Services.AddSingleton<ITicketDocumentFactory, TicketDocumentFactory>();

builder.Services.AddSingleton<ITicketQueuePublisher, ServiceBusTicketQueuePublisher>();
builder.Services.AddScoped<ITicketRepository, CosmosTicketRepository>();
builder.Services.AddScoped<ITicketStatusRepository, CosmosTicketStatusRepository>();
builder.Services.AddScoped<IValidator<TicketIngestedRequest>, TicketIngestedRequestValidator>();

builder.Services.AddScoped<ITicketIngestPipeline, TicketIngestPipeline>();
builder.Services.AddScoped<ITicketProcessingPipeline, TicketProcessingPipeline>();

builder.Services.AddScoped<ITicketIngestService, TicketIngestService>();
//builder.Services.AddScoped<ITicketClassifier, FakeTicketClassifier>();
builder.Services.AddScoped<ITicketClassifier, AzureOpenAITicketClassifier>();




// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
