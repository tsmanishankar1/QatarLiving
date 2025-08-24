using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Company;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System.Linq;
using System.Text.RegularExpressions;

namespace QLN.Company.MS.Service
{
    public class InternalCompanyProfileService : ICompanyProfileService
    {
        private readonly ILogger<InternalCompanyProfileService> _logger;
        private readonly QLCompanyContext _context;
        private readonly QLSubscriptionContext _dbContext;
        private readonly DaprClient _dapr;
        private readonly QLClassifiedContext _classContext;
        public InternalCompanyProfileService(ILogger<InternalCompanyProfileService> logger, QLCompanyContext context, QLSubscriptionContext dbContext, DaprClient dapr, QLClassifiedContext classContext)
        {
            _logger = logger;
            _context = context;
            _dbContext = dbContext;
            _dapr = dapr;
            _classContext = classContext;
        }

        public async Task<string> CreateCompany(string uid, string userName, CompanyProfile dto, CancellationToken cancellationToken = default)
        {
            try
            {
                bool duplicateByUserAndVertical;

                if (dto.Vertical == VerticalType.Services)
                {
                    duplicateByUserAndVertical = await _context.Companies.AnyAsync(
                        c => c.UserId == uid &&
                             c.IsActive &&
                             c.Vertical == VerticalType.Services,
                        cancellationToken);
                }
                else
                {
                    duplicateByUserAndVertical = await _context.Companies.AnyAsync(
                        c => c.UserId == uid &&
                             c.Vertical == dto.Vertical &&
                             c.IsActive &&
                             c.SubVertical == dto.SubVertical,
                        cancellationToken);
                }

                if (duplicateByUserAndVertical)
                {
                    throw new ConflictException(dto.Vertical == VerticalType.Services
                        ? "You can only create one company profile under the Services vertical."
                        : "A company profile already exists for this user under the same subvertical.");
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
                // Update the subscription with the new company ID if it exists
                var subscription = await _dbContext.Subscriptions
                .FirstOrDefaultAsync(s =>
                s.UserId == uid &&
                (int)s.Vertical == (int)(Vertical)dto.Vertical &&
                s.SubVertical == dto.SubVertical && s.ProductType == Common.DTO_s.Payments.ProductType.SUBSCRIPTION,
                cancellationToken);

                if (subscription != null)
                {
                    subscription.CompanyId = newCompanyId;
                    _dbContext.Subscriptions.Update(subscription);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                var upsertRequest = await IndexServiceToAzureSearch(entity, cancellationToken);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.CompanyProfileIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }
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
        private async Task<CommonIndexRequest> IndexServiceToAzureSearch(Common.Infrastructure.Model.Company entity, CancellationToken cancellationToken)
        {
            var indexDoc = new CompanyProfileIndex
            {
                CompanyId = entity.Id.ToString(),
                CompanyName = entity.CompanyName,
                Country = entity.Country,
                City = entity.City,
                PhoneNumber = entity.PhoneNumber,
                PhoneNumberCountryCode = entity.PhoneNumberCountryCode,
                BranchLocations = entity.BranchLocations,
                WhatsAppNumber = entity.WhatsAppNumber,
                WhatsAppCountryCode = entity.WhatsAppCountryCode,
                Email = entity.Email,
                UserName = entity.UserName,
                IsBasicProfile = entity.IsBasicProfile,
                WebsiteUrl = entity.WebsiteUrl,
                FacebookUrl = entity.FacebookUrl,
                InstagramUrl = entity.InstagramUrl,
                CompanyLogo = entity.CompanyLogo,
                StartDay = entity.StartDay,
                EndDay = entity.EndDay,
                StartHour = entity.StartHour.ToString(),
                EndHour = entity.EndHour.ToString(),
                UserDesignation = entity.UserDesignation,
                CoverImage1 = entity.CoverImage1,
                CoverImage2 = entity.CoverImage2,
                IsTherapeuticService = entity.IsTherapeuticService,
                TherapeuticCertificate = entity.TherapeuticCertificate,
                LicenseNumber = entity.LicenseNumber,
                CompanyType = entity.CompanyType.ToString(),
                CompanySize = entity.CompanySize.ToString(),
                NatureOfBusiness = entity.NatureOfBusiness.Select(n => n.ToString()).ToList(),
                BusinessDescription = entity.BusinessDescription,
                CRNumber = entity.CRNumber,
                CRDocument = entity.CRDocument,
                UploadFeed = entity.UploadFeed,
                XMLFeed = entity.XMLFeed,
                CompanyVerificationStatus = entity.CompanyVerificationStatus?.ToString(),
                Status = entity.Status?.ToString(),
                Vertical = entity.Vertical.ToString(),
                SubVertical = entity.SubVertical?.ToString(),
                UserId = entity.UserId,
                Slug = entity.Slug,
                IsActive = entity.IsActive,
                CreatedBy = entity.CreatedBy,
                CreatedUtc = entity.CreatedUtc,
                UpdatedBy = entity.UpdatedBy,
                UpdatedUtc = entity.UpdatedUtc
            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.CompanyProfileIndex,
                CompanyProfile = indexDoc
            };
            return indexRequest;
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
        public static void Validate(Common.Infrastructure.Model.Company dto)
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
        private Common.Infrastructure.Model.Company EntityForCreate(CompanyProfile dto, Guid id, string uid, string userName)
        {
            return new Common.Infrastructure.Model.Company
            {
                Id = id,
                Vertical = dto.Vertical,
                SubVertical = dto.SubVertical,
                Slug = SlugHelper.GenerateSlug(dto.CompanyName, dto.CompanyType.ToString(), dto.Vertical.ToString(), Guid.NewGuid()),
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
                UploadFeed = dto.UploadFeed,
                XMLFeed = dto.XMLFeed,
                StoresURL = dto.StoresURL,
                ImportType = dto.ImportType,
                CompanyVerificationStatus = dto.CompanyVerificationStatus,
                Status = dto.Status,
                CreatedBy = uid,
                CreatedUtc = DateTime.UtcNow,
                IsBasicProfile = true,
                IsActive = true
            };
        }
        public async Task<Common.Infrastructure.Model.Company?> GetCompanyById(Guid id, CancellationToken cancellationToken = default)
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
        public async Task<Common.Infrastructure.Model.Company?> GetCompanyBySlug(string? slug, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, cancellationToken);

                if (result == null)
                    throw new KeyNotFoundException($"Company with slug '{slug}' was not found or is inactive.");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving company profile", slug);
                throw;
            }
        }
        public async Task<string> UpdateCompany(Common.Infrastructure.Model.Company dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == dto.Id && c.IsActive, cancellationToken);

                if (existing == null)
                    throw new KeyNotFoundException($"Company with ID {dto.Id} was not found.");

                //if (existing.Vertical == VerticalType.Classifieds && (int)(existing.SubVertical ?? 0) == (int)SubVertical.Stores)
                //    throw new ArgumentException("Editing companies in the 'Stores' category is not allowed.");

                bool duplicateCompany = await _context.Companies
                    .AnyAsync(c => c.Id != dto.Id &&
                                   c.UserId == dto.UserId &&
                                   c.Vertical == dto.Vertical &&
                                   c.IsActive &&
                                   c.SubVertical == dto.SubVertical,
                              cancellationToken);

                if (duplicateCompany)
                    throw new ConflictException("A company profile already exists for this user under the same subvertical.");

                bool phoneEmailUsed = await _context.Companies
                    .AnyAsync(c => c.Id != dto.Id &&
                                   c.UserId != dto.UserId &&
                                   c.IsActive &&
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
                var upsertRequest = await IndexServiceToAzureSearch(dto, cancellationToken);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.CompanyProfileIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
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
        private Common.Infrastructure.Model.Company EntityForUpdate(Common.Infrastructure.Model.Company dto, Common.Infrastructure.Model.Company existing)
        {
            return new Common.Infrastructure.Model.Company
            {
                Id = dto.Id,
                Vertical = dto.Vertical,
                Slug = SlugHelper.GenerateSlug(dto.CompanyName, dto.CompanyType.ToString(), dto.Vertical.ToString(), Guid.NewGuid()),
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
                CompanyVerificationStatus = dto.CompanyVerificationStatus,
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
                StoresURL = dto.StoresURL,
                ImportType = dto.ImportType,
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
                var upsertRequest = await IndexServiceToAzureSearch(entity, cancellationToken);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.CompanyProfileIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }
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
                if (company.IsBasicProfile == true && (dto.Status == VerifiedStatus.Approved || dto.CompanyVerificationStatus == VerifiedStatus.Approved))
                {
                    throw new InvalidOperationException("Cannot approve a company profile while it is still marked as Basic.");
                }
                if (!company.IsActive && dto.Status != VerifiedStatus.Removed && dto.CompanyVerificationStatus != VerifiedStatus.Removed)
                    throw new InvalidOperationException("Cannot update an inactive company profile.");

                if (company.Status != dto.Status)
                {
                    if (dto.Status == VerifiedStatus.Removed || dto.Status == VerifiedStatus.Rejected)
                    {
                        company.IsActive = false;
                    }
                    company.Status = dto.Status;
                    dto.CompanyVerificationStatus = company.CompanyVerificationStatus;
                }

                if (company.CompanyVerificationStatus != dto.CompanyVerificationStatus)
                {
                    if (dto.CompanyVerificationStatus == VerifiedStatus.Removed || dto.CompanyVerificationStatus == VerifiedStatus.Rejected)
                    {
                        company.IsActive = false;
                    }
                    company.CompanyVerificationStatus = dto.CompanyVerificationStatus;
                    dto.Status = company.Status;
                }

                company.UpdatedUtc = DateTime.UtcNow;
                company.UpdatedBy = userId;
                if (dto.Status == VerifiedStatus.Removed || dto.Status == VerifiedStatus.Rejected ||
                            dto.Status == VerifiedStatus.NeedChanges ||
                            dto.Status == VerifiedStatus.OnHold || dto.CompanyVerificationStatus == VerifiedStatus.Removed || dto.CompanyVerificationStatus == VerifiedStatus.Rejected
                            || dto.CompanyVerificationStatus == VerifiedStatus.NeedChanges || dto.CompanyVerificationStatus == VerifiedStatus.OnHold)
                {
                    var comment = new Comment
                    {
                        CompanyId = dto.CompanyId,               
                        Action = $"Company {dto.Status} || {dto.CompanyVerificationStatus}", 
                        Reason = dto.Reason ?? string.Empty,
                        Comments = dto.Comments,
                        Vertical = (Vertical)company.Vertical,
                        SubVertical = company.SubVertical.HasValue ? company.SubVertical.Value : null,
                        CreatedAt = DateTime.UtcNow,
                        CreatedUserId = company.CreatedBy,
                        CreatedUserName = company.UserName,
                        UpdatedUserId = userId,
                        UpdatedUserName = company.UserName
                    };

                    await _classContext.Comments.AddAsync(comment, cancellationToken);
                    await _classContext.SaveChangesAsync(cancellationToken);
                }
                await _context.SaveChangesAsync(cancellationToken);
                var upsertRequest = await IndexServiceToAzureSearch(company, cancellationToken);
                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.CompanyProfileIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }
                await _dapr.PublishEventAsync("pubsub", "notifications-email", new NotificationEntity
                {
                    Destinations = new List<string> { "email" },
                    Recipients = new List<RecipientDto>
                    {
                        new RecipientDto
                        {
                            Name = company.UserName,
                            Email = company.Email
                        }
                    },
                    Subject = $"Company '{company.CompanyName}' was updated",
                    Plaintext = $"Hello,\n\nYour company titled '{company.CompanyName}' has been updated.\n\nStatus: {dto.Status}\n\nThanks,\nQL Team",
                    Html = $@"
                    <p>Hi,</p>
                    <p>Your company titled '<b>{company.CompanyName}</b>' has been updated.</p>
                    <p>Status: <b>{dto.Status}</b></p>
                    <p>Thanks,<br/>QL Team</p>"
                }, cancellationToken);

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
        public async Task<List<Common.Infrastructure.Model.Company>> GetCompaniesByTokenUser(string userId, CancellationToken cancellationToken = default)
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
        public async Task<List<CompanyWithSubscriptionDto>> GetCompaniesByToken(
            string userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var companies = await _context.Companies
                    .Where(c => c.UserId == userId && c.IsActive)
                    .ToListAsync(cancellationToken);

                if (!companies.Any())
                {
                    return new List<CompanyWithSubscriptionDto>();
                }

                var companyIds = companies.Select(c => c.Id).ToList();

                var latestSubscriptions = await _dbContext.Subscriptions
                    .Where(s => companyIds.Contains((Guid)s.CompanyId))
                    .GroupBy(s => s.CompanyId)
                    .Select(g => g.OrderByDescending(s => s.StartDate).FirstOrDefault())
                    .ToListAsync(cancellationToken);

                var result = companies.Select(c =>
                {
                    var latestSub = latestSubscriptions.FirstOrDefault(s => s.CompanyId == c.Id);

                    return new CompanyWithSubscriptionDto
                    {
                        Id = c.Id,
                        Vertical = c.Vertical,
                        SubVertical = c.SubVertical,
                        Slug = c.Slug,
                        UserId = c.UserId,
                        CompanyName = c.CompanyName,
                        Country = c.Country,
                        City = c.City,
                        BranchLocations = c.BranchLocations,
                        PhoneNumberCountryCode = c.PhoneNumberCountryCode,
                        WhatsAppCountryCode = c.WhatsAppCountryCode,
                        PhoneNumber = c.PhoneNumber,
                        WhatsAppNumber = c.WhatsAppNumber,
                        Email = c.Email,
                        WebsiteUrl = c.WebsiteUrl,
                        FacebookUrl = c.FacebookUrl,
                        InstagramUrl = c.InstagramUrl,
                        StartDay = c.StartDay,
                        EndDay = c.EndDay,
                        StartHour = c.StartHour,
                        EndHour = c.EndHour,
                        UserDesignation = c.UserDesignation,
                        UserName = c.UserName,
                        CoverImage1 = c.CoverImage1,
                        CoverImage2 = c.CoverImage2,
                        NatureOfBusiness = c.NatureOfBusiness,
                        IsTherapeuticService = c.IsTherapeuticService,
                        TherapeuticCertificate = c.TherapeuticCertificate,
                        LicenseNumber = c.LicenseNumber,
                        CompanySize = c.CompanySize,
                        CompanyType = c.CompanyType,
                        BusinessDescription = c.BusinessDescription,
                        CRNumber = c.CRNumber,
                        CompanyLogo = c.CompanyLogo,
                        CRExpiryDate = c.CRExpiryDate,
                        AuthorisedContactPersonName = c.AuthorisedContactPersonName,
                        CRDocument = c.CRDocument,
                        UploadFeed = c.UploadFeed,
                        XMLFeed = c.XMLFeed,
                        CompanyVerificationStatus = c.CompanyVerificationStatus,
                        Status = c.Status,
                        CreatedBy = c.CreatedBy,
                        UpdatedBy = c.UpdatedBy,
                        CreatedUtc = c.CreatedUtc,
                        UpdatedUtc = c.UpdatedUtc,
                        IsBasicProfile = c.IsBasicProfile,
                        IsActive = c.IsActive,
                        SubscriptionStartDate = latestSub?.StartDate,
                        SubscriptionEndDate = latestSub?.EndDate,
                        ProductName = latestSub?.ProductName
                    };
                }).ToList();

                foreach (var company in result)
                {
                    Console.WriteLine($"Final Result: CompanyId={company.Id}, CompanyName={company.CompanyName}, ProductName={company.ProductName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        public async Task<CompanyPaginatedResponse<Common.Infrastructure.Model.Company>> GetAllVerifiedCompanies(CompanyProfileFilterRequest filter, CancellationToken cancellationToken = default)
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

                if (filter.CompanyVerificationStatus != null)
                    query = query.Where(c => c.CompanyVerificationStatus == filter.CompanyVerificationStatus);

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
                              where c.IsActive
                              select new CompanySubscriptionDto
                              {
                                  CompanyId = c.Id,
                                  CompanyName = c.CompanyName,
                                  Email = c.Email,
                                  Mobile = c.PhoneNumber,
                                  WhatsApp = c.WhatsAppNumber,
                                  WebUrl = c.WebsiteUrl,
                                  Status = s.Status,
                                  StartDate = s.StartDate,
                                  EndDate = s.EndDate,
                                  SubscriptionType = s.ProductName,
                                  Slug = c.Slug
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
