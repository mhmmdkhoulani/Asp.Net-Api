using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspCoreDemo.Api.Services
{
   public interface IMailService
    {
        Task<string> SendEmailAsync(string emailTo, string subject, string content);

    }

    public class SendInBlueMailService : IMailService
    {
        private readonly TransactionalEmailsApi _api;

        public SendInBlueMailService()
        {
            _api = new TransactionalEmailsApi();
            Configuration.Default.AddApiKey("api-key","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");
        }
        public async Task<string> SendEmailAsync(string emailTo, string subject, string content)
        {
            SendSmtpEmail mail = new SendSmtpEmail();
            mail.To = new List<SendSmtpEmailTo>();
            mail.To.Add(new SendSmtpEmailTo(emailTo));

            mail.Sender = new SendSmtpEmailSender("Mohammad Khoulani", "mhmmdkhoulani@gmai.com");
            mail.HtmlContent = content;
            mail.Subject = subject;
            try
            {
                var result = await _api.SendTransacEmailAsync(mail);
                return result.MessageId;
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
