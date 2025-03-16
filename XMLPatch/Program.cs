using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;
using CommandLine;
using NLog;

namespace X4XmlDiffAndPatch
{
  class XMLPatch
  {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Options? ParsedOptions { get; set; }

    static void Main(string[] args)
    {
      var program = new XMLPatch();
      Parser
        .Default.ParseArguments<Options>(args)
        .WithParsed<Options>(opts => program.RunOptionsAndReturnExitCode(opts))
        .WithNotParsed<Options>((errs) => HandleParseError(errs));
    }

    private void RunOptionsAndReturnExitCode(Options opts)
    {
      ParsedOptions = opts;
      ConfigureLogging(opts.LogToFile, opts.AppendToLog);

      Assembly assembly = Assembly.GetExecutingAssembly();
      AssemblyName assemblyName = assembly.GetName();

      Logger.Info($"Running {assemblyName.Name} v{assemblyName.Version}");
      string param = "";
      foreach (var prop in typeof(Options).GetProperties())
      {
        param += (string.IsNullOrEmpty(param) ? "" : ", ") + $"{prop.Name}: '{prop.GetValue(opts)}'";
      }
      Logger.Info($"Parameters: {param}");

      var originalXmlPath = opts.OriginalXml;
      var diffXmlPath = opts.DiffXml;
      var outputXmlPath = opts.OutputXml;
      var diffXsdPath = opts.Xsd ?? "diff.xsd";

      bool originalIsDir = Directory.Exists(originalXmlPath);
      bool diffIsDir = Directory.Exists(diffXmlPath);
      bool outputIsDir = Directory.Exists(outputXmlPath);

      XmlReaderSettings? diffReaderSettings = CreateXmlReaderSettings(diffXsdPath!);
      if (originalIsDir && diffIsDir)
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
      private string? logToFile;
      private bool appendToLog;
      private bool allowDoubles;

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

      [Option('l', "log-to-file", HelpText = "Log level (error, warn, info, debug).")]
      public string? LogToFile
      {
        get => logToFile;
        set
        {
          string input = value?.Trim() ?? "info";
          var validLogLevels = new[] { "error", "warn", "info", "debug" };
          if (!validLogLevels.Contains(input.ToLower()))
          {
            throw new ArgumentException($"Invalid log level: {input}. Valid values are: error, warn, info, debug.");
          }
          logToFile = input.ToLower();
        }
      }

      [Option('a', "append-to-log", Required = false, HelpText = "Append logs to the existing log file.", Default = false)]
      public bool AppendToLog
      {
        get => appendToLog;
        set => appendToLog = value;
      }

      [Option("allow-doubles", Required = false, HelpText = "Allow doubles in the diff XML. Useful for scripts patching.", Default = false)]
      public bool AllowDoubles
      {
        get => allowDoubles;
        set => allowDoubles = value;
      }
    }

    #endregion

    #region Logging Configuration

    private static void ConfigureLogging(string? logToFile, bool appendToLog = false)
    {
      var config = new NLog.Config.LoggingConfiguration();

      // Targets
      var logConsole = new NLog.Targets.ConsoleTarget("logConsole");
      LogLevel minLogLevel = LogLevel.Info;
      if (!string.IsNullOrEmpty(logToFile))
      {
        var logFile = new NLog.Targets.FileTarget("logFile")
        {
          FileName = Path.Combine(Environment.CurrentDirectory, "${processname}.log"),
          Layout = "${longdate} ${level} ${message} ${exception}",
          KeepFileOpen = true,
          DeleteOldFileOnStartup = !appendToLog, // Overwrite the log file on each run
          ArchiveAboveSize = 0,
          ConcurrentWrites = true,
        };
        minLogLevel = logToFile switch
        {
          "error" => LogLevel.Error,
          "warn" => LogLevel.Warn,
          "info" => LogLevel.Info,
          "debug" => LogLevel.Debug,
          _ => LogLevel.Info,
        };
        config.AddRule(minLogLevel, LogLevel.Fatal, logFile);
      }
      logConsole.Layout = "${longdate} ${level} ${message} ${exception}";

      // Rules
      config.AddRule(LogLevel.Info < minLogLevel ? minLogLevel : LogLevel.Info, LogLevel.Fatal, logConsole);

      // Apply config
      NLog.LogManager.Configuration = config;
    }

    #endregion

    #region Processing

    private void ProcessSingleFile(string originalXmlPath, string diffXmlPath, string outputXmlPath, XmlReaderSettings? diffReaderSettings)
    {
      Logger.Info($"Patching the original XML file '{originalXmlPath}' with the diff XML file '{diffXmlPath}' to '{outputXmlPath}'.");
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
          Logger.Info("Output directory is null or empty. Using current directory.");
        }
        else if (!Directory.Exists(outputXmlDir))
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
            try
            {
              while (reader.Read()) { }
              Logger.Info($"Parsed diff XML: {diffXmlPath}");
            }
            catch (XmlSchemaValidationException ex)
            {
              Logger.Error($"Validation failed: {ex.Message}");
              return;
            }
          }
          Logger.Info($"Validation successful: {diffXmlPath} is valid against diff.xsd");
        }
        else
        {
          Logger.Warn("Diff XML validation is disabled.");
        }

        diffDoc = XDocument.Load(diffXmlPath, LoadOptions.SetLineInfo);
        if (diffDoc == null)
        {
          Logger.Error("Diff XML root is null.");
          return;
        }

        XElement diffRoot = diffDoc.Root ?? throw new InvalidOperationException("diffDoc.Root is null");
        if (diffRoot.Name.LocalName != "diff")
        {
          Logger.Error($"Root element of diff XML is not 'diff'. Found: '{diffRoot.Name}'.");
          return;
        }

        XElement originalRoot = originalDoc.Root ?? throw new InvalidOperationException("originalDoc.Root is null");
        foreach (XNode node in diffRoot.Nodes())
        {
          if (node is not XElement operation)
          {
            Logger.Warn($"Skipping non-element node: {node}");
            continue;
          }
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
              Logger.Warn($"Unknown operation: '{operation.Name}'. Skipping.");
              break;
          }
        }

        var settings = new XmlWriterSettings
        {
          Indent = true,
          IndentChars = new string(' ', indent),
          NewLineChars = "\r\n",
          NewLineHandling = NewLineHandling.Replace,
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

    private void ProcessDirectories(string originalDir, string diffDir, string outputDir, XmlReaderSettings? diffReaderSettings)
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

    private void ApplyAdd(XElement addElement, XElement originalRoot)
    {
      string? sel = addElement.Attribute("sel")?.Value;
      Logger.Debug($"Applying add operation: '{sel}'");
      if (sel == null)
      {
        Logger.Warn("Add operation missing 'sel' attribute! Skipping operation.");
        return;
      }
      string? type = addElement.Attribute("type")?.Value;
      string? pos = addElement.Attribute("pos")?.Value;
      if (pos == null && type == null)
      {
        pos = "append";
      }

      Logger.Info($"Applying add operation: '{sel}' at {pos!}");

      var targetElements = originalRoot.XPathSelectElements(sel);
      if (targetElements == null || !targetElements.Any())
      {
        Logger.Warn(
          $"No nodes found for add selector: '{sel}'! Existing only: '{LastApplicableNode(sel, originalRoot)}'. Skipping operation."
        );
        return;
      }
      if (targetElements.Count() > 1)
      {
        Logger.Warn($"Multiple nodes found for add selector: '{sel}'! Skipping operation.");
        return;
      }
      var targetElement = targetElements.First();
      if (pos != null)
      {
        XNode? latestAdded = null;
        foreach (var newNode in addElement.Nodes())
        {
          if (latestAdded == null)
          {
            if (newNode is XElement newElem)
            {
              XElement cloned = new XElement(newElem);
              string clonedInfo = GetElementInfo(cloned);
              string targetInfo = GetElementInfo(targetElement);
              string parentInfo = GetElementInfo(targetElement.Parent);
              Logger.Debug($"Cloned element: {clonedInfo}");
              switch (pos)
              {
                case "before":
                case "after":
                  if (
                    (ParsedOptions == null || !ParsedOptions.AllowDoubles)
                    && targetElement
                      .Parent!.Elements()
                      .Any(e =>
                        e.Name == cloned.Name
                        && e.Attributes().All(a => cloned.Attribute(a.Name)?.Value == a.Value)
                        && cloned.Attributes().All(a => e.Attribute(a.Name)?.Value == a.Value)
                      )
                  )
                  {
                    Logger.Warn($"Element '{clonedInfo}' already exists in '{parentInfo}'. Skipping.");
                    continue;
                  }
                  if (pos == "before")
                  {
                    targetElement.AddBeforeSelf(cloned);
                    Logger.Info($"Added new element '{clonedInfo}' before '{targetInfo}' in '{parentInfo}'.");
                  }
                  else
                  {
                    targetElement.AddAfterSelf(cloned);
                    Logger.Info($"Added new element '{clonedInfo}' after '{targetInfo}' in '{parentInfo}'.");
                  }
                  latestAdded = cloned;
                  break;
                case "prepend":
                case "append":
                  if (
                    (ParsedOptions == null || !ParsedOptions.AllowDoubles)
                    && targetElement
                      .Elements()
                      .Any(e =>
                        e.Name == cloned.Name
                        && e.Attributes().All(a => cloned.Attribute(a.Name)?.Value == a.Value)
                        && cloned.Attributes().All(a => e.Attribute(a.Name)?.Value == a.Value)
                      )
                  )
                  {
                    Logger.Warn($"Element '{clonedInfo}' already exists in '{targetInfo}'. Skipping.");
                    continue;
                  }
                  if (pos == "prepend")
                  {
                    targetElement.AddFirst(cloned);
                    Logger.Info($"Prepended new element '{clonedInfo}' to '{targetInfo}'.");
                  }
                  else
                  {
                    targetElement.Add(cloned);
                    Logger.Info($"Appended new element '{clonedInfo}' to '{targetInfo}'.");
                  }
                  latestAdded = cloned;
                  break;
                default:
                  Logger.Warn($"Unknown position: {pos}! Skipping operation.");
                  break;
              }
            }
            else if (newNode is XComment comment)
            {
              XComment cloned = new XComment(comment);
              latestAdded = cloned;
              switch (pos)
              {
                case "before":
                  targetElement.AddBeforeSelf(cloned);
                  break;
                case "after":
                  targetElement.AddAfterSelf(cloned);
                  break;
                case "prepend":
                  targetElement.AddFirst(cloned);
                  break;
                case "append":
                  targetElement.Add(cloned);
                  break;
                default:
                  latestAdded = null;
                  Logger.Warn($"Unknown position: {pos}! Skipping operation.");
                  break;
              }
              if (latestAdded != null)
              {
                Logger.Info($"Added comment: '{cloned.Value}'");
              }
            }
          }
          else if (newNode is XElement || newNode is XComment)
          {
            if (newNode is XElement newElem)
            {
              XElement cloned = new XElement(newElem);
              latestAdded.AddAfterSelf(cloned);
              latestAdded = cloned;
              Logger.Info($"Added new element: {GetElementInfo(cloned)}");
            }
            else if (newNode is XComment comment)
            {
              XComment cloned = new XComment(comment);
              latestAdded.AddAfterSelf(cloned);
              Logger.Info($"Added comment: '{cloned.Value}'");
            }
          }
        }
      }
      else if (type != null)
      {
        if (type.StartsWith('@') && type.Length > 1)
        {
          type = type.Substring(1);
          if (addElement.Value == null)
          {
            Logger.Warn("Attribute add operation missing value! Skipping operation.");
            return;
          }
          targetElement.SetAttributeValue(type, addElement.Value);
          Logger.Info($"Added attribute '{type}' with value '{addElement.Value}' to '{GetElementInfo(targetElement)}'.");
        }
      }
    }

    private void ApplyReplace(XElement replaceElement, XElement originalRoot)
    {
      string? sel = replaceElement.Attribute("sel")?.Value;
      Logger.Info($"Applying replace operation: '{sel}'");
      if (sel == null)
      {
        Logger.Warn("Replace operation missing 'sel' attribute! Skipping operation.");
        return;
      }

      var targetNodes = originalRoot.XPathEvaluate(sel) as IEnumerable<object>;
      if (targetNodes == null || !targetNodes.Any())
      {
        Logger.Warn(
          $"No nodes found for replace selector: '{sel}'! Existing only: '{LastApplicableNode(sel, originalRoot)}'. Skipping operation."
        );
        return;
      }

      foreach (var targetObj in targetNodes)
      {
        if (targetObj is XElement target)
        {
          string targetName = target.Name.LocalName;
          XElement? replaceSubElement = replaceElement.Element(targetName);
          XElement? parent = target.Parent;
          string targetInfo = GetElementInfo(target);
          string parentInfo = GetElementInfo(parent);
          if (replaceSubElement != null)
          {
            string replaceInfo = GetElementInfo(replaceSubElement);
            target.ReplaceWith(replaceSubElement);
            Logger.Info($"Replaced element '{targetInfo}' with '{replaceInfo}' in '{parentInfo}'.");
          }
          else
          {
            Logger.Warn($"Can't process replacement for '{targetInfo}' in '{parentInfo}'. Skipping operation.");
          }
        }
        else if (targetObj is XText textNode)
        {
          textNode.Value = replaceElement.Value;
          Logger.Info("Replaced text node.");
        }
        else if (targetObj is XAttribute attr)
        {
          var oldValue = attr.Value;
          attr.Value = replaceElement.Value;
          Logger.Info($"Replaced value of attribute '{attr.Name}' from '{oldValue}' to '{replaceElement.Value}'.");
        }
      }
    }

    private void ApplyRemove(XElement removeElement, XElement originalRoot)
    {
      string? sel = removeElement.Attribute("sel")?.Value;
      Logger.Info($"Applying remove operation: '{sel}'");
      if (sel == null)
      {
        Logger.Warn("Remove operation missing 'sel' attribute! Skipping operation.");
        return;
      }

      var targetNodes = originalRoot.XPathEvaluate(sel) as IEnumerable<object>;
      if (targetNodes == null || !targetNodes.Any())
      {
        Logger.Warn(
          $"No nodes found for remove selector: '{sel}'! Existing only: '{LastApplicableNode(sel, originalRoot)}'. Skipping operation."
        );
        return;
      }

      foreach (var targetObj in targetNodes)
      {
        if (targetObj is XElement target)
        {
          XElement? parent = target.Parent;
          string parentInfo = GetElementInfo(parent);
          string targetInfo = GetElementInfo(target);
          if (parent == null)
          {
            Logger.Warn($"Element '{targetInfo}' has no parent. Cannot remove.");
            continue;
          }
          target.Remove();
          Logger.Info($"Removed element '{targetInfo}' from '{parentInfo}'.");
        }
        else if (targetObj is XAttribute attr)
        {
          XElement? parent = attr.Parent;
          string parentInfo = parent != null ? GetElementInfo(parent) : "";
          if (parent == null)
          {
            Logger.Warn($"Attribute '{attr.Name}' has no parent. Cannot remove.");
            continue;
          }
          attr.Remove();
          Logger.Info($"Removed attribute '{attr.Name}' from '{parentInfo}'.");
        }
        else if (targetObj is XText textNode)
        {
          textNode.Remove();
          Logger.Info("Removed text node.");
        }
      }
    }

    private static string LastApplicableNode(string selector, XElement originalRoot)
    {
      string lastApplicableNode = "";
      string[] parts = selector.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();
      string xpath = "";
      foreach (var part in parts)
      {
        xpath += "/" + part;
        var nodes = originalRoot.XPathSelectElements(xpath);
        if (nodes.Any())
        {
          lastApplicableNode = xpath;
        }
      }
      return lastApplicableNode;
    }

    #endregion

    #region Element and attr info
    private static string GetElementInfo(XElement? element)
    {
      string info = "<";
      if (element != null)
      {
        info += $"{element.Name}";
        if (element.HasAttributes)
        {
          info += $" {element.FirstAttribute?.Name}=\"{element.FirstAttribute?.Value}\"";
          if (element.Attributes().Count() > 1)
          {
            info += " ...";
          }
        }
        info += ">";
      }
      return info;
    }

    private static string GetAttributeInfo(XAttribute? attr)
    {
      if (attr == null)
      {
        return "";
      }
      return $"{attr.Name}=\"{attr.Value}\"";
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

    private static XmlReaderSettings? CreateXmlReaderSettings(string xsdPath)
    {
      XmlReaderSettings? settings = null;
      if (ValidateXsdPath(xsdPath))
      {
        try
        {
          XmlReaderSettings xsdSettings = new XmlReaderSettings
          {
            DtdProcessing = DtdProcessing.Parse, // Enable DTD processing
            ValidationType = ValidationType.Schema, // Optional, for validation during reading
          };
          using (XmlReader reader = XmlReader.Create(xsdPath, xsdSettings))
          {
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            // Add the schema using the XmlReader
            schemaSet.Add("", reader);
            settings = new XmlReaderSettings();
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
}
