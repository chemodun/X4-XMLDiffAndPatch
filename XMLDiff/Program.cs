using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CommandLine;
using NLog;
using X4XmlDiffAndPatch;

namespace X4XmlDiffAndPatch.Diff;

class Program
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  static void Main(string[] args)
  {
    if (args.Length == 0)
      args = new[] { "--help" };

    var parser = new Parser(cfg =>
    {
      cfg.IgnoreUnknownArguments = true;
      cfg.AutoHelp = true;
      cfg.AutoVersion = true;
      cfg.CaseSensitive = true;
      cfg.HelpWriter = Console.Out;
    });

    parser
      .ParseArguments<Options>(args)
      .WithParsed(Run)
      .WithNotParsed(errs =>
      {
        foreach (var e in errs)
          Console.Error.WriteLine($"Argument error: {e}");
        Environment.Exit(1);
      });
  }

  // ─── Options ─────────────────────────────────────────────────────────────────

  public class Options
  {
    private string? _originalXml;
    private string? _modifiedXml;
    private string? _diffXml;
    private string? _xsd;
    private string? _logToFile;

    [Option('o', "original_xml", Required = true, HelpText = "Original XML file or directory.")]
    public string? OriginalXml
    {
      get => _originalXml;
      set => _originalXml = value?.Trim();
    }

    [Option('m', "modified_xml", Required = true, HelpText = "Modified XML file or directory.")]
    public string? ModifiedXml
    {
      get => _modifiedXml;
      set => _modifiedXml = value?.Trim();
    }

    [Option('d', "diff_xml", Required = true, HelpText = "Output diff file or directory.")]
    public string? DiffXml
    {
      get => _diffXml;
      set => _diffXml = value?.Trim();
    }

    [Option('x', "xsd", Required = false, HelpText = "Path to diff.xsd (default: diff.xsd).")]
    public string? Xsd
    {
      get => _xsd;
      set => _xsd = value?.Trim();
    }

    [Option(
      'l',
      "log-to-file",
      Required = false,
      HelpText = "Enable file logging at the specified level: error|warn|info|debug. Console always logs at info level."
    )]
    public string? LogToFile
    {
      get => _logToFile;
      set
      {
        var v = value?.Trim();
        if (v != null && !new[] { "error", "warn", "info", "debug" }.Contains(v.ToLower()))
          throw new ArgumentException($"Invalid log level '{v}'. Valid: error|warn|info|debug.");
        _logToFile = v?.ToLower();
      }
    }

    [Option('a', "append-to-log", Required = false, Default = false, HelpText = "Append to existing log file instead of overwriting.")]
    public bool AppendToLog { get; set; }

    [Option("only-full-path", Required = false, Default = false, HelpText = "Generate only full absolute XPath (no // shorthand).")]
    public bool OnlyFullPath { get; set; }

    [Option("use-all-attributes", Required = false, Default = false, HelpText = "Include all attributes in XPath predicates.")]
    public bool UseAllAttributes { get; set; }

    [Option("ignore-diff-in-attribute", Required = false, HelpText = "Attribute name to ignore when comparing elements.")]
    public string? IgnoreDiffInAttribute { get; set; }
  }

  // ─── Main logic ──────────────────────────────────────────────────────────────

  private static void Run(Options opts)
  {
    XmlUtils.ConfigureLogging(opts.LogToFile, opts.AppendToLog);

    var asm = Assembly.GetExecutingAssembly();
    Logger.Info($"Running {asm.GetName().Name} v{asm.GetName().Version?.ToString(3)}");

    var originalPath = opts.OriginalXml!;
    var modifiedPath = opts.ModifiedXml!;
    var diffPath = opts.DiffXml!;
    var xsdPath = opts.Xsd ?? "diff.xsd";

    bool originalIsDir = Directory.Exists(originalPath);
    bool modifiedIsDir = Directory.Exists(modifiedPath);

    var xsdSettings = XmlUtils.CreateXmlReaderSettings(xsdPath);
    var diffOptions = new DiffOptions
    {
      OnlyFullPath = opts.OnlyFullPath,
      UseAllAttributes = opts.UseAllAttributes,
      IgnoreDiffInAttribute = opts.IgnoreDiffInAttribute,
    };

    if (originalIsDir && modifiedIsDir)
    {
      ProcessDirectories(originalPath, modifiedPath, diffPath, xsdSettings, diffOptions);
    }
    else if (!originalIsDir && !modifiedIsDir)
    {
      ProcessSingleFile(originalPath, modifiedPath, diffPath, xsdSettings, diffOptions);
    }
    else
    {
      Logger.Error("Mismatch: original and modified must both be files or both be directories.");
      Environment.Exit(1);
    }
  }

  // ─── Directory mode ──────────────────────────────────────────────────────────

  private static void ProcessDirectories(
    string originalDir,
    string modifiedDir,
    string diffDir,
    XmlReaderSettings? xsdSettings,
    DiffOptions options
  )
  {
    Logger.Info("Processing directories recursively.");
    foreach (var modFile in Directory.EnumerateFiles(modifiedDir, "*.xml", SearchOption.AllDirectories))
    {
      var rel = Path.GetRelativePath(modifiedDir, modFile);
      var origFile = Path.Combine(originalDir, rel);
      var diffFile = Path.Combine(diffDir, rel);

      var diffFileDir = Path.GetDirectoryName(diffFile);
      if (!string.IsNullOrEmpty(diffFileDir) && !Directory.Exists(diffFileDir))
      {
        try
        {
          Directory.CreateDirectory(diffFileDir);
        }
        catch (Exception ex)
        {
          Logger.Error($"Cannot create directory '{diffFileDir}': {ex.Message}");
          continue;
        }
      }

      ProcessSingleFile(origFile, modFile, diffFile, xsdSettings, options);
    }
  }

  // ─── Single-file mode ────────────────────────────────────────────────────────

  private static void ProcessSingleFile(
    string originalPath,
    string modifiedPath,
    string diffPath,
    XmlReaderSettings? xsdSettings,
    DiffOptions options
  )
  {
    Logger.Info($"Comparing '{originalPath}' vs '{modifiedPath}' → '{diffPath}'");

    if (!File.Exists(originalPath))
    {
      Logger.Error($"Original file not found: {originalPath}");
      return;
    }
    if (!File.Exists(modifiedPath))
    {
      Logger.Error($"Modified file not found: {modifiedPath}");
      return;
    }

    // Resolve output path
    if (Directory.Exists(diffPath))
      diffPath = Path.Combine(diffPath, Path.GetFileName(originalPath));
    else
    {
      var dir = Path.GetDirectoryName(diffPath);
      if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
      {
        try
        {
          Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
          Logger.Error($"Cannot create output directory '{dir}': {ex.Message}");
          return;
        }
      }
    }

    try
    {
      var originalDoc = XDocument.Load(originalPath);
      var indent = XmlUtils.DetectIndentation(originalPath);
      Logger.Info($"Detected indentation: {indent}");
      var modifiedDoc = XDocument.Load(modifiedPath);

      if (File.Exists(diffPath))
        File.Delete(diffPath);

      var engine = new DiffEngine(options);
      var diffRoot = engine.GenerateDiff(originalDoc, modifiedDoc);

      if (!diffRoot.HasElements)
      {
        Logger.Info("No differences found. Diff file will not be created.");
        return;
      }

      var diffDoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), diffRoot);
      var settings = new XmlWriterSettings { Indent = true, IndentChars = new string(' ', indent) };

      using (var writer = XmlWriter.Create(diffPath, settings))
        diffDoc.Save(writer);

      // XSD validation
      if (xsdSettings != null)
      {
        Logger.Info($"Validating '{diffPath}' against XSD...");
        using var reader = XmlReader.Create(diffPath, xsdSettings);
        try
        {
          while (reader.Read()) { }
          Logger.Info("Validation successful.");
        }
        catch (XmlSchemaValidationException ex)
        {
          Logger.Error($"Validation failed: {ex.Message}");
          File.Delete(diffPath);
          return;
        }
      }

      Logger.Info($"Diff written to '{diffPath}'");
    }
    catch (Exception ex)
    {
      Logger.Error($"Error processing '{originalPath}': {ex.Message}");
    }
  }
}
