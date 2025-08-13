using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using QLN.Backend.Actor.ActorClass;
using QLN.Common.Infrastructure.CustomEndpoints.ProductEndpoints;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Subscriptions.Actor.ActorClass;
using QLN.Subscriptions.Actor.Service;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<DaprClient>(_ => new DaprClientBuilder().Build());
ThreadPool.SetMinThreads(Environment.ProcessorCount * 4, Environment.ProcessorCount * 4);
builder.Services.AddDbContext<QLSubscriptionContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
});

builder.Services.AddScoped<IProductService, InternalProductService>();
builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<SubscriptionActor>();
    options.Actors.RegisterActor<PaymentTransactionActor>();
    options.Actors.RegisterActor<PayToPublishPaymentActor>();
    options.Actors.RegisterActor<PayToPublishActor>();
    options.Actors.RegisterActor<PayToFeatureActor>();
    options.Actors.RegisterActor<PayToFeaturePaymentActor>();
    options.Actors.RegisterActor<AddonActor>();
    options.Actors.RegisterActor<AddonPaymentActor>();
    options.Actors.RegisterActor<UserQuotaActor>();
    options.Actors.RegisterActor<V2SubscriptionActor>();
    options.Actors.RegisterActor<V2UserAddonActor>();
    options.ActorIdleTimeout = TimeSpan.FromMinutes(60);
    options.ActorScanInterval = TimeSpan.FromSeconds(30);
    options.DrainOngoingCallTimeout = TimeSpan.FromSeconds(60);
    options.DrainRebalancedActors = true;
    options.RemindersStoragePartitions = 1;
});

var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapActorsHandlers();
var Product = app.MapGroup("/api/products");
Product.MapProductEndpoints();
app.Run();