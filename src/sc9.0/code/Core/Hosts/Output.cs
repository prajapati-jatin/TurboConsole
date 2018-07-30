using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace TurboConsole.Core.Hosts
{
    public class Output
    {
        public Output(OutputLineType type, String value)
        {
            LineType = type;
            Text = value;
        }

        public OutputLineType LineType { get; }

        public string Text { get; }

        public String ToHtmlString()
        {
            return String.Format("<span>{0}</span>\r\n", Text);
        }

        public void GetHtmlLine(StringBuilder output)
        {
            output.AppendFormat("<span>{0}</span>\r\n", this.Text.TrimEnd());
        }
    }
}