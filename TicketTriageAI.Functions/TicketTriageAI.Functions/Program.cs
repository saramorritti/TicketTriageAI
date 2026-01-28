using Azure.Messaging.ServiceBus;
using FluentValidation;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Ingest;
using TicketTriageAI.Core.Services.Messaging;
using TicketTriageAI.Core.Services.Processing;
using TicketTriageAI.Core.Validators;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton(_ =>
    new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnection")));
builder.Services.AddScoped<ITicketQueuePublisher, ServiceBusTicketQueuePublisher>();
builder.Services.AddScoped<ITicketIngestPipeline, TicketIngestPipeline>();
builder.Services.AddScoped<IValidator<TicketIngestedRequest>, TicketIngestedRequestValidator>();
builder.Services.AddScoped<ITicketIngestService, TicketIngestService>();
builder.Services.AddScoped<ITicketClassifier, FakeTicketClassifier>();
builder.Services.AddScoped<ITicketProcessingPipeline, TicketProcessingPipeline>();


// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
