using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Text.Json;
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
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error while creating company profile for user ID: {UserId}", dto.UserId);
                throw;
            }
        }
        public async Task<CompanyProfileEntity?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.GetStateAsync<CompanyProfileEntity>(ConstantValues.CompanyStoreName, id.ToString(), cancellationToken: cancellationToken);
                if(result == null)
                    throw new KeyNotFoundException($"Company with id '{id}' was not found.");
                return result;
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
        public async Task<string> UpdateCompany(CompanyProfileDto dto, Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await ConvertDtoToEntity(dto, id, cancellationToken);
                entity.UpdatedUtc = DateTime.UtcNow;
                entity.UpdatedBy = dto.UserId;

                await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, id.ToString(), entity);

                var keys = await GetIndex();
                if (!keys.Contains(id.ToString()))
                {
                    keys.Add(id.ToString());
                    await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, ConstantValues.CompanyIndexKey, keys);
                }

                return "Company Profile Updated Successfully"; 
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
                var entity = await _dapr.GetStateAsync<CompanyProfileEntity>(ConstantValues.CompanyStoreName, id.ToString(), cancellationToken: cancellationToken);

                if (entity == null)
                {
                    throw new KeyNotFoundException($"Company with ID {id} not found.");
                }

                entity.IsActive = false;

                await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, id.ToString(), entity, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while soft deleting company profile with ID: {Id}", id);
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

                if (!Enum.IsDefined(typeof(VerticalType), dto.VerticalId))
                    throw new InvalidOperationException($"Invalid VerticalId: {dto.VerticalId}");

                return new CompanyProfileEntity
                {
                    Id = id,
                    VerticalId = dto.VerticalId,
                    CategoryId = dto.CategoryId,
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
                    Status = dto.Status,
                    CreatedBy = dto.UserId,
                    CreatedUtc = DateTime.UtcNow,
                    IsActive = true
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

            }

            using var finalStream = new MemoryStream(fileBytes);
            var savedPath = await _fileStorage.SaveFile(finalStream, filePath, cancellationToken);
            return ToRelative(savedPath);
        }

        public async Task<List<CompanyProfileCompletionStatusDto>> GetCompanyProfileCompletionStatus(
        Guid userId,
        VerticalType vertical,
        CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);
                var companies = allCompanies
                    .Where(c => c.UserId == userId &&
                                Enum.IsDefined(typeof(VerticalType), c.VerticalId) &&
                                (VerticalType)c.VerticalId == vertical)
                    .ToList();

                var list = new List<CompanyProfileCompletionStatusDto>();

                foreach (var company in companies)
                {
                    var requiredFields = new Dictionary<string, Func<CompanyProfileEntity, bool>>
            {
                { "CompanyLogo", c => !string.IsNullOrWhiteSpace(c.CompanyLogo) },
                { "BusinessName", c => !string.IsNullOrWhiteSpace(c.BusinessName) },
                { "Country", c => !string.IsNullOrWhiteSpace(c.Country) },
                { "City", c => !string.IsNullOrWhiteSpace(c.City) },
                { "PhoneNumber", c => !string.IsNullOrWhiteSpace(c.PhoneNumber) },
                { "Email", c => !string.IsNullOrWhiteSpace(c.Email) },
                { "StartDay", c => !string.IsNullOrWhiteSpace(c.StartDay) },
                { "EndDay", c => !string.IsNullOrWhiteSpace(c.EndDay) },
                { "StartHour", c => c.StartHour != TimeSpan.Zero },
                { "EndHour", c => c.EndHour != TimeSpan.Zero },
                { "NatureOfBusiness", c => !string.IsNullOrWhiteSpace(c.NatureOfBusiness) },
                { "CompanySize", c => c.CompanySize != default },
                { "CompanyType", c => c.CompanyType != default },
                { "UserDesignation", c => !string.IsNullOrWhiteSpace(c.UserDesignation) },
                { "BusinessDescription", c => !string.IsNullOrWhiteSpace(c.BusinessDescription) },
                { "CRNumber", c => c.CRNumber > 0 },
                { "VerticalId", c => c.VerticalId > 0 },
                { "CRDocument", c => !string.IsNullOrWhiteSpace(c.CRDocument) },
            };

                    var pendingFields = requiredFields
                        .Where(kvp => !kvp.Value(company))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    var completion = 100 - (pendingFields.Count * 100 / requiredFields.Count);

                    list.Add(new CompanyProfileCompletionStatusDto
                    {
                        CompletionPercentage = completion,
                        PendingFields = pendingFields,
                        BusinessName = company.BusinessName,
                        CompanyId = company.Id
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating profile completion status");
                throw;
            }
        }

        public async Task<List<CompanyProfileVerificationStatusDto>> GetVerificationStatus(
        Guid userId,
        VerticalType verticalType,
        CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);
                var companies = allCompanies
                    .Where(c => c.UserId == userId &&
                                Enum.IsDefined(typeof(VerticalType), c.VerticalId) &&
                                (VerticalType)c.VerticalId == verticalType)
                    .ToList();

                return companies.Select(c => new CompanyProfileVerificationStatusDto
                {
                    CompanyId = c.Id,
                    BusinessName = c.BusinessName,
                    VerticalId = c.VerticalId,
                    IsVerified = c.IsVerified,
                    Status = c.Status?.ToString() ?? "Pending"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification status list");
                throw;
            }
        }

        public async Task<string> ApproveCompany(Guid userId, CompanyApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);
                var company = allCompanies.FirstOrDefault(c => c.Id == dto.CompanyId);

                if (company == null)
                    throw new KeyNotFoundException($"Company with ID {dto.CompanyId} not found.");

                company.IsVerified = dto.IsVerified ?? false;
                company.Status = dto.Status;
                company.UpdatedUtc = DateTime.UtcNow;
                company.UpdatedBy = userId;

                await _dapr.SaveStateAsync(
                   ConstantValues.CompanyStoreName,
                   company.Id.ToString(),
                   company,
                   cancellationToken: cancellationToken
                );

                return "Company Profile Approved Successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving company with ID {CompanyId}", dto.CompanyId);
                throw;
            }
        }

        public async Task<CompanyApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);
                var company = allCompanies.FirstOrDefault(c => c.Id == companyId);

                if (company == null) return null;

                return new CompanyApprovalResponseDto
                {
                    CompanyId = company.Id,
                    Name = company.BusinessName,
                    IsVerified = company.IsVerified,
                    StatusId = company.Status,
                    StatusName = company.Status.ToString(),
                    UpdatedUtc = company.UpdatedUtc ?? DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company with ID {CompanyId}");
                throw;
            }
        }
        public async Task<List<CompanyProfileVerificationStatusDto>> VerificationStatus(Guid userId, bool isVerified, CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);

                var filtered = allCompanies
                    .Where(c => c.UserId == userId && c.IsVerified == isVerified)
                    .Select(c => new CompanyProfileVerificationStatusDto
                    {
                        CompanyId = c.Id,
                        BusinessName = c.BusinessName,
                        VerticalId = c.VerticalId,
                        IsVerified = c.IsVerified,
                        Status = c.Status?.ToString() ?? "Pending"
                    })
                    .ToList();

                return filtered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching verification status for user {UserId} with isVerified = {IsVerified}", userId, isVerified);
                throw;
            }
        }
    }
}