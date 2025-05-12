using Dapr.Client;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.Model;

namespace QLN.Backend.API.Service.BannerService
{
    public class ExternalBannerService : IBannerService
    {
        private readonly DaprClient _dapr;
        private const string SERVICE_APP_ID = "qln-classified-ms";
        private readonly IEventlogger _log;

        public ExternalBannerService(DaprClient dapr, IEventlogger eventlogger)
        {
            _dapr = dapr;
            _log = eventlogger;
        }

        public async Task<Banner> CreateBanner(BannerDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _dapr.InvokeMethodAsync<BannerDto, Banner>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/banner/banner",
                    dto,
                    cancellationToken);

                return response;
            }
            catch(Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Banner?> GetBanner(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result =  await _dapr.InvokeMethodAsync<Banner>(
                   HttpMethod.Get,
                   SERVICE_APP_ID,
                   $"api/banner/banner/{id}",
                   cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<IEnumerable<Banner>> GetAllBanners()
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<IEnumerable<Banner>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/banner/banners"); 
                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<Banner> UpdateBanner(Guid id, BannerDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<BannerDto, Banner>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/banner/banner/{id}",
                    dto,
                    cancellationToken);

                return result;
            }
            catch(Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<bool> DeleteBanner(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.InvokeMethodAsync(
                   HttpMethod.Delete,
                   SERVICE_APP_ID,
                   $"api/banner/banner/{id}",
                   cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
       

        public async Task<BannerImage?> GetImage(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<BannerImage>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/banner/banner/image/{id}",
                    cancellationToken);

                return result;
            }
            catch(Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<IEnumerable<BannerImage>> GetAllImages()
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<IEnumerable<BannerImage>>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    "api/banner/banner/images");

                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<BannerImage> UploadImage(BannerImageUploadRequest form, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<BannerImageUploadRequest, BannerImage>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    "api/banner/banner/image",
                    form,
                    cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<BannerImage> UpdateImage(Guid id, BannerImageUpdateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<BannerImageUpdateDto, BannerImage>(
                    HttpMethod.Put,
                    SERVICE_APP_ID,
                    $"api/banner/banner/image/{id}",
                    dto,
                    cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<bool> DeleteImage(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    $"api/banner/banner/image/{id}",
                    cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
    }
}
