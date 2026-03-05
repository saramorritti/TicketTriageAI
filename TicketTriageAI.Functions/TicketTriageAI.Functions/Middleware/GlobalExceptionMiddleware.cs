using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;


namespace TicketTriageAI.Functions.Middleware
{
    public sealed class GlobalExceptionMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
            => _logger = logger;

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var functionName = context.FunctionDefinition?.Name ?? "UnknownFunction";
                var invocationId = context.InvocationId;

                // Prova a prendere correlationId se è una HTTP function
                string? correlationId = null;
                try
                {
                    var req = await context.GetHttpRequestDataAsync();
                    if (req != null && req.Headers.TryGetValues("x-correlation-id", out var values))
                        correlationId = values.FirstOrDefault();
                }
                catch { /* no-op */ }

                _logger.LogError(ex,
                    "Unhandled exception in Function={FunctionName} InvocationId={InvocationId} CorrelationId={CorrelationId}",
                    functionName, invocationId, correlationId);

                // Se è HTTP: rispondi 500 JSON (senza leak di dettagli)
                var request = await context.GetHttpRequestDataAsync();
                if (request != null)
                {
                    var resp = request.CreateResponse(HttpStatusCode.InternalServerError);

                    if (!string.IsNullOrWhiteSpace(correlationId))
                        resp.Headers.Add("x-correlation-id", correlationId);

                    await resp.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        message = "Internal error",
                        correlationId
                    }));

                    context.GetInvocationResult().Value = resp;
                    return;
                }

                // Non-HTTP (es. ServiceBusTrigger): rilancia per retry/DLQ
                throw;
            }
        }
    }
}