using Roadie.Library.Configuration;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Roadie.Library.Utility
{
    public static class EmailHelper
    {
        public static bool SendEmail(IRoadieSettings configuration, string emailAddress, string subject, string body)
        {
            using (MailMessage mail = new MailMessage(configuration.SmtpFromAddress, emailAddress))
            {
                using (SmtpClient client = new SmtpClient())
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
                    ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                    {
                        return true;
                    };
                    client.Send(mail);
                    return true;
                }
            }
        }
    }
}
