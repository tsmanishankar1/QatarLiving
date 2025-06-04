using Google.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.Utilities;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Security.Claims;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyEndpoints
    {
        public static RouteGroupBuilder MapCreateCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                CompanyProfileDto dto,
                ICompanyService service,
                HttpContext httpContext,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value
                              ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!Guid.TryParse(userId, out var userGuid))
                        return TypedResults.Forbid();

                    dto.UserId = userGuid; 

                    var result = await service.CreateCompany(dto, cancellationToken);
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
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Subscriber" })
            .WithName("CreateCompanyProfile")
            .WithTags("Company")
            .WithSummary("Create a company profile")
            .WithDescription("Creates a new company profile using the user ID from the access token.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

            group.MapPost("/createByUserId", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                CompanyProfileDto dto,
                ICompanyService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    if (dto.UserId == Guid.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    var result = await service.CreateCompany(dto, cancellationToken);
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
            .WithName("CreateCompanyProfileByUserId")
            .WithTags("Company")
            .WithSummary("Create company profile by passing user ID explicitly")
            .WithDescription("Used by external services to create company profiles without requiring authorization.")
            .ExcludeFromDescription()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

            return group;
        }

        public static RouteGroupBuilder MapGetCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapGet("/getById", async Task<IResult> (
            [FromQuery] Guid id,
            [FromServices] ICompanyService service) =>
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
        public static RouteGroupBuilder MapGetAllCompanyProfiles(this RouteGroupBuilder group)
        {
            group.MapGet("/getAll", async Task<IResult>
            ([FromServices] ICompanyService service) =>
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

        public static RouteGroupBuilder MapUpdateCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPut("/update", async Task<Results<
                Ok<string>,
                ForbidHttpResult,
                NotFound<ProblemDetails>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                [FromQuery] Guid id,
                CompanyProfileDto dto,
                ICompanyService service,
                HttpContext httpContext,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var tokenUserId = httpContext.User.FindFirst("sub")?.Value
                                    ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!Guid.TryParse(tokenUserId, out var userGuid))
                        return TypedResults.Forbid();

                    var existingCompany = await service.GetCompanyById(id, cancellationToken);
                    if (existingCompany == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Company with ID '{id}' not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    if (existingCompany.UserId != userGuid)
                        return TypedResults.Forbid();

                    dto.UserId = userGuid;

                    var updated = await service.UpdateCompany(dto, id, cancellationToken);
                    return TypedResults.Ok(updated);
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
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Subscriber" })
            .WithName("UpdateCompanyProfile")
            .WithTags("Company")
            .WithSummary("Update a company profile")
            .WithDescription("Only the company owner (based on token) can update the profile.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

            group.MapPut("/updateByUserId", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                CompanyProfileDto dto,
                Guid id,
                ICompanyService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    if (dto.UserId == Guid.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId must be provided in the payload.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    var result = await service.UpdateCompany(dto, id, cancellationToken);
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
            .WithName("UpdateCompanyProfileByUserId")
            .WithTags("Company")
            .WithSummary("Update a company profile (internal route via Dapr)")
            .WithDescription("Even internal calls must include JWT token and match company ownership.")
            .ExcludeFromDescription()
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

            return group;
        }

        public static RouteGroupBuilder MapDeleteCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapDelete("/delete", async Task<Results<
                    Ok<string>,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>> (
                [FromQuery] Guid id,
                [FromServices] ICompanyService service) =>
            {
                try
                {
                    var result = await service.GetCompanyById(id);
                    await service.DeleteCompany(id);
                    if (result== null)
                        throw new KeyNotFoundException($"Company with ID '{id}' not found.");
                    return TypedResults.Ok("Company Profile deleted successfully");
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
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
        public static RouteGroupBuilder MapGetCompanyProfileCompletionStatus(this RouteGroupBuilder group)
        {
            group.MapGet("/completionstatus", async Task<IResult> (
                [FromQuery] VerticalType vertical,
                [FromServices] ICompanyService service,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var tokenUserId = httpContext.User.FindFirst("sub")?.Value
                                    ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!Guid.TryParse(tokenUserId, out var userGuid))
                        return TypedResults.Forbid();

                    var result = await service.GetCompanyProfileCompletionStatus(userGuid, vertical, cancellationToken);

                    if (result == null || result.Count == 0)
                        throw new KeyNotFoundException("Company profile not found");

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
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetCompanyProfileCompletionStatus")
            .WithTags("Company")
            .WithSummary("Get company profile completion status")
            .WithDescription("Returns completion percentage and pending fields for company profile based on logged-in user")
            .Produces<CompanyProfileCompletionStatusDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/completionstatusbyuserId", async Task<IResult> (
                [FromQuery] Guid userId,
                [FromQuery] VerticalType vertical,
                [FromServices] ICompanyService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (userId == Guid.Empty)
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "UserId is required for internal completion status check.",
                            Status = StatusCodes.Status400BadRequest
                        });

                    var result = await service.GetCompanyProfileCompletionStatus(userId, vertical, cancellationToken);

                    if (result == null || result.Count == 0)
                        throw new KeyNotFoundException("Company profile not found");

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
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetCompanyProfileCompletionStatusByUserId") 
            .WithTags("Company")
            .WithSummary("Get company profile completion status")
            .WithDescription("Dapr-accessible endpoint for completion status with userId passed in query")
            .ExcludeFromDescription()
            .Produces<CompanyProfileCompletionStatusDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapCompanyApproval(this RouteGroupBuilder group)
        {
            group.MapPut("/approve", async Task<IResult> (
                [FromBody] CompanyApproveDto dto,
                [FromServices] ICompanyService service,
                HttpContext httpContext,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value
                                 ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!Guid.TryParse(userId, out var userGuid))
                        return TypedResults.Forbid();

                    await service.ApproveCompany(userGuid, dto, cancellationToken);
                    return Results.Ok(new { message = "Company approved successfully." });
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
            .WithSummary("Approve a company profile")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapPut("/approveByUserId", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                CompanyApproveDto dto,
                ICompanyService service,
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
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        public static RouteGroupBuilder MapGetCompanyApprovalInfo(this RouteGroupBuilder group)
        {
            group.MapGet("/getApproval", async Task<Results<
                Ok<CompanyApprovalResponseDto>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>> (
                [FromQuery] Guid companyId,
                [FromServices] ICompanyService service,
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
        public static RouteGroupBuilder MapVerificationStatus(this RouteGroupBuilder group)
        {
            group.MapGet("/verifiedstatus", async Task<IResult> (
                [FromQuery] bool isVerified,
                [FromQuery] VerticalType vertical,
                HttpContext httpContext,
                [FromServices] ICompanyService service,
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
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<IEnumerable<CompanyProfileVerificationStatusDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/verifiedstatusbyuserId", async Task<IResult> (
                [FromQuery] bool isVerified,
                [FromQuery] VerticalType vertical,
                [FromQuery] Guid userId,
                [FromServices] ICompanyService service,
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
    }
}