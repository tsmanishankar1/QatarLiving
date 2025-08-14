using Microsoft.Extensions.Options;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Notification.MS.Dto;
using QLN.Notification.MS.IService.INotificationService;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;

namespace QLN.Notification.MS.Service
{
    public class InternalNotificationService : INotificationService
    {
        private readonly ILogger<InternalNotificationService> _logger;
        private readonly SmtpSettings _smtp;
        private readonly SmsSettings _sms;
        private readonly HttpClient _httpClient;
        private readonly QLNotificationContext _dbContext;

        public InternalNotificationService(
            ILogger<InternalNotificationService> logger,
            IOptions<SmtpSettings> smtpOptions,
            IOptions<SmsSettings> smsOptions,
            IHttpClientFactory clientFactory,
            QLNotificationContext dbContext) 
        {
            _logger = logger;
            _smtp = smtpOptions.Value;
            _sms = smsOptions.Value;
            _httpClient = clientFactory.CreateClient();
            _dbContext = dbContext; 
        }

        private async Task SaveNotificationToDb(NotificationDto request)
        {
            var entity = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                Destinations = request.Destinations ?? new List<string>(),
                Sender = request.Sender,
                Recipients = request.Recipients ?? new List<RecipientDto>(),
                Subject = request.Subject ?? string.Empty,
                Plaintext = request.Plaintext ?? string.Empty,
                Html = request.Html,
                Attachments = request.Attachments
            };

            _dbContext.Notifications.Add(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SendMail(NotificationDto request)
        {
            await SaveNotificationToDb(request);

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
                    default:
                        _logger.LogWarning("Unknown destination type: {Destination}", dest);
                        break;
                }
            }
        }

        private async Task SendEmail(NotificationDto request)
        {
            var recipients = request.Recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)).ToList();
            if (!recipients.Any())
            {
                _logger.LogWarning("Skipping email - No recipient email available.");
                return;
            }

            using var smtpClient = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.UseSsL,
                UseDefaultCredentials = _smtp.UseDefaultCredentials,
                Credentials = new System.Net.NetworkCredential(_smtp.Username, _smtp.Password)
            };

            var senderEmail = !string.IsNullOrWhiteSpace(request.Sender?.Email)
                ? request.Sender.Email.Trim()
                : _smtp.Email;
            var senderName = !string.IsNullOrWhiteSpace(request.Sender?.Name)
                ? request.Sender.Name.Trim()
                : _smtp.DisplayName;

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogError("Skipping email - No valid sender email available.");
                return;
            }

            if (!IsValidEmail(senderEmail))
            {
                _logger.LogError("Skipping email - Invalid sender email format: {SenderEmail}", senderEmail);
                return;
            }

            foreach (var recipient in recipients)
            {
                var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
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

                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var attachment in request.Attachments)
                    {
                        try
                        {
                            byte[] fileBytes;
                            if (Uri.IsWellFormedUriString(attachment.Content, UriKind.Absolute))
                            {
                                fileBytes = await _httpClient.GetByteArrayAsync(attachment.Content);
                            }
                            else
                            {
                                fileBytes = Convert.FromBase64String(attachment.Content);
                            }
                            var stream = new MemoryStream(fileBytes);
                            var mailAttachment = new Attachment(stream, attachment.Filename, attachment.ContentType);
                            mail.Attachments.Add(mailAttachment);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to attach file {Filename}", attachment.Filename);
                        }
                    }
                }

                try
                {
                    await smtpClient.SendMailAsync(mail);
                    _logger.LogInformation("Email sent to {Email} from {SenderEmail}", recipient.Email, senderEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email} from {SenderEmail}", recipient.Email, senderEmail);
                }
            }
        }
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        private async Task SendSms(NotificationDto request)
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
