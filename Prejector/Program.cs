// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Nokia">
// Copyright (c) 2013, Nokia
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace PreJector
{
    internal class Program
    {
        private static InjectionSpecification _injectionSpecification;
        private static InjectionSpecificationInjection[] _injectionList;
        private static DirectoryInfo _applicationFolder;

        private static readonly Dictionary<string, EmittedKernelSpec> RenderedKernels =
            new Dictionary<string, EmittedKernelSpec>();

        private static readonly Dictionary<string, CSharpFile> FileScanCache = new Dictionary<string, CSharpFile>();
        private static string[] _fileList;
        private static string _outputFile;
        private static string[] _inputTokens;

        private static bool _benchmarking = true;
        private static string _renderAccessModifier = "public";
        private static IList<string> _folderExcludes;

        /// <summary>
        ///     Example command line arguments:
        ///     #PATH# #PATH#\XXX.Client\DependencyInjection\InjectionSpecification.xml NETFX_CORE;RENDERASINTERNAL;NOVIEWMODELLOCATOR;NOBENCHMARK
        /// </summary>
        private static int Main(string[] args)
        {
            try
            {
                return Go(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        private static CSharpFile FileScan(string className, bool required)
        {
            if (FileScanCache.ContainsKey(className))
            {
                return FileScanCache[className];
            }

            int indexer = className.IndexOf('<');
            if (indexer != -1)
            {
                className = className.Substring(0, indexer);
            }

            string fileSpec = string.Format("\\{0}.cs", className);
            string alternativeFileSpec = string.Format("\\{0}.cpp", className);

            List<string> files = _fileList.Where(x => x.EndsWith(fileSpec) || x.EndsWith(alternativeFileSpec)).ToList();

            files = ExcludeFiles(files);

            if (files.Count > 1)
            {
                string error = string.Format("Ambiguous file spec {0}", fileSpec);
                throw new Exception(error);
            }
            if (files.Count == 0 && required)
            {
                string error = string.Format("Cannot find file spec for {0}", fileSpec);
                throw new Exception(error);
            }

            string file = files.SingleOrDefault();

            if (file == null)
            {
                return null;
            }

            var csFile = new CSharpFile(className);
            csFile.ParseFile(new FileInfo(file));

            FileScanCache[className] = csFile;

            return csFile;
        }

        private static List<string> ExcludeFiles(List<string> fileList)
        {
            if (_injectionSpecification.FolderExcludes == null)
            {
                return fileList;
            }

            return fileList.Where(x => !ShouldExclude(x)).ToList();
        }

        private static bool ShouldExclude(string filePath)
        {
            EnsureFolderExcludes();
            bool shouldExclude = _folderExcludes.Any(filePath.StartsWith);
            return shouldExclude;
        }

        private static void EnsureFolderExcludes()
        {
            if (_folderExcludes != null)
            {
                return;
            }

            _folderExcludes = new List<string>();

            foreach (InjectionSpecificationFolderExclude exclude in _injectionSpecification.FolderExcludes)
            {
                string condition = exclude.Condition;

                if (!String.IsNullOrEmpty(condition))
                {
                    bool invertCondition = false;
                    if (condition.StartsWith("!"))
                    {
                        invertCondition = true;
                        condition = condition.Substring(1, condition.Length - 1);
                    }

                    bool valid = _inputTokens.Contains(condition) != invertCondition;

                    if (valid)
                    {
                        _folderExcludes.Add(Path.Combine(_applicationFolder.FullName, exclude.Value) +
                                            Path.DirectorySeparatorChar);
                    }
                }
            }
        }

        private static int Go(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("No files specified");
                return -1;
            }

            if (args.Length > 2)
            {
                string token = args[2];
                _inputTokens = token.Trim('\"').Trim('\'').Split(';');
            }
            else
            {
                _inputTokens = new string[0];
            }

            if (_inputTokens.Contains("RENDERASINTERNAL"))
            {
                _renderAccessModifier = "internal";
            }

            if (_inputTokens.Contains("NOBENCHMARK"))
            {
                _benchmarking = false;
            }

            _applicationFolder = new DirectoryInfo(args[0]);
            string injectionSpec = args[1];

            if (!File.Exists(injectionSpec))
            {
                Console.WriteLine("injectionSpec doesnt exist");
                return -1;
            }

            if (!injectionSpec.EndsWith(".xml"))
            {
                Console.WriteLine("injectionSpec isnt an xml file");
                return -1;
            }

            if (args.Length > 3)
            {
                _outputFile = args[3];
            }
            else
            {
                _outputFile = injectionSpec.Substring(0, injectionSpec.Length - 4) + ".cs";
            }

            _fileList = _applicationFolder.GetFiles("*.c*", SearchOption.AllDirectories)
                                          .Select(x => x.FullName)
                                          .Where(x => !x.EndsWith(".xaml.cs"))
                                          .Where(x => !x.Contains("AcceptanceTestEngine"))
                                          .Where(x => !x.Contains("Test"))
                                          .Where(x => !x.Contains("\\obj\\"))
                                          .Where(x => !x.Contains("\\Bin\\"))
                                          .ToArray();

            using (var sr = new StreamReader(injectionSpec))
            {
                var xml = new XmlSerializer(typeof(InjectionSpecification));
                _injectionSpecification = (InjectionSpecification)xml.Deserialize(sr);
            }

            var stringBuilder = new StringBuilder();

            using (var rw = new StringWriter(stringBuilder))
            {
                rw.WriteLine("namespace {0} {{", _injectionSpecification.Namespace.Trim());

                _injectionList = _injectionSpecification.Injections;

                var viewModels = new List<InjectionSpecificationInjection>();

                foreach (InjectionSpecificationInjection injection in _injectionList)
                {
                    string condition = injection.Condition;

                    if (!String.IsNullOrEmpty(condition))
                    {
                        bool invertCondition = false;
                        if (condition.StartsWith("!"))
                        {
                            invertCondition = true;
                            condition = condition.Substring(1, condition.Length - 1);
                        }

                        bool contains = _inputTokens.Contains(condition);

                        if (contains == invertCondition)
                        {
                            Console.WriteLine(
                                "Ignoring injection '{0}' because of condition '{1}' (Inverted:{2})",
                                injection.ConcreteClassName,
                                condition,
                                invertCondition);
                            continue;
                        }
                    }

                    DateTime start = DateTime.Now;
                    Render(rw, injection);
                    Console.WriteLine("Rendering {0} took {1}", injection.ConcreteClassName,
                                      (DateTime.Now - start).TotalSeconds);

                    if (injection.IsViewModel)
                    {
                        viewModels.Add(injection);
                    }
                }

                // It's actually a performace hit due to forcing more assembly loading earlier on
                // Don't use it...
                if (!_inputTokens.Contains("NOVIEWMODELLOCATOR"))
                {
                    RenderViewModelLocator(rw, viewModels);
                }

                RenderKernelCleardown(rw);

                rw.WriteLine("}");
            }

            using (var rw = new StreamWriter(_outputFile))
            {
                RenderAutogeneratedHeader(rw);

                RenderNamespaces(rw);

                rw.Write(stringBuilder.ToString());
            }

            return 0;
        }

        private static void RenderViewModelLocator(StringWriter rw,
                                                   IEnumerable<InjectionSpecificationInjection> injections)
        {
            rw.WriteLine("[CoverageExclude]");
            rw.WriteLine("{0} static class ViewModelLocator {{", _renderAccessModifier);

            foreach (InjectionSpecificationInjection injection in injections)
            {
                if (injection.DebugOnly)
                {
                    rw.WriteLine("#if DEBUG");
                }

                rw.WriteLine(
                    "{0} static {1} {1} {{ get {{ return Kernel_{1}.Get(); }} }}",
                    _renderAccessModifier,
                    injection.Concrete);

                if (injection.DebugOnly)
                {
                    rw.WriteLine("#endif");
                }
            }

            rw.WriteLine("}");
        }

        private static void RenderKernelCleardown(StringWriter rw)
        {
            rw.WriteLine("[CoverageExclude]");
            rw.WriteLine("{0} static class KernelCleardown {{", _renderAccessModifier);
            rw.WriteLine("{0} static void Clear() {{", _renderAccessModifier);

            foreach (var renderedKernel in RenderedKernels)
            {
                if (!renderedKernel.Value.Singleton)
                {
                    continue;
                }

                var renderedKernelClassName = renderedKernel.Key;

                bool isDebug = renderedKernel.Value.IsDebug;

                if (isDebug)
                {
                    rw.WriteLine("#if DEBUG");
                }

                rw.WriteLine("{0}.Clear();", renderedKernelClassName);

                if (isDebug)
                {
                    rw.WriteLine("#endif");
                }
            }
            rw.WriteLine("}");
            rw.WriteLine("}");
        }

        private static void RenderAutogeneratedHeader(StreamWriter rw)
        {
            rw.WriteLine("//------------------------------------------------------------------------------");
            rw.WriteLine("// <auto-generated>");
            rw.WriteLine("// This code was generated by PreJector.");
            rw.WriteLine("// </auto-generated>");
            rw.WriteLine("//------------------------------------------------------------------------------");
        }

        private static void RenderNamespaces(StreamWriter rw)
        {
            var namespaces = new List<string>();
            var debugNamespaces = new List<string>();

            namespaces.Add("System");
            namespaces.Add("System.Diagnostics");
            namespaces.Add("System.Linq");
            namespaces.Add("System.Reflection");

            foreach (string kernelKey in RenderedKernels.Keys)
            {
                EmittedKernelSpec kernel = RenderedKernels[kernelKey];

                foreach (string @namespace in kernel.Namespaces)
                {
                    if (kernel.IsDebug)
                    {
                        debugNamespaces.Add(@namespace);
                    }
                    else
                    {
                        namespaces.Add(@namespace);
                    }
                }
            }

            // Add explicit namespaces
            if (_injectionSpecification.Namespaces != null)
            {
                foreach (InjectionSpecificationNamespace explicitNamespace in _injectionSpecification.Namespaces)
                {
                    string condition = explicitNamespace.Condition;

                    bool valid = true;

                    if (!String.IsNullOrEmpty(condition))
                    {
                        bool invertCondition = false;

                        if (condition.StartsWith("!"))
                        {
                            invertCondition = true;
                            condition = condition.Substring(1, condition.Length - 1);
                        }

                        valid = _inputTokens.Contains(condition) != invertCondition;
                    }

                    if (valid)
                    {
                        if (explicitNamespace.DebugOnlySpecified && explicitNamespace.DebugOnly)
                        {
                            debugNamespaces.Add(explicitNamespace.Value);
                        }
                        else
                        {
                            namespaces.Add(explicitNamespace.Value);
                        }
                    }
                }
            }

            // Filter and sort
            namespaces = namespaces.Distinct().OrderBy(x => x).ToList();
            debugNamespaces = debugNamespaces.Where(x => !namespaces.Contains(x)).Distinct().OrderBy(x => x).ToList();

            if (debugNamespaces.Count > 0)
            {
                rw.WriteLine("#if DEBUG");
            }

            foreach (string @namespace in debugNamespaces)
            {
                rw.WriteLine("using {0};", @namespace);
            }

            if (debugNamespaces.Count > 0)
            {
                rw.WriteLine("#endif");
            }

            foreach (string @namespace in namespaces)
            {
                rw.WriteLine("using {0};", @namespace);
            }
        }

        private static void Render(StringWriter rw, InjectionSpecificationInjection injection)
        {
            if (injection == null)
            {
                throw new ArgumentNullException("injection");
            }

            bool debugEmit = injection.DebugOnly;
            if (debugEmit)
            {
                rw.WriteLine("#if DEBUG");
            }

            string outerKernelClassName = injection.ConcreteClassName;

            var spec = new EmittedKernelSpec
                           {
                               KernelClassName = outerKernelClassName,
                               IsDebug = injection.DebugOnly,
                               Singleton = injection.Singleton
                           };

            rw.WriteLine("[CoverageExclude]");
            rw.WriteLine("{0} static class {1} {{", _renderAccessModifier, outerKernelClassName);

            if (!string.IsNullOrEmpty(injection.Provider))
            {
                if (injection.Interface.Length == 1)
                {
                    spec.PrivateFieldType = injection.Interface.First().Value;
                }
                else
                {
                    if (string.IsNullOrEmpty(injection.Concrete))
                    {
                        throw new Exception("Providers with no concrete and interfaces.Count != 1 not supported");
                    }

                    spec.PrivateFieldType = injection.Concrete;
                }

                if (injection.Singleton)
                {
                    spec.PrivateFieldName = RenderPrivateField(rw, spec, injection.Singleton);
                }

                spec.ReturnType = injection.Interface != null && injection.Interface.Length == 1
                                      ? injection.Interface.First().Value
                                      : injection.Concrete;

                RenderProviderGetter(rw, injection, spec);

                if (injection.Singleton)
                {
                    RenderClearAndRebind(rw, spec);
                }
            }
            else if (!string.IsNullOrEmpty(injection.Concrete))
            {
                spec.PrivateFieldType = injection.Interface != null && injection.Interface.Length == 1
                                            ? injection.Interface.First().Value
                                            : injection.Concrete;
                spec.ReturnType = spec.PrivateFieldType;

                if (injection.Singleton)
                {
                    spec.PrivateFieldName = RenderPrivateField(rw, spec, injection.Singleton);
                }

                RenderConcreteBuilderGetter(rw, injection, spec);

                if (injection.Singleton)
                {
                    RenderClearAndRebind(rw, spec);
                }
            }
            else
            {
                throw new ParseException("Injection defined with no Concrete or Provider?");
            }

            rw.WriteLine('}');

            AddRenderedKernel(spec, injection);

            // Render additional interfaces...
            if (injection.Interface != null)
            {
                foreach (InjectionSpecificationInjectionInterface @interface in injection.Interface)
                {
                    string interfaceName = @interface.Value;

                    if (!interfaceName.StartsWith("I"))
                    {
                        throw new ParseException(
                            string.Format(
                                "All interfaces need to start with 'I'. Check the InjectionSpecification is formated correctly without line breaks or similare in the node. Current interface name being read: '{0}'.",
                                interfaceName));
                    }

                    string interfaceNameNoI = interfaceName.Substring(1, interfaceName.Length - 1).RemoveDodgyTokens();

                    EmittedKernelSpec existingSpec =
                        RenderedKernels.Where(x => x.Value.ReturnType == interfaceName).Select(x => x.Value).
                                        SingleOrDefault();

                    if (!injection.ShouldHaveConcreteCoreRender && existingSpec != null)
                    {
                        // We've already built this above...
                        continue;
                    }

                    // Render Kernel Reference which points to the concrete...
                    var spec2 = new EmittedKernelSpec
                                    {
                                        KernelClassName = string.Format("Kernel_{0}", interfaceNameNoI),
                                        PrivateFieldType = interfaceName,
                                        IsDebug = false,
                                        Singleton = injection.Singleton
                                    };

                    rw.WriteLine("[CoverageExclude]");
                    rw.WriteLine("{0} static class {1} {{",
                                 _renderAccessModifier,
                                 spec2.KernelClassName);

                    spec2.ReturnType = spec2.PrivateFieldType;

                    if (injection.Singleton)
                    {
                        spec2.PrivateFieldName = RenderPrivateField(rw, spec2, injection.Singleton);

                        rw.WriteLine(
                            "static {0}() {{ {1} = new Lazy<{2}>({3}.Get); }}",
                            spec2.KernelClassName,
                            spec2.PrivateFieldName,
                            spec2.ReturnType,
                            injection.ConcreteClassName);
                        rw.WriteLine(
                            "{0} static {1} Get() {{ return {2}.Value; }}",
                            _renderAccessModifier,
                            spec2.ReturnType,
                            spec2.PrivateFieldName);
                        rw.WriteLine(
                            "{0} static Lazy<{1}> GetLazy() {{ return {2}; }}",
                            _renderAccessModifier,
                            spec2.ReturnType,
                            spec2.PrivateFieldName);

                        RenderClearAndRebind(rw, spec2);
                    }
                    else
                    {
                        rw.WriteLine(
                            "{0} static {1} Get() {{ return {3}.Get(); }}",
                            _renderAccessModifier,
                            spec2.ReturnType,
                            spec2.PrivateFieldName,
                            injection.ConcreteClassName);
                        rw.WriteLine(
                            "{0} static Lazy<{1}> GetLazy() {{ return new Lazy<{1}>({2}.Get); }}",
                            _renderAccessModifier,
                            spec2.ReturnType,
                            injection.ConcreteClassName);
                    }

                    rw.WriteLine('}');

                    AddRenderedKernel(spec2, injection);
                }
            }

            if (debugEmit)
            {
                rw.WriteLine("#endif");
            }
        }

        private static void AddRenderedKernel(EmittedKernelSpec spec, InjectionSpecificationInjection injection)
        {
            spec.Namespaces = CalculateNamespacesForInjection(injection);
            spec.Namespaces = spec.Namespaces.Distinct().ToList();

            if (RenderedKernels.ContainsKey(spec.KernelClassName))
            {
                throw new InvalidOperationException("Not unique?");
            }

            RenderedKernels.Add(spec.KernelClassName, spec);
        }

        private static IList<string> CalculateNamespacesForInjection(InjectionSpecificationInjection injection)
        {
            var usedTypes = new List<string>();

            if (!string.IsNullOrEmpty(injection.Concrete))
            {
                usedTypes.Add(injection.Concrete);
            }

            if (!string.IsNullOrEmpty(injection.Provider))
            {
                usedTypes.Add(injection.Provider);
            }

            if (injection.Interface != null)
            {
                usedTypes.AddRange(
                    from @interface in injection.Interface
                    where !string.IsNullOrEmpty(@interface.Value)
                    select @interface.Value);
            }

            return (from type in usedTypes
                    select FileScan(type, false)
                        into file
                        where file != null
                        select file.Namespace).ToList();
        }

        private static void RenderClearAndRebind(StringWriter rw,
                                                 EmittedKernelSpec spec)
        {
            rw.WriteLine("{0} static void Clear() {{ {2} = new Lazy<{1}>(() => null); }}", _renderAccessModifier, spec.PrivateFieldType, spec.PrivateFieldName);
            rw.WriteLine("{0} static void Rebind({1} value) {{ {2} = new Lazy<{1}>(() => value); }}",
                _renderAccessModifier,
                spec.PrivateFieldType,
                spec.PrivateFieldName);
        }

        private static string RenderPrivateField(StringWriter rw,
                                                 EmittedKernelSpec spec,
                                                 bool singleton)
        {
            int indexOf = spec.KernelClassName.IndexOf('_');
            string fieldName = spec.KernelClassName.Remove(0, indexOf);
            if (singleton)
            {
                rw.WriteLine("private static Lazy<{0}> {1};", spec.PrivateFieldType, fieldName);
            }
            else
            {
                rw.WriteLine("private static {0} {1};", spec.PrivateFieldType, fieldName);
            }
            return fieldName;
        }

        private static void RenderProviderGetter(StringWriter rw,
                                                 InjectionSpecificationInjection injection,
                                                 EmittedKernelSpec spec)
        {
            if (injection.Singleton)
            {
                rw.WriteLine("static {0}() {{", injection.ConcreteClassName);
                rw.WriteLine("{0} = new Lazy<{1}>(() => {{", spec.PrivateFieldName, spec.PrivateFieldType);
            }
            else
            {
                rw.WriteLine("{0} static {1} Get() {{", _renderAccessModifier, spec.ReturnType);
            }

            rw.WriteLine("var x = new {0}().Create();", injection.Provider);
            rw.WriteLine("return x;");

            if (injection.Singleton)
            {
                rw.WriteLine("});");
                rw.WriteLine("}");
                rw.WriteLine("{0} static {1} Get() {{ return {2}.Value; }}", _renderAccessModifier, spec.ReturnType, spec.PrivateFieldName);
                rw.WriteLine("{0} static Lazy<{1}> GetLazy() {{ return {2}; }}", _renderAccessModifier, spec.ReturnType, spec.PrivateFieldName);
            }
            else
            {
                rw.WriteLine("}");
                rw.WriteLine("{0} static Lazy<{1}> GetLazy() {{ return new Lazy<{1}>(Get); }}", _renderAccessModifier, spec.ReturnType);
            }
        }

        private static void RenderConcreteBuilderGetter(StringWriter rw,
                                                        InjectionSpecificationInjection injection,
                                                        EmittedKernelSpec spec)
        {
            if (!string.IsNullOrEmpty(injection.Provider))
            {
                throw new ParseException("Injection concrete '{0}' alse has provider '{1}' defined",
                                         injection.Concrete,
                                         injection.Provider);
            }

            string concreteToBuild = injection.Concrete;

            CSharpFile fileDefinition = injection.NoScan
                                            ? new CSharpFile(concreteToBuild)
                                            : FileScan(concreteToBuild, true);

            if (injection.Singleton)
            {
                rw.WriteLine("static {0}() {{", injection.ConcreteClassName);
                rw.WriteLine("{0} = new Lazy<{1}>(() => {{", spec.PrivateFieldName, spec.PrivateFieldType);
            }
            else
            {
                rw.WriteLine("{0} static {1} Get() {{", _renderAccessModifier, spec.ReturnType);
            }

            // There is no StackTrace on Metro :-(
            if (!_inputTokens.Contains("NETFX_CORE"))
            {
                if (!spec.IsDebug)
                {
                    rw.WriteLine("#if DEBUG");
                }
                rw.WriteLine("var stack = new StackTrace().GetFrames();");
                rw.WriteLine("var methodName = stack.First().GetMethod().DeclaringType.Name;");
                rw.WriteLine("if (stack.Count(y => y.GetMethod().DeclaringType.Name == methodName) > 2) { throw new Exception(\"Infinite loop detected\"); }");
                if (!spec.IsDebug)
                {
                    rw.WriteLine("#endif");
                }
            }
            if (_benchmarking)
            {
                if (!spec.IsDebug)
                {
                    rw.WriteLine("#if DEBUG");
                }
                rw.WriteLine("Debug.WriteLine(\"Thread(\" + System.Threading.Thread.CurrentThread.ManagedThreadId + \") : Kernel TypeConstruct: {0}\");", injection.ConcreteClassName);
                rw.WriteLine("var stopWatch = System.Diagnostics.Stopwatch.StartNew();");
                if (!spec.IsDebug)
                {
                    rw.WriteLine("#endif");
                }
            }

            rw.WriteLine("var x = new {0}(", concreteToBuild);

            if (injection.ConstructorArgument != null && injection.ConstructorArgument.Length > 0)
            {
                InjectionSpecificationInjectionConstructorArgument[] cArgs = injection.ConstructorArgument;
                for (int i = 0; i < cArgs.Length; i++)
                {
                    if (i != 0)
                    {
                        rw.Write(',');
                    }

                    rw.WriteLine(injection.ConstructorArgument[i].Value);
                }
            }
            else
            {
                IList<string> cArgs = fileDefinition.ConstructorArgs ?? new List<string>();
                for (int i = 0; i < cArgs.Count; i++)
                {
                    if (i != 0)
                    {
                        rw.Write(',');
                    }

                    string interfaceRequired = cArgs[i];
                    string concreteFunction = GetConcreteYieldingFunction(interfaceRequired);
                    rw.WriteLine(concreteFunction);
                }
            }

            rw.WriteLine(");");

            if (_benchmarking)
            {
                if (!spec.IsDebug)
                {
                    rw.WriteLine("#if DEBUG");
                }
                rw.WriteLine("stopWatch.Stop();");
                rw.WriteLine("Debug.WriteLine(\"Thread(\" + System.Threading.Thread.CurrentThread.ManagedThreadId + \") : Kernel TypeConstruct: {0}, MillisecondsElapsed: \" + stopWatch.ElapsedMilliseconds);", injection.ConcreteClassName);
                if (!spec.IsDebug)
                {
                    rw.WriteLine("#endif");
                }
            }

            if (!injection.NoScan)
            {
                if (_benchmarking)
                {
                    if (!spec.IsDebug)
                    {
                        rw.WriteLine("#if DEBUG");
                    }
                    rw.WriteLine("stopWatch = System.Diagnostics.Stopwatch.StartNew();");
                    if (!spec.IsDebug)
                    {
                        rw.WriteLine("#endif");
                    }
                }

                var hasProperties = RenderPropertyInjections(rw, fileDefinition);

                if (_benchmarking)
                {
                    if (!spec.IsDebug)
                    {
                        rw.WriteLine("#if DEBUG");
                    }
                    rw.WriteLine("stopWatch.Stop();");
                    if (hasProperties)
                    {
                        rw.WriteLine("Debug.WriteLine(\"Thread(\" + System.Threading.Thread.CurrentThread.ManagedThreadId + \") : Kernel TypePropertiesSet: {0}, MillisecondsElapsed: \" + stopWatch.ElapsedMilliseconds);", injection.ConcreteClassName);
                    }
                    if (!spec.IsDebug)
                    {
                        rw.WriteLine("#endif");
                    }
                }
            }

            rw.WriteLine("return x;");

            if (injection.Singleton)
            {
                rw.WriteLine("});");
                rw.WriteLine("}");
                rw.WriteLine("{0} static {1} Get() {{ return {2}.Value; }}", _renderAccessModifier, spec.ReturnType, spec.PrivateFieldName);
                rw.WriteLine("{0} static Lazy<{1}> GetLazy() {{ return {2}; }}", _renderAccessModifier, spec.ReturnType, spec.PrivateFieldName);
            }
            else
            {
                rw.WriteLine("}");
                rw.WriteLine("{0} static Lazy<{1}> GetLazy() {{ return new Lazy<{1}>(Get); }}", _renderAccessModifier, spec.ReturnType, spec.PrivateFieldName);
            }
        }

        private static bool RenderPropertyInjections(StringWriter rw, CSharpFile fileDefinition)
        {
            var hasProperties = false;

            foreach (var propertyInjection in fileDefinition.PropertyInjections)
            {
                string interfaceRequired = propertyInjection.Key;
                string concreteFunction = GetConcreteYieldingFunction(interfaceRequired);
                rw.WriteLine("x.{0} = {1};", propertyInjection.Value, concreteFunction);
                hasProperties = true;
            }

            foreach (string baseType in fileDefinition.BaseTypes)
            {
                if (_injectionSpecification.Exclude != null)
                {
                    bool isExcludedBaseType = _injectionSpecification.Exclude.Any(b => Regex.IsMatch(baseType, b.Value));

                    if (isExcludedBaseType)
                    {
                        continue;
                    }
                }

                // TODO - really ropey interface detection...
                if (baseType.StartsWith("I") && char.IsUpper(baseType.ElementAt(1)))
                {
                    continue;
                }

                CSharpFile x = FileScan(baseType, true);
                hasProperties |= RenderPropertyInjections(rw, x);
            }

            return hasProperties;
        }

        private static string GetConcreteYieldingFunction(string interfaceRequired)
        {
            var isLazy = interfaceRequired.StartsWith("Lazy<");

            if (isLazy)
            {
                interfaceRequired = interfaceRequired.Substring(5, interfaceRequired.Length - 6);
            }

            // Detect if self-bound
            var selfBound = _injectionList.Where(injection => injection.Concrete == interfaceRequired).ToArray();

            if (selfBound.Length >= 2)
            {
                throw new Exception("Ambiguous reference");
            }

            InjectionSpecificationInjection foundInjection = null;

            if (selfBound.Length == 1)
            {
                foundInjection = selfBound.First(); //.ConcreteYieldingFunction;
            }

            foreach (var injection in _injectionList)
            {
                if (injection.Interface == null)
                {
                    continue;
                }

                if (injection.Interface.Any(x => x.Value == interfaceRequired))
                {
                    foundInjection = injection;
                    break;
                }
            }

            if (foundInjection == null)
            {
                string message = string.Format(
                    "The interface {0} is required by injection, but not defined in the InjectionSpecification",
                    interfaceRequired);
                throw new InvalidOperationException(message);
            }

            if (foundInjection.Interface != null && foundInjection.Interface.Length > 1)
            {
                // return the specific interface rather than the underlying concrete
                return foundInjection.InterfaceYieldingFunction(interfaceRequired, isLazy);
            }

            return foundInjection.ConcreteYieldingFunction(isLazy);
        }
    }
}