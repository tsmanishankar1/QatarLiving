using Microsoft.Extensions.Options;
using QLN.Notification.MS.Dto;
using QLN.Notification.MS.IService.INotificationService;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;
using static QLN.Common.DTO_s.NotificationDto;

namespace QLN.Notification.MS.Service
{
    public class InternalNotificationService : INotificationService
    {
        private readonly ILogger<InternalNotificationService> _logger;
        private readonly SmtpSettings _smtp;
        private readonly SmsSettings _sms;
        private readonly HttpClient _httpClient;

        public InternalNotificationService(
            ILogger<InternalNotificationService> logger,
            IOptions<SmtpSettings> smtpOptions,
            IOptions<SmsSettings> smsOptions,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _smtp = smtpOptions.Value;
            _sms = smsOptions.Value;
            _httpClient = clientFactory.CreateClient();
        }

        public async Task SendMail(NotificationRequest request)
        {
            foreach (var dest in request.Destinations)
            {
                switch (dest.ToLower())
                {
                    case "email":
                        await SendEmail(request);
                        break;

                    case "sms":
                        await SendSms(request);
                        break;

                    case "push":
                        _logger.LogInformation("Push not implemented.");
                        break;

                    default:
                        _logger.LogWarning("Unknown destination type: {Destination}", dest);
                        break;
                }
            }
        }

        private async Task SendEmail(NotificationRequest request)
        {
            var recipients = request.Recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)).ToList();
            if (!recipients.Any())
            {
                _logger.LogWarning("Skipping email - No recipient email available.");
                return;
            }

            using var smtpClient = new SmtpClient(_smtp.Host, int.Parse(_smtp.Port))
            {
                EnableSsl = _smtp.SSL,
                UseDefaultCredentials = _smtp.DefaultCredential,
                Credentials = new System.Net.NetworkCredential(_smtp.Username, _smtp.Password)
            };

            foreach (var recipient in recipients)
            {
                var mail = new MailMessage
                {
                    From = new MailAddress(_smtp.Email, _smtp.DisplayName),
                    Subject = request.Subject,
                    IsBodyHtml = true
                };

                mail.To.Add(recipient.Email);

                if (!string.IsNullOrWhiteSpace(request.Plaintext))
                {
                    var plainView = AlternateView.CreateAlternateViewFromString(request.Plaintext, Encoding.UTF8, "text/plain");
                    mail.AlternateViews.Add(plainView);
                }

                if (!string.IsNullOrWhiteSpace(request.Html))
                {
                    var htmlView = AlternateView.CreateAlternateViewFromString(request.Html, Encoding.UTF8, "text/html");
                    mail.AlternateViews.Add(htmlView);
                }
                else
                {
                    mail.Body = request.Plaintext ?? string.Empty;
                    mail.IsBodyHtml = false;
                }

                try
                {
                    await smtpClient.SendMailAsync(mail);
                    _logger.LogInformation("Email sent to {Email}", recipient.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email}", recipient.Email);
                }
            }
        }

        private async Task SendSms(NotificationRequest request)
        {
            var recipients = request.Recipients
                .Where(r => !string.IsNullOrWhiteSpace(r.Phone))
                .ToList();

            if (!recipients.Any())
            {
                _logger.LogWarning("Skipping SMS - No recipient phone available.");
                return;
            }

            foreach (var recipient in recipients)
            {
                var phone = recipient.Phone.Replace("+", "").Replace(" ", "").Trim();

                var xml = new XElement("MESSAGE",
                    new XElement("CUSTOMER", _sms.CustomerId),
                    new XElement("USER", _sms.UserName),
                    new XElement("PASSWORD", _sms.UserPassword),
                    new XElement("ORG", _sms.Originator),
                    new XElement("TEXT", new XText(request.Plaintext ?? "")),
                    new XElement("MOBILE", phone)
                );

                var xmlString = xml.ToString(SaveOptions.DisableFormatting); 
                _logger.LogInformation("Sending SMS XML to {Phone}: {Xml}", phone, xmlString);

                var content = new StringContent(xmlString, Encoding.UTF8, "text/xml");

                try
                {
                    var response = await _httpClient.PostAsync(_sms.ApiUrl, content);
                    var respText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("SMS sent to {Phone}. Response: {Response}", phone, respText);
                    }
                    else
                    {
                        _logger.LogError("SMS failed to {Phone}. Status: {StatusCode}, Response: {Response}", phone, response.StatusCode, respText);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SMS to {Phone}", phone);
                }
            }
        }
    }
}
