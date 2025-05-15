using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dapr.Client;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Backend.API.Service.ClassifiedService
{
    public class ExternalClassifiedService : IClassifiedService
    {
        private const string SERVICE_APP_ID = "qln-classified-ms";
        private readonly DaprClient _dapr;
        private readonly IEventlogger _log;

        public ExternalClassifiedService(DaprClient dapr, IEventlogger log)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<IEnumerable<ClassifiedIndexDto>> SearchAsync(
            string vertical,
            ClassifiedSearchRequest request)
        {
            try
            {
                var result = await _dapr.InvokeMethodAsync<
                    ClassifiedSearchRequest,
                    ClassifiedIndexDto[]>(
                        HttpMethod.Post,
                        SERVICE_APP_ID,
                        $"api/{vertical}/search",
                        request
                    );

                return result ?? Array.Empty<ClassifiedIndexDto>();
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<ClassifiedIndexDto?> GetByIdAsync(
            string vertical,
            string id)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedIndexDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{vertical}/{id}"
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<string> UploadAsync(
            string vertical,
            ClassifiedIndexDto document)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<
                    ClassifiedIndexDto,
                    string>(
                        HttpMethod.Post,
                        SERVICE_APP_ID,
                        $"api/{vertical}/upload",
                        document
                    );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<ClassifiedLandingPageResponse> GetLandingPageAsync(
            string vertical)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedLandingPageResponse>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"api/{vertical}/landing"
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }
    }
}
