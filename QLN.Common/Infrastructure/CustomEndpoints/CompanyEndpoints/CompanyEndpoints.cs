using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.ICompanyService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints
{
    public static class CompanyEndpoints
    {
        public static RouteGroupBuilder MapCompanyProfileEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async (
                [FromForm] CompanyProfileDto dto,
                [FromServices] ICompanyService service,
                HttpContext ctx) =>
            {
                var entity = await service.CreateAsync(dto, ctx);
                return Results.Ok(entity);
            }).DisableAntiforgery();

            group.MapGet("/get", async (Guid id, [FromServices] ICompanyService service) =>
            {
                var result = await service.GetAsync(id);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });

            group.MapGet("/all", async ([FromServices] ICompanyService service) =>
            {
                var list = await service.GetAllAsync();
                return Results.Ok(list);
            });

            group.MapPut("/update", async (
                Guid id,
                [FromForm] CompanyProfileDto dto,
                [FromServices] ICompanyService service,
                HttpContext ctx) =>
            {
                var entity = await service.UpdateAsync(id, dto, ctx);
                return Results.Ok(entity);
            });

            group.MapDelete("/delete", async (
                Guid id,
                [FromServices] ICompanyService service) =>
            {
                await service.DeleteAsync(id);
                return Results.NoContent();
            });

            return group;
        }
    }
}
