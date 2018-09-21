using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace TurboConsole.Client.Controls
{
    /// <summary>
    /// The <see cref="ConsoleJobMonitor"/> is responsible for executing the code snippet.
    /// Thi
    /// </summary>
    public class ConsoleJobMonitor : Control
    {
        /// <summary>
        /// Job handle used to get current executing job while checking the status
        /// </summary>
        public Handle JobHandle
        {
            get
            {
                var viewStateString = GetViewStateString("task");
                return !String.IsNullOrEmpty(viewStateString) ? Handle.Parse(viewStateString) : Handle.Null;
            }
            set
            {
                SetViewStateString("task", value.ToString());
            }
        }

        /// <summary>
        /// Used to check the active state of the current executing job.
        /// </summary>
        public Boolean Active
        {
            get { return GetViewStateBool("active", true); }
            set
            {
                SetViewStateBool("active", value);
                if (value)
                {
                    ScheduleCallback();
                }
            }
        }

        public event EventHandler JobFinished;

        /// <summary>
        ///     Occurs when check message received without current task.
        /// </summary>
        public event EventHandler JobDisappeared;

        /// <summary>
        /// This method handles the status check for the running job
        /// </summary>
        /// <param name="message"></param>
        public override void HandleMessage(Message message)
        {
            base.HandleMessage(message);
            if (message.Name != "tconsoletaskmonitor:check")
                return;
            var jobHandle = JobHandle;
            if (jobHandle.Equals((object)Handle.Null))
                return;

            if (!jobHandle.IsLocal)
            {
                ScheduleCallback();
            }
            else
            {
                var job = JobManager.GetJob(jobHandle);
                if (job.IsNull())
                {
                    OnJobDisappeared();
                    this.Active = false;
                }
                else
                {
                    while (job.MessageQueue.GetMessage(out IMessage iMessage))
                    {
                        iMessage.Execute();
                        if (iMessage is CompleteMessage completeMessage)
                        {
                            OnJobFinished(completeMessage.RunnerOutput);
                            this.Active = false;
                            return;
                        }
                    }
                    ScheduleCallback();
                }
            }
        }

        /// <summary>
        /// Starts the new job using <see cref="JobManager"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="category"></param>
        /// <param name="task"></param>
        /// <param name="options"></param>
        public void Start(String name, String category, ThreadStart task, JobOptions options = null)
        {
            Assert.ArgumentNotNull(name, nameof(name));
            Assert.ArgumentNotNull(task, nameof(task));
            var siteName = Sitecore.Context.Site?.Name ?? String.Empty;
            JobHandle = JobManager.Start(new JobOptions($"{name} - {Sitecore.Context.User?.Name}", category, siteName, new TaskRunner(task), "Run")
            {
                ContextUser = Sitecore.Context.User,
                AtomicExecution = false,
                EnableSecurity = options?.EnableSecurity ?? true,
                ClientLanguage = Sitecore.Context.Language,
                AfterLife = new TimeSpan(0, 0, 0, 10)
            }).Handle;
            this.Active = true;
            ScheduleCallback();
        }

        private void OnJobFinished(RunnerOutput runnerOutput)
        {
            JobHandle = Handle.Null;
            var args = new SessionCompletedEventArgs { RunnerOutput = runnerOutput };
            JobFinished?.Invoke(this, args);
        }

        private void OnJobDisappeared()
        {
            JobHandle = Handle.Null;
            JobDisappeared?.Invoke(this, EventArgs.Empty);
        }

        private void ScheduleCallback()
        {
            if (Active)
            {
                SheerResponse.Timer("tconsoletaskmonitor:check", 500);
            }
        }

        public class TaskRunner
        {
            /// <summary>
            ///     Task to execute
            /// </summary>
            private readonly ThreadStart task;

            /// <summary>
            ///     Initializes a new instance of the <see cref="T:Sitecore.Jobs.AsyncUI.JobMonitor.TaskRunner" /> class.
            /// </summary>
            /// <param name="task">The task.</param>
            public TaskRunner(ThreadStart task)
            {
                Assert.ArgumentNotNull(task, "task");
                this.task = task;
            }

            /// <summary>
            ///     Runs the task inside the job.
            /// </summary>
            public void Run()
            {
                task();
            }
        }
    }
}