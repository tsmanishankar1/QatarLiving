using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.BannerService
{
    public interface IBannerService
    {
        Task<Banner> CreateBanner(BannerDto dto, CancellationToken cancellationToken = default);
        Task<Banner?> GetBanner(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Banner>> GetAllBanners();
        Task<Banner> UpdateBanner(Guid id, BannerDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteBanner(Guid id, CancellationToken cancellationToken = default);        
        Task<BannerImage?> GetImage(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<BannerImage>> GetAllImages();
        Task<List<BannerImage>> UploadImage(BannerImageUploadRequest form, CancellationToken cancellationToken = default);
        Task<BannerImage> UpdateImage(Guid id, BannerImageUpdateDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteImage(Guid id, CancellationToken cancellationToken = default);
    }
}
