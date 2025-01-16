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

namespace X4XmlDiffAndPatch
{
    class XMLPatch
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
            var diffXmlPath = opts.DiffXml;
            var outputXmlPath = opts.OutputXml;
            var diffXsdPath = opts.Xsd;

            bool originalIsDir = Directory.Exists(originalXmlPath);
            bool diffIsDir = Directory.Exists(diffXmlPath);
            bool outputIsDir = Directory.Exists(outputXmlPath);

            XmlReaderSettings diffReaderSettings = CreateXmlReaderSettings(diffXsdPath!);
            if (originalIsDir && diffIsDir && outputIsDir)
            {
                Logger.Info("Processing directories recursively.");
                if (originalXmlPath != null && diffXmlPath != null && outputXmlPath != null)
                {
                    ProcessDirectories(originalXmlPath, diffXmlPath, outputXmlPath, diffReaderSettings);
                }
                else
                {
                    Logger.Error("One or more required paths are null.");
                    Environment.Exit(1);
                }
            }
            else if (!originalIsDir && !diffIsDir)
            {
                Logger.Info("Processing single trio of files.");
                ProcessSingleFile(originalXmlPath!, diffXmlPath!, outputXmlPath!, diffReaderSettings);
            }
            else
            {
                Logger.Error("Mismatch in input paths. Original, Diff, and Output paths should all be directories or all be files.");
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
            private string? diffXml;
            private string? outputXml;
            private string? xsd;
            private bool logToFile;

            [Option('o', "original_xml", Required = true, HelpText = "Path to the original XML file or directory.")]
            public string? OriginalXml
            {
                get => originalXml;
                set => originalXml = value?.Trim();
            }

            [Option('d', "diff_xml", Required = true, HelpText = "Path to the diff XML file or directory.")]
            public string? DiffXml
            {
                get => diffXml;
                set => diffXml = value?.Trim();
            }

            [Option('u', "output_xml", Required = true, HelpText = "Path for the output XML file or directory.")]
            public string? OutputXml
            {
                get => outputXml;
                set => outputXml = value?.Trim();
            }

            [Option('x', "xsd", Required = false, HelpText = "Path to the diff.xsd schema file.")]
            public string? Xsd
            {
                get => xsd;
                set => xsd = value?.Trim();
            }

            [Option('l', "log-to-file", Required = false, HelpText = "Log to a file.", Default = false)]
            public bool LogToFile
            {
                get => logToFile;
                set => logToFile = value;
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

        private static void ProcessSingleFile(string originalXmlPath, string diffXmlPath, string outputXmlPath, XmlReaderSettings diffReaderSettings)
        {
            if (!File.Exists(originalXmlPath))
            {
                Logger.Error($"Original XML file does not exist: {originalXmlPath}");
                return;
            }

            if (!File.Exists(diffXmlPath))
            {
                Logger.Error($"Diff XML file does not exist: {diffXmlPath}");
                return;
            }

            // If outputXmlPath is a directory, append the original filename
            if (Directory.Exists(outputXmlPath))
            {
                string originalFileName = Path.GetFileName(originalXmlPath);
                outputXmlPath = Path.Combine(outputXmlPath, originalFileName);
                Logger.Info($"Output XML will be saved as: {outputXmlPath}");
            }
            else
            {
                // Ensure the output directory exists
                string? outputXmlDir = Path.GetDirectoryName(outputXmlPath);
                if (string.IsNullOrEmpty(outputXmlDir))
                {
                    Logger.Error("Failed to determine the directory for outputXmlPath.");
                    return;
                }

                if (!Directory.Exists(outputXmlDir))
                {
                    try
                    {
                        Directory.CreateDirectory(outputXmlDir);
                        Logger.Info($"Created output directory: {outputXmlDir}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to create output directory '{outputXmlDir}': {e.Message}");
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

                XDocument diffDoc;
                if (diffReaderSettings != null)
                {
                    using (XmlReader reader = XmlReader.Create(diffXmlPath, diffReaderSettings))
                    {
                        try {
                            diffDoc = XDocument.Load(reader);
                            Logger.Info($"Parsed diff XML: {diffXmlPath}");
                        } catch (XmlSchemaValidationException ex) {
                            Logger.Error($"Validation failed: {ex.Message}");
                            return;
                        }

                    }
                    Logger.Info($"Validation successful: {diffXmlPath} is valid against diff.xsd");
                }
                else
                {
                    Logger.Warn("Diff XML validation is disabled.");
                    diffDoc = XDocument.Load(diffXmlPath);
                }

                if (diffDoc == null)
                {
                    Logger.Error("Diff XML root is null.");
                    return;
                }

                XElement diffRoot = diffDoc.Root ?? throw new InvalidOperationException("diffDoc.Root is null");

                XElement originalRoot = originalDoc.Root ?? throw new InvalidOperationException("originalDoc.Root is null");

                foreach (var operation in diffRoot.Elements())
                {
                    switch (operation.Name.LocalName)
                    {
                        case "add":
                            ApplyAdd(operation, originalRoot);
                            break;
                        case "replace":
                            ApplyReplace(operation, originalRoot);
                            break;
                        case "remove":
                            ApplyRemove(operation, originalRoot);
                            break;
                        default:
                            Logger.Warn($"Unknown operation: {operation.Name}. Skipping.");
                            break;
                    }
                }

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = new string(' ', indent)
                };
                using (var writer = XmlWriter.Create(outputXmlPath, settings))
                {
                    originalDoc.Save(writer);
                }

                Logger.Info($"Patched XML successfully written to '{outputXmlPath}'.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing files: {ex.Message}");
            }
        }

        private static void ProcessDirectories(string originalDir, string diffDir, string outputDir, XmlReaderSettings diffReaderSettings)
        {
            var diffFiles = Directory.EnumerateFiles(diffDir, "*.xml", SearchOption.AllDirectories);
            foreach (var diffFilePath in diffFiles)
            {
                string relativePath = Path.GetRelativePath(diffDir, diffFilePath);
                string originalFilePath = Path.Combine(originalDir, relativePath);
                string outputFilePath = Path.Combine(outputDir, relativePath);

                if (!File.Exists(originalFilePath))
                {
                    Logger.Warn($"Original file does not exist for diff file '{diffFilePath}'. Skipping.");
                    continue;
                }

                ProcessSingleFile(originalFilePath, diffFilePath, outputFilePath, diffReaderSettings);
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
                return 4; // Default to four spaces

            var sortedIndents = indentationLevels.OrderBy(s => s.Length).ToList();
            var indentLengths = sortedIndents.Where(s => s.Length > 0).Select(s => s.Length).OrderBy(n => n).ToList();
            var differences = indentLengths.Skip(1).Select((len, idx) => len - indentLengths[idx]).Where(diff => diff > 0).ToList();
            int perLevelIndentLen = differences.Any() ? differences.Min() : sortedIndents[0].Length;
            return perLevelIndentLen;
        }

        #endregion

        #region Apply Operations

        private static void ApplyAdd(XElement addElement, XElement originalRoot)
        {
            string sel = addElement.Attribute("sel")?.Value ?? throw new ArgumentException("The 'sel' attribute is required.");
            string? type = addElement.Attribute("type")?.Value;
            string? pos = addElement.Attribute("pos")?.Value;
            if (pos == null && type == null){
                pos = "append";
            }

            Logger.Debug($"Applying add operation: {sel} at {pos!}");

            var targetElements = originalRoot.XPathSelectElements(sel);
            if (targetElements == null || !targetElements.Any())
            {
                Logger.Warn($"No nodes found for add selector: {sel}");
                return;
            }
            if (targetElements.Count() > 1)
            {
                Logger.Warn($"Multiple nodes found for add selector: {sel}. Skipping.");
                return;
            }
            var targetElement = targetElements.First();
            if (pos != null) {
                var newElements = addElement.Elements();
                foreach (var newElem in newElements)
                {
                    XElement cloned = new XElement(newElem);
                    if (pos == "before")
                    {
                        targetElement.AddBeforeSelf(cloned);
                        Logger.Info($"Added new element '{cloned.Name}' before '{targetElement.Name}' in '{targetElement.Parent?.Name}'.");
                    }
                    else if (pos == "after")
                    {
                        targetElement.AddAfterSelf(cloned);
                        Logger.Info($"Added new element '{cloned.Name}' after '{targetElement.Name}' in '{targetElement.Parent?.Name}'.");
                    }
                    else if (pos == "prepend")
                    {
                        targetElement.AddFirst(cloned);
                        Logger.Info($"Prepended new element '{cloned.Name}' to '{targetElement.Name}'.");
                    }
                    else if (pos == "append")
                    {
                        targetElement.Add(cloned);
                        Logger.Info($"Appended new element '{cloned.Name}' to '{targetElement.Name}'.");
                    }
                    else
                    {
                        Logger.Warn($"Unknown position: {pos}. Skipping insertion.");
                    }
                }
            }
            else if (type != null)
            {
                if (type.StartsWith('@') && type.Length > 1) {
                    type = type.Substring(1);
                    if (addElement.Value == null)
                    {
                        Logger.Warn("Attribute add operation missing value.");
                        return;
                    }
                    targetElement.SetAttributeValue(type, addElement.Value);
                    Logger.Info($"Added attribute '{type}' with value '{addElement.Value}' to '{targetElement.Name}'.");
                }
            }

        }

        private static void ApplyReplace(XElement replaceElement, XElement originalRoot)
        {
            string? sel = replaceElement.Attribute("sel")?.Value;
            if (sel == null)
            {
                Logger.Warn("Replace operation missing 'sel' attribute.");
                return;
            }

            var targetNodes = originalRoot.XPathEvaluate(sel) as IEnumerable<object>;
            if (targetNodes == null || !targetNodes.Any())
            {
                Logger.Warn($"No nodes found for replace selector: {sel}");
                return;
            }

            foreach (var targetObj in targetNodes)
            {
                if (targetObj is XElement target)
                {
                    var newContent = replaceElement.Value;
                    if (!string.IsNullOrEmpty(newContent))
                    {
                        target.Value = newContent;
                        Logger.Debug($"Replaced text of element '{target.Name}' with '{newContent}'.");
                    }

                    var newElement = replaceElement.Element("new");
                    if (newElement != null)
                    {
                        XElement replacement = new XElement(newElement);
                        target.ReplaceWith(replacement);
                        Logger.Info($"Replaced element '{target.Name}' with '{replacement.Name}'.");
                    }
                }
                else if (targetObj is XText textNode)
                {
                    textNode.Value = replaceElement.Value;
                    Logger.Debug("Replaced text node.");
                }
                else if (targetObj is XAttribute attr)
                {
                    attr.Value = replaceElement.Value;
                    Logger.Debug($"Replaced attribute '{attr.Name}' with '{replaceElement.Value}'.");
                }
            }
        }

        private static void ApplyRemove(XElement removeElement, XElement originalRoot)
        {
            string? sel = removeElement.Attribute("sel")?.Value;
            if (sel == null)
            {
                Logger.Warn("Remove operation missing 'sel' attribute.");
                return;
            }

            var targetNodes = originalRoot.XPathEvaluate(sel) as IEnumerable<object>;
            if (targetNodes == null || !targetNodes.Any())
            {
                Logger.Warn($"No nodes found for remove selector: {sel}");
                return;
            }

            foreach (var targetObj in targetNodes)
            {
                if (targetObj is XElement target)
                {
                    XElement? parent = target.Parent;
                    if (parent == null)
                    {
                        Logger.Warn($"Element '{target.Name}' has no parent. Cannot remove.");
                        continue;
                    }
                    target.Remove();
                    Logger.Debug($"Removed element '{target.Name}' from '{parent.Name}'.");
                }
                else if (targetObj is XAttribute attr)
                {
                    XElement? parent = attr.Parent;
                    if (parent == null)
                    {
                        Logger.Warn($"Attribute '{attr.Name}' has no parent. Cannot remove.");
                        continue;
                    }
                    attr.Remove();
                    Logger.Debug($"Removed attribute '{attr.Name}' from '{parent.Name}'.");
                }
                else if (targetObj is XText textNode)
                {
                    textNode.Remove();
                    Logger.Debug("Removed text node.");
                }
            }
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
                    XmlReaderSettings xsdSettings = new XmlReaderSettings
                    {
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

        private static void ValidateDiffXml(string diffXmlPath, string? xsdPath)
        {
            try
            {
                if (xsdPath != null)
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ValidationType = ValidationType.Schema;
                    try
                    {
                        // Add the schema to the schema set
                        settings.Schemas.Add("", xsdPath);
                    }
                    catch (XmlSchemaException ex)
                    {
                        Console.WriteLine($"Error adding schema: {ex.Message}");
                    }

                    using (XmlReader reader = XmlReader.Create(diffXmlPath, settings))
                    {
                        XDocument doc = XDocument.Load(reader);
                        // doc.Validate(schemas, (o, e) => { throw new Exception(e.Message); });
                    }
                    Logger.Info($"Validation successful: {diffXmlPath} is valid against {xsdPath}");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Validation failed: {e.Message}");
            }
        }

        #endregion
    }
}