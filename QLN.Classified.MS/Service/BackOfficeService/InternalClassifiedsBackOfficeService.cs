using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IBackOfficeService;

namespace QLN.Classified.MS.Service.BackOfficeService
{
    public class InternalClassifiedsBackOfficeService : IBackOfficeService<LandingBackOfficeIndex>
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<InternalClassifiedsBackOfficeService> _logger;
        private const string STORE_NAME = "classifiedsbackoffice";
        private const string KEY_LIST = "classifieds-backoffice-keys";

        public InternalClassifiedsBackOfficeService(
            DaprClient dapr,
            ILogger<InternalClassifiedsBackOfficeService> logger)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> UpsertState(LandingBackOfficeIndex item, CancellationToken ct)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "Item is required.");

            if (string.IsNullOrWhiteSpace(item.Id))
                throw new InvalidOperationException("Item.Id must be set before upserting.");

            try
            {
                _logger.LogInformation("Upserting document with Id={Id} to {Store}", item.Id, STORE_NAME);

                await _dapr.SaveStateAsync(STORE_NAME, item.Id, item, cancellationToken: ct);

                var keys = await _dapr.GetStateAsync<List<string>>(STORE_NAME, KEY_LIST, cancellationToken: ct)
                           ?? new List<string>();

                if (!keys.Contains(item.Id))
                {
                    keys.Add(item.Id);
                    await _dapr.SaveStateAsync(STORE_NAME, KEY_LIST, keys, cancellationToken: ct);
                }

                _logger.LogInformation("Successfully upserted document Id={Id}", item.Id);
                return "Document uploaded successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert document Id={Id}", item.Id);
                throw;
            }
        }

        public async Task DeleteState(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required for deletion.", nameof(id));

            try
            {
                _logger.LogInformation("Deleting document with Id={Id} from {Store}", id, STORE_NAME);

                await _dapr.DeleteStateAsync(STORE_NAME, id, cancellationToken: ct);

                var keys = await _dapr.GetStateAsync<List<string>>(STORE_NAME, KEY_LIST, cancellationToken: ct)
                           ?? new List<string>();

                if (keys.Remove(id))
                {
                    await _dapr.SaveStateAsync(STORE_NAME, KEY_LIST, keys, cancellationToken: ct);
                    _logger.LogInformation("Removed Id={Id} from key list", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete document with Id={Id}", id);
                throw;
            }
        }

        public Task<LandingBackOfficeIndex?> GetByIdState(string id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<IList<LandingBackOfficeIndex>> GetAllState(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
