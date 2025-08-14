using QLN.Common.DTO_s;

namespace QLN.Notification.MS.IService.INotificationService
{
    public interface INotificationService
    {
        Task SendMail(NotificationDto request);
    }
}
