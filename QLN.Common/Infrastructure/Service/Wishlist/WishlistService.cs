using Dapr.Client;
using QLN.Common.Infrastructure.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service.Wishlist
{
    public class WishlistService : IWishlistService
    {
        private const string Store = "wishliststore";
        private readonly DaprClient _dapr;
        public WishlistService(DaprClient dapr) => _dapr = dapr;

        private static string Key(string userId, string vertical)
            => $"wishlist-{userId}-{vertical}";

        public async Task<List<Guid>> GetFavoritesAsync(string userId, string vertical)
        {
            var list = await _dapr.GetStateAsync<List<Guid>>(Store, Key(userId, vertical));
            return list ?? new List<Guid>();
        }

        public async Task AddFavoriteAsync(string userId, string vertical, Guid itemId)
        {
            var favs = await GetFavoritesAsync(userId, vertical);
            if (!favs.Contains(itemId))
            {
                favs.Add(itemId);
                await _dapr.SaveStateAsync(Store, Key(userId, vertical), favs);
            }
        }

        public async Task RemoveFavoriteAsync(string userId, string vertical, Guid itemId)
        {
            var favs = await GetFavoritesAsync(userId, vertical);
            if (favs.Remove(itemId))
                await _dapr.SaveStateAsync(Store, Key(userId, vertical), favs);
        }
    }
}
