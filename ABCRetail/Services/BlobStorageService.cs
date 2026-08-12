using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ABCRetail.Services
{
    public class BlobStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName = "product-images";

        public BlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
        }

        private async Task<BlobContainerClient> GetContainerAsync()
        {
            var serviceClient = new BlobServiceClient(_connectionString);

            var containerClient =
                serviceClient.GetBlobContainerClient(_containerName);

            await containerClient.CreateIfNotExistsAsync();

            return containerClient;
        }

        // Upload image
        public async Task UploadImageAsync(
            Stream fileStream,
            string fileName,
            string contentType)
        {
            var containerClient = await GetContainerAsync();

            var blobClient = containerClient.GetBlobClient(fileName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

            await blobClient.UploadAsync(fileStream, options);
        }

        // Download image from Azure Blob Storage
        public async Task<(Stream Stream, string ContentType)?>
            DownloadImageAsync(string fileName)
        {
            var containerClient = await GetContainerAsync();

            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            var response = await blobClient.DownloadStreamingAsync();

            string contentType =
                response.Value.Details.ContentType;

            // Older blobs may not have a proper content type.
            // Determine it from the file extension.
            if (string.IsNullOrWhiteSpace(contentType) ||
                contentType == "application/octet-stream")
            {
                contentType = GetContentType(fileName);
            }

            return (
                response.Value.Content,
                contentType
            );
        }

        private string GetContentType(string fileName)
        {
            var extension =
                Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }

        public async Task<List<string>> GetImagesAsync()
        {
            var containerClient = await GetContainerAsync();

            var images = new List<string>();

            await foreach (var blobItem in
                containerClient.GetBlobsAsync())
            {
                images.Add(blobItem.Name);
            }

            return images;
        }

        public async Task DeleteImageAsync(string fileName)
        {
            var containerClient = await GetContainerAsync();

            var blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.DeleteIfExistsAsync();
        }
    }
}