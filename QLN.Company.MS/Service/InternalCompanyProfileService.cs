using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Company;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.QLDbContext;
using System.Text.RegularExpressions;

namespace QLN.Company.MS.Service
{
    public class InternalCompanyProfileService : ICompanyProfileService
    {
        private readonly ILogger<InternalCompanyProfileService> _logger;
        private readonly QLCompanyContext _context;
        private readonly QLSubscriptionContext _dbContext;
        public InternalCompanyProfileService(ILogger<InternalCompanyProfileService> logger, QLCompanyContext context, QLSubscriptionContext dbContext)
        {
            _logger = logger;
            _context = context;
            _dbContext = dbContext;
        }

        public async Task<string> CreateCompany(string uid, string userName, CompanyProfile dto, CancellationToken cancellationToken = default)
        {
            try
            {
                bool duplicateByUserAndVertical = await _context.Companies.AnyAsync(
                    c => c.UserId == uid &&
                         c.Vertical == dto.Vertical &&
                         c.SubVertical == dto.SubVertical,
                    cancellationToken);

                if (duplicateByUserAndVertical)
                {
                    throw new ConflictException("A company profile already exists for this user under the same subvertical.");
                }
                bool duplicateContactInfo = await _context.Companies.AnyAsync(
                    c => c.UserId != uid &&
                         (c.PhoneNumber == dto.PhoneNumber || c.Email == dto.Email),
                    cancellationToken);

                if (duplicateContactInfo)
                {
                    throw new ConflictException("Phone number or email is already used by another user.");
                }
                var newCompanyId = Guid.NewGuid();
                var entity = EntityForCreate(dto, newCompanyId, uid, userName);
                Validate(entity);

                _context.Companies.Add(entity);
                await _context.SaveChangesAsync(cancellationToken);

                return "Company Created successfully";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating company profile for user ID: {UserId}", uid);
                throw;
            }
        }

        public async Task<string> MigrateCompany(string guid, string uid, string userName, CompanyProfile dto, CancellationToken cancellationToken = default)
        {
            try
            {
                //bool duplicateByUserAndVertical = await _context.Companies.AnyAsync(
                //    c => c.UserId == uid &&
                //         c.Vertical == dto.Vertical &&
                //         c.SubVertical == dto.SubVertical,
                //    cancellationToken);

                //if (duplicateByUserAndVertical)
                //{
                //    throw new ConflictException("A company profile already exists for this user under the same subvertical.");
                //}
                //bool duplicateContactInfo = await _context.Companies.AnyAsync(
                //    c => c.UserId != uid &&
                //         (c.PhoneNumber == dto.PhoneNumber || c.Email == dto.Email),
                //    cancellationToken);

                //if (duplicateContactInfo)
                //{
                //    throw new ConflictException("Phone number or email is already used by another user.");
                //}
                if(!Guid.TryParse(guid, out var newCompanyId))
                {
                    throw new InvalidDataException("Not a GUID");
                }
                var entity = EntityForCreate(dto, newCompanyId, uid, userName);
                //Validate(entity);

                _context.Companies.Add(entity);
                await _context.SaveChangesAsync(cancellationToken);

                return "Company Migrated successfully";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating company profile for user ID: {UserId}", uid);
                throw;
            }
        }
        private static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) &&
                   Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }
        public static void Validate(QLN.Common.Infrastructure.Model.Company dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CompanyName))
                throw new ArgumentException("Company name is required.", nameof(dto.CompanyName));

            if (!Enum.IsDefined(typeof(CompanyType), dto.CompanyType))
                throw new ArgumentException($"Invalid CompanyType: {dto.CompanyType}");

            if (!Enum.IsDefined(typeof(CompanySize), dto.CompanySize))
                throw new ArgumentException($"Invalid CompanySize: {dto.CompanySize}");

            if (!Enum.IsDefined(typeof(VerticalType), dto.Vertical))
                throw new ArgumentException($"Invalid VerticalType: {dto.Vertical}");

            if (dto.Status.HasValue && !Enum.IsDefined(typeof(VerifiedStatus), dto.Status.Value))
                throw new ArgumentException($"Invalid VerifiedStatus: {dto.Status.Value}");

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
            if (dto.Vertical == VerticalType.Classifieds)
            {
                if (dto.SubVertical.HasValue && new[] { 1, 2, 3, 4, 5 }.Contains((int)dto.SubVertical.Value))
                {
                    if (string.IsNullOrWhiteSpace(dto.UserDesignation))
                        throw new ArgumentException("User designation is required for this vertical and subvertical.");
                }
            }

            if (dto.Vertical == VerticalType.Services)
            {
                if (dto.IsTherapeuticService == null)
                    throw new ArgumentException("IsTherapeuticService must be specified for Vertical 4.");

                if (dto.IsTherapeuticService == true)
                {
                    if (string.IsNullOrWhiteSpace(dto.TherapeuticCertificate))
                        throw new ArgumentException("Therapeutic certificate is required when therapeutic service is selected.");

                    if (string.IsNullOrWhiteSpace(dto.LicenseNumber))
                        throw new ArgumentException("License number is required when therapeutic service is selected.");

                    if (dto.LicenseNumber.Length > 50)
                        throw new ArgumentException("License number cannot exceed 50 characters.");
                }
            }
            if (dto.CRNumber < 100000 || dto.CRNumber > 99999999)
                throw new ArgumentException("CR Number must be between 6 to 8 digits.", nameof(dto.CRNumber));
        }
        private QLN.Common.Infrastructure.Model.Company EntityForCreate(CompanyProfile dto, Guid id, string uid, string userName)
        {
            return new QLN.Common.Infrastructure.Model.Company
            {
                Id = id,
                Vertical = dto.Vertical,
                SubVertical = dto.SubVertical,
                UserId = uid,
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
                UserDesignation = dto.UserDesignation,
                UserName = userName,
                CoverImage1 = dto.CoverImage1,
                CoverImage2 = dto.CoverImage2,
                NatureOfBusiness = dto.NatureOfBusiness,
                IsTherapeuticService = dto.IsTherapeuticService,
                TherapeuticCertificate = dto.TherapeuticCertificate,
                LicenseNumber = dto.LicenseNumber,
                CompanySize = dto.CompanySize,
                CompanyType = dto.CompanyType,
                BusinessDescription = dto.BusinessDescription,
                CRNumber = dto.CRNumber,
                CompanyLogo = dto.CompanyLogo,
                CRDocument = dto.CRDocument,
                UploadFeed=dto.UploadFeed,
                XMLFeed=dto.XMLFeed,
                Status = dto.Status,
                CreatedBy = uid,
                CreatedUtc = DateTime.UtcNow,
                IsBasicProfile = true,
                IsActive = true
            };
        }
        public async Task<QLN.Common.Infrastructure.Model.Company?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

                if (result == null)
                    throw new KeyNotFoundException($"Company with id '{id}' was not found or is inactive.");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving company profile with ID: {Id}", id);
                throw;
            }
        }
        public async Task<string> UpdateCompany(QLN.Common.Infrastructure.Model.Company dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == dto.Id && c.IsActive, cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException($"Company with ID {dto.Id} was not found.");

                if ((int)(existing.SubVertical ?? 0) == (int)SubVertical.Stores)
                    throw new ArgumentException("Editing companies in the 'Stores' category is not allowed.");

                bool duplicateCompany = await _context.Companies
                    .AnyAsync(c => c.Id != dto.Id &&
                                   c.UserId == dto.UserId &&
                                   c.Vertical == dto.Vertical &&
                                   c.SubVertical == dto.SubVertical,
                              cancellationToken);

                if (duplicateCompany)
                    throw new ConflictException("A company profile already exists for this user under the same subvertical.");

                bool phoneEmailUsed = await _context.Companies
                    .AnyAsync(c => c.Id != dto.Id &&
                                   c.UserId != dto.UserId &&
                                   (c.PhoneNumber == dto.PhoneNumber || c.Email == dto.Email),
                              cancellationToken);

                if (phoneEmailUsed)
                    throw new ConflictException("Phone number or email is already used by another user.");

                if (dto.IsBasicProfile == false)
                {
                    if (string.IsNullOrWhiteSpace(dto.AuthorisedContactPersonName))
                        throw new ArgumentException("Authorised Contact Person Name is required");

                    if (!dto.CRExpiryDate.HasValue)
                        throw new ArgumentException("CR Expiry Date is required");
                }

                var updated = EntityForUpdate(dto, existing);

                _context.Entry(existing).CurrentValues.SetValues(updated);

                await _context.SaveChangesAsync(cancellationToken);

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
        private QLN.Common.Infrastructure.Model.Company EntityForUpdate(QLN.Common.Infrastructure.Model.Company dto, QLN.Common.Infrastructure.Model.Company existing)
        {
            return new QLN.Common.Infrastructure.Model.Company
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
                UserDesignation = dto.UserDesignation,
                AuthorisedContactPersonName = dto.AuthorisedContactPersonName,
                UserName = dto.UserName,
                CRExpiryDate = dto.CRExpiryDate,
                CoverImage1 = dto.CoverImage1 ?? existing.CoverImage1,
                CoverImage2 = dto.CoverImage2 ?? existing.CoverImage2,
                IsTherapeuticService = dto.IsTherapeuticService,
                TherapeuticCertificate = dto.TherapeuticCertificate,
                LicenseNumber = dto.LicenseNumber,
                BusinessDescription = dto.BusinessDescription,
                CRNumber = dto.CRNumber,
                CompanyLogo = !string.IsNullOrWhiteSpace(dto.CompanyLogo)
                        ? dto.CompanyLogo
                        : existing.CompanyLogo,
                CRDocument = !string.IsNullOrWhiteSpace(dto.CRDocument)
                        ? dto.CRDocument
                        : existing.CRDocument,
                UploadFeed = dto.UploadFeed,
                XMLFeed = dto.XMLFeed,
                Status = dto.Status,
                CreatedBy = existing.CreatedBy,
                CreatedUtc = existing.CreatedUtc,
                UpdatedBy = dto.UserId,
                UpdatedUtc = DateTime.UtcNow,
                IsActive = true,
                IsBasicProfile = dto.IsBasicProfile
            };
        }
        public async Task DeleteCompany(DeleteCompanyRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == request.Id && c.IsActive, cancellationToken);

                if (entity == null)
                    throw new KeyNotFoundException($"Company with ID {request.Id} not found or already deleted.");

                entity.IsActive = false;
                entity.UpdatedUtc = DateTime.UtcNow;
                entity.UpdatedBy = request.UpdatedBy;

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while soft deleting company profile with ID: {Id}", request.Id);
                throw;
            }
        }
        public async Task<string> ApproveCompany(string userId, CompanyProfileApproveDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Id == dto.CompanyId, cancellationToken);

                if (company == null)
                    throw new KeyNotFoundException($"Company with ID {dto.CompanyId} not found.");

                if (!company.IsActive && dto.Status != VerifiedStatus.Removed)
                    throw new InvalidOperationException("Cannot approve an inactive company profile.");

                company.Status = dto.Status;

                if (dto.Status == VerifiedStatus.Removed)
                {
                    company.IsActive = false;
                }

                company.UpdatedUtc = DateTime.UtcNow;
                company.UpdatedBy = userId;

                await _context.SaveChangesAsync(cancellationToken);
                return "Company Status Changed Successfully";
            }
            catch (KeyNotFoundException)
            {
                throw;
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
        public async Task<List<QLN.Common.Infrastructure.Model.Company>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Companies
                .Where(c => c.UserId == userId && c.IsActive)
                .ToListAsync(cancellationToken);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<CompanyPaginatedResponse<QLN.Common.Infrastructure.Model.Company>> GetAllVerifiedCompanies(CompanyProfileFilterRequest filter, CancellationToken cancellationToken = default)
        {
            try
            {
                if (filter.PageNumber <= 0 || filter.PageSize <= 0)
                    throw new InvalidDataException("PageNumber and PageSize must be greater than 0.");

                var query = _context.Companies.AsQueryable();

                query = query.Where(c => c.Id != Guid.Empty && c.IsActive);

                if (filter.IsBasicProfile != null)
                    query = query.Where(c => c.IsBasicProfile == filter.IsBasicProfile.Value);

                if (filter.Status != null)
                    query = query.Where(c => c.Status == filter.Status);

                if (filter.Vertical != null)
                    query = query.Where(c => c.Vertical == filter.Vertical);

                if (filter.SubVertical != null)
                    query = query.Where(c => c.SubVertical == filter.SubVertical);

                if (!string.IsNullOrWhiteSpace(filter.Search))
                    query = query.Where(c => c.CompanyName.Contains(filter.Search));

                query = (filter.SortBy?.ToLower()) switch
                {
                    "name asc" => query.OrderBy(c => c.CompanyName),
                    "name desc" => query.OrderByDescending(c => c.CompanyName),
                    "date asc" => query.OrderBy(c => c.CreatedUtc),
                    "date desc" => query.OrderByDescending(c => c.CreatedUtc),
                    _ => query.OrderByDescending(c => c.CreatedUtc)
                };

                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                return new CompanyPaginatedResponse<QLN.Common.Infrastructure.Model.Company>
                {
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Items = items
                };
            }
            catch (InvalidDataException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving verified company profiles.");
                throw;
            }
        }
        public async Task<CompanySubscriptionListResponseDto> GetCompanySubscriptions(CompanySubscriptionFilter request, CancellationToken cancellationToken = default)
        {
            try
            {
                int pageNumber = request.PageNumber ?? 1;
                int pageSize = request.PageSize ?? 12;
                if (pageNumber <= 0)
                    throw new InvalidDataException("PageNumber must be greater than 0.");

                if (pageSize <= 0)
                    throw new InvalidDataException("PageSize must be greater than 0.");
                _logger.LogInformation("Starting GetCompanySubscriptionsAsync with request: {@Request}", request);

                var companies = await _context.Companies.ToListAsync(cancellationToken);
                var subscriptions = await _dbContext.Subscriptions.ToListAsync(cancellationToken);

                var joined = (from c in companies
                              join s in subscriptions on c.Id equals s.CompanyId
                              select new CompanySubscriptionDto
                              {
                                  CompanyName = c.CompanyName,
                                  Email = c.Email,
                                  Mobile = c.PhoneNumber,
                                  WhatsApp = c.WhatsAppNumber,
                                  WebUrl = c.WebsiteUrl,
                                  Status = s.Status,
                                  StartDate = s.StartDate,
                                  EndDate = s.EndDate,
                                  SubscriptionType = s.ProductName
                              }).AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SubscriptionType))
                {
                    joined = joined.Where(j =>
                        j.SubscriptionType.Equals(request.SubscriptionType, StringComparison.OrdinalIgnoreCase));
                    _logger.LogInformation("Filtered by product name: {ProductName}", request.SubscriptionType);
                }
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTermLower = request.SearchTerm.Trim().ToLower();

                    joined = joined.Where(j =>
                        (!string.IsNullOrEmpty(j.CompanyName) && j.CompanyName.ToLower().Contains(searchTermLower)) ||
                        (!string.IsNullOrEmpty(j.Email) && j.Email.ToLower().Contains(searchTermLower)) ||
                        (!string.IsNullOrEmpty(j.Mobile) && j.Mobile.ToLower().Contains(searchTermLower)) ||
                        (!string.IsNullOrEmpty(j.WhatsApp) && j.WhatsApp.ToLower().Contains(searchTermLower))
                    );

                    _logger.LogInformation("Filtered by search term: {SearchTerm}", request.SearchTerm);
                }

                if (request.StartDate.HasValue && request.EndDate.HasValue)
                {
                    joined = joined.Where(j =>
                        j.StartDate >= request.StartDate.Value &&
                        j.EndDate <= request.EndDate.Value);
                    _logger.LogInformation("Filtered by date range: {Start} - {End}", request.StartDate, request.EndDate);
                }

                if (request.SortBy?.ToLower() == "desc")
                    joined = joined.OrderByDescending(j => j.EndDate);
                else
                    joined = joined.OrderBy(j => j.EndDate);

                int totalRecords = joined.Count();
                int totalPages = (int)Math.Ceiling((decimal)totalRecords / pageSize);

                var paged = joined
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new CompanySubscriptionListResponseDto
                {
                    Records = paged,
                    TotalRecords = totalRecords,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCompanySubscriptionsAsync");
                throw;
            }
        }
    }
}
