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
        private const string StoreName = "companystatestore";
        private const string IndexKey = "company-index";
        private readonly IWebHostEnvironment _env;

        public InternalCompanyService(DaprClient dapr, IWebHostEnvironment env)
        {
            _dapr = dapr;
            _env = env;
        }
        public async Task<CompanyProfileEntity> CreateAsync(CompanyProfileDto dto, HttpContext context)
        {
            var id = Guid.NewGuid();
            var entity = await ConvertDtoToEntityAsync(dto, id, context);

            await _dapr.SaveStateAsync(StoreName, id.ToString(), entity);

            var keys = await GetIndexAsync();
            if (!keys.Contains(id.ToString()))
            {
                keys.Add(id.ToString());
                await _dapr.SaveStateAsync(StoreName, IndexKey, keys);
            }

            return entity;
        }

        public async Task<CompanyProfileEntity?> GetAsync(Guid id)
        {
            return await _dapr.GetStateAsync<CompanyProfileEntity>(StoreName, id.ToString());
        }

        public async Task<IEnumerable<CompanyProfileEntity>> GetAllAsync()
        {
            var keys = await GetIndexAsync();
            if (!keys.Any()) return Enumerable.Empty<CompanyProfileEntity>();

            var items = await _dapr.GetBulkStateAsync(StoreName, keys, 10);

            return items
                .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                .Select(i => JsonSerializer.Deserialize<CompanyProfileEntity>(i.Value!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!)
                .Where(e => e.Id != Guid.Empty);
        }

        public async Task<CompanyProfileEntity> UpdateAsync(Guid id, CompanyProfileDto dto, HttpContext ctx)
        {
            var entity = await ConvertDtoToEntityAsync(dto, id, ctx);
            await _dapr.SaveStateAsync(StoreName, id.ToString(), entity);
            return entity;
        }

        public async Task DeleteAsync(Guid id)
        {
            await _dapr.DeleteStateAsync(StoreName, id.ToString());
            var keys = await GetIndexAsync();
            keys.Remove(id.ToString());
            await _dapr.SaveStateAsync(StoreName, IndexKey, keys);
        }

        private async Task<List<string>> GetIndexAsync()
        {
            var result = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey);
            return result ?? new List<string>();
        }

        private async Task<CompanyProfileEntity> ConvertDtoToEntityAsync(CompanyProfileDto dto, Guid id, HttpContext ctx)
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
                CRDocumentPath = crPath
            };
        }

        private async Task<string> SaveFileAsync(IFormFile file, string directory)
        {
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, file.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return filePath;
        }
    }
}
