using Dapr.Client;
using Google.Api;
using QLN.Common.Infrastructure.Service;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using QLN.Subscriptions.Actor.ActorClass;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using QLN.Common.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.TokenProvider;
using QLN.Common.Infrastructure.IService.IPayToPublishService;
using QLN.Backend.API.Service.PayToPublishService;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<IExternalSubscriptionService, ExternalSubscriptionService>();
builder.Services.AddScoped<IPayToPublishService, ExternalPayToPublishService>();


builder.Services
  .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
  {
      options.Tokens.EmailConfirmationTokenProvider = "emailconfirmation";
  })
  .AddTokenProvider<
      QLN.Common.Infrastructure.TokenProvider.EmailTokenProvider<ApplicationUser>
  >("emailconfirmation")
  .AddEntityFrameworkStores<QatarlivingDevContext>()
  .AddDefaultTokenProviders();


builder.Services.AddDbContext<QatarlivingDevContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<SubscriptionActor>();
    options.Actors.RegisterActor<PaymentTransactionActor>();
    options.Actors.RegisterActor<PayToPublishPaymentActor>();
    options.Actors.RegisterActor<PayToPublishActor>();
    options.ActorIdleTimeout = TimeSpan.FromMinutes(60);
    options.ActorScanInterval = TimeSpan.FromSeconds(30);
    options.DrainOngoingCallTimeout = TimeSpan.FromSeconds(60);
    options.DrainRebalancedActors = true;
    options.RemindersStoragePartitions = 1;
});

var app = builder.Build();

PaymentTransactionActor.ServiceProvider = app.Services;
PayToPublishPaymentActor.ServiceProvider = app.Services;


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapActorsHandlers();

app.Run();