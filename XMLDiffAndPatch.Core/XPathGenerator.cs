using System.Xml.Linq;
using System.Xml.XPath;

namespace X4XmlDiffAndPatch;

/// <summary>
/// Generates XPath expressions that uniquely identify elements within an XDocument.
/// See spec §5 for the full algorithm.
/// </summary>
public static class XPathGenerator
{
    // ─── Public entry point ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns an XPath string that uniquely identifies <paramref name="element"/> inside its document.
    /// Walks from element toward root, prepending path segments.
    /// </summary>
    public static string GenerateXPath(XElement element, DiffOptions options)
    {
        // steps[0] = segment nearest the element, steps[last] = root name.
        // We Reverse() at the end and join to get root→element order.
        var steps = new List<string>();
        var current = element;

        while (current.Parent != null)
        {
            var parent = current.Parent;
            var doc = !options.OnlyFullPath ? current.Document : null;

            var (step, pathForParent) = GetElementPathStep(current, parent, doc, options);

            if (step.StartsWith("//"))
            {
                // Globally unique — prepend and return; steps below are already in the list.
                steps.Reverse();
                var below = steps.Count > 0 ? "/" + string.Join("/", steps) : "";
                return step + below;
            }

            if (string.IsNullOrEmpty(step))
                step = GetSiblingFallbackStep(current, parent, pathForParent, options);

            steps.Add(step);
            current = current.Parent;
        }

        // current is the document root element (no parent)
        steps.Add(current.Name.LocalName);
        steps.Reverse();
        return "/" + string.Join("/", steps);
    }

    // ─── Path step for a single level ────────────────────────────────────────────

    /// <summary>
    /// Returns (step, pathForParent) where step is the minimal XPath predicate-expression
    /// that uniquely identifies <paramref name="element"/> within <paramref name="parent"/>.
    /// pathForParent is the base name+firstAttr expression (used for sibling fallback).
    /// </summary>
    internal static (string step, string pathForParent) GetElementPathStep(
        XElement element,
        XElement parent,
        XDocument? doc,
        DiffOptions options
    )
    {
        // Start with name only; add attributes only as needed for uniqueness.
        string pathForParent = element.Name.LocalName;

        // Check uniqueness with name only first
        if (IsUniqueInParent(pathForParent, element, parent))
            return TryGlobalUnique(pathForParent, element, doc);

        var firstAttr = element.FirstAttribute;
        if (firstAttr == null)
            return ("", pathForParent); // No attributes at all — can't distinguish here

        pathForParent += AttributeToXpathElement(firstAttr);

        // Check uniqueness with name + first attribute
        if (IsUniqueInParent(pathForParent, element, parent))
            return TryGlobalUnique(pathForParent, element, doc);

        var remainingAttrs = element.Attributes().Skip(1).ToList();

        // --use-all-attributes: add all remaining attributes at once and test
        if (options.UseAllAttributes && remainingAttrs.Count > 0)
        {
            string allPath = pathForParent;
            foreach (var a in remainingAttrs)
                allPath += AttributeToXpathElement(a);

            if (IsUniqueInParent(allPath, element, parent))
                return TryGlobalUnique(allPath, element, doc);
        }

        // Try adding attributes one by one (iterative tightening)
        string current = pathForParent;
        foreach (var attr in remainingAttrs)
        {
            current += AttributeToXpathElement(attr);
            if (IsUniqueInParent(current, element, parent))
                return TryGlobalUnique(current, element, doc);
        }

        // Could not make unique within parent with any attribute combination
        return ("", pathForParent);
    }

    // ─── Global uniqueness check ─────────────────────────────────────────────────

    private static (string step, string pathForParent) TryGlobalUnique(
        string step,
        XElement element,
        XDocument? doc
    )
    {
        if (doc != null)
        {
            var globalMatches = doc.XPathSelectElements("//" + step).ToList();
            if (globalMatches.Count == 1 && globalMatches[0] == element)
                return ("//" + step, step);
        }
        return (step, step);
    }

    // ─── Sibling fallback ────────────────────────────────────────────────────────

    /// <summary>
    /// When no attribute combination can uniquely identify <paramref name="element"/> within
    /// its parent, fall back to a sibling-relative expression or a numeric position index.
    /// </summary>
    internal static string GetSiblingFallbackStep(
        XElement element,
        XElement parent,
        string pathForParent,
        DiffOptions options
    )
    {
        var siblings = parent.Elements().ToList();
        int index = siblings.IndexOf(element);
        var doc = !options.OnlyFullPath ? element.Document : null;

        // Try preceding sibling
        if (index > 0)
        {
            var prev = siblings[index - 1];
            var (prevStep, _) = GetElementPathStep(prev, parent, doc, options);
            if (!string.IsNullOrEmpty(prevStep) && !prevStep.StartsWith("//"))
                return $"{prevStep}/following-sibling::{pathForParent}[1]";
        }

        // Try following sibling
        if (index + 1 < siblings.Count)
        {
            var next = siblings[index + 1];
            var (nextStep, _) = GetElementPathStep(next, parent, doc, options);
            if (!string.IsNullOrEmpty(nextStep) && !nextStep.StartsWith("//"))
                return $"{nextStep}/preceding-sibling::{pathForParent}[1]";
        }

        // Count same-named preceding siblings
        int sameNamePreceding = siblings.Take(index).Count(s => s.Name == element.Name);
        if (sameNamePreceding == 0 && siblings.Count(s => s.Name == element.Name) == 1)
            return pathForParent; // Only one element with this name

        return $"{pathForParent}[{sameNamePreceding + 1}]";
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static bool IsUniqueInParent(string xpath, XElement element, XElement parent)
    {
        var matches = parent.XPathSelectElements(xpath).ToList();
        return matches.Count == 1 && matches[0] == element;
    }

    /// <summary>
    /// Builds an XPath attribute predicate like [@name='value'] or [@name="value"] when the
    /// value contains a single quote.  Double-quotes inside the value are escaped as &amp;quot;.
    /// </summary>
    public static string AttributeToXpathElement(XAttribute attr)
    {
        var value = attr.Value.Replace("\"", "&quot;");
        return value.Contains('\'')
            ? $"[@{attr.Name.LocalName}=\"{value}\"]"
            : $"[@{attr.Name.LocalName}='{value}']";
    }
}
