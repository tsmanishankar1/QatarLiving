using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using QLN.AIPOV.Frontend.Blazor.Client;
using QLN.AIPOV.Frontend.Blazor.Client.Services.Implementation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices()
    .AddServices()
    .AddHttpClientServices(builder.Configuration);

var chatBackendUrl = builder.Configuration.GetValue<string>("ChatBackend:Endpoint")
                     ?? throw new InvalidOperationException("ChatBackend:Endpoint is not configured");
builder.Services.AddHttpClient<ChatService>(client =>
{
    client.BaseAddress = new Uri(chatBackendUrl);
});

await builder.Build().RunAsync();
