using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Content.MS.Service;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IV2EventService, V2InternalEventService>();
builder.Services.AddScoped<IV2NewsService, V2InternalNewsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var eventGroup = app.MapGroup("v2/api/event");
eventGroup.MapEventEndpoints();
var newsGroup = app.MapGroup("v2/api/News");
newsGroup.MapNewsEndpoints();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
