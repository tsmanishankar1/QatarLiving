using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using SixLabors.ImageSharp;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QLN.Company.MS.Service
{
    public class InternalVerifiedCompany : ICompanyVerifiedService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<InternalVerifiedCompany> _logger;
        public InternalVerifiedCompany(
            DaprClient dapr,
            ILogger<InternalVerifiedCompany> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateCompany(VerifiedCompanyDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                Validate(dto);
                var keys = await GetIndex();
                foreach (var key in keys)
                {
                    var existing = await _dapr.GetStateAsync<VerifiedCompanyDto>(ConstantValues.CompanyStoreName, key, cancellationToken: cancellationToken);
                    if (existing != null)
                    {
                        if (existing.UserId == dto.UserId &&
                            existing.UserName == dto.UserName &&
                            existing.Vertical == dto.Vertical &&
                            existing.SubVertical == dto.SubVertical)
                        {
                            throw new ConflictException("A company profile already exists for this user under the same subvertical.");
                        }

                        if (existing.UserId != dto.UserId &&
                            (existing.PhoneNumber == dto.PhoneNumber || existing.Email == dto.Email))
                        {
                            throw new ConflictException("Phone number or email is already used by another user.");
                        }
                    }
                }
                var id = Guid.NewGuid();
                var entity = EntityForCreate(dto, id);
                entity.IsVerified = false;
                await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, id.ToString(), entity);

                if (!keys.Contains(id.ToString()))
                {
                    keys.Add(id.ToString());
                    await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, ConstantValues.CompanyVerifiedIndex, keys);
                }

                return "Company Created successfully";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating company profile for user ID: {UserId}", dto.UserId);
                throw;
            }
        }
        private static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) &&
                   Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }
        public static void Validate(VerifiedCompanyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CompanyName))
                throw new ArgumentException("Business name is required.", nameof(dto.CompanyName));

            if (!Enum.IsDefined(typeof(CompanyType), dto.CompanyType))
                throw new ArgumentException($"Invalid CompanyType: {dto.CompanyType}");

            if (!Enum.IsDefined(typeof(CompanySize), dto.CompanySize))
                throw new ArgumentException($"Invalid CompanySize: {dto.CompanySize}");

            if (!Enum.IsDefined(typeof(VerticalType), dto.Vertical))
                throw new ArgumentException($"Invalid VerticalType: {dto.Vertical}");

            if (dto.Status.HasValue && !Enum.IsDefined(typeof(CompanyStatus), dto.Status.Value))
                throw new ArgumentException($"Invalid CompanyStatus: {dto.Status.Value}");

            if (dto.SubVertical.HasValue && !Enum.IsDefined(typeof(SubVertical), dto.SubVertical.Value))
                throw new ArgumentException($"Invalid CompanyCategory: {dto.SubVertical.Value}");

            if (dto.NatureOfBusiness == null || dto.NatureOfBusiness.Count == 0)
                throw new ArgumentException("NatureOfBusiness list is required and cannot be empty.");
            foreach (var val in dto.NatureOfBusiness)
            {
                if (!Enum.IsDefined(typeof(NatureOfBusiness), val))
                    throw new ArgumentException($"Invalid NatureOfBusiness: {val}");
            }
            if (!string.IsNullOrWhiteSpace(dto.BusinessDescription))
            {
                var wordCount = Regex.Matches(dto.BusinessDescription, @"\b\w+\b").Count;

                if (wordCount > 300)
                    throw new ArgumentException("Business description should not exceed 300 words.");
            }

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                throw new ArgumentException("Phone number is required.");

            var phoneRegex = new Regex(@"^\d{6,15}$");

            if (!phoneRegex.IsMatch(dto.PhoneNumber))
                throw new ArgumentException("Invalid phone number format.");

            if (!string.IsNullOrWhiteSpace(dto.WhatsAppNumber) &&
                !phoneRegex.IsMatch(dto.WhatsAppNumber))
                throw new ArgumentException("Invalid WhatsApp number format.");

            if (!IsValidEmail(dto.Email))
                throw new ArgumentException("Invalid email format.");
        }
        private VerifiedCompanyDto EntityForCreate(VerifiedCompanyDto dto, Guid id)
        {
            return new VerifiedCompanyDto
            {
                Id = id,
                Vertical = dto.Vertical,
                SubVertical = dto.SubVertical,
                UserId = dto.UserId,
                CompanyName = dto.CompanyName,
                Country = dto.Country,
                City = dto.City,
                BranchLocations = dto.BranchLocations,
                PhoneNumberCountryCode = dto.PhoneNumberCountryCode,
                WhatsAppCountryCode = dto.WhatsAppCountryCode,
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
                AuthorisedContactPersonName = dto.AuthorisedContactPersonName,
                CRExpiryDate = dto.CRExpiryDate,
                NatureOfBusiness = dto.NatureOfBusiness,
                CompanySize = dto.CompanySize,
                CompanyType = dto.CompanyType,
                UserDesignation = dto.UserDesignation,
                BusinessDescription = dto.BusinessDescription,
                CRNumber = dto.CRNumber,
                CompanyLogo = dto.CompanyLogo,
                CRDocument = dto.CRDocument,
                IsVerified = dto.IsVerified,
                Status = dto.Status ?? CompanyStatus.Active,
                CreatedBy = dto.UserId,
                UserName=dto.UserName,
                CreatedUtc = DateTime.UtcNow,
                IsActive = true
            };
        }
        public async Task<VerifiedCompanyDto?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.GetStateAsync<VerifiedCompanyDto>(ConstantValues.CompanyStoreName, id.ToString(), cancellationToken: cancellationToken);
                if (result == null)
                    throw new KeyNotFoundException($"Company with id '{id}' was not found.");
                if (!result.IsActive)
                    return null;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving company profile with ID: {Id}", id);
                throw;
            }
        }
        public async Task<List<VerifiedCompanyDto>> GetAllCompanies(CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await GetIndex();
                if (!keys.Any()) return new List<VerifiedCompanyDto>();

                var items = await _dapr.GetBulkStateAsync(ConstantValues.CompanyStoreName, keys, parallelism: 10);

                return items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                    .Select(i => JsonSerializer.Deserialize<VerifiedCompanyDto>(i.Value!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!)
                    .Where(e => e.Id != Guid.Empty && e.IsActive)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving all company profiles.");
                throw;
            }
        }
        public async Task<string> UpdateCompany(VerifiedCompanyDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                Validate(dto);

                var existing = await _dapr.GetStateAsync<VerifiedCompanyDto>(
                    ConstantValues.CompanyStoreName,
                    dto.Id.ToString(),
                    cancellationToken: cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException($"Company with ID {dto.Id} was not found.");

                if ((int)(existing.SubVertical ?? 0) == (int)SubVertical.Stores)
                    throw new InvalidDataException("Editing companies in the 'Stores' category is not allowed.");

                var keys = await GetIndex();
                foreach (var key in keys)
                {
                    if (key == dto.Id.ToString()) continue;

                    var other = await _dapr.GetStateAsync<VerifiedCompanyDto>(
                        ConstantValues.CompanyStoreName,
                        key,
                        cancellationToken: cancellationToken);

                    if (other == null) continue;

                    if (other.UserId == dto.UserId &&
                        other.Vertical == dto.Vertical &&
                        other.SubVertical == dto.SubVertical)
                    {
                        throw new ConflictException("A company profile already exists for this user under the same subvertical.");
                    }

                    if (other.UserId != dto.UserId &&
                        (other.PhoneNumber == dto.PhoneNumber || other.Email == dto.Email))
                    {
                        throw new ConflictException("Phone number or email is already used by another user.");
                    }
                }

                var entity = EntityForUpdate(dto, existing);
                entity.IsVerified = false;

                await _dapr.SaveStateAsync(
                    ConstantValues.CompanyStoreName,
                    dto.Id.ToString(),
                    entity);

                if (!keys.Contains(dto.Id.ToString()))
                {
                    keys.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(ConstantValues.CompanyStoreName, ConstantValues.CompanyVerifiedIndex, keys);
                }

                return "Company Profile Updated Successfully";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating company profile with ID: {Id}", dto.Id);
                throw;
            }
        }
        private VerifiedCompanyDto EntityForUpdate(VerifiedCompanyDto dto, VerifiedCompanyDto existing)
        {
            return new VerifiedCompanyDto
            {
                Id = dto.Id,
                Vertical = dto.Vertical,
                SubVertical = dto.SubVertical,
                UserId = dto.UserId,
                CompanyName = dto.CompanyName,
                Country = dto.Country,
                City = dto.City,
                BranchLocations = dto.BranchLocations,
                PhoneNumber = dto.PhoneNumber,
                PhoneNumberCountryCode = dto.PhoneNumberCountryCode,
                WhatsAppCountryCode = dto.WhatsAppCountryCode,
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
                AuthorisedContactPersonName = dto.AuthorisedContactPersonName,
                CRExpiryDate = dto.CRExpiryDate,
                UserDesignation = dto.UserDesignation,
                BusinessDescription = dto.BusinessDescription,
                CRNumber = dto.CRNumber,
                CompanyLogo = !string.IsNullOrWhiteSpace(dto.CompanyLogo)
                        ? dto.CompanyLogo
                        : existing.CompanyLogo,
                CRDocument = !string.IsNullOrWhiteSpace(dto.CRDocument)
                        ? dto.CRDocument
                        : existing.CRDocument,
                IsVerified = dto.IsVerified,
                Status = dto.Status ?? CompanyStatus.Active,
                UserName=dto.UserName,
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
                var entity = await _dapr.GetStateAsync<VerifiedCompanyDto>(ConstantValues.CompanyStoreName, id.ToString(), cancellationToken: cancellationToken);
                if (entity == null)
                {
                    throw new KeyNotFoundException($"Company with ID {id} not found.");
                }
                if (!entity.IsActive)
                {
                    throw new InvalidDataException("Company already soft deleted.");
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
                var result = await _dapr.GetStateAsync<List<string>>(ConstantValues.CompanyStoreName, ConstantValues.CompanyVerifiedIndex);
                return result ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving index.");
                throw;
            }
        }
        public async Task<string> ApproveCompany(string userId, CompanyVerificationApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var company = await _dapr.GetStateAsync<VerifiedCompanyDto>(
                            ConstantValues.CompanyStoreName,
                            dto.CompanyId.ToString(),
                            cancellationToken: cancellationToken
                        );
                if (company == null)
                    throw new KeyNotFoundException($"Company with ID {dto.CompanyId} not found.");
                if (!company.IsActive)
                    throw new InvalidOperationException("Cannot approve an inactive company profile.");
                if (company.IsVerified == true)
                    throw new InvalidOperationException("This company is already approved.");

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
            catch (KeyNotFoundException ex)
            {
                throw new InvalidDataException(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving company with ID {CompanyId}", dto.CompanyId);
                throw;
            }
        }
        public async Task<CompanyVerifyApprovalResponseDto?> GetCompanyApprovalInfo(Guid companyId, CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);
                var company = allCompanies.FirstOrDefault(c => c.Id == companyId && c.IsActive);

                if (company == null) return null;

                return new CompanyVerifyApprovalResponseDto
                {
                    CompanyId = company.Id,
                    Name = company.CompanyName,
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
        public async Task<List<CompanyVerificationStatusDto>> VerificationStatus(string userId, VerticalType vertical, bool isVerified, CancellationToken cancellationToken = default)
        {
            try
            {
                var allCompanies = await GetAllCompanies(cancellationToken);

                var filtered = allCompanies
                    .Where(c => c.IsActive)
                    .Where(c => c.IsVerified == isVerified && c.Vertical == vertical)
                    .Select(c => new CompanyVerificationStatusDto
                    {
                        CompanyId = c.Id,
                        BusinessName = c.CompanyName,
                        Vertical = c.Vertical,
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
        public async Task<List<VerifiedCompanyDto>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var all = await GetAllCompanies(cancellationToken);
                return all
                    .Where(c => c.UserId == userId && c.IsActive)
                    .ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<List<VerificationProfileStatus>> GetStatusByTokenUser(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.CompanyStoreName,
                    ConstantValues.CompanyVerifiedIndex,
                    cancellationToken: cancellationToken
                ) ?? new();

                var items = await _dapr.GetBulkStateAsync(
                    ConstantValues.CompanyStoreName,
                    keys,
                    null,
                    cancellationToken: cancellationToken
                );

                var companies = items
                    .Select(i => JsonSerializer.Deserialize<VerifiedCompanyDto>(i.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }))
                    .Where(c => c != null && c.UserId == userId)
                    .Select(c => new VerificationProfileStatus
                    {
                        CompanyId = c.Id,
                        UserId = c.UserId,
                        BusinessName = c.CompanyName,
                        Vertical = c.Vertical,
                        SubVertical = c.SubVertical ?? SubVertical.Items,
                        IsActive = c.IsActive
                    })
                    .ToList();

                return companies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch company summaries for user {UserId}", userId);
                throw;
            }
        }
        public async Task<List<VerificationProfileStatus>> GetAllVerificationProfiles(VerticalType vertical,SubVertical? subVertical,CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.CompanyStoreName,
                    ConstantValues.CompanyVerifiedIndex,
                    cancellationToken: cancellationToken
                ) ?? new();

                var items = await _dapr.GetBulkStateAsync(
                    ConstantValues.CompanyStoreName,
                    keys,
                    null,
                    cancellationToken: cancellationToken
                );

                var companies = items
                    .Select(i => JsonSerializer.Deserialize<VerifiedCompanyDto>(i.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }))
                    .Where(c => c != null && c.Vertical == vertical && (!subVertical.HasValue || c.SubVertical == subVertical.Value))
                    .Select(c => new VerificationProfileStatus
                    {
                        CompanyId = c.Id,
                        UserId = c.UserId,
                        CRFile=c.CRDocument,
                        CRLicense=c.CRNumber,
                        Enddate=c.CRExpiryDate,
                        Username=c.UserName,
                        BusinessName = c.CompanyName,
                        Vertical = c.Vertical,
                        SubVertical = c.SubVertical ?? SubVertical.Items,
                        IsActive = c.IsActive
                    })
                    .ToList();

                return companies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch company summaries by vertical and subvertical");
                throw;
            }
        }

    }
}