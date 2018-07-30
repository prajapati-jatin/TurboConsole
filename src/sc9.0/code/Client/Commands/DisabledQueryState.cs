using Sitecore.Shell.Framework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TurboConsole.Client.Commands
{
    public class DisabledQueryState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return CommandState.Disabled;
        }

        public override void Execute(CommandContext context)
        {
            //TODO: Nothing
        }
    }
}