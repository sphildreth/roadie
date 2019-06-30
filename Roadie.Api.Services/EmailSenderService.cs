using Microsoft.AspNetCore.Identity.UI.Services;
using Roadie.Library.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public class EmailSenderService : IEmailSender
    {
        protected IRoadieSettings Configuration { get; }

        public EmailSenderService(IRoadieSettings configuration)
        {
            Configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using (var mail = new MailMessage(Configuration.SmtpFromAddress, email))
            {
                using (var client = new SmtpClient())
                {
                    client.Port = Configuration.SmtpPort;
                    client.EnableSsl = Configuration.SmtpUseSSl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(Configuration.SmtpUsername, Configuration.SmtpPassword);
                    client.Host = Configuration.SmtpHost;
                    mail.Subject = subject;
                    mail.IsBodyHtml = true;
                    mail.Body = htmlMessage;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    await client.SendMailAsync(mail);
                }
            }
        }
    }
}