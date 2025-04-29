using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QLN.Common.Infrastructure.AuthUser;
using QLN.Common.Infrastructure.DbContext;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.ServiceConfiguration;
using QLN.Common.Infrastructure.TokenProvider;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1.1.1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Qatar Management API",
        Version = "v1.1.1",
        Description = "API documentation for Qatar Management system."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});


builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
{
    opt.TokenLifespan = TimeSpan.FromMinutes(30);
});

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;              
    options.Password.RequireLowercase = true;          
    options.Password.RequireUppercase = true;          
    options.Password.RequireNonAlphanumeric = true;    
    options.Password.RequiredLength = 8;               
    options.Password.RequiredUniqueChars = 1;          
});

#region dbinject
builder.Services.AddDbContext<QatarlivingDevContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion



// Custom Token Providers registration
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;

    options.Tokens.ProviderMap["EmailVerification"] = new TokenProviderDescriptor(typeof(QLN.Common.Infrastructure.TokenProvider.EmailTokenProvider<ApplicationUser>));
    options.Tokens.ProviderMap["PhoneVerification"] = new TokenProviderDescriptor(typeof(PhoneTokenProvider<ApplicationUser>));

    options.Tokens.EmailConfirmationTokenProvider = "EmailVerification";
    options.Tokens.ChangePhoneNumberTokenProvider = "PhoneVerification";
})
.AddEntityFrameworkStores<QatarlivingDevContext>()
.AddDefaultTokenProviders();


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var config = builder.Configuration;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = config["Jwt:Issuer"],
        ValidAudience = config["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
    };
})
.AddGoogle("Google", options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.SignInScheme = IdentityConstants.ExternalScheme;
});



builder.Services.AddAuthorization();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.ServicesConfiguration(builder.Configuration);

//builder.Services.ServicesConfiguration(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1.1.1/swagger.json", "v1.1.1");
        options.RoutePrefix = "Swagger";
        options.DocumentTitle = "Qatar Management API";
    });
}

// Group Auth Routes
var authGroup = app.MapGroup("/auth");
authGroup.MapAuthEndpoints();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();