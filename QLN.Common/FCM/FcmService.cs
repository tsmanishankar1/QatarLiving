using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.FCM;

public class FcmService
{
    private readonly ILogger<FcmService> _logger;
    private readonly FirebaseApp _iosFirebaseApp;
    private readonly FirebaseApp _androidFirebaseApp;
    private readonly DeviceRepository _deviceRepository;

    public FcmService(ILogger<FcmService> logger, IConfiguration configuration, DeviceRepository deviceRepository)
    {
        _logger = logger;
        _deviceRepository = deviceRepository;

        _iosFirebaseApp = FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(configuration["FCM_IOS_CREDENTIAL_JSON"]),
        }, "ios");

        _androidFirebaseApp = FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(configuration["FCM_ANDROID_CREDENTIAL_JSON"]),
        }, "android");
    }

    public async Task SendNotificationToUserAsync(int userId, string title, string body, Dictionary<string, string> data = null)
    {
        var devices = await _deviceRepository.GetActiveDevicesByUserIdAsync(userId);

        if (!devices.Any())
        {
            _logger.LogWarning($"No active devices found for user {userId}");
            return;
        }

        var tokens = devices.Select(d => d.FcmToken).ToList();
        await SendNotificationAsync(tokens, title, body, data);
    }

    public async Task SendNotificationAsync(List<string> tokens, string title, string body, Dictionary<string, string> data = null)
    {
        if (!tokens.Any())
        {
            _logger.LogWarning("No device tokens provided");
            return;
        }

        var message = new MulticastMessage
        {
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Android = new AndroidConfig
            {
                Notification = new AndroidNotification
                {
                    ChannelId = data?.GetValueOrDefault("channelId", "default")
                }
            },
            Apns = new ApnsConfig
            {
                Aps = new Aps
                {
                    Alert = new ApsAlert
                    {
                        Title = title,
                        Body = body
                    },
                    Sound = "default"
                }
            },
            Data = data ?? new Dictionary<string, string>(),
            Tokens = tokens
        };

        try
        {
            var androidTask = FirebaseMessaging.GetMessaging(_androidFirebaseApp).SendEachForMulticastAsync(message);
            var iosTask = FirebaseMessaging.GetMessaging(_iosFirebaseApp).SendEachForMulticastAsync(message);

            var results = await Task.WhenAll(androidTask, iosTask);

            foreach (var result in results)
            {
                foreach (var response in result.Responses)
                {
                    if (!response.IsSuccess)
                    {
                        _logger.LogWarning($"Failed to send notification to token: {tokens}, error: {response.Exception.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM notification");
            throw;
        }
    }
}

public class DeviceRepository
{
    public Task<List<Device>> GetActiveDevicesByUserIdAsync(int userId)
    {
        // Simulate fetching devices from a database
        return Task.FromResult(new List<Device>());
    }
}

public class Device
{
    public string FcmToken { get; set; }
    public bool IsActive { get; set; }
}