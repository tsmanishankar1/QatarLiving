using Dapr.Client;
using Google.Api;



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
    // Register actor types and configure actor settings
    options.Actors.RegisterActor<SubscriptionActor>();

    // Configure default settings
    // options.ActorIdleTimeout = TimeSpan.FromMinutes(10);
    // options.ActorScanInterval = TimeSpan.FromSeconds(35);
    // options.DrainOngoingCallTimeout = TimeSpan.FromSeconds(35);
    // options.DrainRebalancedActors = true;
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

