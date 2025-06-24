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

        public async Task<string> SaveBase64Filess(string base64Content, string fileName, string containerName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (base64Content.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = base64Content.Split(',', 2);
                    if (parts.Length != 2)
                        throw new ArgumentException("Invalid base64 content.");
                    base64Content = parts[1];
                }
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

        public async Task<string> SaveBase64File(string base64Content, string fileName, string containerName, CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(fileName);

                // Strip 'data:image/png;base64,' or similar if present
                if (base64Content.Contains(","))
                {
                    base64Content = base64Content.Substring(base64Content.IndexOf(",") + 1);
                }

                byte[] fileBytes = Convert.FromBase64String(base64Content);
                using var stream = new MemoryStream(fileBytes);

                await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
                return blobClient.Uri.ToString();
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 format in file content for container {ContainerName}", containerName);
                throw new InvalidDataException("Invalid base64 string. Please ensure it's correctly encoded.", ex);
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
