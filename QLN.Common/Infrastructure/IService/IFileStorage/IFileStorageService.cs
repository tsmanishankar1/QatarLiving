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
        Task<string> SaveFile(IFormFile file, string path);
        Task DeleteFile(string path);
        Task<byte[]> ReadFile(string path);
    }

}
