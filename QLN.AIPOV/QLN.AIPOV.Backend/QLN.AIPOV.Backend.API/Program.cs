using QLN.AIPOV.Backend.API;
using QLN.AIPOV.Backend.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Access IConfiguration like this:
var configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOpenApi()
    .AddApiConfig(configuration)
    .AddApiServices()
    .AddHttpClients()
    .AddAzureOpenAIClient(configuration)
    .AddAzureAISearchClient(configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
