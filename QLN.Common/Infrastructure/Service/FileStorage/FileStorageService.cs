using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.IService.IFileStorage;

namespace QLN.Common.Infrastructure.Service.FileStorage
{
    public class FileStorageService : IFileStorageService
    {
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(ILogger<FileStorageService> logger)
        {
            _logger = logger;
        }

        public async Task<string> SaveFile(Stream fileStream, string path, CancellationToken cancellationToken = default)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.Create);
                await fileStream.CopyToAsync(stream, cancellationToken);
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file to path: {Path}", path);
                throw;
            }
        }


        public async Task<byte[]> ReadFile(string path)
        {
            try
            {
                return await File.ReadAllBytesAsync(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {Path}", path);
                throw;
            }
        }

        public Task DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Path}", path);
                throw;
            }
        }
    }

}
