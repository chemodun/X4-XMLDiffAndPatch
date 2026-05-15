using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CommandLine;
using NLog;
using X4XmlDiffAndPatch;

namespace X4XmlDiffAndPatch.Patch;

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
        private string? _diffXml;
        private string? _outputXml;
        private string? _xsd;
        private string? _logToFile;

        [Option('o', "original_xml", Required = true, HelpText = "Original XML file or directory.")]
        public string? OriginalXml
        {
            get => _originalXml;
            set => _originalXml = value?.Trim();
        }

        [Option('d', "diff_xml", Required = true, HelpText = "Diff XML file or directory.")]
        public string? DiffXml
        {
            get => _diffXml;
            set => _diffXml = value?.Trim();
        }

        [Option('u', "output_xml", Required = true, HelpText = "Output XML file or directory.")]
        public string? OutputXml
        {
            get => _outputXml;
            set => _outputXml = value?.Trim();
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
            HelpText = "File log level: error|warn|info|debug."
        )]
        public string? LogToFile
        {
            get => _logToFile;
            set
            {
                var v = value?.Trim();
                if (v != null && !new[] { "error", "warn", "info", "debug" }.Contains(v.ToLower()))
                    throw new ArgumentException(
                        $"Invalid log level '{v}'. Valid: error|warn|info|debug."
                    );
                _logToFile = v?.ToLower();
            }
        }

        [Option(
            'a',
            "append-to-log",
            Required = false,
            Default = false,
            HelpText = "Append to existing log file instead of overwriting."
        )]
        public bool AppendToLog { get; set; }

        [Option(
            "allow-doubles",
            Required = false,
            Default = false,
            HelpText = "Skip duplicate-element guard when applying <add> operations."
        )]
        public bool AllowDoubles { get; set; }
    }

    // ─── Main logic ──────────────────────────────────────────────────────────────

    private static void Run(Options opts)
    {
        XmlUtils.ConfigureLogging(opts.LogToFile, opts.AppendToLog);

        var asm = Assembly.GetExecutingAssembly();
        Logger.Info($"Running {asm.GetName().Name} v{asm.GetName().Version}");

        var originalPath = opts.OriginalXml!;
        var diffPath = opts.DiffXml!;
        var outputPath = opts.OutputXml!;
        var xsdPath = opts.Xsd ?? "diff.xsd";

        bool originalIsDir = Directory.Exists(originalPath);
        bool diffIsDir = Directory.Exists(diffPath);

        var xsdSettings = XmlUtils.CreateXmlReaderSettings(xsdPath);

        if (originalIsDir && diffIsDir)
        {
            ProcessDirectories(originalPath, diffPath, outputPath, xsdSettings, opts.AllowDoubles);
        }
        else if (!originalIsDir && !diffIsDir)
        {
            ProcessSingleFile(originalPath, diffPath, outputPath, xsdSettings, opts.AllowDoubles);
        }
        else
        {
            Logger.Error("Mismatch: original and diff must both be files or both be directories.");
            Environment.Exit(1);
        }
    }

    // ─── Directory mode ──────────────────────────────────────────────────────────

    private static void ProcessDirectories(
        string originalDir,
        string diffDir,
        string outputDir,
        XmlReaderSettings? xsdSettings,
        bool allowDoubles
    )
    {
        Logger.Info("Processing directories recursively.");
        foreach (
            var diffFile in Directory.EnumerateFiles(diffDir, "*.xml", SearchOption.AllDirectories)
        )
        {
            var rel = Path.GetRelativePath(diffDir, diffFile);
            var origFile = Path.Combine(originalDir, rel);
            var outputFile = Path.Combine(outputDir, rel);

            if (!File.Exists(origFile))
            {
                Logger.Warn($"Original file not found for diff '{diffFile}'. Skipping.");
                continue;
            }

            var outputFileDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputFileDir) && !Directory.Exists(outputFileDir))
            {
                try
                {
                    Directory.CreateDirectory(outputFileDir);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Cannot create directory '{outputFileDir}': {ex.Message}");
                    continue;
                }
            }

            ProcessSingleFile(origFile, diffFile, outputFile, xsdSettings, allowDoubles);
        }
    }

    // ─── Single-file mode ────────────────────────────────────────────────────────

    private static void ProcessSingleFile(
        string originalPath,
        string diffPath,
        string outputPath,
        XmlReaderSettings? xsdSettings,
        bool allowDoubles
    )
    {
        Logger.Info($"Patching '{originalPath}' with '{diffPath}' → '{outputPath}'");

        if (!File.Exists(originalPath))
        {
            Logger.Error($"Original file not found: {originalPath}");
            return;
        }
        if (!File.Exists(diffPath))
        {
            Logger.Error($"Diff file not found: {diffPath}");
            return;
        }

        // Resolve output path
        if (Directory.Exists(outputPath))
            outputPath = Path.Combine(outputPath, Path.GetFileName(originalPath));
        else
        {
            var dir = Path.GetDirectoryName(outputPath);
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
            // Validate diff against XSD if available
            if (xsdSettings != null)
            {
                Logger.Info($"Validating '{diffPath}' against XSD...");
                using var valReader = XmlReader.Create(diffPath, xsdSettings);
                try
                {
                    while (valReader.Read()) { }
                    Logger.Info("Diff validation successful.");
                }
                catch (XmlSchemaValidationException ex)
                {
                    Logger.Error($"Diff validation failed: {ex.Message}");
                    return;
                }
            }
            else
            {
                Logger.Warn("Diff XSD validation is disabled.");
            }

            // Load diff (with line info for better warning messages)
            var diffDoc = XDocument.Load(diffPath, LoadOptions.SetLineInfo);
            var diffRoot = diffDoc.Root;
            if (diffRoot == null || diffRoot.Name.LocalName != "diff")
            {
                Logger.Error(
                    $"Root element of diff is not 'diff'. Found: '{diffRoot?.Name}'. Skipping."
                );
                return;
            }

            if (!diffRoot.HasElements)
            {
                Logger.Info("Diff has no operations. Nothing to do.");
                return;
            }

            // Load original and apply operations in-memory
            var originalDoc = XDocument.Load(originalPath);
            var indent = XmlUtils.DetectIndentation(originalPath);
            Logger.Info($"Detected indentation: {indent}");
            var originalRoot = originalDoc.Root!;

            foreach (var node in diffRoot.Nodes())
            {
                if (node is not XElement operation)
                {
                    Logger.Warn($"Skipping non-element node in diff: {node.NodeType}");
                    continue;
                }

                switch (operation.Name.LocalName)
                {
                    case "add":
                        PatchEngine.ApplyAdd(operation, originalRoot, allowDoubles);
                        break;
                    case "replace":
                        PatchEngine.ApplyReplace(operation, originalRoot);
                        break;
                    case "remove":
                        PatchEngine.ApplyRemove(operation, originalRoot);
                        break;
                    default:
                        Logger.Warn(
                            $"Unknown diff operation '{operation.Name.LocalName}'. Skipping."
                        );
                        break;
                }
            }

            // Write output
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = new string(' ', indent),
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
            };

            using var writer = XmlWriter.Create(outputPath, settings);
            originalDoc.Save(writer);

            Logger.Info($"Patched XML written to '{outputPath}'");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error processing '{originalPath}': {ex.Message}");
        }
    }
}
