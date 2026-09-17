using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ABCRetail.Functions1
{
    public class FileFunction
    {
        private readonly string _connectionString;

        private const string ShareName =
            "application-logs";

        public FileFunction()
        {
            _connectionString =
                Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
        }

        private async Task<ShareDirectoryClient>
            GetRootDirectoryAsync()
        {
            var shareClient =
                new ShareClient(
                    _connectionString,
                    ShareName);

            await shareClient.CreateIfNotExistsAsync();

            return shareClient.GetRootDirectoryClient();
        }

        // CREATE - POST
        [Function("WriteAzureFile")]
        public async Task<HttpResponseData> WriteAzureFile(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestData req)
        {
            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var fileName =
                query["fileName"];

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badResponse =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Provide a fileName query parameter.");

                return badResponse;
            }

            fileName =
                Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badResponse =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "A valid file name is required.");

                return badResponse;
            }

            var directory =
                await GetRootDirectoryAsync();

            var fileClient =
                directory.GetFileClient(fileName);

            var memoryStream =
                new MemoryStream();

            await req.Body.CopyToAsync(
                memoryStream);

            memoryStream.Position = 0;

            await fileClient.CreateAsync(
                memoryStream.Length);

            await fileClient.UploadAsync(
                memoryStream);

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                message =
                    "File written successfully to Azure Files.",
                share = ShareName,
                fileName = fileName
            });

            return response;
        }

        // GET
        [Function("ListAzureFiles")]
        public async Task<HttpResponseData> ListAzureFiles(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequestData req)
        {
            var directory =
                await GetRootDirectoryAsync();

            var files =
                new List<string>();

            foreach (
                var item in directory.GetFilesAndDirectories())
            {
                if (!item.IsDirectory)
                {
                    files.Add(item.Name);
                }
            }

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(
                files);

            return response;
        }

        // EDIT - PUT
        [Function("UpdateAzureFile")]
        public async Task<HttpResponseData> UpdateAzureFile(
            [HttpTrigger(AuthorizationLevel.Function, "put")]
            HttpRequestData req)
        {
            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var fileName =
                query["fileName"];

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badResponse =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Provide a fileName query parameter.");

                return badResponse;
            }

            fileName =
                Path.GetFileName(fileName);

            var directory =
                await GetRootDirectoryAsync();

            var fileClient =
                directory.GetFileClient(fileName);

            if (!await fileClient.ExistsAsync())
            {
                var notFoundResponse =
                    req.CreateResponse(
                        HttpStatusCode.NotFound);

                await notFoundResponse.WriteStringAsync(
                    "File not found.");

                return notFoundResponse;
            }

            var memoryStream =
                new MemoryStream();

            await req.Body.CopyToAsync(
                memoryStream);

            memoryStream.Position = 0;

            await fileClient.DeleteIfExistsAsync();

            await fileClient.CreateAsync(
                memoryStream.Length);

            await fileClient.UploadAsync(
                memoryStream);

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                message =
                    "File updated successfully in Azure Files.",
                share = ShareName,
                fileName = fileName
            });

            return response;
        }

        // DELETE
        [Function("DeleteAzureFile")]
        public async Task<HttpResponseData> DeleteAzureFile(
            [HttpTrigger(AuthorizationLevel.Function, "delete")]
            HttpRequestData req)
        {
            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var fileName =
                query["fileName"];

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badResponse =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await badResponse.WriteStringAsync(
                    "Provide a fileName query parameter.");

                return badResponse;
            }

            fileName =
                Path.GetFileName(fileName);

            var directory =
                await GetRootDirectoryAsync();

            var fileClient =
                directory.GetFileClient(fileName);

            if (!await fileClient.ExistsAsync())
            {
                var notFoundResponse =
                    req.CreateResponse(
                        HttpStatusCode.NotFound);

                await notFoundResponse.WriteStringAsync(
                    "File not found.");

                return notFoundResponse;
            }

            await fileClient.DeleteAsync();

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                message =
                    "File deleted successfully from Azure Files.",
                share = ShareName,
                fileName = fileName
            });

            return response;
        }
    }
}