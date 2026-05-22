using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace azureFunctions.Events.Consumers;

public sealed class Consumer
{
    private const string TableName = "incomingrequests";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Function("Consumer")]
    [TableOutput(TableName, Connection = "AzureWebJobsStorage")]
    public IncomingRequestEntity RunAsync(
        [QueueTrigger("incoming-requests", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        var incomingRequest = JsonSerializer.Deserialize<QueueMessage>(queueMessage, JsonOptions)
            ?? throw new InvalidOperationException("Le message de queue est invalide.");

        return new IncomingRequestEntity(
            PartitionKey: "incoming-requests",
            RowKey: Guid.NewGuid().ToString("N"),
            Name: incomingRequest.Name.Trim(),
            Message: incomingRequest.Message.Trim(),
            EnqueuedAtUtc: incomingRequest.EnqueuedAtUtc,
            ProcessedAtUtc: DateTimeOffset.UtcNow);
    }

    private sealed record QueueMessage(string Name, string Message, DateTimeOffset EnqueuedAtUtc);

    public sealed record IncomingRequestEntity(
        string PartitionKey,
        string RowKey,
        string Name,
        string Message,
        DateTimeOffset EnqueuedAtUtc,
        DateTimeOffset ProcessedAtUtc);
}
