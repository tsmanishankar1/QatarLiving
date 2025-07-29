using Microsoft.AspNetCore.Authorization;
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
    public static class CompanyEndpoints
    {
        public static RouteGroupBuilder MapCreateClassifiedsCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPost("/createclassifiedcompany", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                CompanyProfileDto dto,
                ICompanyClassifiedService service,
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
            .WithName("CreateCompanyProfile")
            .WithTags("Company")
            .WithSummary("Create a company profile")
            .WithDescription("Creates a new company profile using the user ID from the access token.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPost("/createclassifiedcompanybyuserid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                CompanyProfileDto dto,
                ICompanyClassifiedService service,
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
        public static RouteGroupBuilder MapClassifiedsGetCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapGet("/getclassifiedcompanybyid", async Task<IResult> (
            [FromQuery] Guid id,
            [FromServices] ICompanyClassifiedService service) =>
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
        public static RouteGroupBuilder MapGetAllClassifiedsCompanyProfiles(this RouteGroupBuilder group)
        {
            group.MapGet("/getallclassifiedcompany", async Task<IResult>
            ([FromServices] ICompanyClassifiedService service) =>
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
            .WithName("GetAllCompanyProfiles")
            .WithTags("Company")
            .WithSummary("Get all company profiles")
            .WithDescription("Fetches all company profiles.")
            .Produces<IEnumerable<CompanyProfileDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapUpdateClassifiedsCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPut("/updateclassifiedcompany", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                NotFound<ProblemDetails>,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                CompanyProfileDto dto,
                ICompanyClassifiedService service,
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

            group.MapPut("/updateclassifiedcompanybyuserid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                Conflict<string>,
                ProblemHttpResult>>
            (
                CompanyProfileDto dto,
                ICompanyClassifiedService service,
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
        public static RouteGroupBuilder MapDeleteClassifiedsCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapDelete("/deleteclassifiedcompany", async Task<Results<
                    Ok<string>,
                    NotFound<ProblemDetails>, BadRequest<ProblemDetails>,
                    ProblemHttpResult>> (
                [FromQuery] Guid id,
                [FromServices] ICompanyClassifiedService service) =>
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
            .WithName("DeleteCompanyProfile")
            .WithTags("Company")
            .WithSummary("Delete a company profile")
            .WithDescription("Deletes the specified company profile.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapCompanyClassifiedsApproval(this RouteGroupBuilder group)
        {
            group.MapPut("/approveclassifiedcompany", async Task<IResult> (
            [FromBody] CompanyApproveDto dto,
            [FromServices] ICompanyClassifiedService service,
            HttpContext httpContext,
            CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
                    if (string.IsNullOrWhiteSpace(userClaim))
                        return TypedResults.Forbid();

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();

                    if (!Guid.TryParse(uid, out var userGuid))
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
            .WithName("ApproveCompanyInternal")
            .WithTags("Company")
            .AllowAnonymous()
            .WithSummary("Approve a company profile")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapPut("/approveclassifiedcompanybyuserid", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
            (
                CompanyApproveDto dto,
                ICompanyClassifiedService service,
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
            .WithName("ApproveCompanyInternalViaDapr")
            .WithTags("Company")
            .RequireAuthorization() 
            .WithSummary("Approve a company profile internally via Dapr")
            .ExcludeFromDescription()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetCompanyClassifiedsApprovalInfo(this RouteGroupBuilder group)
        {
            group.MapGet("/getclassifiedcompanyapproval", async Task<Results<
                Ok<CompanyApprovalResponseDto>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>> (
                [FromQuery] Guid companyId,
                [FromServices] ICompanyClassifiedService service,
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
            .WithName("GetCompanyApprovalInfo")
            .WithTags("Company")
            .WithSummary("Get approval info of a company")
            .WithDescription("Returns company ID, name, verification status, and status details.")
            .Produces<CompanyApprovalResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapClassifiedsVerificationStatus(this RouteGroupBuilder group)
        {
            group.MapGet("/classifiedcompanyverifiedstatus", async Task<IResult> (
                [FromQuery] bool isVerified,
                [FromQuery] VerticalType vertical,
                HttpContext httpContext,
                [FromServices] ICompanyClassifiedService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;

                    var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
                    var uid = userData.GetProperty("uid").GetString();

                    var result = await service.VerificationStatus(uid, vertical, isVerified, cancellationToken);
                    if (result == null || result.Count == 0)
                        throw new KeyNotFoundException("Company profile not found");

                    return TypedResults.Ok(result ?? new List<CompanyProfileVerificationStatusDto>());
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
            .WithName("GetCompaniesByVerificationStatus")
            .WithTags("Company")
            .WithSummary("Get companies by verification status")
            .WithDescription("Returns companies matching isVerified for current user.")
            .Produces<IEnumerable<CompanyProfileVerificationStatusDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/classifiedcompanyverifiedstatusbyuserid", async Task<IResult> (
                [FromQuery] bool isVerified,
                [FromQuery] VerticalType vertical,
                [FromQuery] string userId,
                [FromServices] ICompanyClassifiedService service,
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

                    var result = await service.VerificationStatus(userId, vertical, isVerified, cancellationToken);
                    if (result == null || result.Count == 0)
                        throw new KeyNotFoundException("Company profile not found");

                    return TypedResults.Ok(result ?? new List<CompanyProfileVerificationStatusDto>());
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
            .WithName("GetCompaniesByVerificationStatusByUserId")
            .WithTags("Company")
            .WithSummary("Get companies by verification status")
            .WithDescription("Used internally by Dapr with userId passed in query.")
            .ExcludeFromDescription()
            .Produces<IEnumerable<CompanyProfileVerificationStatusDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetClassifiedsCompanyProfilesByTokenUser(this RouteGroupBuilder group)
        {
            group.MapGet("/getclassifiedcompanytokenuser", async Task<IResult> (
                HttpContext httpContext,
                [FromServices] ICompanyClassifiedService service,
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
            .Produces<List<CompanyProfileDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/getclassifiedcompanytokenuserbyuserid", async Task<IResult> (
            [FromQuery] string userId,
            ICompanyClassifiedService service,
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
            .Produces<List<CompanyProfileDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetClassifiedsStatusByTokenUser(this RouteGroupBuilder group)
        {
            group.MapGet("/classifiedcompanyprofilestatus", async Task<IResult> (
                HttpContext httpContext,
                [FromQuery] VerticalType vertical,
                [FromQuery] SubVertical subVertical,
                [FromServices] ICompanyClassifiedService service,
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
            .WithName("GetCompanyStatusByTokenUser")
            .WithTags("Company")
            .WithSummary("Get filtered company profiles for token user")
            .WithDescription("Returns company profiles matching vertical and subvertical for the current user.")
            .Produces<List<ProfileStatus>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/classifiedcompanystatusbyuserid", async Task<IResult> (
                [FromQuery] string userId,
                [FromServices] ICompanyClassifiedService service,
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
            .WithName("GetCompanyStatusByUserId")
            .WithTags("Company")
            .WithSummary("Get all company profiles for given userId")
            .WithDescription("Used for internal filtering of user companies")
            .ExcludeFromDescription()
            .Produces<List<ProfileStatus>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}