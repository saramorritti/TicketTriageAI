using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Ingest;
using TicketTriageAI.Functions.Common;

namespace TicketTriageAI.Functions.Functions;

public class IngestTicketFunction
{
    private const string QueueName = "tickets-ingest";

    private readonly ILogger<IngestTicketFunction> _logger;
    private readonly ITicketIngestService _ingestService;
    private readonly ITicketIngestPipeline _pipeline;


    public IngestTicketFunction(
    ILogger<IngestTicketFunction> logger,
    ITicketIngestService ingestService,
    ITicketIngestPipeline pipeline)
    {
        _logger = logger;
        _ingestService = ingestService;
        _pipeline = pipeline;
    }


    [Function("IngestTicket")]
    public async Task<IActionResult> RunAsync(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = Routes.IngestTicketV1)] HttpRequest req)
    {
        var correlationId = GetOrCreateCorrelationId(req);

        using (_logger.BeginCorrelationScope(correlationId))
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
                return BadRequest(ApiMessages.EmptyBody, correlationId);

            var (request, validation) = await _ingestService.ParseAndValidateAsync(body);
            if (request is null)
                return BadRequest(ApiMessages.InvalidPayload, correlationId);

            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    error = e.ErrorMessage
                });

                _logger.LogWarning(
                    "Validation failed for ingest. MessageId: {MessageId}. Errors: {Errors}",
                    request.MessageId,
                    errors);

                return BadRequest(ApiMessages.ValidationFailed, correlationId, errors);
            }

            await _pipeline.ExecuteAsync(request, correlationId);

            _logger.LogInformation(
                "Ticket ingest accepted and enqueued. MessageId: {MessageId}",
                request.MessageId);

            return Accepted(request.MessageId, correlationId);
        }
    }


    #region helpers

    private static string GetOrCreateCorrelationId(HttpRequest req)
    {
        var correlationId = req.Headers.TryGetValue(HeaderNames.CorrelationId, out var cid)
            ? cid.ToString()
            : Guid.NewGuid().ToString();

        req.HttpContext.Response.Headers[HeaderNames.CorrelationId] = correlationId;
        return correlationId;
    }

    private static BadRequestObjectResult BadRequest(
        string message,
        string correlationId,
        object? errors = null)
    {
        return new BadRequestObjectResult(new
        {
            message,
            correlationId,
            errors
        });
    }

    private static ObjectResult Accepted(string messageId, string correlationId)
    {
        return new ObjectResult(new
        {
            message = ApiMessages.Accepted,
            correlationId,
            messageId
        })
        {
            StatusCode = StatusCodes.Status202Accepted
        };
    }
    #endregion
}
