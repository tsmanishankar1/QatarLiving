using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IBackOfficeService;

namespace QLN.Backend.API.Service.BackOffice
{
    public class ExternalLandingBackOfficeService : IBackOfficeService<LandingBackOfficeIndex>
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalLandingBackOfficeService> _logger;
        private readonly string SERVICE_APP_ID = ConstantValues.ServiceAppIds.ClassifiedServiceApp;

        public ExternalLandingBackOfficeService(
            DaprClient dapr,
            ILogger<ExternalLandingBackOfficeService> logger)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> UpsertState(LandingBackOfficeIndex item, CancellationToken cancellationToken)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "Backoffice master item is required.");

            // Build the request DTO
            var request = new LandingBackOfficeRequestDto
            {
                Title = item.Title,
                Description = item.Description,
                Order = item.Order,
                ParentId = item.ParentId,
                IsActive = item.IsActive,
                RediectUrl = item.RediectUrl,
                ImageUrl = item.ImageUrl,
                ListingCount = item.ListingCount,
                RotationSeconds = item.RotationSeconds,
                AdId = item.AdId,
                PayloadJson = !string.IsNullOrWhiteSpace(item.PayloadJson)
                                    ? JsonSerializer.Deserialize<CommonSearchRequest>(item.PayloadJson)
                                    : null
            };

            // Determine vertical and route
            var parts = item.Id.Split('-', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new ArgumentException("Item.Id must be in format '<vertical>-<entityType>-<guid>'", nameof(item.Id));

            var vertical = parts[0].ToLowerInvariant();
            var entityType = parts[1];
            var route = ToSlug(entityType);
            var method = $"/api/{vertical}/landing/{route}";

            _logger.LogInformation("Invoking Dapr POST to {AppId}{Method} for Id={Id}",
                                    SERVICE_APP_ID, method, item.Id);

            try
            {
                // POST the request DTO (not the index)
                var result = await _dapr.InvokeMethodAsync<LandingBackOfficeRequestDto, string>(
                    HttpMethod.Post,
                    SERVICE_APP_ID,
                    method,
                    request,
                    cancellationToken
                );

                _logger.LogInformation("Successfully upserted Id={Id} to back-office state store", item.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert Id={Id} via Dapr. Route={Route}", item.Id, method);
                throw;
            }
        }

        public Task<LandingBackOfficeIndex?> GetByIdState(string id, CancellationToken ct)
        {
            throw new NotSupportedException("GetByIdState is not supported in ExternalBackOfficeService.");
        }

        public Task<IList<LandingBackOfficeIndex>> GetAllState(CancellationToken ct)
        {
            throw new NotSupportedException("GetAllState is not supported in ExternalBackOfficeService.");
        }

        public async Task DeleteState(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required for deletion.", nameof(id));

            try
            {
                var parts = id.Split('-', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    throw new ArgumentException("Id must be in format '<vertical>-<entityType>-<guid>'", nameof(id));

                var vertical = parts[0].ToLowerInvariant();
                var entityType = parts[1];
                var route = ToSlug(entityType);

                var method = $"/api/{vertical}/landing/{route}/{id}";

                _logger.LogInformation("Invoking Dapr DELETE to {AppId}{Method} for Id={Id}", SERVICE_APP_ID, method, id);

                await _dapr.InvokeMethodAsync(
                    HttpMethod.Delete,
                    SERVICE_APP_ID,
                    method,
                    cancellationToken: ct
                );

                _logger.LogInformation("Successfully deleted Id={Id} from back-office state", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Id={Id} via Dapr", id);
                throw;
            }
        }

        private static string ToSlug(string pascal)
        {
            return string.Concat(pascal
                .Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + c : c.ToString()))
                .ToLowerInvariant();
        }
    }
}
