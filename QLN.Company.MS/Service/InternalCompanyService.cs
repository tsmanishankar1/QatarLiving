using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Text.Json;
using System.Windows.Input;
using Dapr.Client;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IFileStorage;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace QLN.Company.MS.Service
{
    public class InternalCompanyService : ICompanyService
    {
        private readonly DaprClient _dapr;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<InternalCompanyService> _logger;
        private readonly IFileStorageService _fileStorage;
        public InternalCompanyService(DaprClient dapr, IWebHostEnvironment env, ILogger<InternalCompanyService> logger, IFileStorageService fileStorage)
        {
            _dapr = dapr;
            _env = env;
            _logger = logger;
            _fileStorage = fileStorage;
        }
        public async Task<string> CreateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                var entity = await ConvertDtoToEntity(dto, id, cancellationToken);
                await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, id.ToString(), entity);
                var keys = await GetIndex();
                if (!keys.Contains(id.ToString()))
                {
                    keys.Add(id.ToString());
                    await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, ConstantValues.CompanyIndexKey, keys);
                }
                return "Company Created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating company profile.");
                throw;
            }
        }

        public async Task<CompanyProfileEntity?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.GetStateAsync<CompanyProfileEntity>(ConstantValues.CompanyStoreName, id.ToString(), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving company profile with ID: {Id}", id);
                throw;
            }
        }
        public async Task<List<CompanyProfileEntity>> GetAllCompanies(CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await GetIndex();
                if (!keys.Any()) return new List<CompanyProfileEntity>();

                var items = await _dapr.GetBulkStateAsync(ConstantValues.CompanyStoreName, keys, parallelism: 10);

                return items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                    .Select(i => JsonSerializer.Deserialize<CompanyProfileEntity>(i.Value!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!)
                    .Where(e => e.Id != Guid.Empty)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving all company profiles.");
                throw;
            }
        }
        public async Task<CompanyProfileEntity> UpdateCompany(Guid id, CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await ConvertDtoToEntity(dto, id, cancellationToken);
                await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, id.ToString(), entity);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating company profile with ID: {Id}", id);
                throw;
            }
        }

        public async Task DeleteCompany(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.DeleteStateAsync(ConstantValues.CompanyStoreName, id.ToString(), cancellationToken: cancellationToken);

                var keys = await GetIndex();
                keys.Remove(id.ToString());
                await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, ConstantValues.CompanyIndexKey, keys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting company profile with ID: {Id}", id);
                throw;
            }
        }

        private async Task<List<string>> GetIndex()
        {
            try
            {
                var result = await _dapr.GetStateAsync<List<string>>(ConstantValues.CompanyStoreName, ConstantValues.CompanyIndexKey);
                return result ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving index.");
                throw;
            }
        }
        private async Task<CompanyProfileEntity> ConvertDtoToEntity(
            CompanyProfileDto dto,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? "wwwroot";
                var root = Path.Combine(webRoot, "uploads", "company", id.ToString());
                Directory.CreateDirectory(root);

                var logoPath = !string.IsNullOrWhiteSpace(dto.CompanyLogo)
                    ? await SaveBase64FileAsync(dto.CompanyLogo, Path.Combine(root, "logo", "company-logo.png"), cancellationToken)
                    : null;

                var crPath = !string.IsNullOrWhiteSpace(dto.CRDocument)
                    ? await SaveBase64FileAsync(dto.CRDocument, Path.Combine(root, "cr", "cr-document.pdf"), cancellationToken)
                    : null;

                return new CompanyProfileEntity
                {
                    Id = id,
                    VerticalId = dto.VerticalId,
                    UserId = dto.UserId,
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
                    CRDocument = crPath,
                    IsVerified = dto.IsVerified,
                    Status = dto.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while converting DTO to entity for ID: {Id}", id);
                throw;
            }
        }
        private string? ToRelative(string? physicalPath)
        {
            if (string.IsNullOrWhiteSpace(physicalPath))
                return null;

            var webRoot = _env.WebRootPath ?? "wwwroot";
            var rel = Path
                .GetRelativePath(webRoot, physicalPath)
                .Replace(Path.DirectorySeparatorChar, '/');

            return "/" + rel;
        }
        private async Task<string?> SaveBase64FileAsync(string? base64String, string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return null;

            var base64Parts = base64String.Split(',');
            var actualBase64 = base64Parts.Length > 1 ? base64Parts[1] : base64Parts[0];
            var fileBytes = Convert.FromBase64String(actualBase64);

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (filePath.Contains("cr", StringComparison.OrdinalIgnoreCase))
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                if (!allowedExtensions.Contains(extension))
                    throw new InvalidOperationException("CR Document must be a PDF, JPG, or PNG.");

                var fileSizeInMb = fileBytes.Length / (1024.0 * 1024.0);
                if (fileSizeInMb > 10)
                    throw new InvalidOperationException("CR Document must be less than or equal to 10MB.");
            }

            if (filePath.Contains("logo", StringComparison.OrdinalIgnoreCase))
            {
                var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowedImageExtensions.Contains(extension))
                    throw new InvalidOperationException("Logo must be a PNG or JPG file.");

                using var imageStream = new MemoryStream(fileBytes);
                using var image = await Image.LoadAsync<Rgba32>(imageStream, cancellationToken);

                if (image.Width < 1920 || image.Height < 1200)
                    throw new InvalidOperationException("Logo image must be at least 1920x1200 pixels.");
            }

            using var finalStream = new MemoryStream(fileBytes);
            var savedPath = await _fileStorage.SaveFile(finalStream, filePath, cancellationToken);
            return ToRelative(savedPath);
        }
    }
}