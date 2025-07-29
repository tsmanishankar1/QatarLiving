using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System.Security.Claims;
using System.Text.Json;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyVerifiedEndpoints
    {
        public static RouteGroupBuilder MapCreateVerificationCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                VerifiedCompanyDto dto,
                ICompanyVerifiedService service,
                HttpContext httpContext,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    var isSubcriber = userData.GetProperty("roles").EnumerateArray()
                        .Any(r => r.GetString() == "subscription");
                    dto.UserId = uid;
                    if (dto.UserId != uid)
                        return TypedResults.Forbid();
                    var result = await service.CreateCompany(dto, cancellationToken);
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
            .WithName("CreateVerificationCompanyProfile")
            .WithTags("Company")
            .WithSummary("Create a company profile")
            .WithDescription("Creates a new company profile using the user ID from the access token.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

            group.MapPost("/createByUserId", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                VerifiedCompanyDto dto,
                ICompanyVerifiedService service,
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

                    var result = await service.CreateCompany(dto, cancellationToken);
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
            .WithName("CreateVerificationCompanyProfileByUserId")
            .WithTags("Company")
            .WithSummary("Create company profile by passing user ID explicitly")
            .WithDescription("Used by external services to create company profiles without requiring authorization.")
            .ExcludeFromDescription()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

            return group;
        }
        public static RouteGroupBuilder MapGetVerificationCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapGet("/getById", async Task<IResult> (
            [FromQuery] Guid id,
            [FromServices] ICompanyVerifiedService service) =>
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
            .WithName("GetVerificationCompanyProfile")
            .WithTags("Company")
            .WithSummary("Get a company profile")
            .WithDescription("Retrieves a company profile by ID.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetAllVerificationCompanyProfiles(this RouteGroupBuilder group)
        {
            group.MapGet("/getAll", async Task<IResult>
            ([FromServices] ICompanyVerifiedService service) =>
            {
                try
                {
                    var result = await service.GetAllCompanies();
                    return TypedResults.Ok(result);
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
            .WithName("GetAllVerificationCompanyProfiles")
            .WithTags("Company")
            .WithSummary("Get all company profiles")
            .WithDescription("Fetches all company profiles.")
            .Produces<IEnumerable<VerifiedCompanyDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapUpdateVerificationCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPut("/update", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                NotFound<ProblemDetails>,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                VerifiedCompanyDto dto,
                ICompanyVerifiedService service,
                HttpContext httpContext,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();
                    var isSubcriber = userData.GetProperty("roles").EnumerateArray()
                        .Any(r => r.GetString() == "subscription");

                    var existingCompany = await service.GetCompanyById(dto.Id.Value, cancellationToken);
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

                    var updated = await service.UpdateCompany(dto, cancellationToken);
                    return TypedResults.Ok(updated);
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
                    return TypedResults.Problem("Internal Server Error", ex.Message, 500);
                }
            })
            .WithName("UpdateVerificationCompanyProfile")
            .WithTags("Company")
            .WithSummary("Update a company profile")
            .WithDescription("Only the company owner (based on token) can update the profile.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

            group.MapPut("/updateByUserId", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                VerifiedCompanyDto dto,
                ICompanyVerifiedService service,
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
            .WithName("UpdateVerificationCompanyProfileByUserId")
            .WithTags("Company")
            .WithSummary("Update a company profile (internal route via Dapr)")
            .WithDescription("Even internal calls must include JWT token and match company ownership.")
            .ExcludeFromDescription()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

            return group;
        }
        public static RouteGroupBuilder MapDeleteVerificationCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapDelete("/delete", async Task<Results<
                    Ok<string>,
                    NotFound<ProblemDetails>, BadRequest<ProblemDetails>,
                    ProblemHttpResult>> (
                [FromQuery] Guid id,
                [FromServices] ICompanyVerifiedService service) =>
            {
                try
                {
                    var result = await service.GetCompanyById(id);
                    await service.DeleteCompany(id);
                    if (result == null)
                        throw new KeyNotFoundException($"Company with ID '{id}' not found.");
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
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("DeleteVerificationCompanyProfile")
            .WithTags("Company")
            .WithSummary("Delete a company profile")
            .WithDescription("Deletes the specified company profile.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapVerificationCompanyApproval(this RouteGroupBuilder group)
        {
            group.MapPut("/approve", async Task<IResult> (
                [FromBody] CompanyVerificationApproveDto dto,
                [FromServices] ICompanyVerifiedService service,
                HttpContext httpContext,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value
                                 ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!Guid.TryParse(userId, out var userGuid))
                        return TypedResults.Forbid();
                    if (dto == null)
                        throw new KeyNotFoundException($"Company with ID '{dto.CompanyId}' not found.");
                    await service.ApproveCompany(userGuid, dto, cancellationToken);
                    return Results.Ok(new { message = "Company approved successfully." });
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
            .WithName("ApproveVerificationCompanyInternal")
            .WithTags("Company")
            .WithSummary("Approve a company profile")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/approveByUserId", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                CompanyVerificationApproveDto dto,
                ICompanyVerifiedService service,
                Guid userId,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    if (userId == Guid.Empty)
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
            .WithName("ApproveVerificationCompanyInternalViaDapr")
            .WithTags("Company")
            .WithSummary("Approve a company profile internally via Dapr")
            .ExcludeFromDescription()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetVerificationCompanyApprovalInfo(this RouteGroupBuilder group)
        {
            group.MapGet("/getApproval", async Task<Results<
                Ok<CompanyVerifyApprovalResponseDto>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>> (
                [FromQuery] Guid companyId,
                [FromServices] ICompanyVerifiedService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var info = await service.GetCompanyApprovalInfo(companyId, cancellationToken);
                    if (info == null)
                        throw new KeyNotFoundException($"Company with ID '{companyId}' not found.");
                    return TypedResults.Ok(info);
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
            .WithName("GetVerificationCompanyApprovalInfo")
            .WithTags("Company")
            .WithSummary("Get approval info of a company")
            .WithDescription("Returns company ID, name, verification status, and status details.")
            .Produces<CompanyVerifyApprovalResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapVerifyVerificationStatus(this RouteGroupBuilder group)
        {
            group.MapGet("/verifiedstatus", async Task<IResult> (
                [FromQuery] bool isVerified,
                [FromQuery] VerticalType vertical,
                HttpContext httpContext,
                [FromServices] ICompanyVerifiedService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var tokenUserId = httpContext.User.FindFirst("sub")?.Value
                                    ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!Guid.TryParse(tokenUserId, out var userGuid))
                        return TypedResults.Forbid();

                    var result = await service.VerificationStatus(userGuid, vertical, isVerified, cancellationToken);
                    if (result == null || result.Count == 0)
                        throw new KeyNotFoundException("Company profile not found");

                    return TypedResults.Ok(result ?? new List<CompanyVerificationStatusDto>());
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
                    return TypedResults.Problem("Internal Server Error", ex.Message, 500);
                }
            })
            .WithName("GetVerificationCompaniesByVerificationStatus")
            .WithTags("Company")
            .WithSummary("Get companies by verification status")
            .WithDescription("Returns companies matching isVerified for current user.")
            .Produces<IEnumerable<CompanyVerificationStatusDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/verifiedstatusbyuserId", async Task<IResult> (
                [FromQuery] bool isVerified,
                [FromQuery] VerticalType vertical,
                [FromQuery] Guid userId,
                [FromServices] ICompanyVerifiedService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.VerificationStatus(userId, vertical, isVerified, cancellationToken);
                    if (result == null || result.Count == 0)
                        throw new KeyNotFoundException("Company profile not found");

                    return TypedResults.Ok(result ?? new List<CompanyVerificationStatusDto>());
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
                    return TypedResults.Problem("Internal Server Error", ex.Message, 500);
                }
            })
            .WithName("GetVerificationCompaniesByVerificationStatusByUserId")
            .WithTags("Company")
            .WithSummary("Get companies by verification status")
            .WithDescription("Used internally by Dapr with userId passed in query.")
            .ExcludeFromDescription()
            .Produces<IEnumerable<CompanyVerificationStatusDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetVerificationCompanyProfilesByTokenUser(this RouteGroupBuilder group)
        {
            group.MapGet("/getByTokenUser", async Task<IResult> (
                HttpContext httpContext,
                [FromServices] ICompanyVerifiedService service,
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
            .WithName("GetVerificationCompanyProfilesByTokenUser")
            .WithTags("Company")
            .WithSummary("Get company profiles for logged-in user")
            .WithDescription("Fetches all companies owned by the current token user")
            .Produces<List<CompanyProfileDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getByUserId", async Task<IResult> (
            [FromQuery] string userId,
            ICompanyVerifiedService service,
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
            .WithName("GetVerificationCompaniesByUserId")
            .WithTags("Company")
            .WithSummary("Get companies by user ID")
            .WithDescription("Used internally by Dapr or system components.")
            .ExcludeFromDescription()
            .Produces<List<CompanyProfileDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetVerificationStatusByTokenUser(this RouteGroupBuilder group)
        {
            group.MapGet("/profileStatus", async Task<IResult> (
                HttpContext httpContext,
                [FromQuery] VerticalType vertical,
                [FromQuery] SubVertical subVertical,
                [FromServices] ICompanyVerifiedService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();

                    var companies = await service.GetStatusByTokenUser(uid, cancellationToken);

                    var filtered = companies
                        .Where(c => c.Vertical == vertical &&
                                    c.SubVertical == subVertical &&
                                    c.UserId == uid)
                        .ToList();

                    if (filtered.Count == 0)
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "No company profiles matched the given vertical and subvertical for this user.",
                            Status = StatusCodes.Status404NotFound
                        });

                    return TypedResults.Ok(filtered);
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
            .WithName("GetVerificationCompanyStatusByTokenUser")
            .WithTags("Company")
            .WithSummary("Get filtered company profiles for token user")
            .WithDescription("Returns company profiles matching vertical and subvertical for the current user.")
            .Produces<List<VerificationProfileStatus>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/statusByUserId", async Task<IResult> (
                [FromQuery] string userId,
                [FromServices] ICompanyVerifiedService service,
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

                    var companies = await service.GetStatusByTokenUser(userId, cancellationToken);
                    if (companies == null || companies.Count == 0)
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "No company profiles found for the provided user ID.",
                            Status = StatusCodes.Status404NotFound
                        });

                    return TypedResults.Ok(companies);
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
            .WithName("GetVerificationCompanyStatusByUserId")
            .WithTags("Company")
            .WithSummary("Get all company profiles for given userId")
            .WithDescription("Used for internal filtering of user companies")
            .ExcludeFromDescription()
            .Produces<List<VerificationProfileStatus>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
