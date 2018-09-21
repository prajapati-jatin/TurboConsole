using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using TurboConsole.Client.Controls;
using TurboConsole.Core.Hosts;
using TurboConsole.Diagnostics;

namespace TurboConsole.Client.Applications
{
    public class CodeRunner
    {
        public delegate void ScriptRunnerMethod(ScriptSession session, String script);

        public String Script { get; private set; }

        public ScriptSession Session { get; private set; }

        public ScriptRunnerMethod Method { get; private set; }

        public CodeRunner(ScriptRunnerMethod method, ScriptSession session, String script)
        {
            Assert.ArgumentNotNull(script, nameof(script));
            Assert.ArgumentNotNull(method, nameof(method));
            Assert.ArgumentNotNull(session, nameof(session));
            this.Session = session;
            this.Script = script;
            this.Method = method;
        }

        public void Run()
        {
            try
            {
                Method(this.Session, Script);
                if (Context.Job == null) return;

                var output = new RunnerOutput
                {
                    Exception = null,
                    Output = this.Session.Output.ToHtml(),
                    HasErrors = this.Session.Output.HasErrors,
                };

                Context.Job.Status.Result = output;
                JobContext.MessageQueue.PutMessage(new CompleteMessage { RunnerOutput = output });
            }
            catch (ThreadAbortException ex)
            {
                TurboConsoleLog.Error("Script was aborted", ex);
                if (!Environment.HasShutdownStarted)
                {
                    Thread.ResetAbort();
                }
            }
            catch (Exception ex)
            {
                TurboConsoleLog.Error("Error while executing script.", ex);
                if (!Context.Job.IsNull())
                {
                    var output = new RunnerOutput()
                    {
                        Exception = ex,
                        Output = String.Empty, //TODO: Implement
                        HasErrors = true
                    };
                    Context.Job.Status.Result = output;
                    var message = new CompleteMessage { RunnerOutput = output };
                    JobContext.MessageQueue.PutMessage(message);
                }
            }
        }
    }
}