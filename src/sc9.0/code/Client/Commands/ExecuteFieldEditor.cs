using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.WebEdit;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace TurboConsole.Client.Commands
{
    [Serializable]
    public class ExecuteFieldEditor : Command
    {
        protected const String FieldName = "Fields";
        protected const String Header = "Header";
        protected const String Icon = "Icon";
        protected const String UriParameter = "uri";
        protected const String ButtonParameter = "button";
        protected const String PathParameter = "path";
        protected const String SaveChangesParameter = "savechanges";
        protected const String ReloadAfterParameter = "reloadafter";
        protected const String PreserveSectionsParamter = "preservesections";
        protected const String CurrentItemIsNull = "Current item is null";
        protected const String SettingsItemIsNull = "Settings item is null";
        protected const String RequireTemplateParamter = "requiretemplate";

        protected ItemUri CurrentItemUri { get; set; }

        protected ItemUri SettingsItemUri { get; set; }

        protected Item CurrentItem
        {
            get
            {
                return CurrentItemUri == null ? null : Database.GetItem(CurrentItemUri);
            }
            set
            {
                CurrentItemUri = value?.Uri;
            }
        }

        protected Item SettingsItem
        {
            get
            {
                return Database.GetItem(SettingsItemUri);
            }
            set
            {
                SettingsItemUri = value.Uri;
            }
        }

        public override CommandState QueryState(CommandContext context)
        {
            var requiredTemplate = context.Parameters[RequireTemplateParamter];
            if (!String.IsNullOrEmpty(requiredTemplate))
            {
                if(context.Items.Length != 1)
                {
                    return CommandState.Disabled;
                }

                var template = TemplateManager.GetTemplate(context.Items[0]);
                var result = template.InheritsFrom(requiredTemplate) ? base.QueryState(context) : CommandState.Disabled;
                return result;
            }

            return context.Items.Length != 1 || context.Parameters["ScriptRunning"] == "1" ? CommandState.Disabled : CommandState.Enabled;
        }

        protected virtual PageEditFieldEditorOptions GetOptions(ClientPipelineArgs args, NameValueCollection form)
        {
            EnsureContext(args);
        }

        protected virtual void EnsureContext(ClientPipelineArgs args)
        {
            var path = args.Parameters[PathParameter];
            var currentItem = Database.GetItem(ItemUri.Parse(args.Parameters[UriParameter]));

            currentItem = String.IsNullOrEmpty(path) ? currentItem : Sitecore.Client.ContentDatabase.GetItem(path);
            Assert.IsNotNull(currentItem, CurrentItemIsNull);
            CurrentItem = currentItem;
            var settingsItem = Sitecore.Client.CoreDatabase.GetItem(args.Parameters[ButtonParameter]);
            Assert.IsNotNull(settingsItem, SettingsItemIsNull);
            SettingsItem = settingsItem;
        }

        public override void Execute(CommandContext context)
        {
            throw new NotImplementedException();
        }
    }
}