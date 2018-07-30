using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TurboConsole.Client.Commands
{
    [Serializable]
    public class EditConsoleSettingsDropdown : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["ScriptRunning"] != "1" ? CommandState.Enabled : CommandState.Disabled;
        }

        public override void Execute(CommandContext context)
        {
            SheerResponse.DisableOutput();
            var subMenu = new Sitecore.Web.UI.HtmlControls.ContextMenu();
            var menuItems = new List<Control>();
            var menuItemId = "consoleSettingsPopup";

            if (String.IsNullOrEmpty(menuItemId))
            {
                var parameters = new UrlString("?" + Context.Items["SC_FORM"]);
                menuItemId = parameters.Parameters["__EVENTTARGET"];
            }

            var menuRootItem = Factory.GetDatabase("core").GetItem("/sitecore/content/Applications/Turbo Console/Turbo Console/Menus/Settings");
            GetMenuItems(menuItems, menuRootItem);

            foreach(MenuItem item in menuItems)
            {
                var subItem = subMenu.Add(item.ID, item.Header, item.Icon, item.Hotkey, item.Click, item.Checked, item.Radiogroup, item.Type);
                subItem.Disabled = item.Disabled;
            }
            SheerResponse.EnableOutput();
            subMenu.Visible = true;
            SheerResponse.ShowContextMenu(menuItemId, "down", subMenu);
        }

        private static void GetMenuItems(ICollection<Control> menuItems, Item parent)
        {
            if (parent == null) return;
            foreach(Item menuDataItem in parent.Children)
            {
                var menuItem = new MenuItem
                {
                    Header = menuDataItem.DisplayName,
                    Icon = menuDataItem.Appearance.Icon,
                    ID = menuDataItem.ID.ToShortID().ToString(),
                    Click = menuDataItem["Message"]
                };
                menuItems.Add(menuItem);
            }
        }
    }
}