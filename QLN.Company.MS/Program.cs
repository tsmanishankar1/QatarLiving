using QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Company.MS.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICompanyService, InternalCompanyService>();
builder.Services.AddScoped<ICompanyVerifiedService, InternalVerifiedCompany>();
builder.Services.AddScoped<ICompanyDealsStoresService, InternalDealsStoresCompany>();
builder.Services.AddScoped<ICompanyClassifiedService, InternalClassifiedCompanyService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var companyServiceGroup = app.MapGroup("/api/companyservice");
companyServiceGroup.MapCompanyServiceEndpoints();
var companyClassifiedsGroup = app.MapGroup("/api/companyprofile");
companyClassifiedsGroup.MapCompanyEndpoints();
var companyDsGroup = app.MapGroup("/api/companyds");
companyDsGroup.MapCompanyDealsStoresEndpoints();
var companyVerifiedGroup = app.MapGroup("/api/companyverified");
companyVerifiedGroup.MapVerifiedCompanyEndpoints();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

