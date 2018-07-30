using Sitecore.Shell.Framework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TurboConsole.Client.Commands
{
    [Serializable]
    public class EditConsoleSettings : Command
    {
        protected const String AppNameParameter = "appName";
        protected const String PersonalParameter = "personal";

        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["ScriptRunning"] != "1" ? CommandState.Enabled : CommandState.Disabled;
        }

        public override void Execute(CommandContext context)
        {
            
        }
    }
}