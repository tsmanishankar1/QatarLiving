using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Text.Json;
using Dapr.Client;
using QLN.Common.Infrastructure.Constants;
using SixLabors.ImageSharp;

namespace QLN.Company.MS.Service
{
    public class InternalCompanyService : ICompanyService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<InternalCompanyService> _logger;
        public InternalCompanyService(
            DaprClient dapr,
            ILogger<InternalCompanyService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = Guid.NewGuid();
                var entity = EntityForCreate(dto, id);

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
                _logger.LogError(ex, "Error while creating company profile for user ID: {UserId}", dto.UserId);
                throw;
            }
        }

        private CompanyProfileDto EntityForCreate(CompanyProfileDto dto, Guid id)
        {
            return new CompanyProfileDto
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
                CompanyLogo = dto.CompanyLogo,
                CRDocument = dto.CRDocument,
                IsVerified = dto.IsVerified,
                Status = dto.Status,
                CreatedBy = dto.UserId,
                CreatedUtc = DateTime.UtcNow,
                IsActive = true
            };
        }
        public async Task<CompanyProfileDto?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.GetStateAsync<CompanyProfileDto>(ConstantValues.CompanyStoreName, id.ToString(), cancellationToken: cancellationToken);
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
        public async Task<List<CompanyProfileDto>> GetAllCompanies(CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await GetIndex();
                if (!keys.Any()) return new List<CompanyProfileDto>();

                var items = await _dapr.GetBulkStateAsync(ConstantValues.CompanyStoreName, keys, parallelism: 10);

                return items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                    .Select(i => JsonSerializer.Deserialize<CompanyProfileDto>(i.Value!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!)
                    .Where(e => e.Id != Guid.Empty)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving all company profiles.");
                throw;
            }
        }
        public async Task<string> UpdateCompany(CompanyProfileDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _dapr.GetStateAsync<CompanyProfileDto>(ConstantValues.CompanyStoreName, dto.Id.ToString(), cancellationToken: cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException($"Company with ID {dto.Id} was not found.");

                var entity = EntityForUpdate(dto, existing);

                await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, dto.Id.ToString(), entity);

                var keys = await GetIndex();
                if (!keys.Contains(dto.Id.ToString()))
                {
                    keys.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, ConstantValues.CompanyIndexKey, keys);
                }

                return "Company Profile Updated Successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating company profile with ID: {Id}", dto.Id);
                throw;
            }
        }
        private CompanyProfileDto EntityForUpdate(CompanyProfileDto dto, CompanyProfileDto existing)
        {
            return new CompanyProfileDto
            {
                Id = dto.Id,
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
                CompanyLogo = dto.CompanyLogo,
                CRDocument = dto.CRDocument,
                IsVerified = dto.IsVerified,
                Status = dto.Status,
                CreatedBy = existing.CreatedBy,
                CreatedUtc = existing.CreatedUtc,
                UpdatedBy = dto.UserId,
                UpdatedUtc = DateTime.UtcNow,
                IsActive = true
            };
        }
        public async Task DeleteCompany(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _dapr.GetStateAsync<CompanyProfileDto>(ConstantValues.CompanyStoreName, id.ToString(), cancellationToken: cancellationToken);

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
        public async Task<List<CompanyProfileCompletionStatusDto>> GetCompanyProfileCompletionStatus(
        Guid userId,
        VerticalType vertical,
        CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);
                var companies = allCompanies
                    .Where(c => Enum.IsDefined(typeof(VerticalType), c.VerticalId) &&
                                (VerticalType)c.VerticalId == vertical)
                    .ToList();

                var list = new List<CompanyProfileCompletionStatusDto>();

                foreach (var company in companies)
                {
                    var requiredFields = new Dictionary<string, Func<CompanyProfileDto, bool>>
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
        public async Task<string> ApproveCompany(Guid userId, CompanyApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);
                var company = allCompanies.FirstOrDefault(c => c.Id == dto.CompanyId);
                if (company == null)
                    throw new KeyNotFoundException($"Company with ID {dto.CompanyId} not found.");

                var wasPreviouslyVerified = company.IsVerified;
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
        public async Task<List<CompanyProfileVerificationStatusDto>> VerificationStatus(Guid userId, VerticalType vertical, bool isVerified, CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);

                var filtered = allCompanies
                    .Where(c => c.IsVerified == isVerified && c.VerticalId == vertical)
                    .Select(c => new CompanyProfileVerificationStatusDto
                    {
                        CompanyId = c.Id,
                        BusinessName = c.BusinessName,
                        VerticalId = c.VerticalId,
                        IsVerified = c.IsVerified,
                        Status = c.Status
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
        public async Task<List<CompanyProfileDto>> GetCompaniesByTokenUser(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var all = await GetAllCompanies(cancellationToken);
                return all
                    .Where(c => c.UserId == userId)
                    .ToList();
            }
            catch(Exception)
            {
                throw;
            }
        }
    }
}