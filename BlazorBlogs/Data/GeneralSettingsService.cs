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
            var settings = (await _context.Settings.AsNoTracking().ToListAsync())
                .ToDictionary(x => x.SettingName, x => x.SettingValue ?? "", StringComparer.OrdinalIgnoreCase);

            string Get(string name, string fallback = "") =>
                settings.TryGetValue(name, out var value) ? value : fallback;
            bool GetBoolean(string name, bool fallback = false) =>
                bool.TryParse(Get(name), out var value) ? value : fallback;

            return new GeneralSettings
            {
                SMTPServer = Get("SMTPServer"),
                SMTPAuthendication = Get("SMTPAuthendication"),
                SMTPSecure = GetBoolean("SMTPSecure"),
                SMTPUserName = Get("SMTPUserName"),
                SMTPPassword = Get("SMTPPassword"),
                SMTPFromEmail = Get("SMTPFromEmail"),
                AllowRegistration = GetBoolean("AllowRegistration"),
                VerifiedRegistration = GetBoolean("VerifiedRegistration"),
                ApplicationName = Get("ApplicationName"),
                ApplicationLogo = Get("ApplicationLogo"),
                ApplicationHeader = Get("ApplicationHeader"),
                ApplicationHeaderEnabled = GetBoolean("ApplicationHeaderEnabled", true),
                ApplicationHeaderEditorMode = Get("ApplicationHeaderEditorMode", string.IsNullOrWhiteSpace(Get("ApplicationHeader")) ? "Rich" : "Raw"),
                SiteBaseUrl = Get("SiteBaseUrl"),
                ThemeAccentColor = Get("ThemeAccentColor", ThemeDefaults.AccentColor),
                DisqusEnabled = Get("DisqusEnabled"),
                DisqusShortName = Get("DisqusShortName"),
                GoogleTrackingID = Get("GoogleTrackingID"),
                VersionNumber = Get("VersionNumber")
            };
        }
        #endregion

        public async Task<string> GetSettingAsync(string settingName, string defaultValue = "")
        {
            var value = await _context.Settings
                .AsNoTracking()
                .Where(x => x.SettingName == settingName)
                .Select(x => x.SettingValue)
                .FirstOrDefaultAsync();

            return value ?? defaultValue;
        }

        public async Task SetSettingAsync(string settingName, string value)
        {
            var setting = await _context.Settings
                .FirstOrDefaultAsync(x => x.SettingName == settingName);

            if (setting is null)
            {
                setting = new Settings { SettingName = settingName };
                _context.Settings.Add(setting);
            }

            setting.SettingValue = value ?? "";
            await _context.SaveChangesAsync();
        }

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

        #region UpdateGoogleTrackingID
        public Task<bool> UpdateGoogleTrackingIDAsync(string GoogleTrackingID)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "GoogleTrackingID"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(GoogleTrackingID);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion

        #region UpdateVersionNumber
        public Task<bool> UpdateVersionNumberNameAsync(string VersionNumber)
        {
            var resuts = from Settings in _context.Settings
                         where Settings.SettingName == "VersionNumber"
                         select Settings;

            resuts.FirstOrDefault().SettingValue = Convert.ToString(VersionNumber);
            _context.SaveChanges();

            return Task.FromResult(true);
        }
        #endregion
    }
}
