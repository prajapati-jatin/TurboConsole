using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TurboConsole.Client.Controls
{
    public class RunnerOutput
    {
        public string Output { get; set; }
        public Exception Exception { get; set; }
        public bool HasErrors { get; set; }
        public bool CloseRunner { get; set; }
        public List<string> CloseMessages { get; set; }
    }
}