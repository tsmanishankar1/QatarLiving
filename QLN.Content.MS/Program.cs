using Dapr.Client;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Content.MS.Service.NewsInternalService;

var builder = WebApplication.CreateBuilder(args);

//jwt

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

builder.Services.AddDaprClient();

// ✅ Register your internal service with DI
builder.Services.AddScoped<IV2ContentNews, NewsInternalService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// ✅ Map custom minimal endpoints
app.MapGroup("/api/v2").MapContentNewsEndpoints(); // Adjust prefix if needed
app.UseHttpsRedirection();
app.UseAuthorization();

app.Run();
