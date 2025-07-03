using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.V2IContent
{
    public interface IV2CommunityPostService
    {
        Task<string> CreateCommunityPostAsync(string userId, V2CommunityPostDto dto, CancellationToken cancellationToken = default);
        //Task<List<V2CommunityPostDto>> GetAllCommunityPostsAsync(CancellationToken ct = default);

    }
}
