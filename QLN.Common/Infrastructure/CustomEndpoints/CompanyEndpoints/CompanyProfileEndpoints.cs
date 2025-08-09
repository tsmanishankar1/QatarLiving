using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.IService.ICompanyService;
using Microsoft.AspNetCore.Builder;
using QLN.Common.DTO_s.Company;
using System.Text.Json;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Auditlog;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyProfileEndpoints
    {
        const string ModuleName = "Company";
        public static RouteGroupBuilder MapCreateProfile(this RouteGroupBuilder group)
        {
            group.MapPost("/createcompany", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                CompanyProfile dto,
                ICompanyProfileService service,
                AuditLogger auditLogger,
                HttpContext httpContext,
                CancellationToken cancellationToken = default) =>
            {
                string? uid = "unknown";
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();
                    var userName = userData.GetProperty("name").GetString();
                    var isSubcriber = userData.GetProperty("roles").EnumerateArray()
                        .Any(r => r.GetString() == "subscription");
                    var result = await service.CreateCompany(uid, userName, dto, cancellationToken);
                    await auditLogger.LogAuditAsync(
                    module: ModuleName,
                    httpMethod: "POST",
                    apiEndpoint: "/api/companyprofile/createcompany",
                    message: "Company Profile created successfully",
                    createdBy: uid,
                    payload: dto,
                    cancellationToken: cancellationToken
                    ); 
                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/createcompany", ex, uid, cancellationToken);
                    return TypedResults.Problem(
                        title: "Conflict",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict
                    );
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/createcompany", ex, uid, cancellationToken);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/createcompany", ex, uid, cancellationToken);
                    return TypedResults.Problem(
                           title: "Internal Server Error",
                           detail: ex.Message,
                           statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("CreateCompanyProfile")
            .WithTags("Company")
            .WithSummary("Create a company profile")
            .WithDescription("Creates a new company profile using the user ID from the access token.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/createcompanybyuserid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                [FromQuery] string uid,
                [FromQuery] string userName,
                CompanyRequest dto,
                ICompanyProfileService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    dto.CreatedBy = uid;
                    dto.UserName = userName;
                    if (dto.CreatedBy == string.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    var result = await service.CreateCompany(uid, userName, dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Problem(
                        title: "Conflict",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict
                    );
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                           title: "Internal Server Error",
                           detail: ex.Message,
                           statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("CreateCompanyProfileByUserId")
            .WithTags("Company")
            .WithSummary("Create company profile by passing user ID explicitly")
            .WithDescription("Used by external services to create company profiles without requiring authorization.")
            .ExcludeFromDescription()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetByCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapGet("/getcompanybyid", async Task<IResult> (
            [FromQuery] Guid id,
            [FromServices] ICompanyProfileService service) =>
            {
                try
                {
                    var result = await service.GetCompanyById(id);
                    if (result == null)
                        throw new KeyNotFoundException($"Company with ID '{id}' not found.");
                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                          title: "Internal Server Error",
                          detail: "An unexpected error occurred.",
                          statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetCompanyProfile")
            .WithTags("Company")
            .WithSummary("Get a company profile")
            .WithDescription("Retrieves a company profile by ID.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapUpdateCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPut("/updatecompanyprofile", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                NotFound<ProblemDetails>,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                Company dto,
                ICompanyProfileService service,
                AuditLogger auditLogger,
                HttpContext httpContext,
                CancellationToken cancellationToken = default) =>
            {
                string? uid = "unknown";
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();
                    var username = userData.GetProperty("name").GetString();
                    var isSubcriber = userData.GetProperty("roles").EnumerateArray()
                        .Any(r => r.GetString() == "subscription");

                    var existingCompany = await service.GetCompanyById(dto.Id, cancellationToken);
                    if (existingCompany == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Company with ID not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    if (existingCompany.UserId != uid)
                        return TypedResults.Forbid();

                    dto.UserId = uid;
                    dto.UserName = username;
                    var updated = await service.UpdateCompany(dto, cancellationToken);
                    await auditLogger.LogAuditAsync(
                       module: ModuleName,
                       httpMethod: "PUT",
                       apiEndpoint: "/api/companyprofile/updatecompanyprofile",
                       message: "Company Profile updated successfully",
                       createdBy: uid,
                       payload: dto,
                       cancellationToken: cancellationToken
                   );
                    return TypedResults.Ok(updated);
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/updatecompanyprofile", ex, uid, cancellationToken);
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (ConflictException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/updatecompanyprofile", ex, uid, cancellationToken);
                    return TypedResults.Problem(
                        title: "Conflict",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict
                    );
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/updatecompanyprofile", ex, uid, cancellationToken);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/updatecompanyprofile", ex, uid, cancellationToken);
                    return TypedResults.Problem("Internal Server Error", ex.Message, 500);
                }
            })
            .WithName("UpdateCompanyProfile")
            .WithTags("Company")
            .WithSummary("Update a company profile")
            .WithDescription("Only the company owner (based on token) can update the profile.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/updatecompanybyuserid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                Company dto,
                ICompanyProfileService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    if (dto.UserId == string.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    var result = await service.UpdateCompany(dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Problem(
                        title: "Conflict",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict
                    );
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                           title: "Internal Server Error",
                           detail: ex.Message,
                           statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("UpdateCompanyProfileByUserId")
            .WithTags("Company")
            .WithSummary("Update a company profile (internal route via Dapr)")
            .WithDescription("Even internal calls must include JWT token and match company ownership.")
            .ExcludeFromDescription()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapDeleteCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPost("/deletecompanyprofile", async Task<Results<Ok<string>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
                [FromQuery] Guid id,
                [FromServices] ICompanyProfileService service,
                CancellationToken cancellationToken,
                HttpContext httpContext,
                AuditLogger auditLogger) =>
            {
                string uid = "unknown";
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrEmpty(userClaim))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User information is missing or invalid in the token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();
                    if (string.IsNullOrEmpty(uid))
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Unauthorized Access",
                            Detail = "User ID could not be extracted from token.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }

                    var result = await service.GetCompanyById(id, cancellationToken);
                    if (result == null)
                        throw new KeyNotFoundException($"Company with ID '{id}' not found.");

                    await service.DeleteCompany(new DeleteCompanyRequest { Id = id, UpdatedBy = uid }, cancellationToken);
                    await auditLogger.LogAuditAsync(
                        module: ModuleName,
                        httpMethod: "DELETE",
                        apiEndpoint: "/api/companyprofile/deletecompanyprofile",
                        message: "Company Profile deleted successfully",
                        createdBy: uid,
                        payload: new { Id = id },
                        cancellationToken: cancellationToken
                    );

                    return TypedResults.Ok("Company Profile deleted successfully");
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/deletecompanyprofile", ex, uid, cancellationToken);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/deletecompanyprofile", ex, uid, cancellationToken);
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/deletecompanyprofile", ex, uid, cancellationToken);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("DeleteCompanyProfile")
            .WithTags("Company")
            .WithSummary("Delete a company profile")
            .WithDescription("Deletes the specified company profile.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/deletecompanyprofilebyuserid", async Task<Results<Ok<string>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, ProblemHttpResult>> (
                [FromBody] DeleteCompanyRequest request,
                ICompanyProfileService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetCompanyById(request.Id, cancellationToken);
                    if (result == null)
                        throw new KeyNotFoundException($"Company with ID '{request.Id}' not found.");

                    await service.DeleteCompany(request, cancellationToken);

                    return TypedResults.Ok("Company Profile deleted successfully");
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .ExcludeFromDescription()
            .WithName("DeleteCompanyProfileByUserId")
            .WithTags("Company")
            .WithSummary("Delete a company profile by user ID")
            .WithDescription("Deletes the specified company profile using the user ID.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapCompanyApproval(this RouteGroupBuilder group)
        {
            group.MapPut("/action", async Task<IResult> (
            [FromBody] CompanyProfileApproveDto dto,
            [FromServices] ICompanyProfileService service,
            HttpContext httpContext,
            AuditLogger auditLogger,
            CancellationToken cancellationToken = default) =>
            {
                string? uid = "unknown";
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    uid = userData.GetProperty("uid").GetString();
                    if (dto == null)
                        throw new KeyNotFoundException($"Company with ID '{dto.CompanyId}' not found.");

                    await service.ApproveCompany(uid, dto, cancellationToken);
                    await auditLogger.LogAuditAsync(
                        module: ModuleName,
                        httpMethod: "POST",
                        apiEndpoint: "/api/companyprofile/action",
                        message: "Company Status Updated successfully",
                        createdBy: uid,
                        payload: dto,
                        cancellationToken: cancellationToken
                    );
                    return Results.Ok(new { message = "Company Status Updated successfully." });
                }
                catch (KeyNotFoundException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/action", ex, uid, cancellationToken);
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (InvalidDataException ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/action", ex, uid, cancellationToken);
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    await auditLogger.LogExceptionAsync(ModuleName, "/api/companyprofile/action", ex, uid, cancellationToken);
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("ApproveCompanyInternal")
            .WithTags("Company")
            .WithSummary("Action for company profile")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPut("/approvecompanybyuserid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                CompanyProfileApproveDto dto,
                ICompanyProfileService service,
                string userId,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    if (userId == string.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    var result = await service.ApproveCompany(userId, dto, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                           title: "Internal Server Error",
                           detail: ex.Message,
                           statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("ApproveCompanyInternalViaDapr")
            .WithTags("Company")
            .WithSummary("Approve a company profile internally via Dapr")
            .ExcludeFromDescription()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetCompanyProfilesByTokenUser(this RouteGroupBuilder group)
        {
            group.MapGet("/getcompanytokenuser", async Task<IResult> (
                HttpContext httpContext,
                [FromServices] ICompanyProfileService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();

                    var allCompanies = await service.GetCompaniesByTokenUser(uid, cancellationToken);
                    var userCompanies = allCompanies
                        .Where(c => c.UserId == uid)
                        .ToList();
                    if (userCompanies.Count == 0)
                        throw new KeyNotFoundException("No company profiles found for the current user.");

                    return TypedResults.Ok(userCompanies);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetCompanyProfilesByTokenUser")
            .WithTags("Company")
            .WithSummary("Get company profiles for logged-in user")
            .WithDescription("Fetches all companies owned by the current token user")
            .Produces<List<Company>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getbytokenuserid", async Task<IResult> (
            [FromQuery] string userId,
            ICompanyProfileService service,
            CancellationToken cancellationToken) =>
            {
                try
                {
                    if (userId == string.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var companies = await service.GetCompaniesByTokenUser(userId, cancellationToken);
                    return TypedResults.Ok(companies);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetCompaniesByUserId")
            .WithTags("Company")
            .WithSummary("Get companies by user ID")
            .WithDescription("Used internally by Dapr or system components.")
            .ExcludeFromDescription()
            .Produces<List<Company>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetAllCompanyProfiles(this RouteGroupBuilder group)
        {
            group.MapPost("/getallcompanies", async Task<IResult> (
                [FromServices] ICompanyProfileService service,
                [FromBody] CompanyProfileFilterRequest filter,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetAllVerifiedCompanies(filter, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch(InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Filter",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("PostGetAllCompanies")
            .WithTags("Company")
            .WithSummary("Get all verified company profiles with filters")
            .WithDescription("Fetches verified companies using a filter object with search, pagination, and sorting.")
            .Produces<CompanyPaginatedResponse<QLN.Common.Infrastructure.Model.Company>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapCompanySubscriptionExternal(this RouteGroupBuilder group)
        {
            group.MapPost("/companysubscriptions", async Task<Results<
                Ok<List<CompanySubscriptionDto>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                CompanySubscriptionFilter filter,
                ICompanyProfileService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var result = await service.GetCompanySubscriptions(filter, cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetCompanySubscriptions")
            .WithTags("Company Subscriptions")
            .WithSummary("Fetch companies with subscriptions")
            .WithDescription("Returns companies joined with their subscriptions based on product and date filters")
            .Produces<List<CompanySubscriptionDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }

    }
}
