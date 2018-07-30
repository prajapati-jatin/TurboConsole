using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TurboConsole.Client.Controls
{
    public class SessionCompletedEventArgs : EventArgs
    {
        public RunnerOutput RunnerOutput { get; set; }
    }
}