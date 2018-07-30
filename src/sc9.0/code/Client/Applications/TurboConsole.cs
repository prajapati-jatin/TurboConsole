using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Security;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TurboConsole.Client.Controls;
using TurboConsole.Core.Hosts;
using TurboConsole.Core.Settings;

namespace TurboConsole.Client.Applications
{
    public class TurboConsole : Sitecore.Web.UI.Sheer.BaseForm
    {
        public const string DefaultSessionName = "IDE_Session";

        protected Sitecore.Web.UI.HtmlControls.Memo Editor;

        protected Sitecore.Web.UI.HtmlControls.Border RibbonPanel;

        protected Sitecore.Web.UI.HtmlControls.Border Terminal;

        public ConsoleJobMonitor Monitor { get; private set; }

        public Boolean MonitorActive
        {
            get
            {
                return this.Monitor.Active;
            }
            set
            {
                this.Monitor.Active = value;
            }
        }

        public bool ScriptRunning
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ScriptRunning"]) == "1"; }
            set { Context.ClientPage.ServerProperties["ScriptRunning"] = value ? "1" : string.Empty; }
        }

        public bool ScriptModified
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ScriptModified"]) == "1"; }
            set { Context.ClientPage.ServerProperties["ScriptModified"] = value ? "1" : string.Empty; }
        }

        public static string CurrentSessionId
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["CurrentSessionId"]); }
            set { Context.ClientPage.ServerProperties["CurrentSessionId"] = value; }
        }

        public static string ContextWebsite
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["ContextWebsite"]); }
            set { Context.ClientPage.ServerProperties["ContextWebsite"] = value; }
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!SecurityHelper.CanRunApplication("Turbo Console/Turbo Console"))
                return;
            base.OnLoad(e);

            ContextWebsite = "website";    //TODO: Implement dynamic assignment
            if (this.Monitor.IsNull())
            {
                if (!Context.ClientPage.IsEvent)
                {
                    Monitor = new ConsoleJobMonitor { ID = "Monitor" };
                    Context.ClientPage.Controls.Add(Monitor);
                }
                else
                {
                    Monitor = (ConsoleJobMonitor)Context.ClientPage.FindControl("Monitor");
                }
            }
            Monitor.JobFinished += MonitorOnJobFinished;
            if (Sitecore.Context.ClientPage.IsEvent)
                return;

            var settings = ApplicationSettings.GetInstance(ApplicationNames.Console);

            if (settings.SaveLastScript)
            {
                Editor.Value = settings.LastScript;
            }

            CurrentSessionId = DefaultSessionName;
            ContextWebsite = "website";    //TODO: Implement dynamic assignment
            UpdateRibbon();
        }

        private void MonitorOnJobFinished(object sender, EventArgs e)
        {
            var args = e as SessionCompletedEventArgs;
            var result = args?.RunnerOutput;
            if (!result.IsNull() && result.Exception.IsNull())
            {
                SheerResponse.SetInnerHtml("ScriptResult", result.Output);
            }
            if (result?.Exception != null)
            {
                //TODO: Implement get exection string through script session
                var error = ScriptSession.GetExceptionString(result.Exception);
                SheerResponse.SetInnerHtml("ScriptResult", error);
            }
            ScriptRunning = false;
            UpdateRibbon();

        }

        [HandleMessage("tconsole:build", true)]
        protected virtual void Build(ClientPipelineArgs args)
        {
            args.Parameters.Add("message", "tconsole:build");
            this.BuildAndRun(args, this.Editor.Value, true);
        }

        [HandleMessage("tconsole:run", true)]
        protected virtual void Run(ClientPipelineArgs args)
        {
            args.Parameters.Add("message", "tconsole:run");
            this.BuildAndRun(args, this.Editor.Value, false);
        }

        protected virtual void BuildAndRun(ClientPipelineArgs args, String codeToExecute, Boolean onlyBuild = false)
        {
            this.ScriptRunning = true;
            this.UpdateRibbon();
            var session = GetSession(onlyBuild);
            var runner = new ScriptRunner(BuildCode, session, codeToExecute);

            Context.ClientPage.ClientResponse.SetInnerHtml("ScriptResult", "Build started...");

            Monitor.Start($"Turbo Console", "TCONSOLE", runner.Run);

            var settings = ApplicationSettings.GetInstance(ApplicationNames.Console);
            if (settings.SaveLastScript)
            {
                settings.Load();
                settings.LastScript = Editor.Value;
                settings.Save();
            }
        }

        private ScriptSession GetSession(Boolean onlyBuild)
        {
            var session = ScriptSessionManager.GetSession(CurrentSessionId, ApplicationNames.Console, true);
            if (session.IsNull())
            {
                session = ScriptSessionManager.NewSession(ApplicationNames.Console, true);
            }
            CurrentSessionId = session.Key;
            //var session = new ScriptSession("IDE", false, HttpContext.Current.Request.Url.Host, onlyBuild);
            session.Initialize();
            return session;
        }

        protected void BuildCode(ScriptSession session, String script)
        {
            session.ExecuteCode(script);
        }

        [HandleMessage("tconsoletaskmonitor:check", true)]
        protected void PrintOutput(ClientPipelineArgs args)
        {
            if (this.Monitor.Active)
            {
                //SheerResponse.Eval($"console.appendOutput(\"Still working\");");
            }
        }

        [HandleMessage("tconsole:settings", true)]
        protected virtual void Settings(ClientPipelineArgs args)
        {
            Context.ClientPage.ClientResponse.Alert("Functionality not implemented yet");
        }

        private void UpdateRibbon()
        {
            var ribbon = new Ribbon() { ID = "TurboConsoleRibbon" };
            ribbon.CommandContext = new Sitecore.Shell.Framework.Commands.CommandContext();
            ribbon.CommandContext.Parameters["ScriptRunning"] = ScriptRunning ? "1" : "0";
            ribbon.CommandContext.Parameters["ScriptModified"] = ScriptModified.ToString();
            ribbon.CommandContext.Parameters["currentSessionId"] = CurrentSessionId ?? String.Empty;

            var ribbonItem = Sitecore.Context.Database.GetItem("/sitecore/content/Applications/Turbo Console/Turbo Console/Ribbon");
            Error.AssertItemFound(ribbonItem, "/sitecore/content/Applications/Turbo Console/Turbo Console/Ribbon");
            ribbon.CommandContext.RibbonSourceUri = ribbonItem.Uri;
            RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);
        }
    }
}