using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace azureFunctions.Events.Producers;

public abstract class Producer
{
    private const string QueueName = "incoming-requests";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Function("Producer")]
    public async Task<HttpResponseData> RunAsync(
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
            return badRequest;
        }

        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrWhiteSpace(storageConnectionString))
        {
            logger.LogError("AzureWebJobsStorage is not configured.");

            var unavailable = request.CreateResponse(HttpStatusCode.InternalServerError);
            await unavailable.WriteAsJsonAsync(new
            {
                error = "La configuration de stockage est manquante."
            });
            return unavailable;
        }

        var queueClient = new QueueClient(storageConnectionString, QueueName);
        await queueClient.CreateIfNotExistsAsync();

        var queueMessage = new QueueMessage();

        await queueClient.SendMessageAsync(JsonSerializer.Serialize(queueMessage));

        var accepted = request.CreateResponse(HttpStatusCode.Accepted);
        await accepted.WriteAsJsonAsync(new
        {
            status = "queued",
            queue = QueueName
        });

        return accepted;
    }

    private sealed record IncomingRequest(string? Name, string? Message);

    private sealed record QueueMessage;
}