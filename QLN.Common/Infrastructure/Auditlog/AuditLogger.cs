using Dapr.Client;
using QLN.Common.DTO_s.AuditLog;
using QLN.Common.Infrastructure.Constants;
using System.Text.Json;

namespace QLN.Common.Infrastructure.Auditlog
{
    public class AuditLogger
    {
        private readonly DaprClient _dapr;

        public AuditLogger(DaprClient dapr)
        {
            _dapr = dapr;
        }

        public async Task LogAsync(string action, string entity, string entityId, string performedBy, string userName, object details, CancellationToken cancellationToken)
        {
            var log = new AuditLog
            {
                Action = action,
                Entity = entity,
                EntityId = entityId,
                PerformedBy = performedBy,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(details)
            };

            var key = $"audit-{log.Id}";
            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, key, log, cancellationToken : cancellationToken);

            var index = await _dapr.GetStateAsync<List<string>>(ConstantValues.Services.StoreName, ConstantValues.Audit.AuditIndexKey, cancellationToken : cancellationToken ) ?? new();
            index.Add(key);
            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, ConstantValues.Audit.AuditIndexKey, index, cancellationToken : cancellationToken);
        }
    }
}
