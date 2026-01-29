using Azure.Messaging.ServiceBus;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;
using TicketTriageAI.Core.Services.Ingest;
using TicketTriageAI.Core.Services.Messaging;
using TicketTriageAI.Core.Services.Processing;
using TicketTriageAI.Core.Validators;


var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton(_ =>
    new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnection")));
builder.Services.AddSingleton(_ =>
    new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbConnection")));


builder.Services.AddSingleton<ITicketIngestedFactory, TicketIngestedFactory>();
builder.Services.AddSingleton<ITicketDocumentFactory, TicketDocumentFactory>();

builder.Services.AddScoped<ITicketQueuePublisher, ServiceBusTicketQueuePublisher>();
builder.Services.AddScoped<ITicketRepository, CosmosTicketRepository>();
builder.Services.AddScoped<IValidator<TicketIngestedRequest>, TicketIngestedRequestValidator>();

builder.Services.AddScoped<ITicketIngestPipeline, TicketIngestPipeline>();
builder.Services.AddScoped<ITicketProcessingPipeline, TicketProcessingPipeline>();

builder.Services.AddScoped<ITicketIngestService, TicketIngestService>();
builder.Services.AddScoped<ITicketClassifier, FakeTicketClassifier>();



// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
