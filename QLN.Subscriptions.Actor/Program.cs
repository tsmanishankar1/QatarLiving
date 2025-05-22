using Dapr.Client;
using Google.Api;
using QLN.Common.Infrastructure.Service;
using QLN.Subscriptions;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// adding DAPR support
//var daprClient = new DaprClientBuilder().Build();
//builder.Services.AddSingleton<DaprClient>(daprClient);


builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<SubscriptionActor>();
    options.Actors.RegisterActor<PaymentTransactionActor>();
    options.ActorIdleTimeout = TimeSpan.FromMinutes(60);
    options.ActorScanInterval = TimeSpan.FromSeconds(30);
    options.DrainOngoingCallTimeout = TimeSpan.FromSeconds(60);
    options.DrainRebalancedActors = true;
    options.RemindersStoragePartitions = 1;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapActorsHandlers();

app.Run();

