using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using NLog;

namespace X4XmlDiffAndPatch;

/// <summary>
/// Generates an XML diff document describing the operations needed to transform
/// <c>original</c> into <c>modified</c>.  See spec §4 for the full algorithm.
/// </summary>
public class DiffEngine
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  private static readonly Regex NumericIndexPattern = new Regex(@"\[\d+\]");

  private readonly DiffOptions _options;

  public DiffEngine(DiffOptions options)
  {
    _options = options;
  }

  // ─── Public entry point ──────────────────────────────────────────────────────

  /// <summary>
  /// Compares two XML documents and returns a &lt;diff&gt; element containing the
  /// minimum set of add/replace/remove operations.
  /// </summary>
  public XElement GenerateDiff(XDocument original, XDocument modified)
  {
    var diffRoot = new XElement("diff");
    if (original.Root == null || modified.Root == null)
      return diffRoot;

    CompareElements(original, modified, diffRoot);
    return diffRoot;
  }

  // ─── Core recursive comparison ───────────────────────────────────────────────

  /// <summary>
  /// Recursively compares elements and emits diff operations into <paramref name="diffRoot"/>.
  /// Returns <c>true</c> if any difference was detected (meaningful only in checkOnly mode).
  /// </summary>
  private bool CompareElements(
    XDocument original,
    XDocument modified,
    XElement diffRoot,
    XElement? originalElem = null,
    XElement? modifiedElem = null,
    bool checkOnly = false
  )
  {
    // ── Step 1: element-level comparison ────────────────────────────────────
    if (originalElem != null && modifiedElem != null)
    {
      if (!checkOnly)
        Logger.Debug($"[Compare] {XmlUtils.GetElementInfo(originalElem)} vs {XmlUtils.GetElementInfo(modifiedElem)}");

      if (originalElem.Name != modifiedElem.Name)
      {
        Logger.Debug($"[Compare] Name mismatch: '{originalElem.Name.LocalName}' vs '{modifiedElem.Name.LocalName}'");
        return true; // name mismatch — only valid in checkOnly context
      }

      var origText = XmlUtils.GetTextValue(originalElem).Trim();
      var modText = XmlUtils.GetTextValue(modifiedElem).Trim();

      if (origText != modText)
      {
        if (checkOnly)
          return true;

        var xpath = XPathGenerator.GenerateXPath(originalElem, _options);
        if (!string.IsNullOrEmpty(modText))
        {
          var replaceOp = new XElement("replace", new XAttribute("sel", xpath), modText);
          DiffRootAddOperation(diffRoot, replaceOp);
          Logger.Info($"[Operation replace] text in {xpath}");
        }
        else
        {
          var removeOp = new XElement("remove", new XAttribute("sel", xpath + "/text()"));
          DiffRootAddOperation(diffRoot, removeOp);
          Logger.Info($"[Operation remove] text in {xpath}");
        }
      }
    }

    // ── Step 2: resolve element references ──────────────────────────────────
    var origEl = originalElem ?? original.Root!;
    var modEl = modifiedElem ?? modified.Root!;
    var originalChildren = origEl.Elements().ToList();
    var modifiedChildren = modEl.Elements().ToList();

    // ── Step 3: root attribute comparison (only at the actual document roots) ─
    if (!checkOnly && originalElem == null)
    {
      var (_, rootSavedOp) = CompareAttributes(original.Root!, modified.Root!, checkOnly: false);
      if (rootSavedOp != null)
      {
        DiffRootAddOperation(diffRoot, rootSavedOp);
        Logger.Info($"[Operation {rootSavedOp.Name.LocalName}] root attribute: {rootSavedOp.Attribute("sel")?.Value}");
      }
    }

    // ── Step 4: early exit in checkOnly when children counts differ ──────────
    if (checkOnly && originalChildren.Count != modifiedChildren.Count)
    {
      Logger.Debug($"[Compare] checkOnly: children count mismatch ({originalChildren.Count} vs {modifiedChildren.Count})");
      return true;
    }

    // ── Step 5: two-pointer child comparison ─────────────────────────────────
    int i = 0,
      j = 0;
    int lastRemovedOrReplaced = -1;

    while (i < originalChildren.Count && j < modifiedChildren.Count)
    {
      var originalChild = originalChildren[i];
      var modifiedChild = modifiedChildren[j];
      bool matchedEnough = false;

      Logger.Debug($"[Loop] i={i} j={j}: orig={XmlUtils.GetElementInfo(originalChild)} mod={XmlUtils.GetElementInfo(modifiedChild)}");

      if (originalChild.Name == modifiedChild.Name)
      {
        var (attrMatched, savedOp) = CompareAttributes(originalChild, modifiedChild, checkOnly);
        matchedEnough = attrMatched;
        Logger.Debug($"[Attrs] matchedEnough={matchedEnough} savedOp={savedOp?.Name.LocalName ?? "none"}");

        if (matchedEnough && savedOp != null)
        {
          // One attribute diff — verify children and next siblings also align.
          bool childrenMatch = !CompareElements(original, modified, diffRoot, originalChild, modifiedChild, checkOnly: true);

          bool modifiedMatchesLaterOriginal = originalChildren.Skip(i + 1).Any(oc => ExactlyMatches(oc, modifiedChild));

          Logger.Debug($"[Attrs] savedOp lookahead: childrenMatch={childrenMatch} modMatchesLater={modifiedMatchesLaterOriginal}");
          if (childrenMatch && !modifiedMatchesLaterOriginal)
          {
            if (!checkOnly)
            {
              DiffRootAddOperation(diffRoot, savedOp);
              Logger.Info($"[Operation {savedOp.Name.LocalName}] attribute: {savedOp.Attribute("sel")?.Value}");
            }
            // matchedEnough stays true
          }
          else
          {
            matchedEnough = false;
          }
        }
      }

      if (matchedEnough)
      {
        if (checkOnly)
        {
          if (CompareElements(original, modified, diffRoot, originalChild, modifiedChild, checkOnly: true))
            return true;
        }
        else
        {
          CompareElements(original, modified, diffRoot, originalChild, modifiedChild, checkOnly: false);
        }
        i++;
        j++;
      }
      else
      {
        if (checkOnly)
          return true;

        // ── Phase A: look for originalChild further ahead in modifiedChildren ──
        bool foundMatch = false;
        for (int k = j + 1; k < modifiedChildren.Count; k++)
        {
          if (ExactlyMatches(modifiedChildren[k], originalChild))
          {
            // modifiedChildren[j..k-1] are new → emit a single <add>
            var addOp = BuildAddOperation(originalChildren, modifiedChildren, origEl, i, j, k, lastRemovedOrReplaced);
            DiffRootAddOperation(diffRoot, addOp);
            Logger.Info($"[Operation add] {k - j} element(s) before {XmlUtils.GetElementInfo(originalChild)}");
            j = k;
            foundMatch = true;
            break;
          }
        }

        if (!foundMatch)
        {
          // ── Phase B: decide between remove and replace ─────────────────
          bool nextOriginalFoundLaterInModified =
            i + 1 < originalChildren.Count && modifiedChildren.Skip(j + 1).Any(mc => ExactlyMatches(mc, originalChildren[i + 1]));

          bool nextOriginalIsCurrentModified = originalChildren.Skip(i + 1).Any(oc => ExactlyMatches(oc, modifiedChild));

          Logger.Debug(
            $"[PhaseB] nextOrigFoundLater={nextOriginalFoundLaterInModified} nextOrigIsCurrentMod={nextOriginalIsCurrentModified}"
          );

          bool shouldReplace =
            !nextOriginalIsCurrentModified
            && (
              (
                originalChild.Name == modifiedChild.Name
                && originalChild.Attributes().Any(a => modifiedChild.Attribute(a.Name)?.Value == a.Value)
              )
              || i + 1 == originalChildren.Count
              || nextOriginalFoundLaterInModified
            );

          Logger.Debug($"[PhaseB] shouldReplace={shouldReplace}");
          if (shouldReplace)
          {
            var xpath = XPathGenerator.GenerateXPath(originalChild, _options);
            var replaceOp = new XElement("replace", new XAttribute("sel", xpath));
            replaceOp.Add(new XElement(modifiedChild));

            // Bundle additional modified children that don't match the next original
            int k = j + 1;
            while (k < modifiedChildren.Count)
            {
              if (i + 1 < originalChildren.Count && ExactlyMatches(modifiedChildren[k], originalChildren[i + 1]))
                break;
              replaceOp.Add(new XElement(modifiedChildren[k]));
              k++;
            }

            DiffRootAddOperation(diffRoot, replaceOp);
            Logger.Info($"[Operation replace] {xpath}");
            lastRemovedOrReplaced = i;
            i++;
            j = k;
          }
          else
          {
            var xpath = XPathGenerator.GenerateXPath(originalChild, _options);
            var removeOp = new XElement("remove", new XAttribute("sel", xpath));
            DiffRootAddOperation(diffRoot, removeOp);
            Logger.Info($"[Operation remove] {xpath}");
            lastRemovedOrReplaced = i;
            i++;
            // j intentionally stays the same
          }
        }
      }
    }

    // ── Drain remaining original children (all removed) ──────────────────────
    while (i < originalChildren.Count)
    {
      var xpath = XPathGenerator.GenerateXPath(originalChildren[i], _options);
      var removeOp = new XElement("remove", new XAttribute("sel", xpath));
      DiffRootAddOperation(diffRoot, removeOp);
      Logger.Info($"[Operation remove] {xpath}");
      i++;
    }

    // ── Drain remaining modified children (all appended) ─────────────────────
    if (j < modifiedChildren.Count)
    {
      var parentXPath = XPathGenerator.GenerateXPath(origEl, _options);
      var addOp = new XElement("add", new XAttribute("sel", parentXPath));
      for (int k = j; k < modifiedChildren.Count; k++)
        addOp.Add(new XElement(modifiedChildren[k]));
      DiffRootAddOperation(diffRoot, addOp);
      Logger.Info($"[Operation add] append to {parentXPath}");
    }

    return false;
  }

  // ─── Attribute comparison ────────────────────────────────────────────────────

  /// <summary>
  /// Compares attributes of two elements.  Returns (matchedEnough, savedOp) where:
  /// <list type="bullet">
  ///   <item>matchedEnough = true when there is at most one attribute difference</item>
  ///   <item>savedOp = the single diff operation to emit (null when 0 or &gt;1 diffs, or in checkOnly mode)</item>
  /// </list>
  /// </summary>
  private (bool matchedEnough, XElement? savedOp) CompareAttributes(
    XElement originalElement,
    XElement modifiedElement,
    bool checkOnly = false
  )
  {
    bool matchedEnough = true;
    XElement? savedOp = null;
    int differencesCount = 0;

    var originalAttrs = originalElement.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);
    var modifiedAttrs = modifiedElement.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);

    // Check attributes present in modified
    foreach (var kvp in modifiedAttrs)
    {
      if (kvp.Key == _options.IgnoreDiffInAttribute)
        continue;

      if (!originalAttrs.TryGetValue(kvp.Key, out var origValue))
      {
        differencesCount++;
        Logger.Debug($"[Attrs] '{kvp.Key}' added in modified");
        if (differencesCount > 1)
        {
          matchedEnough = false;
          break;
        }
        if (!checkOnly)
        {
          var xpath = XPathGenerator.GenerateXPath(originalElement, _options);
          savedOp = new XElement("add", new XAttribute("sel", xpath), new XAttribute("type", "@" + kvp.Key), kvp.Value);
        }
      }
      else if (origValue != kvp.Value)
      {
        differencesCount++;
        Logger.Debug($"[Attrs] '{kvp.Key}' changed: '{origValue}' → '{kvp.Value}'");
        if (differencesCount > 1)
        {
          matchedEnough = false;
          break;
        }
        if (!checkOnly)
        {
          var xpath = XPathGenerator.GenerateXPath(originalElement, _options);
          savedOp = new XElement("replace", new XAttribute("sel", $"{xpath}/@{kvp.Key}"), kvp.Value);
        }
      }
    }

    // Check attributes present in original but absent from modified
    if (matchedEnough)
    {
      foreach (var kvp in originalAttrs)
      {
        if (kvp.Key == _options.IgnoreDiffInAttribute)
          continue;

        if (!modifiedAttrs.ContainsKey(kvp.Key))
        {
          Logger.Debug($"[Attrs] '{kvp.Key}' removed from original");
          if (checkOnly)
            return (true, null); // has diff but ≤1 total — matched enough

          differencesCount++;
          if (differencesCount > 1)
          {
            matchedEnough = false;
            break;
          }
          var xpath = XPathGenerator.GenerateXPath(originalElement, _options);
          savedOp = new XElement("remove", new XAttribute("sel", $"{xpath}/@{kvp.Key}"));
        }
      }
    }

    if (matchedEnough && differencesCount == 1)
    {
      if (checkOnly)
        return (true, null);
      return (true, savedOp);
    }

    return (matchedEnough, null);
  }

  // ─── Add operation builder ───────────────────────────────────────────────────

  /// <summary>
  /// Builds the &lt;add&gt; element for inserting modifiedChildren[j..k-1] into the patched tree.
  /// Determines whether to use pos="before", pos="after", or pos="prepend".
  /// </summary>
  private XElement BuildAddOperation(
    List<XElement> originalChildren,
    List<XElement> modifiedChildren,
    XElement origEl,
    int i,
    int j,
    int k,
    int lastRemovedOrReplaced
  )
  {
    string pos;
    string sel;

    if (i == 0)
    {
      pos = "prepend";
      sel = XPathGenerator.GenerateXPath(origEl, _options);
    }
    else
    {
      bool usePosBeforeComment = XmlUtils.IsElementPrecededByPosBeforeComment(modifiedChildren[j]);
      bool prevAnchorRemoved = lastRemovedOrReplaced == i - 1;
      string prevXPath = XPathGenerator.GenerateXPath(originalChildren[i - 1], _options);
      bool prevHasNumericIndex = NumericIndexPattern.IsMatch(prevXPath);

      Logger.Debug($"[AddPos] posBeforeComment={usePosBeforeComment} prevRemoved={prevAnchorRemoved} prevNumericIdx={prevHasNumericIndex}");

      if (usePosBeforeComment || prevAnchorRemoved || prevHasNumericIndex)
      {
        // Prefer pos="before" on the current original element
        string beforeXPath = XPathGenerator.GenerateXPath(originalChildren[i], _options);
        if (!NumericIndexPattern.IsMatch(beforeXPath))
        {
          pos = "before";
          sel = beforeXPath;
        }
        else
        {
          // Fall back to pos="after" the previous element
          pos = "after";
          sel = prevXPath;
        }
      }
      else
      {
        pos = "after";
        sel = prevXPath;
      }
    }

    var addOp = new XElement("add", new XAttribute("sel", sel), new XAttribute("pos", pos));

    for (int n = j; n < k; n++)
      addOp.Add(new XElement(modifiedChildren[n]));

    return addOp;
  }

  // ─── Ordering rule for diffRoot ──────────────────────────────────────────────

  /// <summary>
  /// Appends <paramref name="op"/> to <paramref name="diffRoot"/>, but inserts it BEFORE
  /// the last child when that last child is a &lt;remove&gt; with the same sel as op.
  /// This ensures correct sequential patching order.
  /// </summary>
  private static void DiffRootAddOperation(XElement diffRoot, XElement op)
  {
    var last = diffRoot.Elements().LastOrDefault();
    if (
      last != null
      && last.Name.LocalName == "remove"
      && last.Attribute("sel")?.Value == op.Attribute("sel")?.Value
      && op.Name.LocalName != "remove"
    )
    {
      last.AddBeforeSelf(op);
    }
    else
    {
      diffRoot.Add(op);
    }
  }

  // ─── Exact element match ─────────────────────────────────────────────────────

  /// <summary>
  /// Two elements exactly match when they have the same name, the same number of attributes,
  /// and every attribute value is identical.
  /// </summary>
  private static bool ExactlyMatches(XElement a, XElement b)
  {
    if (a.Name != b.Name)
      return false;
    var aAttrs = a.Attributes().ToList();
    var bAttrs = b.Attributes().ToList();
    if (aAttrs.Count != bAttrs.Count)
      return false;
    if (!aAttrs.All(attr => b.Attribute(attr.Name)?.Value == attr.Value))
      return false;
    return XmlUtils.GetTextValue(a).Trim() == XmlUtils.GetTextValue(b).Trim();
  }
}
