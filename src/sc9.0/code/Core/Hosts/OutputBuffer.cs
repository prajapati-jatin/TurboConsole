using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace TurboConsole.Core.Hosts
{
    public class OutputBuffer : List<Output>
    {
        private Int32 updatePointer = 0;

        public Boolean HasErrors => this.Any(o => o.LineType == OutputLineType.Error);

        public Boolean SilenceOutput { get; set; }

        public OutputBuffer SilencedOutput { get; set; }

        public String ToHtml()
        {
            var output = new StringBuilder(10240);
            foreach (var outputLine in this)
            {
                outputLine.GetHtmlLine(output);
            }
            return output.ToString();
        }

        public new void Clear()
        {
            updatePointer = 0;
            base.Clear();
        }

    }
}