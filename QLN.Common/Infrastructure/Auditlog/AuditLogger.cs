using Dapr.Client;
using QLN.Common.Infrastructure.Constants;
using System.Text.Json;
using QLN.Common.DTO_s.AuditLog;

namespace QLN.Common.Infrastructure.Auditlog
{
    public class AuditLogger
    {
        private readonly DaprClient _dapr;

        public AuditLogger(DaprClient dapr)
        {
            _dapr = dapr;
        }

        public async Task CreateAuditLog(
            Guid id,
            string module,
            string httpMethod,
            string apiEndpoint,
            string successMessage,
            string createdBy,
            object? payload,
            CancellationToken cancellationToken)
        {
            var log = new AuditLog
            {
                Id = id,
                Module = module,
                HttpMethod = httpMethod,
                ApiEndpoint = apiEndpoint,
                SuccessMessage = successMessage,
                CreatedBy = createdBy,
                Payload = payload != null ? JsonSerializer.Serialize(payload) : null,
                CreatedUtc = DateTime.UtcNow
            };

            var key = $"auditlog-{module}-{id}";
            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, key, log, cancellationToken: cancellationToken);
        }
        public async Task UpdateAuditLog(
           Guid id,
           string module,
           string httpMethod,
           string apiEndpoint,
           string successMessage,
           string updatedBy,
           object? payload,
           CancellationToken cancellationToken)
        {
            var log = new UpdateAuditLog
            {
                Id = id,
                Module = module,
                HttpMethod = httpMethod,
                ApiEndpoint = apiEndpoint,
                SuccessMessage = successMessage,
                UpdatedBy = updatedBy,
                Payload = payload != null ? JsonSerializer.Serialize(payload) : null,
                UpdatedUtc = DateTime.UtcNow
            };

            var key = $"auditlog-{module}-{id}";
            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, key, log, cancellationToken: cancellationToken);
        }
    }
}
