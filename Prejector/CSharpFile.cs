using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PreJector
{
    [DebuggerDisplay("{_className}")]
    public class CSharpFile
    {
        private readonly string _className;
        public IList<string> BaseTypes;
        public IList<string> ConstructorArgs;
        public IDictionary<string, string> PropertyInjections;
        public string Namespace { get; private set; }

        public CSharpFile(string className)
        {
            if (String.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException("className");
            }
            _className = className;
        }

        public void ParseFile(FileInfo file)
        {
            string fileContents;
            using (var re = new StreamReader(file.FullName))
            {
                fileContents = re.ReadToEnd();
            }

            Namespace = ParseNamespace(fileContents);
            BaseTypes = ParseBaseTypes(fileContents);
            ConstructorArgs = ParseConstructorArgs(fileContents);
            PropertyInjections = ParsePropertyInjections(fileContents);
        }

        private string ParseNamespace(string fileContents)
        {
            const string TagName = "namespace";
            const string pattern = "namespace (?<" + TagName + ">[a-zA-Z0-9<>.]+)";
            var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled);

            var match = regex.Matches(fileContents);

            if (match.Count == 0 || !match[0].Success || !match[0].Groups[TagName].Success)
            {
                throw new NotSupportedException("Cannot detect namespace");
            }

            return match[0].Groups[TagName].Captures[0].Value;
        }

        private IList<string> ParseBaseTypes(string fileContents)
        {
            const string tagName = "baseType";
            string regexPattern = "class\\s+?" + _className + "(?<" + tagName + ">(.|\\s)*?)(\\{|where)";

            var regex = new Regex(regexPattern, RegexOptions.Multiline | RegexOptions.Compiled);

            var match = regex.Match(fileContents);

            if (!match.Success)
            {
                return new List<string>();
            }

            var contents = match.Groups[tagName].Value;
            var firstColonIndex = contents.IndexOf(':');

            if (firstColonIndex < 0)
            {
                return new List<string>();
            }

            contents = contents.Substring(firstColonIndex + 1, contents.Length - firstColonIndex - 1);
            contents = contents.Replace("\r", String.Empty);
            contents = contents.Replace("\n", String.Empty);
            contents = contents.Replace("\t", String.Empty);
            contents = contents.Replace(" ", String.Empty);
            var result = contents.Split(',').ToList();

            return result;
        }

        private IDictionary<string, string> ParsePropertyInjections(string fileContents)
        {
            const string PropertyDefinition = "PropertyDefinition";
            const string RegexPattern =
                "\\[Inject\\]\\s+(public|internal)\\s?(?<" + PropertyDefinition + ">[a-zA-Z0-9<>,]+\\s+[a-zA-Z0-9]+)\\s+\\{";

            var regex = new Regex(RegexPattern, RegexOptions.Singleline | RegexOptions.Compiled);

            var matches = regex.Matches(fileContents);

            if (matches.Count == 0 || !matches[0].Success || !matches[0].Groups[PropertyDefinition].Success)
            {
                return new Dictionary<string, string>();
            }

            var injections = new Dictionary<string, string>();

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                string injection = match.Groups[PropertyDefinition].Value;
                var pair = Regex.Split(injection, "\\s+");
                if (pair.Length != 2)
                {
                    throw new InvalidOperationException("Something wrong here");
                }

                injections[pair[0]] = pair[1];
            }

            return injections;
        }

        private IList<string> ParseConstructorArgs(string fileContents)
        {
            const string TagName = "interface";
            string regexPattern = "(public|internal)\\s?" + _className + "\\s?\\((?:\\s*(?<" + TagName +
                                  ">\\w+)\\s*\\w+\\s*,?\\s?)*\\)";

            var regex = new Regex(regexPattern, RegexOptions.Singleline | RegexOptions.Compiled);

            var match = regex.Matches(fileContents);

            if (match.Count == 0 || !match[0].Success || !match[0].Groups[TagName].Success)
            {
                return new List<string>();
            }

            var interfaces = match[0].Groups[TagName];

            return (from object cap in interfaces.Captures select cap.ToString()).ToList();
        }
    }
}