using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.IBackOfficeService;

namespace QLN.Classified.MS.Service.BackOfficeService
{
    public class InternalClassifiedsBackOfficeService : IBackOfficeService<BackofficemasterIndex>
    {
        private readonly DaprClient _dapr;
        private const string STORE_NAME = "classifiedsbackoffice";
        private const string KEY_LIST = "classifieds-backoffice-keys";

        public InternalClassifiedsBackOfficeService(DaprClient dapr)
            => _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));

        public async Task UpsertState(BackofficemasterIndex item, CancellationToken ct)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(item.Id))
                throw new InvalidOperationException("Item.Id must be set before upserting.");

            await _dapr.SaveStateAsync(STORE_NAME, item.Id, item, cancellationToken: ct);

            var keys = await _dapr.GetStateAsync<List<string>>(STORE_NAME, KEY_LIST, cancellationToken: ct)
                       ?? new List<string>();
            if (!keys.Contains(item.Id))
            {
                keys.Add(item.Id);
                await _dapr.SaveStateAsync(STORE_NAME, KEY_LIST, keys, cancellationToken: ct);
            }
        }

        public async Task<BackofficemasterIndex?> GetByIdState(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required", nameof(id));

            return await _dapr.GetStateAsync<BackofficemasterIndex>(STORE_NAME, id, cancellationToken: ct);
        }

        public async Task<IList<BackofficemasterIndex>> GetAllState(CancellationToken ct)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(STORE_NAME, KEY_LIST, cancellationToken: ct)
                       ?? new List<string>();
            if (keys.Count == 0)
                return new List<BackofficemasterIndex>();

            var bulk = await _dapr.GetBulkStateAsync<BackofficemasterIndex>(
                STORE_NAME, keys, cancellationToken: ct, parallelism: 10);

            return bulk
                .Select(r => r.Value)
                .Where(x => x != null)
                .ToList()!;
        }

        public async Task DeleteState(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required", nameof(id));

            await _dapr.DeleteStateAsync(STORE_NAME, id, cancellationToken: ct);

            var keys = await _dapr.GetStateAsync<List<string>>(STORE_NAME, KEY_LIST, cancellationToken: ct)
                       ?? new List<string>();
            if (keys.Remove(id))
            {
                await _dapr.SaveStateAsync(STORE_NAME, KEY_LIST, keys, cancellationToken: ct);
            }
        }
    }
}
