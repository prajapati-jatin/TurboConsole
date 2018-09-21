using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using TurboConsole.Diagnostics;

namespace TurboConsole.Core.Settings
{
    /// <summary>
    /// TODO: Implement application settings
    /// </summary>
    public class ApplicationSettings
    {
        public const String IDESettingsItemAllUsers = "All Users";

        public const String SettingsItemPath = "/sitecore/system/Modules/Turbo Console/Settings/";

        private const string FolderIcon = "Office/32x32/folder.png";

        private static readonly Dictionary<String, ApplicationSettings> instances = new Dictionary<string, ApplicationSettings>();

        private static readonly Regex validNameRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        private const String LastScriptSettingFieldName = "LastScript";
        private const String SaveLastScriptSettingFieldName = "SaveLastScript";

        private ApplicationSettings(String applicationName, Boolean personalizedSettings)
        {
            ApplicationName = applicationName;
            IsPersonalized = personalizedSettings;
        }

        public static String CurrentDomain => validNameRegex.Replace(User.Current.Domain.Name, "_");

        public static String CurrentUserName => validNameRegex.Replace(User.Current.LocalName, "_");

        public string ApplicationName { get; }

        public bool IsPersonalized { get; }

        public String LastScript { get; set; }

        public Boolean SaveLastScript { get; set; }

        public bool Loaded { get; private set; }

        private String AppSettingsPath => SettingsItemPath + ApplicationName + "/";

        private String CurrentUserSettingsPath => AppSettingsPath + CurrentDomain + "/" + CurrentUserName;

        private String AllUsersSettingsPath => AppSettingsPath + IDESettingsItemAllUsers;

        private static String settingsDb;
        public static String SettingsDb
        {
            get
            {
                GetDatabaseName(ref settingsDb, "turboconsole/workingDatabase/settings");
                return settingsDb;
            }
        }

        private static void GetDatabaseName(ref string databaseName, string settingPath)
        {
            if (String.IsNullOrEmpty(databaseName))
            {
                databaseName = Factory.GetString(settingPath, false);
                if (String.IsNullOrEmpty(databaseName))
                {
                    databaseName = "master";
                }
            }
        }

        public static ApplicationSettings GetInstance(String applicationName, Boolean personalizedSettings = true)
        {
            var settingsPath = GetSettingsName(applicationName, personalizedSettings);
            ApplicationSettings instance = null;
            lock (instances)
            {
                if (instances.ContainsKey(settingsPath))
                {
                    instance = instances[settingsPath];
                }
                if (instance.IsNull() || !instance.Loaded)
                {
                    instance = new ApplicationSettings(applicationName, personalizedSettings);
                    instance.Load();
                    instances.Add(settingsPath, instance);
                }
            }
            return instance;
        }

        internal void Load()
        {
            var configuration = GetSettingsConfiguration();
            if (!configuration.IsNull())
            {
                try
                {
                    LastScript = TryGetSettingValue(LastScriptSettingFieldName, DefaultCode, () => HttpUtility.HtmlDecode(configuration[LastScriptSettingFieldName]));
                    SaveLastScript = TryGetSettingValue(SaveLastScriptSettingFieldName, true, () => ((CheckboxField)configuration.Fields[SaveLastScriptSettingFieldName]).Checked);
                    Loaded = true;
                }
                catch (Exception ex)
                {
                    SetToDefault();
                }
            }
            else
            {
                SetToDefault();
            }
        }

        private static String DefaultCode = @"using System;
using Sitecore;
namespace Learn{
    public class SC{
        public String Output(){
            return ""I am Turbo Console!!!"";
        }
}
}";

        private void SetToDefault()
        {
            LastScript = string.Empty;
            SaveLastScript = true;
            Loaded = true;
        }

        private T TryGetSettingValue<T>(string fieldName, T defaultValue, Func<T> action)
        {
            try
            {
                var result = action();
                if(result != null && !String.IsNullOrEmpty(result.ToString()))
                    return result;
                return defaultValue;
            }
            catch (Exception ex)
            {
                TurboConsoleLog.Error($"Error while restoring setting {fieldName}.", ex);
                return defaultValue;
            }
        }

        private Item GetSettingsConfiguration()
        {
            var db = Sitecore.Configuration.Factory.GetDatabase(SettingsDb);
            if (IsPersonalized)
            {
                return db?.GetItem(CurrentUserSettingsPath) ?? db?.GetItem(AllUsersSettingsPath);
            }
            return db?.GetItem(AllUsersSettingsPath);
        }

        internal void Save()
        {
            var configuration = GetSettingsConfigurationForSave();
            if (!configuration.IsNull())
            {
                using (new SecurityDisabler())
                {
                    configuration.Edit(p =>
                    {
                        configuration[LastScriptSettingFieldName] = HttpUtility.HtmlEncode(LastScript);
                        ((CheckboxField)configuration.Fields[SaveLastScriptSettingFieldName]).Checked = SaveLastScript;
                        if (IsPersonalized)
                        {
                            configuration.Fields[Sitecore.FieldIDs.DisplayName].Reset();
                        }
                    });
                }
            }
        }

        private Item GetSettingsConfigurationForSave()
        {
            var db = Sitecore.Configuration.Factory.GetDatabase(SettingsDb);
            var appSettingsPath = AppSettingsPath;
            using (new SecurityDisabler())
            {
                var currentUserItem = db.GetItem(CurrentUserSettingsPath);
                if (currentUserItem.IsNull())
                {
                    var settingsRootItem = db.GetItem(appSettingsPath);
                    if (settingsRootItem.IsNull())
                    {
                        return null;
                    }
                    var folderTemplateItem = db.GetItem(TemplateIDs.Folder);
                    var currentDomainItem = db.CreateItemPath(appSettingsPath + CurrentDomain, folderTemplateItem, folderTemplateItem);
                    currentDomainItem.Edit(args => currentDomainItem.Appearance.Icon = FolderIcon);
                    var defaultItem = db.GetItem(appSettingsPath + IDESettingsItemAllUsers);
                    currentUserItem = defaultItem.CopyTo(currentDomainItem, CurrentUserName);
                }
                return currentUserItem;
            }
        }

        public static String GetSettingsPath(String applicationName, Boolean personalizedSettings)
        {
            return SettingsItemPath + GetSettingsName(applicationName, personalizedSettings);
        }

        private static String GetSettingsName(String applicationName, Boolean personalizedSettings)
        {
            return String.Format("{0}/{1}", applicationName, personalizedSettings ? $"{CurrentDomain}/{CurrentUserName}" : "All Users");
        }

        public static void ReloadInstance(String applicationName, Boolean personalizedSettings)
        {
            var settingsPath = GetSettingsName(applicationName, personalizedSettings);
            lock (instances)
            {
                if (instances.ContainsKey(settingsPath))
                {
                    instances.Remove(settingsPath);
                }
            }
        }
    }
}