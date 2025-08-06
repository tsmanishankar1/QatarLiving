using Microsoft.EntityFrameworkCore;
using Npgsql;
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

builder.Configuration
    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../QLN.Backend.API"))
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true);
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<QLCompanyContext>(options =>
    options.UseNpgsql(dataSource));

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

