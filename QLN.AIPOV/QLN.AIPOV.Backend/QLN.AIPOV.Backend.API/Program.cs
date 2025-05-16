using QLN.AIPOV.Backend.API;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Access IConfiguration like this:
var configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOpenApi()
    .AddApiConfig(configuration)
    .AddApiServices()
    .AddHttpClients(); ;

var app = builder.Build();

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
