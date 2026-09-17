using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABCRetail.Functions1
{
    public class QueueFunction
    {
        private readonly string _connectionString;
        private const string QueueName = "order-processing";

        public QueueFunction()
        {
            _connectionString =
                Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
        }

        private QueueClient GetQueueClient()
        {
            var client = new QueueClient(
                _connectionString,
                QueueName);

            client.CreateIfNotExists();

            return client;
        }

        // CREATE - POST
        [Function("WriteTransactionToQueue")]
        public async Task<HttpResponseData> WriteTransactionToQueue(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req)
        {
            var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Transaction data is required.");

                return badResponse;
            }

            try
            {
                JsonDocument.Parse(body);

                var queueClient = GetQueueClient();

                await queueClient.SendMessageAsync(body);

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    message =
                        "Transaction written to Azure Queue Storage.",
                    queue = QueueName
                });

                return response;
            }
            catch (JsonException)
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Request body must contain valid JSON.");

                return badResponse;
            }
        }

        // GET
        [Function("PeekTransactionQueue")]
        public async Task<HttpResponseData> PeekTransactionQueue(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequestData req)
        {
            var queueClient = GetQueueClient();

            var result =
                await queueClient.PeekMessageAsync();

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            if (result.Value == null)
            {
                await response.WriteAsJsonAsync(new
                {
                    message =
                        "No transactions are currently queued."
                });

                return response;
            }

            await response.WriteAsJsonAsync(new
            {
                messageId = result.Value.MessageId,
                transaction = result.Value.MessageText
            });

            return response;
        }

        // EDIT - PUT
        [Function("UpdateTransactionQueue")]
        public async Task<HttpResponseData> UpdateTransactionQueue(
            [HttpTrigger(AuthorizationLevel.Function, "put")]
            HttpRequestData req)
        {
            var queueClient = GetQueueClient();

            var reader = new StreamReader(req.Body);
            var newBody = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(newBody))
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Updated transaction data is required.");

                return badResponse;
            }

            try
            {
                JsonDocument.Parse(newBody);
            }
            catch (JsonException)
            {
                var badResponse =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Request body must contain valid JSON.");

                return badResponse;
            }

            var result =
                await queueClient.ReceiveMessageAsync();

            if (result.Value == null)
            {
                var response =
                    req.CreateResponse(HttpStatusCode.NotFound);

                await response.WriteStringAsync(
                    "No transaction is currently queued to update.");

                return response;
            }

            var oldTransaction =
                result.Value.MessageText;

            await queueClient.DeleteMessageAsync(
                result.Value.MessageId,
                result.Value.PopReceipt);

            await queueClient.SendMessageAsync(newBody);

            var successResponse =
                req.CreateResponse(HttpStatusCode.OK);

            await successResponse.WriteAsJsonAsync(new
            {
                message =
                    "Transaction updated successfully.",
                oldTransaction = oldTransaction,
                newTransaction = newBody
            });

            return successResponse;
        }

        // DELETE
        [Function("DeleteTransactionQueue")]
        public async Task<HttpResponseData> DeleteTransactionQueue(
            [HttpTrigger(AuthorizationLevel.Function, "delete")]
            HttpRequestData req)
        {
            var queueClient = GetQueueClient();

            var result =
                await queueClient.ReceiveMessageAsync();

            if (result.Value == null)
            {
                var response =
                    req.CreateResponse(HttpStatusCode.NotFound);

                await response.WriteStringAsync(
                    "No transaction is currently queued to delete.");

                return response;
            }

            var transaction =
                result.Value.MessageText;

            await queueClient.DeleteMessageAsync(
                result.Value.MessageId,
                result.Value.PopReceipt);

            var successResponse =
                req.CreateResponse(HttpStatusCode.OK);

            await successResponse.WriteAsJsonAsync(new
            {
                message =
                    "Transaction deleted from the queue.",
                transaction = transaction
            });

            return successResponse;
        }
    }
}