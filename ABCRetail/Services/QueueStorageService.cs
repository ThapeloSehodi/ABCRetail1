using Azure.Storage.Queues;
using System.Text.Json;

namespace ABCRetail.Services
{
    public class QueueStorageService
    {
        private readonly string _connectionString;
        private readonly string _queueName = "order-processing";

        public QueueStorageService(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
        }

        private QueueClient GetQueueClient()
        {
            var queueClient = new QueueClient(
                _connectionString,
                _queueName);

            return queueClient;
        }

        public async Task SendMessageAsync<T>(T message)
        {
            var queueClient = GetQueueClient();

            await queueClient.CreateIfNotExistsAsync();

            string json =
                JsonSerializer.Serialize(message);

            await queueClient.SendMessageAsync(json);
        }

        public async Task<List<Azure.Storage.Queues.Models.PeekedMessage>>
     PeekMessagesAsync()
        {
            var queueClient = GetQueueClient();

            await queueClient.CreateIfNotExistsAsync();

            var response =
                await queueClient.PeekMessagesAsync(
                    maxMessages: 32);

            return response.Value.ToList();
        }

        public async Task DeleteMessageAsync(
            string messageId,
            string popReceipt)
        {
            var queueClient = GetQueueClient();

            await queueClient.DeleteMessageAsync(
                messageId,
                popReceipt);
        }
    }
}