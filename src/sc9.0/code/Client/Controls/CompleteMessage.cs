using Sitecore.Jobs.AsyncUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TurboConsole.Client.Controls
{
    public class CompleteMessage : FlushMessage
    {
        public RunnerOutput RunnerOutput { get; set; }
    }
}