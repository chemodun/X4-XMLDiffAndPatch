using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace X4XmlDiffAndPatch;

public static class XmlUtils
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  // ─── Indentation ────────────────────────────────────────────────────────────

  /// <summary>
  /// Reads the file and detects the per-level indentation size by examining the distinct
  /// leading-whitespace lengths on lines that start XML content.
  /// Returns 4 if no indented lines are found.
  /// </summary>
  public static int DetectIndentation(string xmlPath)
  {
    var indentLengths = new SortedSet<int>();
    var indentPattern = new Regex(@"^(\s+)<");

    foreach (var line in File.ReadLines(xmlPath))
    {
      var m = indentPattern.Match(line);
      if (m.Success)
        indentLengths.Add(m.Groups[1].Value.Length);
    }

    if (indentLengths.Count < 2)
      return 4;

    var sorted = indentLengths.ToList();
    int minDiff = int.MaxValue;
    for (int i = 1; i < sorted.Count; i++)
    {
      int diff = sorted[i] - sorted[i - 1];
      if (diff > 0 && diff < minDiff)
        minDiff = diff;
    }

    return minDiff == int.MaxValue ? 4 : minDiff;
  }

  // ─── Text value ─────────────────────────────────────────────────────────────

  /// <summary>
  /// Returns the content of the first XmlNodeType.Text child node, or "".
  /// Does NOT include content from child elements.
  /// </summary>
  public static string GetTextValue(XElement element)
  {
    foreach (var node in element.Nodes())
    {
      if (node is XText text)
        return text.Value;
    }
    return "";
  }

  // ─── Debug helpers ──────────────────────────────────────────────────────────

  /// <summary>Returns a human-readable string like &lt;tagName firstAttr="value" ...&gt;.</summary>
  public static string GetElementInfo(XElement? element)
  {
    if (element == null)
      return "<null>";
    var sb = new System.Text.StringBuilder("<");
    sb.Append(element.Name.LocalName);
    var first = element.FirstAttribute;
    if (first != null)
    {
      sb.Append($" {first.Name.LocalName}=\"{first.Value}\"");
      if (element.Attributes().Count() > 1)
        sb.Append(" ...");
    }
    sb.Append('>');
    return sb.ToString();
  }

  // ─── pos=before comment ──────────────────────────────────────────────────────

  /// <summary>
  /// Returns true if the node immediately before element in its parent's node list
  /// is an XComment whose value (trimmed) contains pos=before / pos="before" / pos='before'.
  /// </summary>
  public static bool IsElementPrecededByPosBeforeComment(XElement element)
  {
    if (element.Parent == null)
      return false;

    XNode? prev = null;
    foreach (var node in element.Parent.Nodes())
    {
      if (node == element)
        break;
      prev = node;
    }

    if (prev is XComment comment)
    {
      var val = comment.Value.Trim();
      return val.Contains("pos=before", StringComparison.OrdinalIgnoreCase)
        || val.Contains("pos=\"before\"", StringComparison.OrdinalIgnoreCase)
        || val.Contains("pos='before'", StringComparison.OrdinalIgnoreCase);
    }
    return false;
  }

  // ─── Logging ─────────────────────────────────────────────────────────────────

  /// <summary>
  /// Configures NLog with a console target (always at Info or higher) and optionally
  /// a file target at the specified level.
  /// </summary>
  public static void ConfigureLogging(string? logToFile, bool appendToLog)
  {
    var config = new LoggingConfiguration();

    var consoleTarget = new ConsoleTarget("console") { Layout = "${longdate} ${level} ${message} ${exception}" };
    config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);

    if (!string.IsNullOrEmpty(logToFile))
    {
      var fileLevel = ParseLogLevel(logToFile);
      var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
      var fileTarget = new FileTarget("file")
      {
        FileName = Path.Combine(Environment.CurrentDirectory, $"{processName}.log"),
        Layout = "${longdate} ${level} ${message} ${exception}",
        DeleteOldFileOnStartup = !appendToLog,
      };
      config.AddRule(fileLevel, LogLevel.Fatal, fileTarget);
    }

    LogManager.Configuration = config;
  }

  private static LogLevel ParseLogLevel(string level) =>
    level.ToLowerInvariant() switch
    {
      "error" => LogLevel.Error,
      "warn" => LogLevel.Warn,
      "info" => LogLevel.Info,
      "debug" => LogLevel.Debug,
      _ => LogLevel.Info,
    };

  // ─── XSD validation ──────────────────────────────────────────────────────────

  /// <summary>
  /// Loads the XSD at xsdPath and returns configured XmlReaderSettings.
  /// Returns null (with a warning log) if the file does not exist.
  /// </summary>
  public static XmlReaderSettings? CreateXmlReaderSettings(string xsdPath)
  {
    if (!File.Exists(xsdPath))
    {
      Logger.Warn($"XSD file not found: {xsdPath}. Validation will be skipped.");
      return null;
    }

    try
    {
      var xsdSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };
      XmlSchemaSet schemas;
      using (var reader = XmlReader.Create(xsdPath, xsdSettings))
      {
        schemas = new XmlSchemaSet();
        schemas.Add("", reader);
      }

      return new XmlReaderSettings
      {
        Schemas = schemas,
        ValidationType = ValidationType.Schema,
        DtdProcessing = DtdProcessing.Parse,
      };
    }
    catch (Exception ex)
    {
      Logger.Warn($"Failed to load XSD '{xsdPath}': {ex.Message}. Validation will be skipped.");
      return null;
    }
  }
}
