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
                        throw new UnauthorizedAccessException("Invalid user ID in JWT");

                    var entity = await service.CreateCompany(dto, cancellationToken);
                    return TypedResults.Ok("Company Profile created successfully");
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
                           detail: ex.ToString(),
                           statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Subscriber" })
            .WithName("CreateCompanyProfile")
            .WithTags("Company")
            .WithSummary("Create a company profile")
            .WithDescription("Creates a new company profile.")
            .Produces<CompanyProfileEntity>(StatusCodes.Status200OK)
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
            .Produces<IEnumerable<CompanyProfileEntity>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }

        public static RouteGroupBuilder MapUpdateCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPut("/update", async Task<Results<
                Ok<object>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                Guid id,
                CompanyProfileDto dto,
                ICompanyService service,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    var result = await service.UpdateCompany(id, dto, cancellationToken);
                    return TypedResults.Ok<object>(result);
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
                catch (Exception)
                {
                    return TypedResults.Problem(
                          title: "Internal Server Error",
                          detail: "An unexpected error occurred.",
                          statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("UpdateCompanyProfile")
            .WithTags("Company")
            .WithSummary("Update a company profile")
            .WithDescription("Updates an existing company profile.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
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
            group.MapGet("/completion-status", async Task<IResult> (
                [FromQuery] Guid userId,
                [FromQuery] string vertical,
                [FromServices] ICompanyService service) =>
            {
                try
                {
                    var result = await service.GetCompanyProfileCompletionStatus(userId, vertical);
                    if (result == null)
                        throw new KeyNotFoundException($"Company with ID not found.");
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
            .WithName("GetCompanyProfileCompletionStatus")
            .WithTags("Company")
            .WithSummary("Get company profile completion status")
            .WithDescription("Returns completion percentage and pending fields for company profile, used to block publishing until 100% complete.")
            .Produces<CompanyProfileCompletionStatusDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapGetVerificationStatus(this RouteGroupBuilder group)
        {
            group.MapGet("/verification-status", async Task<IResult> (
                [FromQuery] Guid userId,
                [FromQuery] VerticalType vertical,
                [FromServices] ICompanyService service,
                HttpContext context) =>
            {
                try
                {
                    var result = await service.GetVerificationStatus(userId, vertical);
                    if (result == null)
                        throw new KeyNotFoundException($"Company with ID not found.");
                    return Results.Ok(result);
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
            .WithName("GetCompanyProfileVerificationStatus")
            .WithTags("Company")
            .WithSummary("Get company profile verification status")
            .WithDescription("Returns verification status")
            .Produces<CompanyProfileVerificationStatusDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
        public static RouteGroupBuilder MapCompanyApproval(this RouteGroupBuilder group)
        {
            group.MapPost("/approve", async Task<IResult> (
                [FromBody] CompanyApproveDto dto,
                [FromServices] ICompanyService service,
                HttpContext httpContext,
                CancellationToken cancellationToken = default) =>
            {
                try
                {
                    await service.ApproveCompany(dto, cancellationToken);
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
            .WithSummary("Approve a company profile internally")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
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
                [FromServices] ICompanyService service) =>
            {
                try
                {
                    var result = await service.VerificationStatus(isVerified);
                    if (result == null)
                        throw new KeyNotFoundException("Verification status not found");
                    return Results.Ok(result);
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
            .WithName("GetCompaniesByVerificationStatus")
            .WithTags("Company")
            .WithSummary("Get companies by verification status")
            .WithDescription("Returns verified or unverified companies based on the isverified query parameter")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<IEnumerable<CompanyProfileVerificationStatusDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}