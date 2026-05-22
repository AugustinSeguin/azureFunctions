using System.Text.Json;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace azureFunctions.Events.Producers;

public sealed class Producer
{
    private const string QueueName = "incoming-requests";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Function("Producer")]
    public async Task<ProducerOutput> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData request,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger<Producer>();

        var incomingRequest = await JsonSerializer.DeserializeAsync<IncomingRequest>(request.Body, JsonOptions);

        if (incomingRequest is null || string.IsNullOrWhiteSpace(incomingRequest.Name) || string.IsNullOrWhiteSpace(incomingRequest.Message))
        {
            var badRequest = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new
            {
                error = "Le corps JSON doit contenir name et message."
            });
            return new ProducerOutput
            {
                QueueMessage = string.Empty,
                HttpResponse = badRequest
            };
        }

        var queueMessage = JsonSerializer.Serialize(new QueueMessage(
            incomingRequest.Name.Trim(),
            incomingRequest.Message.Trim(),
            DateTimeOffset.UtcNow));

        logger.LogInformation("Queue message prepared for {QueueName}.", QueueName);

        var accepted = request.CreateResponse(HttpStatusCode.Accepted);
        await accepted.WriteAsJsonAsync(new
        {
            status = "queued",
            queue = QueueName
        });

        return new ProducerOutput
        {
            QueueMessage = queueMessage,
            HttpResponse = accepted
        };
    }

    private sealed record IncomingRequest(string? Name, string? Message);

    private sealed record QueueMessage(string Name, string Message, DateTimeOffset EnqueuedAtUtc);

    public sealed class ProducerOutput
    {
        [QueueOutput(QueueName, Connection = "AzureWebJobsStorage")]
        public string QueueMessage { get; set; } = string.Empty;

        [HttpResult]
        public HttpResponseData HttpResponse { get; set; } = default!;
    }
}