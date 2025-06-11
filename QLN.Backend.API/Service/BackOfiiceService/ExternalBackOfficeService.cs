// QLN.Backend.API/Service/BackOffice/ExternalBackOfficeService.cs
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IBackOfficeService;

namespace QLN.Backend.API.Service.BackOffice
{
    public class ExternalBackOfficeService : IBackOfficeService<BackofficemasterIndex>
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalBackOfficeService> _logger;
        private readonly string _appId = ConstantValues.ClassifiedServiceApp;
        // e.g. "classifieds-ms" or whatever you named your back-office microservice

        public ExternalBackOfficeService(
            DaprClient dapr,
            ILogger<ExternalBackOfficeService> logger)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task UpsertState(BackofficemasterIndex item, CancellationToken ct)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var route = ToSlug(item.EntityType);
            var method = $"api/classifieds/landing/{route}";

            _logger.LogDebug("Upserting back-office state via Dapr: {AppId}/{Method}", _appId, method);
            await _dapr.InvokeMethodAsync<BackofficemasterIndex, object>(
                HttpMethod.Post, _appId, method, item, cancellationToken: ct);
        }

        public async Task<BackofficemasterIndex?> GetByIdState(string id, CancellationToken ct)
        {
            throw new NotSupportedException("GET By ID is not used by back-office endpoints");
        }

        public async Task<IList<BackofficemasterIndex>> GetAllState(CancellationToken ct)
        {
            throw new NotSupportedException("GetAllState is not used by back-office endpoints");
        }

        public async Task DeleteState(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required", nameof(id));

            var parts = id.Split('-', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                throw new ArgumentException("Id must be in format '<vertical>-<entityType>-<guid>'", nameof(id));

            var (vertical, entityType) = (parts[0], parts[1]);
            var route = ToSlug(entityType);
            var method = $"api/classifieds/landing/{route}/{id}";

            _logger.LogDebug("Deleting back-office state via Dapr: {AppId}/{Method}", _appId, method);
            await _dapr.InvokeMethodAsync(
                HttpMethod.Delete, _appId, method, cancellationToken: ct);
        }

        /// <summary>
        /// Converts a PascalCase EntityType (e.g. "HeroBanner") into a kebab-case routeSegment ("hero-banner").
        /// </summary>
        private static string ToSlug(string pascal)
        {
            return string.Concat(pascal
                .Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + c : c.ToString()))
                .ToLowerInvariant();
        }
    }
}
