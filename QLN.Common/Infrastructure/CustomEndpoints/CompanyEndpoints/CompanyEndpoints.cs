using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.Utilities;
using System.Net.Http;
using System.Security.Claims;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyEndpoints
    {
        public static RouteGroupBuilder MapCreateCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPost("/create", /*[Authorize(Roles = "Subscriber")]*/ async Task<Results<
                Ok<string>,
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
                catch (Exception)
                {
                    return TypedResults.Problem(
                           title: "Internal Server Error",
                           detail: "An unexpected error occurred.",
                           statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
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
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Company with id '{id}' was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
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
                    NoContent,
                    NotFound<ProblemDetails>,
                    ProblemHttpResult>> (
                [FromQuery] Guid id,
                [FromServices] ICompanyService service) =>
            {
                try
                {
                    var result = await service.GetCompanyById(id);
                    await service.DeleteCompany(id);
                    return TypedResults.NoContent();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Company with id '{id}' was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
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
            .Produces(StatusCodes.Status204NoContent)
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
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Company with userid was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
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

                //if (!context.User.IsInRole("Admin"))
                //    return Results.Forbid();

                try
                {
                    var result = await service.GetVerificationStatus(userId, vertical);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Company with userid was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
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
    }
}