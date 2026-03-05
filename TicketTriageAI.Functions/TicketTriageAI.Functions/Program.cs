using Azure.Messaging.ServiceBus;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;
using TicketTriageAI.Core.Services.Ingest;
using TicketTriageAI.Core.Services.Messaging;
using TicketTriageAI.Core.Services.Notifications;
using TicketTriageAI.Core.Services.Processing;
using TicketTriageAI.Core.Services.Processing.AI;
using TicketTriageAI.Core.Services.Text;
using TicketTriageAI.Core.Validators;
using TicketTriageAI.Functions.Middleware;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddOptions<ServiceBusOptions>()
    .Bind(builder.Configuration.GetSection("ServiceBus"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<CosmosOptions>()
    .Bind(builder.Configuration.GetSection("Cosmos"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<TicketProcessingOptions>()
    .Bind(builder.Configuration.GetSection("Processing"))
    .ValidateOnStart();

builder.Services
    .AddOptions<AzureOpenAIClassifierOptions>()
    .Bind(builder.Configuration.GetSection("AzureOpenAI:Classifier"))
    .ValidateOnStart();

builder.Services
    .AddOptions<NotificationOptions>()
    .Bind(builder.Configuration.GetSection("Notifications"))
    .ValidateOnStart();

builder.Services.AddSingleton(_ =>
{
    var cs = Environment.GetEnvironmentVariable("ServiceBusConnection");
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Missing ServiceBusConnection in local.settings.json Values.");
    return new ServiceBusClient(cs);
});

builder.Services.AddSingleton(_ =>
{
    var cs = Environment.GetEnvironmentVariable("CosmosDbConnection");
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Missing CosmosDbConnection in local.settings.json Values.");
    return new CosmosClient(cs);
});

builder.Services.AddKeyedSingleton<ServiceBusSender>("ingest", (sp, _) =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServiceBusOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opt.QueueName))
        throw new InvalidOperationException("Missing ServiceBus:QueueName (ServiceBus__QueueName).");

    return client.CreateSender(opt.QueueName);
});

builder.Services.AddKeyedSingleton<ServiceBusSender>("notify", (sp, _) =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NotificationOptions>>().Value;

    if (string.IsNullOrWhiteSpace(opts.NotifyQueueName))
        throw new InvalidOperationException("Missing Notifications:NotifyQueueName (Notifications__NotifyQueueName).");

    return client.CreateSender(opts.NotifyQueueName);
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

builder.Services.AddScoped<ITextNormalizer, EmailTextNormalizer>();
builder.Services.AddScoped<ITicketNormalizationFactory, TicketNormalizationFactory>();

builder.Services.AddSingleton<ITicketQueuePublisher, ServiceBusTicketQueuePublisher>();
builder.Services.AddScoped<ITicketRepository, CosmosTicketRepository>();
builder.Services.AddScoped<ITicketStatusRepository, CosmosTicketStatusRepository>();
builder.Services.AddScoped<IValidator<TicketIngestedRequest>, TicketIngestedRequestValidator>();

builder.Services.AddScoped<ITicketIngestPipeline, TicketIngestPipeline>();
builder.Services.AddScoped<ITicketProcessingPipeline, TicketProcessingPipeline>();

builder.Services.AddScoped<ITicketIngestService, TicketIngestService>();
//builder.Services.AddScoped<ITicketClassifier, FakeTicketClassifier>();
builder.Services.AddScoped<ITicketClassifier, AzureOpenAITicketClassifier>();

//builder.Services.AddSingleton<ITicketNotificationService, LoggingTicketNotificationService>();
builder.Services.AddScoped<ITicketNotificationService, ServiceBusTicketNotificationService>();

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IFunctionsWorkerMiddleware, GlobalExceptionMiddleware>();

builder.Build().Run();
