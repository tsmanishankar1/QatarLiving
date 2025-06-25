using System.Text;
using Dapr.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QLN.Classified.MS.Service;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;
using Microsoft.EntityFrameworkCore;
using System;
using QLN.Common.Infrastructure.DbContext;
using Microsoft.AspNetCore.Identity;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.ServiceConfiguration;
using QLN.Classifieds.MS.ServiceConfiguration;
using QLN.Common.Infrastructure.CustomEndpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts => {
    opts.SwaggerDoc("v1", new OpenApiInfo { Title = "QLN.Classified.MS", Version = "v1" });
    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT auth"
    });
    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }]
        = new string[] { }
    });
});

builder.Services.AddAuthorization();
builder.Services.AddDaprClient();
builder.Services.ClassifiedInternalServicesConfiguration(builder.Configuration);


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.MapGroup("/api/classifieds")
   .MapClassifiedEndpoints();

var servicesGroup = app.MapGroup("/api/services");
servicesGroup.MapServicesEndpoints();
app.MapAllBackOfficeEndpoints();

app.Run();
