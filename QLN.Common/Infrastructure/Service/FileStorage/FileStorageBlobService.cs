using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.IService.IFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service.FileStorage
{
    public class FileStorageBlobService : IFileStorageBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<FileStorageBlobService> _logger;

        public FileStorageBlobService(IConfiguration configuration, ILogger<FileStorageBlobService> logger)
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            _blobServiceClient = new BlobServiceClient(connectionString);
            _logger = logger;
        }

        public async Task<string> SaveBase64File(string base64Content, string fileName, string containerName, CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(fileName);

                byte[] fileBytes = Convert.FromBase64String(base64Content);
                using var stream = new MemoryStream(fileBytes);

                await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file to blob container {ContainerName}", containerName);
                throw;
            }
        }

        public async Task<byte[]> ReadFile(string blobName, string containerName, CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.DownloadContentAsync();
                return response.Value.Content.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read blob {BlobName} from container {ContainerName}", blobName, containerName);
                throw;
            }
        }

        public async Task DeleteFile(string blobName, string containerName, CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete blob {BlobName} from container {ContainerName}", blobName, containerName);
                throw;
            }
        }
    }
}
