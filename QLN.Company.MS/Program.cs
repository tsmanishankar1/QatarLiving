using Microsoft.EntityFrameworkCore;
using Npgsql;
using QLN.Common.Infrastructure.Auditlog;
using QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Company.MS.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICompanyProfileService, InternalCompanyProfileService>();
builder.Services.AddScoped<AuditLogger>();
#region
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<QLCompanyContext>(options =>
    options.UseNpgsql(dataSource));
builder.Services.AddDbContext<QLLogContext>(options =>
    options.UseNpgsql(dataSource));
builder.Services.AddDbContext<QLSubscriptionContext>(options =>
    options.UseNpgsql(dataSource));
#endregion
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var companyProfileGroup = app.MapGroup("/api/companyprofile");
companyProfileGroup.MapCompanyProfile();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

