using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using System.ComponentModel;

namespace MULTITUDE.Class.Facility
{
    public enum MessageType
    {
        UserMessage,
        SupportAndSuggestion,
        ExceptionReport
    }
    static class NetworkHelper
    {
        #region Email Service
        private static MailAddress fromAddress = new MailAddress("mailagent@totalimagine.com", "TI Mail Agent");
        private static MailAddress toAddress = new MailAddress("contact@totalimagine.com", "User Service");
        private const string fromPassword = "123!Qw23";
        static bool bSendingMessage = false;
        private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
            object token = (object)e.UserState;

            if (e.Cancelled)
            {
                Console.WriteLine("[{0}] Send canceled.", token);
            }
            if (e.Error != null)
            {
                Console.WriteLine("[{0}] {1}", token, e.Error.ToString());
            }
            else
            {
                Console.WriteLine("Message sent.");
            }
            bSendingMessage = false;
        }

        // Ref: https://stackoverflow.com/questions/32260/sending-email-in-net-through-gmail
        // Ref: https://msdn.microsoft.com/en-us/library/x5x13z6h(v=vs.110).aspx
        /// <summary>
        /// Returns whetheror not current server is available
        /// </summary>
        /// <param name="ExtraSubject"></param>
        /// <param name="Message"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool SendEmailReport(string ExtraSubject, string Message, MessageType type)
        {
            if (bSendingMessage == true) return false;

            string subject = string.IsNullOrWhiteSpace(ExtraSubject) ? "Subject: " : string.Format("[{0}]Subject: ", ExtraSubject);
            switch (type)
            {
                case MessageType.UserMessage:
                    subject += "Message";
                    break;
                case MessageType.SupportAndSuggestion:
                    subject += "Support";
                    break;
                case MessageType.ExceptionReport:
                    subject += "Exception";
                    break;
                default:
                    break;
            }
            string body = Message;

            using (SmtpClient smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            })
            using (MailMessage message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                try { smtp.Send(message); }
                catch (Exception) { }
            }

            return true;
        }

        public static bool SendEmailReportAsync(string ExtraSubject, string Message, MessageType type, SendCompletedEventHandler callback)
        {
            if (bSendingMessage == true) return false;

            string subject = string.IsNullOrWhiteSpace(ExtraSubject) ? "Subject: " : string.Format("[{0}]Subject: ", ExtraSubject);
            switch (type)
            {
                case MessageType.UserMessage:
                    subject += "Message";
                    break;
                case MessageType.SupportAndSuggestion:
                    subject += "Support";
                    break;
                case MessageType.ExceptionReport:
                    subject += "Exception";
                    break;
                default:
                    break;
            }
            string body = Message;

            SmtpClient smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            MailMessage message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            };
            smtp.SendCompleted += (s, e) => {
                SendCompletedCallback(s, e);
                callback(s, e);
                smtp.Dispose();
                message.Dispose();
            };
            try
            {
                smtp.SendAsync(message, null);
            }
            catch (Exception e)
            {
                callback(e, null);
            }

            return true;
        }
        #endregion
    }
}
