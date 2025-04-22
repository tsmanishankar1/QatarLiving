using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QLN.Backend.UnitTest.IService;

namespace QLN.Backend.UnitTest.Service
{
    public class LinkGeneratorWrapper : ILinkGeneratorWrapper
    {
        private readonly LinkGenerator _linkGenerator;

        public LinkGeneratorWrapper(LinkGenerator linkGenerator)
        {
            _linkGenerator = linkGenerator;
        }

        public string GetUriByName(HttpContext httpContext, string endpointName, object values, string scheme,
            HostString? host, PathString? pathBase, FragmentString fragment, LinkOptions options)
        {
            return _linkGenerator.GetUriByName(httpContext, endpointName, values, scheme, host, pathBase, fragment, options);
        }
    }
}
