using QLN.Common.DTO_s.InstagramDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IInstagramPost
{
    public  interface IInstaService
    {
        Task<List<InstagramPost>> GetLatestPostsAsync(int count = 3, CancellationToken cancellationToken = default);
    }
}
