using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DocAssistShared.Helpers
{
    /// <summary>
    ///  A helper class that simplifies the use of C# feature of compiling source code from string 
    /// </summary>
    public static class CompilationHelper
    {
        #region Fields

        /// <summary>
        ///  A string used to describe a compilation failure exception
        /// </summary>
        public const string CompilationFailure = "Compilation failure";

        /// <summary>
        ///  Key to the errors information in the data sent along with the exception
        /// </summary>
        public const string DataKeyErrors = "Errors";

        /// <summary>
        ///  Keys to the output information in the data sent along with the exception
        /// </summary>
        public const string DataKeyOutput = "Output";

        #endregion

        #region Methods

        /// <summary>
        ///  Compiles code in string form in specified language and with specified supporting assemblies
        /// </summary>
        /// <param name="code">The code as a string to compile</param>
        /// <param name="language">The name of the language the code is in such as "CSharp"</param>
        /// <param name="referencedAssemblies">Assembly dependencies</param>
        /// <returns>The assembly the code has compiled into</returns>
        /// <references>
        ///   1. http://www.codeproject.com/Articles/39130/Compiling-Source-Code-from-a-String
        ///   2. http://stackoverflow.com/questions/1361965/compile-simple-string
        /// </references>
        public static Assembly Compile(this string code, string language, params string[] referencedAssemblies)
        {
            if (String.IsNullOrEmpty(code))
            {
                throw new ArgumentException("You must supply some code", "code");
            }

            if (String.IsNullOrEmpty(language))
            {
                throw new ArgumentException("You must supply the name of a known language", "language");
            }

            if (!CodeDomProvider.IsDefinedLanguage(language))
            {
                throw new ArgumentException("That language is not known on this system", "language");
            }

            using (var cdp = CodeDomProvider.CreateProvider(language))
            {
                var cp = CodeDomProvider.GetCompilerInfo(language).CreateDefaultCompilerParameters();

                cp.GenerateInMemory = true;
                cp.TreatWarningsAsErrors = true;
                cp.WarningLevel = 4;
                cp.ReferencedAssemblies.Add("System.dll");

                if (referencedAssemblies != null && referencedAssemblies.Length > 0)
                {
                    cp.ReferencedAssemblies.AddRange(referencedAssemblies);
                }

                var cr = cdp.CompileAssemblyFromSource(cp, code);

                if (cr.Errors.HasErrors)
                {
                    var err = new Exception(CompilationFailure);
                    err.Data[DataKeyErrors] = cr.Errors;
                    err.Data[DataKeyOutput] = cr.Output;
                    throw (err);
                }

                return (cr.CompiledAssembly);
            }
        }

        /// <summary>
        ///  Returns a list of paths to the assembly files required by compilation based on
        ///  the list from the file whose name is specified
        /// </summary>
        /// <param name="assembliesFile">The name of the file that contains a list of user specified assemblies</param>
        /// <returns>The list of assembly files to be used by the compilation</returns>
        public static string[] GetReferencedAssemblyList(this string assembliesFile)
        {
            var referencedList = new List<string>
                {
                    "System.IO.dll",
                    "System.Core.dll",      // for LinQ
                };  // referenced by default
            var execPath = Assembly.GetExecutingAssembly().Location;
            var execDir = Path.GetDirectoryName(execPath);
            var docAssistRtPath = execDir != null ? Path.Combine(execDir, "DocAssistRuntime.dll") : null;
            if (docAssistRtPath != null && File.Exists(docAssistRtPath))
            {
                referencedList.Add(docAssistRtPath);
            }
            var added = new HashSet<string> { "System.dll" }; // this is added by default by the Compile() method
            foreach (var referenceName in referencedList)
            {
                added.Add(referenceName);
            }

            if (assembliesFile != null)
            {
                using (var sr = new StreamReader(assembliesFile))
                {
                    string line;
                    while (!sr.EndOfStream && (line = sr.ReadLine()) != null)
                    {
                        var referenceName = line;
                        if (added.Contains(referenceName)) continue;
                        referencedList.Add(referenceName);
                        added.Add(referenceName);
                    }
                }
            }
            return referencedList.ToArray();
        }

        #endregion
    }
}
