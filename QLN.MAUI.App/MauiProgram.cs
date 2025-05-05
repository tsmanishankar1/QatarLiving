using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Services;
using QLN.MAUI.App.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;

namespace QLN.MAUI.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });


        // Add device-specific services used by the QLN.MAUI.App.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        //return builder.Build();

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthorizationCore();
        builder.Services.TryAddScoped<AuthenticationStateProvider, ExternalAuthStateProvider>();
        builder.Services.AddSingleton<AuthenticatedUser>();
        var host = builder.Build();

        var authenticatedUser = host.Services.GetRequiredService<AuthenticatedUser>();

        /*
        Provide OpenID/MSAL code to authenticate the user. See your identity provider's 
        documentation for details.

        The user is represented by a new ClaimsPrincipal based on a new ClaimsIdentity.
        */
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        authenticatedUser.Principal = user;

        return host;
    }
}
