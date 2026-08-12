using Azure.Storage.Files.Shares;

namespace ABCRetail.Services
{
    public class FileStorageService
    {
        private readonly string _connectionString;
        private readonly string _shareName = "application-logs";

        public FileStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
        }

        private async Task<ShareDirectoryClient> GetRootDirectoryAsync()
        {
            var shareClient = new ShareClient(
                _connectionString,
                _shareName);

            await shareClient.CreateIfNotExistsAsync();

            return shareClient.GetRootDirectoryClient();
        }

        public async Task UploadLogAsync(
            string fileName,
            string content)
        {
            var directoryClient = await GetRootDirectoryAsync();

            var fileClient = directoryClient.GetFileClient(fileName);

            using var stream = new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(content));

            await fileClient.CreateAsync(stream.Length);

            await fileClient.UploadAsync(stream);
        }

        public async Task<List<string>> GetLogsAsync()
        {
            var directoryClient = await GetRootDirectoryAsync();

            var logs = new List<string>();

            await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    logs.Add(item.Name);
                }
            }

            return logs;
        }
    }
}
