using QLN.Notification.MS.CustomEndpoints.NotificationEndpoints;
using QLN.Notification.MS.Dto;
using QLN.Notification.MS.IService.INotificationService;
using QLN.Notification.MS.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("OoredooSmsApi"));
builder.Services.AddScoped<INotificationService, InternalNotificationService>();
builder.Services.AddHttpClient();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCloudEvents();
app.MapSubscribeHandler();
var notifyGroup = app.MapGroup("/api/notification");
notifyGroup.MapNotificationEndpoints();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

