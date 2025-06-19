using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService
{
    public interface IWishlistService
    {
        Task<List<Guid>> GetFavoritesAsync(string userId, string vertical);
        Task AddFavoriteAsync(string userId, string vertical, Guid itemId);
        Task RemoveFavoriteAsync(string userId, string vertical, Guid itemId);
    }
}
