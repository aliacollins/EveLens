using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace EVEMon.Common.Service
{
    public class CodeCompiler
    {
        private readonly CompilerParameters m_compilerParameters = new CompilerParameters();

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeCompiler"/> class.
        /// </summary>
        /// <param name="referenceAssemblies">The reference assemblies.</param>
        private CodeCompiler(string[] referenceAssemblies)
        {
            m_compilerParameters.GenerateInMemory = true;
            m_compilerParameters.GenerateExecutable = false;
            m_compilerParameters.OutputAssembly = null;
            m_compilerParameters.ReferencedAssemblies.Add(GetType().Assembly.Location);
            m_compilerParameters.ReferencedAssemblies.AddRange(referenceAssemblies);
        }

        /// <summary>
        /// Generates the assembly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="referenceAssemblies">The reference assemblies.</param>
        /// <param name="codeText">The code text.</param>
        /// <returns></returns>
        public static T GenerateAssembly<T>(string[] referenceAssemblies, string codeText) where T : class
        {
            var compiler = new CodeCompiler(referenceAssemblies);
            return compiler.CreateInstanceFrom<T>(codeText);
        }

        /// <summary>
        /// Creates the instance from the specified code text.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="codeText">The code text.</param>
        /// <returns></returns>
        private T CreateInstanceFrom<T>(string codeText) where T : class
        {
            Type type = Compile(codeText)?.GetExportedTypes()
                .FirstOrDefault(exportedType => exportedType.IsSubclassOf(typeof(T)));

            if (type == null)
                return null;

            return Activator.CreateInstance(type) as T;
        }

        /// <summary>
        /// Compiles the specified code text.
        /// </summary>
        /// <param name="codeText">The code text.</param>
        /// <returns></returns>
        private Assembly Compile(string codeText)
        {
            try
            {
                CompilerResults results;
                using (CodeDomProvider csProvider = new CSharpCodeProvider())
                {
                    results = csProvider.CompileAssemblyFromSource(m_compilerParameters, codeText);
                }

                if (!results.Errors.HasErrors)
                {
                    EveMonClient.Trace("Success");
                    return results.CompiledAssembly;
                }

                results.Errors.OfType<CompilerError>().ToList().ForEach(error => EveMonClient.Trace(error.ErrorText));
            }
            catch (Exception exc)
            {
                EveMonClient.Trace("Failed");
                Helpers.ExceptionHandler.LogException(exc, true);
            }

            return null;
        }
    }
}