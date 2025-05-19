using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using QLN.Web.Shared.Services;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Models;
using Microsoft.JSInterop;


public class CustomAuthStateProvider : AuthenticationStateProvider
{
    [CascadingParameter] public GlobalAppState? AppState { get; set; }
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    private readonly ApiService Api;

    private ClaimsPrincipal _user = new ClaimsPrincipal(new ClaimsIdentity());
    private readonly IJSRuntime _jsRuntime;

    public CustomAuthStateProvider(ApiService apiService, NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        Api = apiService;
        NavigationManager = navigationManager;
        _jsRuntime = jsRuntime;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_user));
    }

    public async Task AuthenticateUser(string Username, string password)
    {
        _user = new ClaimsPrincipal(new ClaimsIdentity());
        var payload = new { usernameOrEmailOrPhone = Username, password = password };
        var response = await Api.PostAsync<object, LoginResponse>("auth/login", payload);
        Console.WriteLine("Login Response",response);
        if (response == null)
        {
            NavigationManager.NavigateTo("/login");
        }
        else if (response != null)
        {
            if (response.Username == null)
            {
                NavigationManager.NavigateTo("/login");
            }
            else
            {
                if (!string.IsNullOrEmpty(response.AccessToken))
                {
                    Console.WriteLine($"Token: {response.AccessToken}");
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", response.AccessToken);
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Token stored: {response.AccessToken}");
                }
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, response.Username),
                    new Claim(ClaimTypes.Email, response.Emailaddress),
                    new Claim(ClaimTypes.MobilePhone, response.Mobilenumber),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("IsTwoFactorEnabled", response.IsTwoFactorEnabled?.ToString() ?? "false")
                }, "CustomAuth");
                _user = new ClaimsPrincipal(identity);
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
            }
        }
    }

    public void LogoutUser()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
    }

}
