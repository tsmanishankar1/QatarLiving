using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using System.Text.Json;

namespace QLN.Common.Infrastructure.Auditlog
{
    public class AuditLogger
    {
        private readonly QLLogContext _dbContext;

        public AuditLogger(QLLogContext dbContext)
        {
            _dbContext = dbContext;
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
                CreatedUtc = DateTime.UtcNow,
                Payload = payload != null ? JsonSerializer.Serialize(payload) : null
            };

            await _dbContext.AuditLogs.AddAsync(log, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task LogAuditAsync(
            string module,
            string httpMethod,
            string apiEndpoint,
            string message,
            string createdBy,
            object? payload,
            CancellationToken cancellationToken)
        {
            await CreateAuditLog(
                id: Guid.NewGuid(),
                module: module,
                httpMethod: httpMethod,
                apiEndpoint: apiEndpoint,
                successMessage: message,
                createdBy: createdBy,
                payload: payload,
                cancellationToken: cancellationToken
            );
        }

        public async Task LogExceptionAsync(
            string module,
            string apiEndpoint,
            Exception exception,
            string createdBy,
            CancellationToken cancellationToken)
        {
            var log = new ErrorLog
            {
                Id = Guid.NewGuid(),
                Module = module,
                ApiEndpoint = apiEndpoint,
                ExceptionMessage = exception.Message,
                StackTrace = exception.StackTrace,
                CreatedBy = createdBy,
                CreatedUtc = DateTime.UtcNow
            };

            await _dbContext.ErrorLogs.AddAsync(log, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
