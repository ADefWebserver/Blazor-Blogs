using BlazorBlogs.Classes;
using BlazorBlogs.Data.Models;
using BlazorBlogs.Models;
using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBlogs.Data
{
    public class EmailService
    {
        private readonly BlazorBlogsContext _context;
        private readonly GeneralSettingsService _generalSettingsService;

        public EmailService(BlazorBlogsContext context, GeneralSettingsService generalSettingsService)
        {
            _context = context;
            _generalSettingsService = generalSettingsService;
        }

        #region public async Task<string> SendMailAsync(bool SendAsync, string MailTo, string MailToDisplayName, string Cc, string Bcc, string ReplyTo, string Subject, string Body)
        public async Task<string> SendMailAsync(bool SendAsync, string MailTo, string MailToDisplayName, string Cc, string Bcc, string ReplyTo, string Subject, string Body)
        {
            GeneralSettings GeneralSettings = await _generalSettingsService.GetGeneralSettingsAsync();

            if (GeneralSettings.SMTPServer.Trim().Length == 0)
            {
                return "Error: Cannot send email - SMTPServer not set";
            }

            string[] arrAttachments = new string[0];

            return await SendMailAsync(SendAsync,
                GeneralSettings.SMTPFromEmail, MailTo, MailToDisplayName,
                Cc, Bcc, ReplyTo, System.Net.Mail.MailPriority.Normal,
                Subject, Encoding.UTF8, Body, arrAttachments, "", "", "", "",
                GeneralSettings.SMTPSecure);
        }
        #endregion

        #region private async Task<string> SendMailAsync(bool SendAsync, string MailFrom, string MailTo, string MailToDisplayName, string Cc, string Bcc, string ReplyTo, System.Net.Mail.MailPriority Priority, string Subject, Encoding BodyEncoding, string Body, string[] Attachment, string SMTPServer, string SMTPAuthentication, string SMTPUsername, string SMTPPassword, bool SMTPEnableSSL)
        private async Task<string> SendMailAsync(bool SendAsync, string MailFrom, string MailTo, string MailToDisplayName, string Cc, string Bcc, string ReplyTo, System.Net.Mail.MailPriority Priority,
            string Subject, Encoding BodyEncoding, string Body, string[] Attachment, string SMTPServer, string SMTPAuthentication, string SMTPUsername, string SMTPPassword, bool SMTPEnableSSL)
        {
            string strSendMail = "";
            GeneralSettings GeneralSettings = await _generalSettingsService.GetGeneralSettingsAsync();

            // SMTP server configuration
            if (SMTPServer == "")
            {
                SMTPServer = GeneralSettings.SMTPServer;

                if (SMTPServer.Trim().Length == 0)
                {
                    return "Error: Cannot send email - SMTPServer not set";
                }
            }

            if (SMTPAuthentication == "")
            {
                SMTPAuthentication = GeneralSettings.SMTPAuthendication;
            }

            if (SMTPUsername == "")
            {
                SMTPUsername = GeneralSettings.SMTPUserName;
            }

            if (SMTPPassword == "")
            {
                SMTPPassword = GeneralSettings.SMTPPassword;
            }

            MailTo = MailTo.Replace(";", ",");
            Cc = Cc.Replace(";", ",");
            Bcc = Bcc.Replace(";", ",");

            System.Net.Mail.MailMessage objMail = null;
            try
            {
                System.Net.Mail.MailAddress SenderMailAddress = new System.Net.Mail.MailAddress(MailFrom, MailFrom);
                System.Net.Mail.MailAddress RecipientMailAddress = new System.Net.Mail.MailAddress(MailTo, MailToDisplayName);

                objMail = new System.Net.Mail.MailMessage(SenderMailAddress, RecipientMailAddress);

                if (Cc != "")
                {
                    objMail.CC.Add(Cc);
                }
                if (Bcc != "")
                {
                    objMail.Bcc.Add(Bcc);
                }

                if (ReplyTo != string.Empty)
                {
                    objMail.ReplyToList.Add(new System.Net.Mail.MailAddress(ReplyTo));
                }

                objMail.Priority = (System.Net.Mail.MailPriority)Priority;
                objMail.IsBodyHtml = IsHTMLMail(Body);

                foreach (string myAtt in Attachment)
                {
                    if (myAtt != "") objMail.Attachments.Add(new System.Net.Mail.Attachment(myAtt));
                }

                // message
                objMail.SubjectEncoding = BodyEncoding;
                objMail.Subject = Subject.Trim();
                objMail.BodyEncoding = BodyEncoding;

                System.Net.Mail.AlternateView PlainView =
                    System.Net.Mail.AlternateView.CreateAlternateViewFromString(Utility.ConvertToText(Body),
                    null, "text/plain");

                objMail.AlternateViews.Add(PlainView);

                //if body contains html, add html part
                if (IsHTMLMail(Body))
                {
                    System.Net.Mail.AlternateView HTMLView =
                        System.Net.Mail.AlternateView.CreateAlternateViewFromString(Body, null, "text/html");

                    objMail.AlternateViews.Add(HTMLView);
                }
            }
            catch (Exception objException)
            {
                // Problem creating Mail Object
                strSendMail = MailTo + ": " + objException.Message;

                // Log Error 
                BlazorBlogs.Data.Models.Logs objLog = new Data.Models.Logs();
                objLog.LogDate = DateTime.Now;
                objLog.LogAction = $"{Constants.EmailError} - Error: {strSendMail}";
                objLog.LogUserName = null;
                objLog.LogIpaddress = "127.0.0.1";
            }

            if (objMail != null)
            {
                // external SMTP server alternate port
                int? SmtpPort = null;
                int portPos = SMTPServer.IndexOf(":");
                if (portPos > -1)
                {
                    SmtpPort = Int32.Parse(SMTPServer.Substring(portPos + 1, SMTPServer.Length - portPos - 1));
                    SMTPServer = SMTPServer.Substring(0, portPos);
                }

                System.Net.Mail.SmtpClient smtpClient = new System.Net.Mail.SmtpClient();

                if (SMTPServer != "")
                {
                    smtpClient.Host = SMTPServer;
                    smtpClient.Port = (SmtpPort == null) ? (int)25 : (Convert.ToInt32(SmtpPort));

                    switch (SMTPAuthentication)
                    {
                        case "":
                        case "0":
                            // anonymous
                            break;
                        case "1":
                            // basic
                            if (SMTPUsername != "" & SMTPPassword != "")
                            {
                                smtpClient.UseDefaultCredentials = false;
                                smtpClient.Credentials = new System.Net.NetworkCredential(SMTPUsername, SMTPPassword);
                            }

                            break;
                        case "2":
                            // NTLM
                            smtpClient.UseDefaultCredentials = true;
                            break;
                    }
                }
                smtpClient.EnableSsl = SMTPEnableSSL;

                try
                {
                    if (SendAsync) // Send Email using SendAsync
                    {
                        // Set the method that is called back when the send operation ends.
                        smtpClient.SendCompleted += SmtpClient_SendCompleted;

                        // Send the email
                        MailMessage objMailMessage = new MailMessage();
                        objMailMessage = objMail;

                        smtpClient.SendAsync(objMail, objMailMessage);
                        strSendMail = "";
                    }
                    else // Send email and wait for response
                    {
                        smtpClient.Send(objMail);
                        strSendMail = "";

                        // Log the Email
                        LogEmail(objMail);

                        objMail.Dispose();
                        smtpClient.Dispose();
                    }
                }
                catch (Exception objException)
                {
                    // mail configuration problem
                    if (!(objException.InnerException == null))
                    {
                        strSendMail = string.Concat(objException.Message, Environment.NewLine, objException.InnerException.Message);
                    }
                    else
                    {
                        strSendMail = objException.Message;
                    }

                    // Log Error 
                    BlazorBlogs.Data.Models.Logs objLog = new Data.Models.Logs();
                    objLog.LogDate = DateTime.Now;
                    objLog.LogAction = $"{Constants.EmailError} - Error: {strSendMail}";
                    objLog.LogUserName = null;
                    objLog.LogIpaddress = "127.0.0.1";

                    _context.Logs.Add(objLog);
                    _context.SaveChanges();
                }
            }

            return strSendMail;
        }
        #endregion

        #region private void SmtpClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        private void SmtpClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            // Get the MailMessage object 
            MailMessage objMailMessage = (MailMessage)e.UserState;
            System.Net.Mail.SmtpClient objSmtpClient = (System.Net.Mail.SmtpClient)sender;

            if (e.Error != null)
            {
                BlazorBlogs.Data.Models.Logs objLog = new Data.Models.Logs();
                objLog.LogDate = DateTime.Now;
                objLog.LogAction = $"{Constants.EmailError} - Error: {e.Error.GetBaseException().Message} - To: {objMailMessage.To} Subject: {objMailMessage.Subject}";
                objLog.LogUserName = null;
                objLog.LogIpaddress = "127.0.0.1";

                _context.Logs.Add(objLog);
                _context.SaveChanges();
            }
            else
            {
                // Log the Email
                LogEmail(objMailMessage);

                objMailMessage.Dispose();
                objSmtpClient.Dispose();
            }
        }
        #endregion

        #region IsHTMLMail
        public static bool IsHTMLMail(string Body)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(Body, "<[^>]*>");
        }
        #endregion

        #region private void LogEmail(System.Net.Mail.MailMessage objMailMessage)
        private void LogEmail(System.Net.Mail.MailMessage objMailMessage)
        {
            // Loop through all recipients
            foreach (var item in objMailMessage.To)
            {
                BlazorBlogs.Data.Models.Logs objLog = new Data.Models.Logs();
                objLog.LogDate = DateTime.Now;
                objLog.LogAction = $"{Constants.EmailSent} - To: {item.DisplayName} ({item.Address}) Subject: {objMailMessage.Subject}";
                objLog.LogUserName = null;
                objLog.LogIpaddress = "127.0.0.1";

                _context.Logs.Add(objLog);
                _context.SaveChanges();
            }
        }
        #endregion
    }
}
