using Roadie.Library.Configuration;
using System.Net;
using System.Net.Mail;

namespace Roadie.Library.Utility
{
    public static class EmailHelper
    {
        public static bool SendEmail(IRoadieSettings configuration, string emailAddress, string subject, string body)
        {
            using (var mail = new MailMessage(configuration.SmtpFromAddress, emailAddress))
            {
                using (var client = new SmtpClient())
                {
                    client.Port = configuration.SmtpPort;
                    client.EnableSsl = configuration.SmtpUseSSl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(configuration.SmtpUsername, configuration.SmtpPassword);
                    client.Host = configuration.SmtpHost;
                    mail.Subject = subject;
                    mail.IsBodyHtml = true;
                    mail.Body = body;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    client.Send(mail);
                    return true;
                }
            }
        }
    }
}