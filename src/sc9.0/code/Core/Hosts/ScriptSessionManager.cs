using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using TurboConsole.Core.Settings;
using TurboConsole.Diagnostics;

namespace TurboConsole.Core.Hosts
{
    public static class ScriptSessionManager
    {
        private const String sessionIdPrefix = "$scriptSession$";
        private const String expirationSetting = "TurboConsole..PersistentSessionExpirationMinutes";
        private static readonly HashSet<String> sessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static ScriptSession GetSession(String persistentId, String defaultId)
        {
            var sessionId = persistentId.IfNullOrEmpty(defaultId);
            return GetSession(sessionId);
        }

        public static ScriptSession NewSession(String applicationType, Boolean personalizedSettings) => GetSession(String.Empty, applicationType, personalizedSettings);

        public static ScriptSession GetSession(String persistentId) => GetSession(persistentId, ApplicationNames.Default, false);

        public static ScriptSession GetSession(String persistentId, String applicationType, Boolean personalizedSettings)
        {
            var autoDispose = String.IsNullOrEmpty(persistentId);
            if (autoDispose)
            {
                persistentId = Guid.NewGuid().ToString();
            }
            var sessionKey = GetSessionKey(persistentId);
            lock (sessions)
            {
                if (SessionExists(persistentId))
                {
                    return HttpRuntime.Cache[sessionKey] as ScriptSession;
                }

                var session = new ScriptSession(applicationType, personalizedSettings)
                {
                    ID = persistentId
                };

                TurboConsoleLog.Debug($"New script session with key '{sessionKey}' created.");
                if (autoDispose)
                {
                    //this only should be set if new session has been created - do not change!
                    session.AutoDispose = true;
                }
                var expiration = Sitecore.Configuration.Settings.GetIntSetting(expirationSetting, 30);
                HttpRuntime.Cache.Add(sessionKey, session, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, expiration, 0), CacheItemPriority.Normal, CacheItemRemoved);
                sessions.Add(sessionKey);
                session.ID = persistentId;
                session.Key = sessionKey;
                session.Initialize();
                return session;
            }
        }

        public static Boolean SessionExists(String persistentId)
        {
            var sessionKey = GetSessionKey(persistentId);
            lock (sessions)
            {
                return sessions.Contains(sessionKey) && !HttpRuntime.Cache[sessionKey].IsNull();
            }
        }

        public static void RemoveSession(String key)
        {
            lock (sessions)
            {
                if (sessions.Contains(key))
                {
                    sessions.Remove(key);
                }
                if (HttpRuntime.Cache[key].IsNull()) return;
                var session = HttpRuntime.Cache.Remove(key) as ScriptSession;
                if (!session.IsNull())
                {
                    session.Dispose();
                }
                TurboConsoleLog.Debug($"Script session '{key}' disposed.");
            }
        }

        public static void Clear()
        {
            lock (sessions)
            {
                foreach (var key in sessions)
                {
                    var sessionKey = GetSessionKey(key);
                    var session = HttpRuntime.Cache.Remove(sessionKey) as ScriptSession;
                    session?.Dispose();
                }
                sessions.Clear();
            }
        }

        private static void CacheItemRemoved(string key, object value, CacheItemRemovedReason reason)
        {
            RemoveSession(key);
        }

        private static String GetSessionKey(string persistentId)
        {
            if (persistentId != null && persistentId.StartsWith(sessionIdPrefix))
            {
                return persistentId;
            }
            var key = new StringBuilder();
            key.Append(sessionIdPrefix);
            key.Append("|");
            if (!HttpContext.Current.IsNull() && !HttpContext.Current.Session.IsNull())
            {
                key.Append(HttpContext.Current.Session.SessionID);
                key.Append("|");
            }
            key.Append(persistentId);
            return key.ToString();
        }
    }
}