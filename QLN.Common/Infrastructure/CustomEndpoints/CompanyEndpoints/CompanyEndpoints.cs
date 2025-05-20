using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.Utilities;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyEndpoints
    {
        public static RouteGroupBuilder MapCreateCompanyProfile(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async Task<Results<
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
                    if (result is null)
                        throw new KeyNotFoundException($"Company with id '{id}' was not found.");

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
                catch (Exception)
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
                    /*var castedResult = result.Cast<object>();*/
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
                    if (result is null)
                        throw new KeyNotFoundException($"Company with id '{id}' was not found.");
                    await service.DeleteCompany(id);
                    return TypedResults.NoContent();
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
            .WithName("DeleteCompanyProfile")
            .WithTags("Company")
            .WithSummary("Delete a company profile")
            .WithDescription("Deletes the specified company profile.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            return group;
        }
    }
}