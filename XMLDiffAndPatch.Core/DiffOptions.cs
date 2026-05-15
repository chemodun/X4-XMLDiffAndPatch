namespace X4XmlDiffAndPatch;

/// <summary>Options controlling diff generation behaviour.</summary>
public class DiffOptions
{
    /// <summary>When true, restrict to full absolute XPath; when false (default), use // shorthand when element is globally unique.</summary>
    public bool OnlyFullPath { get; set; }

    /// <summary>Include all attributes (not just the minimal distinguishing set) in XPath predicates.</summary>
    public bool UseAllAttributes { get; set; }

    /// <summary>Attribute name to ignore when comparing elements. Null = compare all attributes.</summary>
    public string? IgnoreDiffInAttribute { get; set; }
}
