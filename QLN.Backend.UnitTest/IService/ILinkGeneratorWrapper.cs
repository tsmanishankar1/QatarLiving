using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace QLN.Backend.UnitTest.IService
{
    public interface ILinkGeneratorWrapper
    {
        string GetUriByName(HttpContext httpContext, string endpointName, object values, string scheme,
            HostString? host, PathString? pathBase, FragmentString fragment, LinkOptions options);
    }
}
