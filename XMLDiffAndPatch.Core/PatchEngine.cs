using System.Xml.Linq;
using System.Xml.XPath;
using NLog;

namespace X4XmlDiffAndPatch;

/// <summary>
/// Applies an XML diff document to an original XML document, producing a patched document.
/// See spec §8 for the full algorithm.
/// </summary>
public class PatchEngine
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // ─── Public patch operations ─────────────────────────────────────────────────

    /// <summary>
    /// Applies an &lt;add&gt; operation to <paramref name="originalRoot"/>.
    /// </summary>
    public static void ApplyAdd(XElement addElement, XElement originalRoot, bool allowDoubles)
    {
        var sel = addElement.Attribute("sel")?.Value;
        var type = addElement.Attribute("type")?.Value;
        var pos = addElement.Attribute("pos")?.Value;

        if (sel == null)
        {
            Logger.Warn("Add operation missing 'sel' attribute. Skipping.");
            return;
        }

        if (pos == null && type == null)
            pos = "append";

        Logger.Info($"[Operation add] sel='{sel}' pos='{pos}' type='{type}'");

        var targets = originalRoot.XPathSelectElements(sel).ToList();
        if (targets.Count == 0)
        {
            Logger.Warn(
                $"[Operation add] No element found for sel='{sel}'. Last resolvable: '{LastApplicableNode(sel, originalRoot)}'. Skipping."
            );
            return;
        }
        if (targets.Count > 1)
        {
            Logger.Warn(
                $"[Operation add] Multiple elements ({targets.Count}) found for sel='{sel}'. Skipping."
            );
            return;
        }

        var target = targets[0];

        // ── Attribute add (type="@attrName") ────────────────────────────────────
        if (type != null)
        {
            if (type.StartsWith('@') && type.Length > 1)
            {
                target.SetAttributeValue(type[1..], addElement.Value);
                Logger.Info(
                    $"[Operation add] Set attribute '{type[1..]}' = '{addElement.Value}' on {XmlUtils.GetElementInfo(target)}"
                );
            }
            else
            {
                Logger.Warn($"[Operation add] Unsupported type value '{type}'. Skipping.");
            }
            return;
        }

        // ── Element / comment add (positional) ──────────────────────────────────
        XNode? latestAdded = null;
        foreach (var newNode in addElement.Nodes())
        {
            if (newNode is not XElement && newNode is not XComment)
                continue;

            XNode cloned = newNode is XElement ne
                ? new XElement(ne)
                : new XComment(((XComment)newNode).Value);

            if (latestAdded == null)
            {
                // First node: apply duplicate check for elements, then insert at position
                if (cloned is XElement clonedElem && !allowDoubles)
                {
                    // Duplicate check: both-direction attribute equality
                    IEnumerable<XElement> searchIn = pos is "before" or "after"
                        ? target.Parent!.Elements()
                        : target.Elements();

                    bool isDuplicate = searchIn.Any(e =>
                        e.Name == clonedElem.Name
                        && e.Attributes().All(a => clonedElem.Attribute(a.Name)?.Value == a.Value)
                        && clonedElem.Attributes().All(a => e.Attribute(a.Name)?.Value == a.Value)
                    );

                    if (isDuplicate)
                    {
                        Logger.Warn(
                            $"[Operation add] Duplicate element {XmlUtils.GetElementInfo(clonedElem)} already exists. Skipping."
                        );
                        continue;
                    }
                }

                switch (pos)
                {
                    case "before":
                        target.AddBeforeSelf(cloned);
                        Logger.Info(
                            $"[Operation add] Inserted before {XmlUtils.GetElementInfo(target)}"
                        );
                        break;
                    case "after":
                        target.AddAfterSelf(cloned);
                        Logger.Info(
                            $"[Operation add] Inserted after {XmlUtils.GetElementInfo(target)}"
                        );
                        break;
                    case "prepend":
                        target.AddFirst(cloned);
                        Logger.Info(
                            $"[Operation add] Prepended to {XmlUtils.GetElementInfo(target)}"
                        );
                        break;
                    case "append":
                        target.Add(cloned);
                        Logger.Info(
                            $"[Operation add] Appended to {XmlUtils.GetElementInfo(target)}"
                        );
                        break;
                    default:
                        Logger.Warn($"[Operation add] Unknown pos='{pos}'. Skipping.");
                        continue;
                }

                latestAdded = cloned;
            }
            else
            {
                // Subsequent nodes: always AddAfterSelf to preserve insertion order
                latestAdded.AddAfterSelf(cloned);
                latestAdded = cloned;
                Logger.Info($"[Operation add] Added subsequent node after previous");
            }
        }
    }

    /// <summary>
    /// Applies a &lt;replace&gt; operation to <paramref name="originalRoot"/>.
    /// </summary>
    public static void ApplyReplace(XElement replaceElement, XElement originalRoot)
    {
        var sel = replaceElement.Attribute("sel")?.Value;
        if (sel == null)
        {
            Logger.Warn("Replace operation missing 'sel' attribute. Skipping.");
            return;
        }

        Logger.Info($"[Operation replace] sel='{sel}'");

        var results = (originalRoot.XPathEvaluate(sel) as IEnumerable<object>)?.ToList();
        if (results == null || results.Count == 0)
        {
            Logger.Warn(
                $"[Operation replace] No nodes found for sel='{sel}'. Last resolvable: '{LastApplicableNode(sel, originalRoot)}'. Skipping."
            );
            return;
        }

        foreach (var result in results)
        {
            switch (result)
            {
                case XElement target:
                    var replaceContent = replaceElement
                        .Elements()
                        .Select(e => new XElement(e))
                        .ToArray();
                    if (replaceContent.Length > 0)
                    {
                        target.ReplaceWith(replaceContent.Cast<object>().ToArray());
                        Logger.Info(
                            $"[Operation replace] Replaced {XmlUtils.GetElementInfo(target)} with {replaceContent.Length} element(s)"
                        );
                    }
                    else
                    {
                        Logger.Warn(
                            $"[Operation replace] No child elements in replace for sel='{sel}'. Skipping."
                        );
                    }
                    break;

                case XText textNode:
                    textNode.Value = replaceElement.Value;
                    Logger.Info(
                        $"[Operation replace] Set text node value to '{replaceElement.Value}'"
                    );
                    break;

                case XAttribute attr:
                    attr.Value = replaceElement.Value;
                    Logger.Info(
                        $"[Operation replace] Set attribute '{attr.Name}' = '{replaceElement.Value}'"
                    );
                    break;

                default:
                    Logger.Warn(
                        $"[Operation replace] Unsupported node type '{result?.GetType().Name}' for sel='{sel}'. Skipping."
                    );
                    break;
            }
        }
    }

    /// <summary>
    /// Applies a &lt;remove&gt; operation to <paramref name="originalRoot"/>.
    /// </summary>
    public static void ApplyRemove(XElement removeElement, XElement originalRoot)
    {
        var sel = removeElement.Attribute("sel")?.Value;
        if (sel == null)
        {
            Logger.Warn("Remove operation missing 'sel' attribute. Skipping.");
            return;
        }

        Logger.Info($"[Operation remove] sel='{sel}'");

        var results = (originalRoot.XPathEvaluate(sel) as IEnumerable<object>)?.ToList();
        if (results == null || results.Count == 0)
        {
            Logger.Warn(
                $"[Operation remove] No nodes found for sel='{sel}'. Last resolvable: '{LastApplicableNode(sel, originalRoot)}'. Skipping."
            );
            return;
        }

        foreach (var result in results)
        {
            switch (result)
            {
                case XElement element when element.Parent != null:
                    element.Remove();
                    Logger.Info(
                        $"[Operation remove] Removed element {XmlUtils.GetElementInfo(element)}"
                    );
                    break;

                case XAttribute attr when attr.Parent != null:
                    attr.Remove();
                    Logger.Info($"[Operation remove] Removed attribute '{attr.Name}'");
                    break;

                case XText textNode:
                    textNode.Remove();
                    Logger.Info("[Operation remove] Removed text node");
                    break;

                default:
                    Logger.Warn(
                        $"[Operation remove] Cannot remove node type '{result?.GetType().Name}' or node has no parent. Skipping."
                    );
                    break;
            }
        }
    }

    // ─── Debug helper ────────────────────────────────────────────────────────────

    /// <summary>
    /// Splits sel on '/' and returns the longest prefix path that still matches something in root.
    /// Used in warning messages to aid debugging.
    /// </summary>
    public static string LastApplicableNode(string selector, XElement root)
    {
        var parts = selector.Split('/');
        var current = "";
        var last = "";
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                current += "/";
                continue;
            }
            current += (current.EndsWith("/") ? "" : "/") + part;
            try
            {
                var matches = root.XPathEvaluate(current) as IEnumerable<object>;
                if (matches?.Any() == true)
                    last = current;
                else
                    break;
            }
            catch
            {
                break;
            }
        }
        return last;
    }
}
