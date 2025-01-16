using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using NLog;
using System.Xml.XPath;
using CommandLine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata;
using System.Linq.Expressions; // Add this line

namespace X4XmlDiffAndPatch
{
    class XMLDiff
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {

            ConfigureLogging(opts.LogToFile);

            var originalXmlPath = opts.OriginalXml;
            var modifiedXmlPath = opts.ModifiedXml;
            var diffXmlPath = opts.DiffXml;
            var diffXsdPath = opts.Xsd;
            var pathOptions = new PathOptions
            {
                OnlyFullPath = opts.OnlyFullPath,
                UseAllAttributes = opts.UseAllAttributes
            };

            bool originalIsDir = Directory.Exists(originalXmlPath);
            bool modifiedIsDir = Directory.Exists(modifiedXmlPath);
            bool diffIsDir = Directory.Exists(diffXmlPath);

            XmlReaderSettings diffReaderSettings =  CreateXmlReaderSettings(diffXsdPath!);
            if (originalIsDir && modifiedIsDir && diffIsDir)
            {
                Logger.Info("Processing directories recursively.");
                if (originalXmlPath != null && modifiedXmlPath != null && diffXmlPath != null)
                {
                    ProcessDirectories(originalXmlPath, modifiedXmlPath, diffXmlPath, diffReaderSettings, pathOptions);
                }
                else
                {
                    Logger.Error("One or more required paths are null.");
                    Environment.Exit(1);
                }
            }
            else if (!originalIsDir && !modifiedIsDir)
            {
                Logger.Info("Processing single trio of files.");
                ProcessSingleFile(originalXmlPath!, modifiedXmlPath!, diffXmlPath!, diffReaderSettings, pathOptions);
            }
            else
            {
                Logger.Error("Mismatch in input paths. Original and modified paths should both be directories or both be files.");
                Environment.Exit(1);
            }
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            // Handle errors
            foreach (var err in errs)
            {
                Logger.Error($"Error parsing arguments: {err}");
            }
            Environment.Exit(1);
        }

        #region Argument Parsing

        public class Options
        {
            private string? originalXml;
            private string? modifiedXml;
            private string? diffXml;
            private string? xsd;
            private bool logToFile;
            private bool onlyFullPath;
            private bool useAllAttributes;

            [Option('o', "original_xml", Required = true, HelpText = "Path to the original XML file or directory.")]
            public string? OriginalXml
            {
                get => originalXml;
                set => originalXml = value?.Trim();
            }

            [Option('m', "modified_xml", Required = true, HelpText = "Path to the modified XML file or directory.")]
            public string? ModifiedXml
            {
                get => modifiedXml;
                set => modifiedXml = value?.Trim();
            }

            [Option('d', "diff_xml", Required = true, HelpText = "Path for the diff XML file or directory.")]
            public string? DiffXml
            {
                get => diffXml;
                set => diffXml = value?.Trim();
            }

            [Option('x', "xsd", Required = false, HelpText = "Path to the diff.xsd schema file.")]
            public string? Xsd
            {
                get => xsd;
                set => xsd = value?.Trim();
            }

            [Option('l', "log-to-file", Required = false, HelpText = "Log to a file.", Default = false)]
            public bool LogToFile {
                get => logToFile;
                set => logToFile = value;
            }

            [Option("only-full-path", Required = false, HelpText = "Generate only full path.", Default = false)]
            public bool OnlyFullPath
            {
                get => onlyFullPath;
                set => onlyFullPath = value;
            }

            [Option("use-all-attributes", Required = false, HelpText = "Use all attributes in XPath.", Default = false)]
            public bool UseAllAttributes
            {
                get => useAllAttributes;
                set => useAllAttributes = value;
            }

        }

        #endregion

        #region Logging Configuration

        private static void ConfigureLogging(bool logToFile)

        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets
            var logConsole = new NLog.Targets.ConsoleTarget("logConsole");
            if (logToFile)
            {
                var logFile = new NLog.Targets.FileTarget("logFile")
                {
                    FileName = "${basedir}/${processname}.log",
                    Layout = "${longdate} ${level} ${message} ${exception}",
                    KeepFileOpen = true,
                    DeleteOldFileOnStartup = true, // Overwrite the log file on each run
                    ArchiveAboveSize = 0,
                    ConcurrentWrites = true
                };
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);
            }
            logConsole.Layout = "${longdate} ${level} ${message} ${exception}";

            // Rules
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);

            // Apply config
            NLog.LogManager.Configuration = config;
        }

        #endregion

        #region Processing

        private static void ProcessSingleFile(string originalXmlPath, string modifiedXmlPath, string diffXmlPath, XmlReaderSettings diffReaderSettings, PathOptions pathOptions)
        {
            if (!File.Exists(originalXmlPath))
            {
                Logger.Error($"Original XML file does not exist: {originalXmlPath}");
                return;
            }

            if (!File.Exists(modifiedXmlPath))
            {
                Logger.Error($"Modified XML file does not exist: {modifiedXmlPath}");
                return;
            }

            // If diffXmlPath is a directory, append the original filename
            if (Directory.Exists(diffXmlPath))
            {
                string originalFileName = Path.GetFileName(originalXmlPath);
                diffXmlPath = Path.Combine(diffXmlPath, originalFileName);
                Logger.Info($"Diff XML will be saved as: {diffXmlPath}");
            }
            else
            {
                // Ensure the output directory exists
                string? diffXmlDir = Path.GetDirectoryName(diffXmlPath);
                if (string.IsNullOrEmpty(diffXmlDir))
                {
                    Logger.Error("Failed to determine the directory for diffXmlPath.");
                    return;
                }

                if (!Directory.Exists(diffXmlDir))
                {
                    try
                    {
                        Directory.CreateDirectory(diffXmlDir);
                        Logger.Info($"Created output directory: {diffXmlDir}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to create output directory '{diffXmlDir}': {e.Message}");
                        return;
                    }
                }
            }

            try
            {
                XDocument originalDoc = XDocument.Load(originalXmlPath);
                Logger.Info($"Parsed original XML: {originalXmlPath}");

                int indent = DetectIndentation(originalXmlPath);
                Logger.Info($"Detected indentation: {indent}");

                XDocument modifiedDoc = XDocument.Load(modifiedXmlPath);
                Logger.Info($"Parsed modified XML: {modifiedXmlPath}");

                if (File.Exists(diffXmlPath))
                {
                    File.Delete(diffXmlPath);
                    Logger.Debug($"Deleted diff file at {diffXmlPath}.");
                }

                XElement diffRoot = GenerateDiff(originalDoc, modifiedDoc, pathOptions);

                if (!diffRoot.HasElements)
                {
                    Logger.Info("No differences found. Diff file will not be created.");
                    return;
                }

                XDocument diffDoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), diffRoot);


                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = new string(' ', indent)
                };
                using (var writer = XmlWriter.Create(diffXmlPath, settings))
                {
                    diffDoc.Save(writer);
                }

                if (diffReaderSettings != null)
                {
                    Logger.Info($"Diff XML written to {diffXmlPath} and will be validated");
                    using (XmlReader reader = XmlReader.Create(diffXmlPath, diffReaderSettings))
                    {
                        try {
                            while (reader.Read()) {}
                        } catch (XmlSchemaValidationException ex) {
                            Logger.Error($"Validation failed: {ex.Message}");
                            if (File.Exists(diffXmlPath))
                            {
                                File.Delete(diffXmlPath);
                                Logger.Debug($"Deleted diff file at {diffXmlPath}.");
                            }
                            return;
                        }

                    }
                    Logger.Info($"Validation successful: {diffXmlPath} is valid against diff.xsd");
                }
                Logger.Info($"Diff XML written to {diffXmlPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing files: {ex.Message}");
            }
        }

        private static void ProcessDirectories(string originalDir, string modifiedDir, string diffDir, XmlReaderSettings diffReaderSettings, PathOptions pathOptions)
        {
            foreach (var modifiedFilePath in Directory.EnumerateFiles(modifiedDir, "*.xml", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(modifiedDir, modifiedFilePath);
                string originalFilePath = Path.Combine(originalDir, relativePath);
                string diffFilePath = Path.Combine(diffDir, relativePath);

                if (!File.Exists(modifiedFilePath))
                {
                    Logger.Warn($"Modified file does not exist: {modifiedFilePath}. Skipping.");
                    continue;
                }

                string? diffFileDir = Path.GetDirectoryName(diffFilePath);
                if (!Directory.Exists(diffFileDir))
                {
                    try
                    {
                        Directory.CreateDirectory(diffFileDir!);
                        Logger.Info($"Created directory: {diffFileDir}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to create directory '{diffFileDir}': {e.Message}");
                        continue;
                    }
                }

                ProcessSingleFile(originalFilePath, modifiedFilePath, diffFilePath, diffReaderSettings, pathOptions);
            }
        }

        #endregion

        #region Indentation Detection

        private static int DetectIndentation(string xmlPath)
        {
            var indentationLevels = new HashSet<string>();
            var indentPattern = new Regex(@"^(\s+)<");

            foreach (var line in File.ReadLines(xmlPath))
            {
                var match = indentPattern.Match(line);
                if (match.Success)
                {
                    indentationLevels.Add(match.Groups[1].Value);
                }
            }

            if (indentationLevels.Count == 0)
                return 4;

            var sortedIndents = indentationLevels.OrderBy(s => s.Length).ToList();

            var indentLengths = sortedIndents.Where(s => s.Length > 0).Select(s => s.Length).OrderBy(n => n).ToList();

            var differences = indentLengths.Skip(1).Select((len, idx) => len - indentLengths[idx]).Where(diff => diff > 0).ToList();

            int perLevelIndentLen = differences.Any() ? differences.Min() : (sortedIndents[0].Length);

            return perLevelIndentLen;
        }

        #endregion

        #region Diff Generation

        private static XElement GenerateDiff(XDocument original, XDocument modified, PathOptions pathOptions)
        {
            XElement diffRoot = new XElement("diff");
            if (original.Root == null || modified.Root == null)
            {
                Logger.Error("Original or modified XML does not have a root element.");
                return diffRoot;
            }
            CompareElements(original, modified, diffRoot, pathOptions);
            return diffRoot;
        }

        private static bool CompareElements(XDocument original, XDocument modified, XElement diffRoot, PathOptions pathOptions, XElement ?originalElem = null, XElement ?modifiedElem = null, bool checkOnly = false)
        {
            if (originalElem != null && modifiedElem != null)
            {
                if (originalElem.Name != modifiedElem.Name)
                {
                    // Process can be there only in case of changes detection, not for the real diff generation
                    Logger.Error($"Element names do not match: {originalElem.Name} vs {modifiedElem.Name}");
                    return true;
                }

                // Compare text
                string originalText = (originalElem.Value ?? "").Trim();
                string modifiedText = (modifiedElem.Value ?? "").Trim();

                Logger.Debug($"Comparing text in element '{originalElem.Name}': '{originalText}' vs '{modifiedText}'");
                if (originalText != modifiedText)
                {
                    string sel = GenerateXPath(originalElem, pathOptions);
                    if (!string.IsNullOrEmpty(modifiedText))
                    {
                        if (checkOnly) {
                            return true;
                        }
                        XElement replaceOp = new XElement("replace",
                            new XAttribute("sel", sel),
                            modifiedText
                        );
                        diffRoot.Add(replaceOp);
                        Logger.Debug($"Replaced text in element '{originalElem.Name}' from '{originalText}' to '{modifiedText}'.");
                    }
                    else
                    {
                        if (checkOnly) {
                            return true;
                        }
                        XElement removeOp = new XElement("remove",
                            new XAttribute("sel", $"{sel}/text()")
                        );
                        diffRoot.Add(removeOp);
                        Logger.Debug($"Removed text from element '{originalElem.Name}'.");
                    }
                }
            }

            originalElem = originalElem ?? original.Root;
            modifiedElem = modifiedElem ?? modified.Root;
            if (originalElem == null || modifiedElem == null)
            {
                Logger.Error("Original or modified element is null.");
                return true;
            }
            // Compare children
            var originalChildren = originalElem.Elements().ToList();
            var modifiedChildren = modifiedElem.Elements().ToList();

            if (checkOnly && originalChildren.Count != modifiedChildren.Count) {
                return true;
            }

            int i = 0, j = 0;
            while (i < originalChildren.Count && j < modifiedChildren.Count)
            {
                var originalChild = originalChildren[i];
                var modifiedChild = modifiedChildren[j];

                bool matchedEnough = false;
                if (originalChild.Name == modifiedChild.Name)
                {
                    var originalAttributes = originalChild.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);
                    var modifiedAttributes = modifiedChild.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);

                    int differencesInAttributesCount = 0;

                    XElement? savedOp = null;

                    matchedEnough = true;

                    foreach (var attr in modifiedAttributes)
                    {
                        Logger.Debug($"Checking attribute '{attr.Key}' in original element '{originalAttributes.Keys}'.");
                        if (!originalAttributes.ContainsKey(attr.Key))
                        {
                            Logger.Debug($"Original attributes does not contain key '{attr.Key}'.");
                            differencesInAttributesCount++;
                            if (differencesInAttributesCount > 1) {
                                matchedEnough = false;
                                break;
                            }
                            string sel = GenerateXPath(originalChild, pathOptions);
                            savedOp = new XElement("add",
                                new XAttribute("sel", sel),
                                new XAttribute("type", $"@{attr.Key}"),
                                attr.Value
                            );
                            Logger.Debug($"Found added attribute '{attr.Key}' with value '{attr.Value}' to element '{originalChild.Name}'.");
                            break;
                        }
                        else if (originalAttributes[attr.Key] != attr.Value)
                        {
                            Logger.Debug($"Original attributes value '{originalAttributes[attr.Key]}' does not match with modified attributes value '{attr.Value}'.");
                            differencesInAttributesCount++;
                            if (differencesInAttributesCount > 1) {
                                matchedEnough = false;
                                break;
                            }
                            string sel = $"{GenerateXPath(originalChild, pathOptions)}/@{attr.Key}";
                            savedOp = new XElement("replace",
                                new XAttribute("sel", sel),
                                attr.Value
                            );
                            /*! To add the children checks and next elements on this level. Only checks, without generation of the diff
                             */
                            Logger.Debug($"Found replaced attribute '{attr.Key}' value from '{originalAttributes[attr.Key]}' to '{attr.Value}' in element '{originalChild.Name}'.");
                            break;
                        }
                    }
                    if (matchedEnough) {
                        foreach (var attr in originalAttributes.Keys)
                        {
                            if (!modifiedAttributes.ContainsKey(attr))
                            {
                                Logger.Debug($"Modified attributes does not contain key '{attr}'.");
                                if (checkOnly) {
                                    return true;
                                }
                                matchedEnough = false;
                                break;
                            }
                        }
                    }
                    if (matchedEnough && differencesInAttributesCount == 1) {
                        if (checkOnly) {
                            return true;
                        }
                        matchedEnough = false;
                        if (! CompareElements(original, modified, diffRoot, pathOptions, originalChild, modifiedChild, true)) {
                            bool nextMatched = true;
                            if ( i + 1 < originalChildren.Count && j + 1 < modifiedChildren.Count) {
                                XElement originalTemp = new XElement("temp");
                                originalTemp.Add(originalChildren[i + 1]);
                                XElement modifiedTemp = new XElement("temp");
                                modifiedTemp.Add(modifiedChildren[j + 1]);
                                nextMatched = ! CompareElements(original, modified, diffRoot, pathOptions, originalTemp, modifiedTemp, true);
                            }
                            if (nextMatched) {
                                if (savedOp != null) {
                                    diffRoot.Add(savedOp);
                                    Logger.Debug($"Added the saved operation to the diff.");
                                }
                                matchedEnough = true;
                            }
                        }
                    }
                    Logger.Debug($"Matched enough: {matchedEnough}, i: {i}, j: {j}");
                    if (matchedEnough) {
                        if (CompareElements(original, modified, diffRoot, pathOptions, originalChild, modifiedChild, checkOnly))
                        {
                            if (checkOnly) {
                                return true;
                            }
                        }
                        i++;
                        j++;
                    }
                }
                if (!matchedEnough)
                {
                    if (checkOnly) {
                        return true;
                    }
                    bool foundMatch = false;
                    for (int k = j + 1; k < modifiedChildren.Count; k++)
                    {
                        var nextModifiedChild = modifiedChildren[k];
                        if (originalChild.Name == nextModifiedChild.Name &&
                            originalChild.Attributes().All(attr => nextModifiedChild.Attribute(attr.Name)?.Value == attr.Value))
                        {
                            XElement addOp = new XElement("add",
                                new XAttribute("sel", GenerateXPath(originalChild, pathOptions)),
                                new XAttribute("pos", "before")
                            );
                            for (int l = j; l < k; l++)
                            {
                                var addedChild = modifiedChildren[l];
                                addOp.Add(addedChild);
                                Logger.Debug($"Added element '{addedChild.Name}' to parent '{originalElem.Name}'.");
                            }
                            diffRoot.Add(addOp);
                            j = k;
                            foundMatch = true;
                            break;
                        }
                    }

                    if (!foundMatch)
                    {
                        string sel = GenerateXPath(originalChild, pathOptions);
                        XElement removeOp = new XElement("remove",
                            new XAttribute("sel", sel)
                        );
                        diffRoot.Add(removeOp);
                        Logger.Debug($"Removed element '{originalChild.Name}' from parent '{originalElem.Name}'.");
                        i++;
                    }
                }
            }

            while (i < originalChildren.Count)
            {
                var originalChild = originalChildren[i];
                string sel = GenerateXPath(originalChild, pathOptions);
                XElement removeOp = new XElement("remove",
                    new XAttribute("sel", sel)
                );
                diffRoot.Add(removeOp);
                Logger.Debug($"Removed element '{originalChild.Name}' from parent '{originalElem.Name}'.");
                i++;
            }

            if (j + 1 <= modifiedChildren.Count)
            {
                XElement? originalLast = originalChildren.LastOrDefault();
                XElement addOp;
                if (originalLast != null) {
                    addOp = new XElement("add",
                        new XAttribute("sel", GenerateXPath(originalLast, pathOptions)),
                        new XAttribute("pos", "after")
                    );
                }
                else {
                    addOp = new XElement("add",
                        new XAttribute("sel", GenerateXPath(originalElem, pathOptions))
                    );
                }
                while (j < modifiedChildren.Count)
                {
                    var addedChild = modifiedChildren[j];
                    addOp.Add(addedChild);
                    Logger.Debug($"Added element '{addedChild.Name}' to parent '{originalElem.Name}'.");
                    j++;
                }
                diffRoot.Add(addOp);
            }
            return false;
        }

        private static (string step, string patchForParent) GetElementPathStep(XElement element, XElement parent, XDocument? doc, PathOptions pathOptions, string patchForParent = "")
        {
            if (element == null)
                return (string.Empty, string.Empty);
            if (parent == null)
                return (string.Empty, string.Empty);
            string step = "";
            if (patchForParent == "")
                patchForParent = $"{element.Name.LocalName}";
            var attributes = element.Attributes().ToList();
            if (attributes.Count > 0)
            {
                if (pathOptions.UseAllAttributes)
                {
                    string xpath = $"{patchForParent}";
                    foreach (var attr in attributes)
                    {
                        string attrValue = attr.Value.Replace("\"", "&quot;");
                        xpath += $"[@{attr.Name.LocalName}=\"{attrValue}\"]";
                    }
                    var matches = parent.XPathSelectElements(xpath);
                    if (matches.Count() == 1 && matches.First() == element)
                    {
                        step = xpath;
                        if (doc != null)
                        {
                            var doc_matches = doc.XPathSelectElements($"//{xpath}");
                            if (doc_matches.Count() == 1)
                                return ($"//{xpath}", patchForParent);
                        }
                    }
                }
                foreach (var attr in attributes)
                {
                    string attrValue = attr.Value.Replace("\"", "&quot;");
                    string xpath = $"{patchForParent}[@{attr.Name.LocalName}=\"{attrValue}\"]";
                    var matches = parent.XPathSelectElements(xpath);
                    if (matches.Count() == 1 && matches.First() == element)
                    {
                        if (step == "")
                            step = xpath;
                        if (doc != null)
                        {
                            var doc_matches = doc.XPathSelectElements($"//{xpath}");
                            if (doc_matches.Count() == 1)
                                return ($"//{xpath}", patchForParent);
                        }
                    }
                }
            }
            else if (doc != null)
            {
                var doc_matches = doc.XPathSelectElements($"//{patchForParent}");
                if (doc_matches.Count() == 1)
                {
                    return ($"//{patchForParent}", patchForParent);
                }
            }
            return (step, patchForParent);
        }
        private static string GenerateXPath(XElement element, PathOptions pathOptions)
        {
            if (element == null)
                return string.Empty;
            XElement? root = element.Document?.Root;
            if (root == null)
                return string.Empty;
            XDocument? doc = null;
            if (! pathOptions.OnlyFullPath)
            {
                doc = element.Document;
            }
            var path = new System.Text.StringBuilder();
            XElement? current = element;
            while (current != null)
            {
                XElement? parent = current.Parent;
                if (parent != null) {
                    (string step, string patchForParent) = GetElementPathStep(current, parent, doc, pathOptions);
                    if (step.StartsWith("//"))
                    {
                        return step;
                    }
                    if (step == "") {
                        var siblings = parent.Elements().ToList();
                        string xpathWithSiblings = "";
                        int index = siblings.IndexOf(current);
                        if (index + 1 < siblings.Count) {
                            XElement sibling = siblings[index + 1];
                            (step, xpathWithSiblings) =  GetElementPathStep(sibling, parent, doc, pathOptions, $"{patchForParent}/following-sibling::{sibling.Name.LocalName}");
                        }
                        if (step.StartsWith("//"))
                        {
                            return step;
                        } else if (step == "")
                        {
                            if (siblings.Count == 1)
                            {
                                step = $"{patchForParent}";
                            }
                            else
                            {
                                step = $"{patchForParent}[{index + 1}]";
                            }
                        }
                    }
                    path.Insert(0, "/" + step);
                } else if (current == root)
                {
                    path.Insert(0, "/" + root.Name.LocalName);
                }
                current = parent;
            }
            if (path.Length == 0)
                path.Append("/" + root.Name.LocalName);
            return path.ToString();
        }

        #endregion

        #region Diff Validation

        private static bool ValidateXsdPath(string? xsdPath)
        {
            if (xsdPath == null || !File.Exists(xsdPath))
            {
                Logger.Warn($"diff.xsd file does not exist: {xsdPath}");
                return false;
            }

            Logger.Info($"Using diff.xsd path: {xsdPath}");
            return true;
        }

        private static XmlReaderSettings CreateXmlReaderSettings(string xsdPath)
        {
            XmlReaderSettings settings = new XmlReaderSettings();

            if (ValidateXsdPath(xsdPath))
            {
                try
                {
                    XmlReaderSettings xsdSettings = new XmlReaderSettings{
                        DtdProcessing = DtdProcessing.Parse, // Enable DTD processing
                        ValidationType = ValidationType.Schema // Optional, for validation during reading
                    };
                    using (XmlReader reader = XmlReader.Create(xsdPath, xsdSettings))
                    {
                        XmlSchemaSet schemaSet = new XmlSchemaSet();

                        // Add the schema using the XmlReader
                        schemaSet.Add("", reader);
                        settings.DtdProcessing = DtdProcessing.Parse; // Enable DTD processing
                        settings.ValidationType = ValidationType.Schema; // Optional, for validation during reading
                        settings.Schemas = schemaSet;
                    }
                }
                catch (XmlSchemaException ex)
                {
                    Logger.Error($"Schema Exception: {ex.Message}");
                    Logger.Error($"Line: {ex.LineNumber}, Position: {ex.LinePosition}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"General Exception: {ex.Message}");
                }
            }
            return settings;
        }


        #endregion
    }

    public class PathOptions
    {
        public bool OnlyFullPath { get; set; }
        public bool UseAllAttributes { get; set; }
    }
}