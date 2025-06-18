using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Content.MS.Service.EventInternalService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IV2EventService, V2InternalEventService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var eventGroup = app.MapGroup("/api/v2/event");
eventGroup.MapEventEndpoints();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
