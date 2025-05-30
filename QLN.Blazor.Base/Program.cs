using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MudBlazor;
using MudBlazor.Services;
using QLN.Blazor.Base.Services;
using QLN.Web.Shared;
using QLN.Web.Shared;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Pages;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(Options=>{
//     Options.Cookie.Name = "auth-name";
//     Options.LoginPath = "/login";
//     Options.Cookie.MaxAge=TimeSpan.FromMinutes(10);

// });

#region Authentication - Cookie configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "qat";
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            var jwt = context.Request.Cookies["qat"];
            if (string.IsNullOrEmpty(jwt))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = false,
                SignatureValidator = (token, parameters) =>
                {
                    // Bypass signature validation for demo purposes
                    return new JwtSecurityToken(token);
                },
                //ValidIssuer = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Issuer"],
                //ValidAudience = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Audience"],
                //IssuerSigningKey = new SymmetricSecurityKey(
                //    Encoding.UTF8.GetBytes(
                //        context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Key"]!
                //    )
                //),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };

            try
            {
                var principal = tokenHandler.ValidateToken(jwt, validationParameters, out var validatedToken);
                context.ReplacePrincipal(principal);
                context.ShouldRenew = false;
            }
            catch
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        }
    };
});
#endregion

// Add this before registering AuthenticationStateProvider
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CookieAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CookieAuthStateProvider>());
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<ICommunityService,CommunityMockService>();

//builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ICompanyProfileService, CompanyProfileService>();
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddHttpClient<ApiService>();
builder.Services.AddWebSharedServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
