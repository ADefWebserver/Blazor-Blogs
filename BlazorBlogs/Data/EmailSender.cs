﻿using BlazorBlogs.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace BlazorBlogs.Data
{
    public class EmailSender : IEmailSender<ApplicationUser>
    {
        private readonly BlazorBlogsContext _context;
        private readonly EmailService _EmailService;
        private readonly GeneralSettingsService _GeneralSettingsService;

        public EmailSender(BlazorBlogsContext context, EmailService EmailService, GeneralSettingsService generalSettingsService)
        {
            _context = context;
            _EmailService = EmailService;
            _GeneralSettingsService = generalSettingsService;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            return EmailSendAsync(email, subject, message);
        }

        public async Task EmailSendAsync(string email, string subject, string message)
        {
            var objGeneralSettings = await _GeneralSettingsService.GetGeneralSettingsAsync();

            string strError = await _EmailService.SendMailAsync(
                false,
                email,
                email,
                "", "",
                objGeneralSettings.SMTPFromEmail,
                $"Account Confirmation From: {objGeneralSettings.ApplicationName} {subject}",
                $"This is an account confirmation email from: {objGeneralSettings.ApplicationName}. {message}");

            if (strError != "")
            {
                BlazorBlogs.Data.Models.Logs objLog = new Data.Models.Logs();
                objLog.LogDate = DateTime.Now;
                objLog.LogUserName = email;
                objLog.LogIpaddress = "127.0.0.1";
                objLog.LogAction = $"{Constants.EmailError} - Error: {strError} - To: {email} Subject: Account Confirmation From: {objGeneralSettings.ApplicationName} {subject}";
                _context.Logs.Add(objLog);
                _context.SaveChanges();
            }
        }

        Task IEmailSender<ApplicationUser>.SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            return EmailSendAsync(email, "Account Confirmation", $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a>");
        }

        Task IEmailSender<ApplicationUser>.SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            return EmailSendAsync(email, "Password Reset", $"Please reset your password by clicking this link: <a href='{resetLink}'>link</a>");
        }

        Task IEmailSender<ApplicationUser>.SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            return EmailSendAsync(email, "Password Reset Code", $"Please reset your password by using this code: {resetCode}");
        }
    }
}