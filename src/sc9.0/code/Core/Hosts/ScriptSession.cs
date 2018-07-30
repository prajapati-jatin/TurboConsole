using Sitecore.Jobs;
using Sitecore.Security.Accounts;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using TurboConsole.Core.Compilers;

namespace TurboConsole.Core.Hosts
{
    public class ScriptSession : IDisposable
    {
        private const String HtmlExceptionFormatString = "<div style=\"white-space: normal; width: 70%;\">{1}</div>{1}<strong>Of type</strong>: {3}{0}<strong>Stack trace</strong>:{0}{2}{0}";
        private const string HtmlInnerExceptionPrefix = "{0}<strong>Inner Exception:</strong>{1}";

        private readonly CSCompiler compiler;

        private Boolean disposed;

        private Boolean initialized;

        public string UserName { get; private set; }

        internal String JobScript { get; set; }

        internal JobOptions JobOptions { get; set; }

        public string ApplicationType { get; }

        public OutputBuffer Output { get; }

        public Boolean OnlyBuildCode { get; set; }

        public String ContextWebsite { get; set; }

        public string ID { get; internal set; }
        public bool AutoDispose { get; internal set; }
        public string Key { get; internal set; }

        static ScriptSession()
        {

        }

        internal ScriptSession(String applicationType, Boolean personalizedSettings, String contextWebsite = "website", Boolean onlyBuild = false)
        {
            ApplicationType = applicationType;

            this.Output = new OutputBuffer();

            this.OnlyBuildCode = onlyBuild;

            this.ContextWebsite = contextWebsite;

            this.compiler = new CSCompiler();

            //TODO: Implement settings
            if (!initialized)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            Initialize(false);
        }

        public void Initialize(Boolean reinitialize)
        {
            lock (this)
            {
                if (initialized && !reinitialize) return;
                initialized = true;
                UserName = User.Current.Name;
            }
        }

        public void ExecuteCode(String codeToExecute)
        {
            compiler.SourceToMemory(codeToExecute, null, compilerResults =>
            {
                if (compilerResults.NativeCompilerReturnValue == 0)
                {
                    if (!OnlyBuildCode)
                    {
                        //Clear previous output
                        this.Output.Clear();
                        var siteContext = Sitecore.Configuration.Factory.GetSite(this.ContextWebsite);
                        if (siteContext.IsNull())
                        {
                            siteContext = Sitecore.Configuration.Factory.GetSite("website");
                        }
                        if (siteContext != null && siteContext.Database.Name.ToLowerInvariant().Equals("web"))
                        {
                            siteContext.Database = Sitecore.Configuration.Factory.GetDatabase("master");
                        }
                        using (var context = new Sitecore.Sites.SiteContextSwitcher(siteContext))
                        {
                            Assembly assemblyToExecute = compilerResults.CompiledAssembly; //LoadAssembly(compilerResults.PathToAssembly);
                            var outputMethod = GetOutputMethod(assemblyToExecute);
                            if (!outputMethod.IsNull())
                            {
                                object classObject = Activator.CreateInstance(outputMethod.DeclaringType);
                                var output = outputMethod.Invoke(classObject, BindingFlags.InvokeMethod | BindingFlags.Default, null, null, null);
                                this.Output.Add(new Hosts.Output(OutputLineType.Output, output.ToString()));
                            }
                            else
                            {
                                this.Output.Add(new Hosts.Output(OutputLineType.Error, "Method \"public System.String Output()\" not found"));
                            }
                        }
                    }
                    else
                    {
                        this.Output.Add(new Hosts.Output(OutputLineType.Output, "Build succeeded"));
                        foreach (String s in compilerResults.Output)
                        {
                            this.Output.Add(new Hosts.Output(OutputLineType.Output, s));
                        }
                    }
                }
                else
                {
                    foreach (CompilerError error in compilerResults.Errors)
                    {
                        this.Output.Add(new Hosts.Output(error.IsWarning ? OutputLineType.Warning : OutputLineType.Error, $"{error.ErrorNumber}: {error.ErrorText}"));
                    }
                }
            });
        }

        private MethodInfo GetOutputMethod(Assembly assembly)
        {
            var classes = assembly.GetTypes().Where(t => t.IsClass);
            foreach (Type t in classes)
            {
                var outputMethod = t.GetMethods().Where(m => m.Name.ToLower().Equals("output") && m.IsPublic && m.ReturnType == typeof(String)).FirstOrDefault();
                return outputMethod;
            }
            return null;
        }

        private IEnumerable<String> GetMethods(Assembly assembly)
        {
            return null;
        }

        private IEnumerable<String> GetClasses(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => t.IsClass).Select(t => t.FullName).Distinct();
        }

        private IEnumerable<String> GetNamespaces(Assembly assembly)
        {
            var namespaces = assembly.GetTypes().Select(t => t.Namespace).Distinct();
            return namespaces;
        }

        public void Dispose()
        {

        }

        private const String HtmlLineEndFormat = "<br/>";

        public static String GetExceptionString(Exception e)
        {
            var stackTrace = e.Message;
            if (!e.InnerException.IsNull())
            {
                stackTrace = e.InnerException.StackTrace;
            }
            StringBuilder sbException = new StringBuilder();
            sbException.AppendFormat(HtmlExceptionFormatString, HtmlLineEndFormat, e.Message, stackTrace, e.GetType());
            return sbException.ToString();
        }
    }
}