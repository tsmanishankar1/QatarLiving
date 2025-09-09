using QLN.Notification.MS.IService.INotificationService;
using Dapr;
using QLN.Common.DTO_s;

namespace QLN.Notification.MS.CustomEndpoints.NotificationEndpoints
{
    internal class NotificationSubscriber { }

    public static class NotificationSubscriberEndpoints
    {
        public static RouteGroupBuilder MapNotificationSubscriber(this RouteGroupBuilder app)
        {
            app.MapPost("/notifications/email", [Topic("pubsub", "notifications-email")]
            async (NotificationDto request,
                       INotificationService service,
                       ILogger<NotificationSubscriber> logger) =>
                {
                    try
                    {
                        logger.LogInformation("Received email notification request via pubsub. Subject: {Subject}", request.Subject);

                        await service.SendMail(request);

                        logger.LogInformation("Notification processed successfully.");
                        return Results.Ok();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error while processing notification.");
                        return Results.Problem("Internal failure while sending notification.");
                    }
                })
            .WithName("NotificationEmailSubscriber")
            .WithTags("NotificationSubscriber")
            .WithSummary("Handles email notifications via Dapr pubsub")
            .Accepts<NotificationDto>("application/json")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
            return app;
        }
    }
}
