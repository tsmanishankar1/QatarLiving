using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService.BannerService;

namespace QLN.Backend.API.Service.ClassifiedService
{
    public class ExternalClassifiedService : IClassifiedService
    {
        private const string SERVICE_APP_ID = ConstantValues.ClassifiedServiceApp;
        private const string Vertical = ConstantValues.ClassifiedsVertical;

        private readonly DaprClient _dapr;
        private readonly IEventlogger _log;

        public ExternalClassifiedService(DaprClient dapr, IEventlogger log)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<IEnumerable<ClassifiedIndexDto>> Search(CommonSearchRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            try
            {
                var result = await _dapr.InvokeMethodAsync<
                    CommonSearchRequest,
                    ClassifiedIndexDto[]>(
                        HttpMethod.Post,
                        SERVICE_APP_ID,
                        $"/api/{Vertical}/search",
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

        public async Task<ClassifiedIndexDto?> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id is required", nameof(id));

            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedIndexDto>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"/api/{Vertical}/{id}"
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw;
            }
        }

        public async Task<string> Upload(ClassifiedIndexDto document)
        {
            if (document is null) throw new ArgumentNullException(nameof(document));

            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedIndexDto, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    $"/api/{Vertical}/upload",
                    document,
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                _log.LogException(ex);
                throw new InvalidOperationException(
                    $"Dapr invoke to '/api/{Vertical}/upload' failed: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<ClassifiedLandingPageResponse> GetLandingPage()
        {
            try
            {
                return await _dapr.InvokeMethodAsync<ClassifiedLandingPageResponse>(
                    HttpMethod.Get,
                    SERVICE_APP_ID,
                    $"/api/{Vertical}/landing"
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
