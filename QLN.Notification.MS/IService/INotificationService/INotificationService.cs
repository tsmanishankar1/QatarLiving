using static QLN.Common.DTO_s.NotificationDto;

namespace QLN.Notification.MS.IService.INotificationService
{
    public interface INotificationService
    {
        Task SendMail(NotificationRequest request);
    }
}
