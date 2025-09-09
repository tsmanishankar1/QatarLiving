using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.IService.IAuth;
using QLN.Common.Infrastructure.IService.IContentService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.User
{
    public static class DrupalUserEndpoint
    {
        public static RouteGroupBuilder MapUserAutoCompleteEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/user/autocomplete/{search}", async (
                [FromRoute] string search,
                [FromServices] IDrupalUserService svc,
                CancellationToken cancellationToken
            ) =>
            {
                if (string.IsNullOrWhiteSpace(search) || search.Length < 3)
                    return Results.BadRequest("Search query must be at least 3 characters long.");

                var results = await svc.GetUserAutocompleteFromDrupalAsync(search, cancellationToken);

                return Results.Ok(results ?? new List<DrupalUserAutocompleteResponse>());
            })
            .WithName("PostUserAutocomplete")
            .WithTags("DrupalUser");
            return group;
        }
    }
}
