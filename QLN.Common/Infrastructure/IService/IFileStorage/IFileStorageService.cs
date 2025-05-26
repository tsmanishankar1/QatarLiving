using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IFileStorage
{
    public interface IFileStorageService
    {
        Task<string> SaveFile(Stream stream, string path, CancellationToken cancellationToken = default);
        Task DeleteFile(string path);
        Task<byte[]> ReadFile(string path);
    }

}
