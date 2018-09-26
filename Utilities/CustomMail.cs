using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Utilities
{
    class CustomMail
    {
        public String ExceptionCollector { get; set; }
        private String ServerNameSTMP { get; set; }
        private int ServerPortSTMP { get; set; }
        private String transmitterAccountID { get; set; }
        private String transmitterAccountPass { get; set; }
        private Boolean ServerUseSSL { get; set; }

        public CustomMail(String SMTPServerName, Int32 SMTPServerPort, String OriginMailAccount, String OriginMailPass, Boolean useSSL)
        {
            ServerNameSTMP = SMTPServerName;
            ServerPortSTMP = SMTPServerPort;
            transmitterAccountID = OriginMailAccount;
            transmitterAccountPass = OriginMailPass;
            ServerUseSSL = useSSL;
        }

        public String sendCustomMail(String subject, String body, String destinationMail)
        {
            ExceptionCollector = String.Empty;
            try
            {
                MailMessage message = new MailMessage();

                message.To.Add(new MailAddress(destinationMail));
                message.From = new MailAddress(transmitterAccountID);
                message.Subject = subject;
                message.Body = body;

                SmtpClient server = new SmtpClient(ServerNameSTMP, ServerPortSTMP);
                server.Credentials = new NetworkCredential(transmitterAccountID, transmitterAccountPass);
                server.EnableSsl = ServerUseSSL;

                ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                };
                server.Send(message);
            }
            catch (System.Net.Mail.SmtpException x)
            {
                Console.WriteLine(String.Format("sendCustomMail > SmtpException [{0}] \n\n ExceptionDescription -> {1}", x.Message, x.StackTrace));
                ExceptionCollector = String.Format("sendCustomMail > SmtpException [{0}] \n\n ExceptionDescription -> {1}", x.Message, x.StackTrace);
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("sendCustomMail > SystemException [{0}] \n\n ExceptionDescription -> {1}", e.Message, e.StackTrace));
                ExceptionCollector = String.Format("sendCustomMail > SystemException [{0}] \n\n ExceptionDescription -> {1}", e.Message, e.StackTrace);
            }

            return ExceptionCollector;
        }
    }
}
