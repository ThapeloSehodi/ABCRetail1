using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.Text;

namespace ABCRetail.Services
{
    public class LogStorageService
    {
        private readonly string _connectionString;
        private readonly string _shareName = "logs";
        private readonly string _fileName = "application.log";

        public LogStorageService(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
        }

        private ShareFileClient GetFileClient()
        {
            var shareClient = new ShareClient(
                _connectionString,
                _shareName);

            shareClient.CreateIfNotExists();

            var directoryClient =
                shareClient.GetDirectoryClient("application");

            directoryClient.CreateIfNotExists();

            var fileClient =
                directoryClient.GetFileClient(_fileName);

            return fileClient;
        }

        public async Task WriteLogAsync(string message)
        {
            var fileClient = GetFileClient();

            string existingContent = string.Empty;

            if (await fileClient.ExistsAsync())
            {
                var download =
                    await fileClient.DownloadAsync();

                using var reader =
                    new StreamReader(download.Value.Content);

                existingContent =
                    await reader.ReadToEndAsync();
            }

            string logEntry =
                $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - {message}";

            string newContent;

            if (string.IsNullOrEmpty(existingContent))
            {
                newContent = logEntry;
            }
            else
            {
                newContent =
                    existingContent +
                    Environment.NewLine +
                    logEntry;
            }

            byte[] contentBytes =
                Encoding.UTF8.GetBytes(newContent);

            using var stream =
                new MemoryStream(contentBytes);

            await fileClient.CreateAsync(
                maxSize: contentBytes.Length);

            await fileClient.UploadAsync(
                stream,
                new ShareFileUploadOptions());
        }

        public async Task<string> ReadLogAsync()
        {
            var fileClient = GetFileClient();

            if (!await fileClient.ExistsAsync())
            {
                return "No log entries found.";
            }

            var download =
                await fileClient.DownloadAsync();

            using var reader =
                new StreamReader(download.Value.Content);

            return await reader.ReadToEndAsync();
        }

        public async Task<Stream?> DownloadLogAsync()
        {
            var fileClient = GetFileClient();

            if (!await fileClient.ExistsAsync())
            {
                return null;
            }

            var download =
                await fileClient.DownloadAsync();

            var memoryStream = new MemoryStream();

            await download.Value.Content.CopyToAsync(
                memoryStream);

            memoryStream.Position = 0;

            return memoryStream;
        }
        // C#
        public async Task UploadPdfAsync(IFormFile file)
        {
            var shareClient = new ShareClient(_connectionString, _shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetDirectoryClient("documents");
            await directoryClient.CreateIfNotExistsAsync();

            var fileName = Path.GetFileName(file.FileName);
            var fileClient = directoryClient.GetFileClient(fileName);

            long fileLength = file.Length;
            await fileClient.CreateAsync(fileLength);

            const int MaxRangeSize = 4 * 1024 * 1024; // 4,194,304 bytes
            byte[] buffer = new byte[MaxRangeSize];

            using (var stream = file.OpenReadStream())
            {
                long offset = 0;
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    using var ms = new MemoryStream(buffer, 0, bytesRead, writable: false);
                    await fileClient.UploadRangeAsync(new Azure.HttpRange(offset, bytesRead), ms);
                    offset += bytesRead;
                }
            }
        }
        public async Task<List<string>> GetPdfFilesAsync()
        {
            var shareClient = new ShareClient(
                _connectionString,
                _shareName);

            await shareClient.CreateIfNotExistsAsync();

            var directoryClient =
                shareClient.GetDirectoryClient("documents");

            await directoryClient.CreateIfNotExistsAsync();

            var files = new List<string>();

            await foreach (var item in
                directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory &&
                    item.Name.EndsWith(
                        ".pdf",
                        StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(item.Name);
                }
            }

            return files;
        }
        public async Task<Stream?> DownloadPdfAsync(string fileName)
        {
            var shareClient = new ShareClient(
                _connectionString,
                _shareName);

            var directoryClient =
                shareClient.GetDirectoryClient("documents");

            var fileClient =
                directoryClient.GetFileClient(
                    Path.GetFileName(fileName));

            if (!await fileClient.ExistsAsync())
            {
                return null;
            }

            var download =
                await fileClient.DownloadAsync();

            var memoryStream = new MemoryStream();

            await download.Value.Content.CopyToAsync(
                memoryStream);

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
