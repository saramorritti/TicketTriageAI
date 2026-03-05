using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Ingest;
using TicketTriageAI.Core.Services.Processing;
using TicketTriageAI.Common.Http;
using TicketTriageAI.Common.Logging;

namespace TicketTriageAI.Functions.Functions;

public class IngestTicketFunction
{
    private readonly ILogger<IngestTicketFunction> _logger;
    private readonly ITicketIngestService _ingestService;
    private readonly ITicketIngestPipeline _pipeline;
    private readonly ITicketStatusRepository _statusRepo;

    public IngestTicketFunction(
    ILogger<IngestTicketFunction> logger,
    ITicketIngestService ingestService,
    ITicketIngestPipeline pipeline,
    ITicketStatusRepository statusRepo)
    {
        _logger = logger;
        _ingestService = ingestService;
        _pipeline = pipeline;
        _statusRepo = statusRepo;
    }


    [Function("IngestTicket")]
    public async Task<IActionResult> RunAsync(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = Routes.IngestTicketV1)] HttpRequest req, CancellationToken ct)
    {
        var correlationId = GetOrCreateCorrelationId(req);

        using (_logger.BeginCorrelationScope(correlationId))
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(body))
                return BadRequest(ApiMessages.EmptyBody, correlationId);

            var (request, validation) = await _ingestService.ParseAndValidateAsync(body, ct);
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
                    "Validation failed for ingest. Errors: {Errors}",
                    errors);

                return BadRequest(ApiMessages.ValidationFailed, correlationId, errors);
            }

            var idemKey = req.Headers.TryGetValue("Idempotency-Key", out var v) ? v.ToString() : null;
            var published = await _pipeline.ExecuteAsync(request, correlationId, idempotencyKey: idemKey, ct: ct);

            if (!published)
            {
                _logger.LogInformation(
                    "Duplicate ticket suppressed."
                    );

                return AcceptedResult(new
                {
                    message = "Duplicate suppressed",
                    correlationId,
                });
            }

            _logger.LogInformation(
                "Ticket ingest accepted and enqueued"
                );

            return AcceptedResult(new
            {
                message = "Ticket accepted for processing",
                correlationId,
            });

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

    private static ObjectResult AcceptedResult(object payload)
    {
        return new ObjectResult(payload)
        {
            StatusCode = StatusCodes.Status202Accepted
        };
    }

    #endregion
}
