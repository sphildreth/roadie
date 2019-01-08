using Mapster;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Models;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class EmailSenderService : IEmailSender
    {
        protected IRoadieSettings Configuration { get;}

        public EmailSenderService(IRoadieSettings configuration)
        {
            this.Configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using (MailMessage mail = new MailMessage(this.Configuration.SmtpFromAddress, email))
            {
                using (SmtpClient client = new SmtpClient())
                {
                    client.Port = this.Configuration.SmtpPort;
                    client.EnableSsl = this.Configuration.SmtpUseSSl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(this.Configuration.SmtpUsername, this.Configuration.SmtpPassword);
                    client.Host = this.Configuration.SmtpHost;
                    mail.Subject = subject;
                    mail.IsBodyHtml = true;
                    mail.Body = htmlMessage;
                    ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                    {
                        return true;
                    };
                    await client.SendMailAsync(mail);
                }
            }
        }
    }
}
