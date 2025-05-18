using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Text.Json;
using System.Windows.Input;
using Dapr.Client; 

namespace QLN.Company.MS.Service
{
    public class InternalCompanyService : ICompanyService
    {
        private readonly DaprClient _dapr;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<InternalCompanyService> _logger;
        private const string StoreName = "companystatestore";
        private const string IndexKey = "company-index";

        public InternalCompanyService(DaprClient dapr, IWebHostEnvironment env, ILogger<InternalCompanyService> logger)
        {
            _dapr = dapr;
            _env = env;
            _logger = logger;
        }

        public async Task<CompanyProfileEntity> CreateAsync(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                var entity = await ConvertDtoToEntityAsync(dto, id, cancellationToken);

                await _dapr.SaveStateAsync(StoreName, id.ToString(), entity);

                var keys = await GetIndexAsync();
                if (!keys.Contains(id.ToString()))
                {
                    keys.Add(id.ToString());
                    await _dapr.SaveStateAsync(StoreName, IndexKey, keys);
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating company profile.");
                throw;
            }
        }

        public async Task<CompanyProfileEntity?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.GetStateAsync<CompanyProfileEntity>(StoreName, id.ToString(), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving company profile with ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CompanyProfileEntity>> GetAllAsync()
        {
            try
            {
                var keys = await GetIndexAsync();
                if (!keys.Any()) return Enumerable.Empty<CompanyProfileEntity>();

                var items = await _dapr.GetBulkStateAsync(StoreName, keys, parallelism: 10);

                return items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                    .Select(i => JsonSerializer.Deserialize<CompanyProfileEntity>(i.Value!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!)
                    .Where(e => e.Id != Guid.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving all company profiles.");
                throw;
            }
        }

        public async Task<CompanyProfileEntity> UpdateAsync(Guid id, CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await ConvertDtoToEntityAsync(dto, id, cancellationToken);
                await _dapr.SaveStateAsync(StoreName, id.ToString(), entity);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating company profile with ID: {Id}", id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.DeleteStateAsync(StoreName, id.ToString(), cancellationToken: cancellationToken);

                var keys = await GetIndexAsync();
                keys.Remove(id.ToString());
                await _dapr.SaveStateAsync(StoreName, IndexKey, keys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting company profile with ID: {Id}", id);
                throw;
            }
        }

        private async Task<List<string>> GetIndexAsync()
        {
            try
            {
                var result = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey);
                return result ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving index.");
                throw;
            }
        }

        private async Task<CompanyProfileEntity> ConvertDtoToEntityAsync(CompanyProfileDto dto, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var root = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "company", id.ToString());
                Directory.CreateDirectory(root);

                var logoPath = dto.CompanyLogo is not null
                    ? await SaveFileAsync(dto.CompanyLogo, Path.Combine(root, "logo"))
                    : null;

                var crPath = dto.CRDocument is not null
                    ? await SaveFileAsync(dto.CRDocument, Path.Combine(root, "cr"))
                    : null;

                return new CompanyProfileEntity
                {
                    Id = id,
                    VerticalId = dto.VerticalId,
                    BusinessName = dto.BusinessName,
                    Country = dto.Country,
                    City = dto.City,
                    BranchLocations = dto.BranchLocations,
                    PhoneNumber = dto.PhoneNumber,
                    WhatsAppNumber = dto.WhatsAppNumber,
                    Email = dto.Email,
                    WebsiteUrl = dto.WebsiteUrl,
                    FacebookUrl = dto.FacebookUrl,
                    InstagramUrl = dto.InstagramUrl,
                    StartDay = dto.StartDay,
                    EndDay = dto.EndDay,
                    StartHour = dto.StartHour,
                    EndHour = dto.EndHour,
                    NatureOfBusiness = dto.NatureOfBusiness,
                    CompanySize = dto.CompanySize,
                    CompanyType = dto.CompanyType,
                    UserDesignation = dto.UserDesignation,
                    BusinessDescription = dto.BusinessDescription,
                    CRNumber = dto.CRNumber,
                    CompanyLogo = logoPath,
                    CRDocumentPath = crPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while converting DTO to entity for ID: {Id}", id);
                throw;
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file, string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, file.FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while saving file {FileName} to {Directory}", file.FileName, directory);
                throw;
            }
        }
    }
}
