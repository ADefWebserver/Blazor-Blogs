using System;
using BlazorBlogs.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BlazorBlogs.Data
{
    public class GeneralSettingsService
    {
        private readonly BlazorBlogsContext _context;

        public GeneralSettingsService(BlazorBlogsContext context)
        {
            _context = context;
        }

        #region public async Task<GeneralSettings> GetGeneralSettingsAsync()
        public async Task<GeneralSettings> GetGeneralSettingsAsync()
        {
            GeneralSettings objGeneralSettings = new GeneralSettings();

            var resuts = await _context.Settings.ToListAsync();

            objGeneralSettings.SMTPServer = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "SMTPServer").SettingValue);
            objGeneralSettings.SMTPAuthendication = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "SMTPAuthendication").SettingValue);
            objGeneralSettings.SMTPSecure = Convert.ToBoolean(resuts.FirstOrDefault(x => x.SettingName == "SMTPSecure").SettingValue);
            objGeneralSettings.SMTPUserName = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "SMTPUserName").SettingValue);
            objGeneralSettings.SMTPPassword = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "SMTPPassword").SettingValue);
            objGeneralSettings.SMTPFromEmail = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "SMTPFromEmail").SettingValue);

            objGeneralSettings.AllowRegistration = Convert.ToBoolean(resuts.FirstOrDefault(x => x.SettingName == "AllowRegistration").SettingValue);
            objGeneralSettings.VerifiedRegistration = Convert.ToBoolean(resuts.FirstOrDefault(x => x.SettingName == "VerifiedRegistration").SettingValue);

            objGeneralSettings.ApplicationName = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "ApplicationName").SettingValue);
            objGeneralSettings.ApplicationLogo = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "ApplicationLogo").SettingValue);
            objGeneralSettings.ApplicationHeader = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "ApplicationHeader").SettingValue);

            objGeneralSettings.DisqusEnabled = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "DisqusEnabled").SettingValue);
            objGeneralSettings.DisqusShortName = Convert.ToString(resuts.FirstOrDefault(x => x.SettingName == "DisqusShortName").SettingValue);
            
            return objGeneralSettings;
        }
        #endregion

        #region UpdateSMTPServer
        public Task<bool> UpdateSMTPServerAsync(string SMTPServer)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "SMTPServer"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(SMTPServer);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateSMTPAuthentication
        public Task<bool> UpdateSMTPAuthenticationAsync(string SMTPAuthendication)
        {

            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "SMTPAuthendication"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(SMTPAuthendication);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateSMTPSecure
        public Task<bool> UpdateSMTPSecureAsync(bool SMTPSecure)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "SMTPSecure"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(SMTPSecure);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateSMTPUserName
        public Task<bool> UpdateSMTPUserNameAsync(string SMTPUserName)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "SMTPUserName"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(SMTPUserName);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateSMTPPassword
        public Task<bool> UpdateSMTPPasswordAsync(string SMTPPassword)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "SMTPPassword"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(SMTPPassword);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateSMTPFromEmail
        public Task<bool> UpdateSMTPFromEmailAsync(string SMTPFromEmail)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "SMTPFromEmail"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(SMTPFromEmail);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion      

        #region UpdateAllowRegistration
        public Task<bool> UpdateAllowRegistrationAsync(bool AllowRegistration)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "AllowRegistration"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(AllowRegistration);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateVerifiedRegistration
        public Task<bool> UpdateVerifiedRegistrationAsync(bool VerifiedRegistration)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "VerifiedRegistration"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(VerifiedRegistration);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateApplicationName
        public Task<bool> UpdateApplicationNameAsync(string ApplicationName)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "ApplicationName"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(ApplicationName);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateApplicationLogo
        public Task<bool> UpdateApplicationLogoAsync(string ApplicationLogo)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "ApplicationLogo"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(ApplicationLogo);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateApplicationHeader
        public Task<bool> UpdateApplicationHeaderAsync(string ApplicationHeader)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "ApplicationHeader"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(ApplicationHeader);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateDisqusEnabled
        public Task<bool> UpdateDisqusEnabledAsync(bool DisqusEnabled)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "DisqusEnabled"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(DisqusEnabled);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateDisqusShortName
        public Task<bool> UpdateDisqusShortNameAsync(string DisqusShortName)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "DisqusShortName"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(DisqusShortName);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion
    }
}
