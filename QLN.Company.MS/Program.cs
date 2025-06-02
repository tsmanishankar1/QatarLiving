using QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Company.MS.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICompanyService, InternalCompanyService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var companyGroup = app.MapGroup("/api/companyprofile");
companyGroup.MapCompanyEndpoints();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

