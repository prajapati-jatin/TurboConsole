using Microsoft.CSharp;
using Sitecore.Diagnostics;
using Sitecore.IO;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Web;

namespace TurboConsole.Core.Compilers
{
    /// <summary>
    /// C# compiler.
    /// </summary>
    public class CSCompiler
    {
        //private CompilerResults compilerResults;

        public CompilerResults CompilerResults { get; private set; }

        private static IEnumerable<String> _appDomainAssemblies;

        private static IEnumerable<String> AppDomainAssemblies
        {
            get
            {
                if(_appDomainAssemblies == null)
                {
                    _appDomainAssemblies = GetAssembliesLoadedInAppDomain();
                }
                return _appDomainAssemblies;
            }
        }

        public CSCompiler()
        {

        }

        private List<String> ExcludedAssemblies = new List<string>()
        {
            "System.Runtime",
            "System.Runtime.Extensions",
            "System.Reflection",
            "System.IO"
        };

        private void AddReferences(CompilerParameters parameters, StringCollection referencedAssemblies)
        {
            Error.AssertObject(parameters, nameof(parameters));
            //TODO: Add assenblies from web.config            
            foreach (var ass in AppDomainAssemblies)
            {
                parameters.ReferencedAssemblies.Add(ass);
            }
            referencedAssemblies?.Cast<String>().ToList().ForEach(referencedAssenbly =>
            {
                parameters.ReferencedAssemblies.Add(referencedAssenbly);
            });
        }

        private static IEnumerable<String> GetAssembliesLoadedInAppDomain()
        {
            var assemblies = new Dictionary<String, String>();
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !String.IsNullOrEmpty(a.Location)))
            {
                var name = assembly.GetName().Name;
                if (!assemblies.ContainsKey(name))
                {
                    assemblies.Add(name, assembly.Location);
                }
            }
            return assemblies.Values.ToList();
        }

        private void AssertResult(String sourceFile, CompilerResults results)
        {
            //TODO: Implement the method
        }

        public CompilerResults Compile(String code, StringCollection referencedAssemblies)
        {
            Error.AssertString(code, nameof(code), false);
            return this.Compile(String.Empty, code, null, referencedAssemblies);
        }

        public CompilerResults Compile(String sourceFile, String code, String assemblyFile, StringCollection referencedAssemblies)
        {
            Error.AssertString(code, nameof(code), false);
            CodeDomProvider compiler = this.GetCompiler();
            CompilerParameters parameters = this.GetParameters(assemblyFile, referencedAssemblies);
            if (compiler.IsNull() || parameters.IsNull())
            {
                return null;
            }
            CompilerResults compilerResults = compiler.CompileAssemblyFromSource(parameters, new String[] { code });
            this.AssertResult(sourceFile, compilerResults);
            return compilerResults;
        }

        public void FileToFile(String sourceFile, String assemblyFile, StringCollection referencedAssemblies)
        {
            Error.AssertString(sourceFile, nameof(sourceFile), false);
            Error.AssertFile(sourceFile);
            String str = FileUtil.ReadFromFile(sourceFile);
            if (str.Length > 0)
            {
                this.SourceToFile(sourceFile, str, assemblyFile, referencedAssemblies);
            }
        }

        public Assembly FileToMemory(String sourceFile, StringCollection referencedAssemblies)
        {
            Error.AssertString(sourceFile, nameof(sourceFile), false);
            Error.AssertFile(sourceFile);
            String str = FileUtil.ReadFromFile(sourceFile);
            if (str.Length <= 0)
            {
                return null;
            }
            return this.SourceToMemory(str, referencedAssemblies);
        }

        public void SourceToFile(String sourceFile, String code, String assemblyFile, StringCollection referencedAssemblies)
        {
            Error.AssertString(sourceFile, nameof(sourceFile), false);
            Error.AssertString(code, nameof(code), false);
            this.CompilerResults = this.Compile(sourceFile, code, assemblyFile, referencedAssemblies);
        }

        public Assembly SourceToMemory(String code, StringCollection referencedAssemblies)
        {
            Error.AssertString(code, nameof(code), false);
            return this.Compile(code, referencedAssemblies).CompiledAssembly;
        }

        public void SourceToMemory(String code, StringCollection referencedAssemblies, Action<CompilerResults> action)
        {
            Error.AssertObject(action, nameof(action));
            CompilerResults compilerResults = this.Compile(code, referencedAssemblies);
            action(compilerResults);
        }

        private CompilerParameters GetParameters(string assemblyFile, StringCollection referencedAssemblies)
        {
            CompilerParameters compilerParameters = new CompilerParameters();
            if (assemblyFile.IsNull())
            {
                compilerParameters.GenerateInMemory = true;
            }
            else
            {
                compilerParameters.OutputAssembly = assemblyFile;
            }
            compilerParameters.CompilerOptions = "/optimize";
            compilerParameters.IncludeDebugInformation = false;
            foreach (String s in compilerParameters.LinkedResources)
            {
                System.Diagnostics.Debug.WriteLine(s);
            }
            this.AddReferences(compilerParameters, referencedAssemblies);
            return compilerParameters;
        }

        private CodeDomProvider GetCompiler()
        {
            return new CSharpCodeProvider();
        }
    }
}