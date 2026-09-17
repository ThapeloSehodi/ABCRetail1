using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ABCRetail.Functions1
{
    public class BlobFunction
    {
        private readonly string _connectionString;
        private const string ContainerName = "product-images";

        public BlobFunction()
        {
            _connectionString =
                Environment.GetEnvironmentVariable(
                    "AzureWebJobsStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
        }

        private async Task<BlobContainerClient>
            GetContainerClientAsync()
        {
            var serviceClient =
                new BlobServiceClient(_connectionString);

            var container =
                serviceClient.GetBlobContainerClient(ContainerName);

            await container.CreateIfNotExistsAsync();

            return container;
        }

        
        // CREATE / UPLOAD BLOB
        // POST: /api/StoreBlob?fileName=example.jpg
        

        [Function("StoreBlob")]
        public async Task<HttpResponseData> StoreBlob(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post")]
            HttpRequestData req)
        {
            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var fileName = query["fileName"];

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badResponse =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Provide a fileName query parameter.");

                return badResponse;
            }

            fileName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badResponse =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "A valid file name is required.");

                return badResponse;
            }

            var container =
                await GetContainerClientAsync();

            var blob =
                container.GetBlobClient(fileName);

            var contentType =
                req.Headers.TryGetValues(
                    "Content-Type",
                    out var values)
                    ? values.FirstOrDefault()
                        ?? "application/octet-stream"
                    : "application/octet-stream";

            await blob.UploadAsync(
                req.Body,
                new BlobUploadOptions
                {
                    HttpHeaders =
                        new BlobHttpHeaders
                        {
                            ContentType = contentType
                        }
                });

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                message = "Blob stored successfully.",
                container = ContainerName,
                fileName = fileName
            });

            return response;
        }

        
        // GET ONE BLOB
        // GET: /api/GetBlob?fileName=example.jpg
        

        [Function("GetBlob")]
        public async Task<HttpResponseData> GetBlob(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get")]
            HttpRequestData req)
        {
            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var fileName = query["fileName"];

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badResponse =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Provide a fileName query parameter.");

                return badResponse;
            }

            fileName = Path.GetFileName(fileName);

            var container =
                await GetContainerClientAsync();

            var blob =
                container.GetBlobClient(fileName);

            if (!await blob.ExistsAsync())
            {
                var notFoundResponse =
                    req.CreateResponse(
                        HttpStatusCode.NotFound);

                await notFoundResponse.WriteStringAsync(
                    "Blob not found.");

                return notFoundResponse;
            }

            var properties =
                await blob.GetPropertiesAsync();

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                fileName = fileName,
                container = ContainerName,
                contentType =
                    properties.Value.ContentType,
                size =
                    properties.Value.ContentLength,
                lastModified =
                    properties.Value.LastModified
            });

            return response;
        }

       
        // GET ALL BLOBS
        // GET: /api/GetAllBlobs
        

        [Function("GetAllBlobs")]
        public async Task<HttpResponseData> GetAllBlobs(
     [HttpTrigger(
        AuthorizationLevel.Function,
        "get")]
    HttpRequestData req)
        {
            var container =
                await GetContainerClientAsync();

            var blobs =
                new List<object>();

            foreach (BlobItem blobItem in container.GetBlobs())
            {
                blobs.Add(new
                {
                    fileName = blobItem.Name,
                    contentType =
                        blobItem.Properties.ContentType,
                    size =
                        blobItem.Properties.ContentLength,
                    lastModified =
                        blobItem.Properties.LastModified
                });
            }

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(blobs);

            return response;
        }

        
        // DELETE BLOB
        // DELETE: /api/DeleteBlob?fileName=example.jpg
        

        [Function("DeleteBlob")]
        public async Task<HttpResponseData> DeleteBlob(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "delete")]
            HttpRequestData req)
        {
            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var fileName = query["fileName"];

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badResponse =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Provide a fileName query parameter.");

                return badResponse;
            }

            fileName = Path.GetFileName(fileName);

            var container =
                await GetContainerClientAsync();

            var blob =
                container.GetBlobClient(fileName);

            if (!await blob.ExistsAsync())
            {
                var notFoundResponse =
                    req.CreateResponse(
                        HttpStatusCode.NotFound);

                await notFoundResponse.WriteStringAsync(
                    "Blob not found.");

                return notFoundResponse;
            }

            await blob.DeleteAsync();

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                message = "Blob deleted successfully.",
                container = ContainerName,
                fileName = fileName
            });

            return response;
        }
    }
}