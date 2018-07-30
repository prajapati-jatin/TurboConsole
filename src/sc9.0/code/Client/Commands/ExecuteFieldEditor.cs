using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.WebEdit;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
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
        protected const String PreserveSectionsParameter = "preservesections";
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
            var options = new PageEditFieldEditorOptions(form, BuildListWithFieldsToShow())
            {
                Title = SettingsItem[Header],
                Icon = SettingsItem[Icon]
            };
            options.Parameters["contentitem"] = CurrentItemUri.ToString();
            options.PreserveSections = args.Parameters[PreserveSectionsParameter] == "1";
            options.DialogTitle = SettingsItem[Header];
            options.SaveItem = true;
            return options;
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

        private IEnumerable<FieldDescriptor> BuildListWithFieldsToShow()
        {
            var fieldList = new List<FieldDescriptor>();
            var fieldString = new ListString(SettingsItem[FieldName]);
            var currentItem = CurrentItem;
            foreach(var fieldName in fieldString)
            {
                if(fieldName == "*")
                {
                    GetNonStandardFields(fieldList);
                    continue;
                }
                if(fieldName.IndexOf('-') == 0)
                {
                    var field = currentItem.Fields[fieldName.Substring(1, fieldName.Length - 1)];
                    if(field != null)
                    {
                        var fieldId = field.ID;
                        foreach(var fieldDescriptor in fieldList.Where(fieldDescriptor => fieldDescriptor.FieldID == fieldId))
                        {
                            fieldList.Remove(fieldDescriptor);
                            break;
                        }
                    }
                    continue;
                }

                if(currentItem.Fields[fieldName] != null)
                {
                    fieldList.Add(new FieldDescriptor(currentItem, fieldName));
                }
            }

            return fieldList;
        }

        private void GetNonStandardFields(ICollection<FieldDescriptor> fieldList)
        {
            var currentItem = CurrentItem;
            currentItem.Fields.ReadAll();
            foreach(Field field in currentItem.Fields)
            {
                if(field.GetTemplateField().Template.BaseIDs.Length > 0)
                {
                    fieldList.Add(new FieldDescriptor(currentItem, field.Name));
                }
            }
        }

        public virtual Boolean CanExecute(CommandContext context)
        {
            return context.Items.Length > 0;
        }

        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, nameof(context));
            if (!CanExecute(context))
                return;
            Context.ClientPage.Start(this, "StartFieldEditor", new ClientPipelineArgs(context.Parameters)
            {
                Parameters = { { "uri", context.Items[0].Uri.ToString() } }
            });
        }

        protected virtual void StartFieldEditor(ClientPipelineArgs args)
        {
            var current = HttpContext.Current;
            if (current == null)
                return;
            var page = current.Handler as Page;
            if (page == null)
                return;
            var form = page.Request.Form;

            if (!args.IsPostBack)
            {
                SheerResponse.ShowModalDialog(GetOptions(args, form).ToUrlString().ToString(), "720", "520",
                    string.Empty, true);
                args.WaitForPostBack();
            }
            else
            {
                if (!args.HasResult)
                    return;

                var results = PageEditFieldEditorOptions.Parse(args.Result);
                var currentItem = CurrentItem;
                currentItem.Edit(options =>
                {
                    foreach (var field in results.Fields)
                    {
                        currentItem.Fields[field.FieldID].Value = field.Value;
                    }
                });

                PageEditFieldEditorOptions.Parse(args.Result).SetPageEditorFieldValues();
            }
        }
    }
}