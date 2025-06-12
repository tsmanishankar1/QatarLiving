using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IFileStorage
{
    public interface IFileStorageBlobService
    {
        Task<string> SaveBase64File(string base64Content, string fileName, string containerName, CancellationToken cancellationToken = default);
        Task<byte[]> ReadFile(string blobName, string containerName, CancellationToken cancellationToken = default);
        Task DeleteFile(string blobName, string containerName, CancellationToken cancellationToken = default);
    }
}
