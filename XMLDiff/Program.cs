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
  class XMLDiff
  {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Regex NumericIdsPattern = new Regex(@"\[\d+\]");

    static void Main(string[] args)
    {
      Parser
        .Default.ParseArguments<Options>(args)
        .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
        .WithNotParsed<Options>((errs) => HandleParseError(errs));
    }

    private static void RunOptionsAndReturnExitCode(Options opts)
    {
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
      var modifiedXmlPath = opts.ModifiedXml;
      var diffXmlPath = opts.DiffXml;
      var diffXsdPath = opts.Xsd ?? "diff.xsd";
      var pathOptions = new PathOptions { OnlyFullPath = opts.OnlyFullPath, UseAllAttributes = opts.UseAllAttributes };

      bool originalIsDir = Directory.Exists(originalXmlPath);
      bool modifiedIsDir = Directory.Exists(modifiedXmlPath);
      bool diffIsDir = Directory.Exists(diffXmlPath);

      XmlReaderSettings? diffReaderSettings = CreateXmlReaderSettings(diffXsdPath!);
      if (originalIsDir && modifiedIsDir)
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
      private string? logToFile;
      private bool onlyFullPath;
      private bool useAllAttributes;
      private bool appendToLog;

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

    private static void ConfigureLogging(string? logToFile, bool appendToLog = false)
    {
      var config = new NLog.Config.LoggingConfiguration();

      // Targets
      var logConsole = new NLog.Targets.ConsoleTarget("logConsole");
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
        LogLevel minLogLevel = logToFile switch
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
      config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);

      // Apply config
      NLog.LogManager.Configuration = config;
    }

    #endregion

    #region Processing

    private static void ProcessSingleFile(
      string originalXmlPath,
      string modifiedXmlPath,
      string diffXmlPath,
      XmlReaderSettings? diffReaderSettings,
      PathOptions pathOptions
    )
    {
      Logger.Info($"Comparing original XML '{originalXmlPath}' with modified XML '{modifiedXmlPath}' to the diff XML at '{diffXmlPath}'");
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
          Logger.Info("Output directory is null or empty. Using current directory.");
        }
        else if (!Directory.Exists(diffXmlDir))
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

        var settings = new XmlWriterSettings { Indent = true, IndentChars = new string(' ', indent) };
        using (var writer = XmlWriter.Create(diffXmlPath, settings))
        {
          diffDoc.Save(writer);
        }

        if (diffReaderSettings != null)
        {
          Logger.Info($"Diff XML written to {diffXmlPath} and will be validated");
          using (XmlReader reader = XmlReader.Create(diffXmlPath, diffReaderSettings))
          {
            try
            {
              while (reader.Read()) { }
            }
            catch (XmlSchemaValidationException ex)
            {
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

    private static void ProcessDirectories(
      string originalDir,
      string modifiedDir,
      string diffDir,
      XmlReaderSettings? diffReaderSettings,
      PathOptions pathOptions
    )
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

    private static bool CompareElements(
      XDocument original,
      XDocument modified,
      XElement diffRoot,
      PathOptions pathOptions,
      XElement? originalElem = null,
      XElement? modifiedElem = null,
      bool checkOnly = false
    )
    {
      if (originalElem != null && modifiedElem != null)
      {
        string sel = GenerateXPath(originalElem, pathOptions);
        string selModified = GenerateXPath(modifiedElem, pathOptions);
        if (checkOnly)
        {
          Logger.Debug(
            $"Comparing elements '{GetElementInfo(originalElem)}'({sel}) vs '{GetElementInfo(modifiedElem)}'({selModified}). Check only: {checkOnly}"
          );
        }
        else
        {
          Logger.Info($"Comparing elements '{GetElementInfo(originalElem)}'({sel}) vs '{GetElementInfo(modifiedElem)}'({selModified}).");
        }
        if (originalElem.Name != modifiedElem.Name)
        {
          // Process can be there only in case of changes detection, not for the real diff generation
          Logger.Debug(
            $"Warning. Element names do not match: {originalElem.Name} vs {modifiedElem.Name}. Check only: {checkOnly}. Returning true."
          );
          return true;
        }

        // Compare text
        string originalText = GetValue(originalElem);
        string modifiedText = GetValue(modifiedElem);

        Logger.Debug($"Comparing text in element '{originalElem.Name}': '{originalText}' vs '{modifiedText}'");
        if (originalText != modifiedText)
        {
          if (!string.IsNullOrEmpty(modifiedText))
          {
            if (checkOnly)
            {
              Logger.Debug($"Text in element '{GetElementInfo(originalElem)}' does not match in check only mode. Returning true.");
              return true;
            }
            XElement replaceOp = new XElement("replace", new XAttribute("sel", sel), modifiedText);
            diffRoot.Add(replaceOp);
            Logger.Info($"[Operation Replace] Text in element '{GetElementInfo(originalElem)}' from '{originalText}' to '{modifiedText}'.");
          }
          else
          {
            if (checkOnly)
            {
              Logger.Debug($"Text in element '{originalElem.Name}' removed in check only mode. Returning true.");
              return true;
            }
            XElement removeOp = new XElement("remove", new XAttribute("sel", $"{sel}/text()"));
            diffRoot.Add(removeOp);
            Logger.Info($"[Operation Remove] Text from element '{GetElementInfo(originalElem)}'.");
          }
        }
      }

      originalElem = originalElem ?? original.Root;
      modifiedElem = modifiedElem ?? modified.Root;
      if (originalElem == null || modifiedElem == null)
      {
        Logger.Debug("Warning: Original or modified element is null.");
        return true;
      }
      // Compare children
      var originalChildren = originalElem.Elements().ToList();
      var modifiedChildren = modifiedElem.Elements().ToList();

      if (checkOnly && originalChildren.Count != modifiedChildren.Count)
      {
        Logger.Debug(
          $"Children count does not match for {GetElementInfo(originalElem)} and {GetElementInfo(modifiedElem)}: {originalChildren.Count} vs {modifiedChildren.Count} in check only mode. Returning true."
        );
        return true;
      }

      int i = 0,
        j = 0;
      int lastRemovedOrReplaced = -1;
      while (i < originalChildren.Count && j < modifiedChildren.Count)
      {
        var originalChild = originalChildren[i];
        var modifiedChild = modifiedChildren[j];

        bool matchedEnough = false;
        if (checkOnly)
        {
          Logger.Debug(
            $"Comparing child '{GetElementInfo(originalChild)}' of '{GetElementInfo(originalElem)}' vs '{GetElementInfo(modifiedChild)}' of '{GetElementInfo(modifiedElem)}'. Check only: {checkOnly}"
          );
        }
        else
        {
          Logger.Info(
            $"Comparing child '{GetElementInfo(originalChild)}' of '{GetElementInfo(originalElem)}' vs '{GetElementInfo(modifiedChild)}' of '{GetElementInfo(modifiedElem)}'"
          );
        }
        if (originalChild.Name == modifiedChild.Name)
        {
          var originalAttributes = originalChild.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);
          var modifiedAttributes = modifiedChild.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);

          int differencesInAttributesCount = 0;

          XElement? savedOp = null;

          matchedEnough = true;

          foreach (var attr in modifiedAttributes)
          {
            Logger.Debug($"Checking attribute '{attr.Key}' in original element attributes '{string.Join(", ", originalAttributes.Keys)}'.");
            if (!originalAttributes.ContainsKey(attr.Key))
            {
              Logger.Debug($"Original attributes does not contain key '{attr.Key}'.");
              differencesInAttributesCount++;
              if (differencesInAttributesCount > 1)
              {
                differencesInAttributesCount++;
                matchedEnough = false;
                break;
              }
              string sel = GenerateXPath(originalChild, pathOptions);
              savedOp = new XElement("add", new XAttribute("sel", sel), new XAttribute("type", $"@{attr.Key}"), attr.Value);
              Logger.Debug($"Found added attribute '{attr.Key}' with value '{attr.Value}' to element '{originalChild.Name}'.");
            }
            else if (originalAttributes[attr.Key] != attr.Value)
            {
              Logger.Debug(
                $"Original attributes value '{originalAttributes[attr.Key]}' does not match with modified attributes value '{attr.Value}'."
              );
              differencesInAttributesCount++;
              if (differencesInAttributesCount > 1)
              {
                differencesInAttributesCount++;
                matchedEnough = false;
                break;
              }
              string sel = $"{GenerateXPath(originalChild, pathOptions)}/@{attr.Key}";
              savedOp = new XElement("replace", new XAttribute("sel", sel), attr.Value);
              /*! To add the children checks and next elements on this level. Only checks, without generation of the diff
               */
              Logger.Debug(
                $"Found replaced attribute '{attr.Key}' value from '{originalAttributes[attr.Key]}' to '{attr.Value}' in element '{originalChild.Name}'."
              );
            }
          }
          if (matchedEnough)
          {
            foreach (var attr in originalAttributes)
            {
              if (!modifiedAttributes.ContainsKey(attr.Key))
              {
                Logger.Debug($"Modified attributes does not contain key '{attr.Key}'.");
                if (checkOnly)
                {
                  Logger.Debug(
                    $"Attribute '{attr}' from {GetElementInfo(originalChild)} not exists in {GetElementInfo(modifiedChild)} check only mode. Returning true."
                  );
                  return true;
                }
                differencesInAttributesCount++;
                if (differencesInAttributesCount > 1 || originalAttributes.Count == 1)
                {
                  differencesInAttributesCount++;
                  matchedEnough = false;
                  break;
                }
                string sel = $"{GenerateXPath(originalChild, pathOptions)}/@{attr.Key}";
                savedOp = new XElement("remove", new XAttribute("sel", sel));
                break;
              }
            }
          }
          if (matchedEnough && differencesInAttributesCount == 1)
          {
            if (checkOnly)
            {
              Logger.Debug(
                $"At least one attribute does not match {GetElementInfo(originalChild)} vs {GetElementInfo(modifiedChild)} in check only mode. Returning true."
              );
              return true;
            }
            matchedEnough = false;
            if (
              !CompareElements(original, modified, diffRoot, pathOptions, originalChild, modifiedChild, true)
              && (
                i + 1 == originalChildren.Count
                || j + 1 == modifiedChildren.Count
                || originalChildren[i + 1].Name == modifiedChildren[j + 1].Name
                  && originalChildren[i + 1].Attributes().Count() == modifiedChildren[j + 1].Attributes().Count()
                  && originalChildren[i + 1].Attributes().All(attr => modifiedChildren[j + 1].Attribute(attr.Name)?.Value == attr.Value)
              )
            )
            {
              if (savedOp != null)
              {
                diffRoot.Add(savedOp);
                Logger.Info($"[Operation {savedOp.Name}] Added the saved operation to the diff.");
              }
              matchedEnough = true;
            }
          }
          Logger.Debug($"Matched enough: {matchedEnough}, i: {i}, j: {j}");
          if (matchedEnough)
          {
            if (CompareElements(original, modified, diffRoot, pathOptions, originalChild, modifiedChild, checkOnly))
            {
              if (checkOnly)
              {
                Logger.Debug(
                  $"Children of {GetElementInfo(originalElem)} and {GetElementInfo(modifiedElem)} do not match in check only mode. Returning true."
                );
                return true;
              }
            }
            i++;
            j++;
          }
        }
        if (!matchedEnough)
        {
          if (checkOnly)
          {
            Logger.Debug(
              $"Elements {GetElementInfo(originalElem)} and {GetElementInfo(modifiedElem)} do not match in check only mode. Returning true."
            );
            return true;
          }
          bool foundMatch = false;
          Logger.Debug($"Checking for match for '{GetElementInfo(originalChild)}' in the next child of '{GetElementInfo(modifiedElem)}'.");
          for (int k = j + 1; k < modifiedChildren.Count; k++)
          {
            var nextModifiedChild = modifiedChildren[k];
            if (
              originalChild.Name == nextModifiedChild.Name
              && originalChild.Attributes().Count() == nextModifiedChild.Attributes().Count()
              && originalChild.Attributes().All(attr => nextModifiedChild.Attribute(attr.Name)?.Value == attr.Value)
            )
            {
              Logger.Debug($"Found match for '{GetElementInfo(originalChild)}' in the next child of '{GetElementInfo(modifiedElem)}'.");
              XElement addOp;
              if (i > 0)
              {
                string xpath = GenerateXPath(originalChildren[i - 1], pathOptions);
                string pos = "after";
                if (lastRemovedOrReplaced == i - 1 || NumericIdsPattern.IsMatch(xpath))
                {
                  string xpathBefore = GenerateXPath(originalChild, pathOptions);
                  if (!NumericIdsPattern.IsMatch(xpathBefore))
                  {
                    xpath = xpathBefore;
                    pos = "before";
                  }
                }
                addOp = new XElement("add", new XAttribute("sel", xpath), new XAttribute("pos", pos));
                Logger.Info($"[Operation Add] Element '{GetElementInfo(originalChild)}' to parent '{GetElementInfo(originalElem)}'.");
              }
              else
              {
                addOp = new XElement(
                  "add",
                  new XAttribute("sel", GenerateXPath(originalElem, pathOptions)),
                  new XAttribute("pos", "prepend")
                );
              }
              for (int l = j; l < k; l++)
              {
                var addedChild = modifiedChildren[l];
                addOp.Add(addedChild);
                Logger.Info($"[Operation Add] Element '{GetElementInfo(addedChild)}' to parent '{GetElementInfo(originalElem)}'.");
              }
              diffRoot.Add(addOp);
              j = k;
              foundMatch = true;
              break;
            }
          }

          if (!foundMatch)
          {
            Logger.Debug($"Trying to identify - is it replace or remove operation.");
            if (
              (originalChildren[i].Name == modifiedChildren[j].Name)
              && originalChildren[i].Attributes().Any(attr => modifiedChildren[j].Attribute(attr.Name)?.Value == attr.Value)
            )
            {
              string sel = GenerateXPath(originalChild, pathOptions);
              XElement replaceOp = new XElement("replace", new XAttribute("sel", sel), modifiedChild);
              lastRemovedOrReplaced = i;
              diffRoot.Add(replaceOp);
              Logger.Info($"[Operation replace] Element '{GetElementInfo(originalChild)}' with '{GetElementInfo(modifiedChild)}'.");
              i++;
              j++;
            }
            else
            {
              string sel = GenerateXPath(originalChild, pathOptions);
              XElement removeOp = new XElement("remove", new XAttribute("sel", sel));
              lastRemovedOrReplaced = i;
              diffRoot.Add(removeOp);
              Logger.Info($"[Operation remove] Element '{GetElementInfo(originalChild)}' from parent '{GetElementInfo(originalElem)}'.");
              i++;
            }
          }
        }
      }

      while (i < originalChildren.Count)
      {
        var originalChild = originalChildren[i];
        string sel = GenerateXPath(originalChild, pathOptions);
        XElement removeOp = new XElement("remove", new XAttribute("sel", sel));
        lastRemovedOrReplaced = i;
        diffRoot.Add(removeOp);
        Logger.Info($"[Operation remove] Element '{GetElementInfo(originalChild)}' from parent '{GetElementInfo(originalElem)}'.");
        i++;
      }

      if (j + 1 <= modifiedChildren.Count)
      {
        XElement addOp;
        addOp = new XElement("add", new XAttribute("sel", GenerateXPath(originalElem, pathOptions)));
        while (j < modifiedChildren.Count)
        {
          var addedChild = modifiedChildren[j];
          addOp.Add(addedChild);
          Logger.Info($"[Operation add] Element '{GetElementInfo(addedChild)}' to parent '{GetElementInfo(originalElem)}'.");
          j++;
        }
        diffRoot.Add(addOp);
      }
      if (checkOnly)
      {
        Logger.Debug($"Matched elements of {GetElementInfo(originalElem)} and {GetElementInfo(modifiedElem)}. Check only: {checkOnly}");
      }
      return false;
    }

    private static string AttributeToXpathElement(XAttribute attr)
    {
      string attrValue = attr.Value.Replace("\"", "&quot;");
      if (attrValue.Contains("'"))
      {
        return $"[@{attr.Name.LocalName}=\"{attrValue}\"]";
      }
      else
      {
        return $"[@{attr.Name.LocalName}='{attrValue}']";
      }
    }

    private static string GetElementPathStep(XElement element)
    {
      if (element == null)
        return string.Empty;
      string step = element.Name.LocalName;
      if (element.FirstAttribute != null)
      {
        step += AttributeToXpathElement(element.FirstAttribute);
      }
      return step;
    }

    private static (string step, string patchForParent) GetElementPathStep(
      XElement element,
      XElement parent,
      XDocument? doc,
      PathOptions pathOptions,
      string patchForParent = ""
    )
    {
      if (element == null)
        return (string.Empty, string.Empty);
      if (parent == null)
        return (string.Empty, string.Empty);
      string step = "";
      patchForParent += GetElementPathStep(element);
      IEnumerable<XElement> matches = parent.XPathSelectElements(patchForParent);
      List<XAttribute>? attributes = null;
      if (matches.Count() == 1 && matches.First() == element)
      {
        step = patchForParent;
      }
      else
      {
        attributes = element.Attributes().Skip(1).ToList();
      }
      if (attributes?.Count > 0)
      {
        string xpath = $"{patchForParent}";
        if (pathOptions.UseAllAttributes)
        {
          foreach (var attr in attributes)
          {
            xpath += AttributeToXpathElement(attr);
          }
          matches = parent.XPathSelectElements(xpath);
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
        xpath = $"{patchForParent}";
        foreach (var attr in attributes)
        {
          string attrValue = attr.Value.Replace("\"", "&quot;");
          xpath += AttributeToXpathElement(attr);
          matches = parent.XPathSelectElements(xpath);
          if (matches.Count() == 1 && matches.First() == element)
          {
            if (step == "")
              step = xpath;
            if (doc != null)
            {
              var doc_matches = doc.XPathSelectElements($"//{xpath}");
              if (doc_matches.Count() == 1)
              {
                return ($"//{xpath}", patchForParent);
              }
              else
              {
                return (xpath, patchForParent);
              }
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
        else
        {
          return (patchForParent, patchForParent);
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
      if (!pathOptions.OnlyFullPath)
      {
        doc = element.Document;
      }
      var path = new System.Text.StringBuilder();
      XElement? current = element;
      while (current != null)
      {
        XElement? parent = current.Parent;
        if (parent != null)
        {
          (string step, string patchForParent) = GetElementPathStep(current, parent, doc, pathOptions);
          if (step.StartsWith("//"))
          {
            path.Insert(0, step);
            return path.ToString();
          }
          if (step == "")
          {
            if (path.Length > 0)
            {
              if (parent.XPathSelectElements($"{patchForParent}/{path}").Count() == 1)
              {
                step = GetElementPathStep(current);
              }
            }
            if (string.IsNullOrEmpty(step))
            {
              var siblings = parent.Elements().ToList();
              string xpathWithSiblings = "";
              int index = siblings.IndexOf(current);
              if (index > 0)
              {
                XElement sibling = siblings[index - 1];
                (step, xpathWithSiblings) = GetElementPathStep(sibling, parent, doc, pathOptions);
                if (!string.IsNullOrEmpty(step))
                {
                  step += $"/following-sibling::{patchForParent}";
                }
              }
              if (string.IsNullOrEmpty(step))
              {
                if (index + 1 < siblings.Count)
                {
                  XElement sibling = siblings[index + 1];
                  (step, xpathWithSiblings) = GetElementPathStep(sibling, parent, doc, pathOptions);
                  if (!string.IsNullOrEmpty(step))
                  {
                    step += $"/preceding-sibling::{patchForParent}";
                  }
                }
              }
              if (step.StartsWith("//"))
              {
                return step;
              }
              else if (string.IsNullOrEmpty(step))
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
          }
          path.Insert(0, "/" + step);
        }
        else if (current == root)
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

    #region Element and attr info

    private static string GetValue(XElement? element)
    {
      if (element == null)
      {
        return "";
      }
      IEnumerable<XNode> nodes = element.Nodes().Where(n => n.NodeType == XmlNodeType.Text);
      if (!nodes.Any())
      {
        return "";
      }
      return nodes.First().ToString();
    }

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
          return settings;
        }
        catch (Exception ex)
        {
          Logger.Error($"General Exception: {ex.Message}");
          return settings;
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
