using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;

namespace QLN.Web.Shared.MockServices
{
    public class AdMockService : IAdService
    {
        private readonly AdModel mockAd = new()
        {
            ImageUrl = "/images/content/Ad.png"
        };

        public Task<IEnumerable<AdModel>> GetAdDetail()
        {
            return Task.FromResult<IEnumerable<AdModel>>(new[] { mockAd });
        }
    }
}
