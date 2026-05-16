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

    // ── Step 5: LCS-based child comparison ──────────────────────────────────
    if (checkOnly)
    {
      for (int ci = 0; ci < Math.Min(originalChildren.Count, modifiedChildren.Count); ci++)
      {
        if (!ExactlyMatches(originalChildren[ci], modifiedChildren[ci]))
          return true;
        if (CompareElements(original, modified, diffRoot, originalChildren[ci], modifiedChildren[ci], checkOnly: true))
          return true;
      }
      return false;
    }

    var editSteps = ComputeDiff(originalChildren, modifiedChildren);
    int lastRemovedOrReplaced = -1;
    int s = 0;

    while (s < editSteps.Count)
    {
      var step = editSteps[s];

      if (step.Op == EditOp.Equal)
      {
        CompareElements(original, modified, diffRoot, originalChildren[step.IndexA], modifiedChildren[step.IndexB], checkOnly: false);
        s++;
      }
      else
      {
        // Collect consecutive Delete and Insert steps (edit block)
        var deletes = new List<int>();
        var inserts = new List<int>();
        while (s < editSteps.Count && editSteps[s].Op != EditOp.Equal)
        {
          if (editSteps[s].Op == EditOp.Delete)
            deletes.Add(editSteps[s].IndexA);
          else
            inserts.Add(editSteps[s].IndexB);
          s++;
        }

        // nextOrigIdx: original anchor after this block (for <add> positioning)
        int nextOrigIdx = s < editSteps.Count ? editSteps[s].IndexA : originalChildren.Count;

        ProcessEditBlock(
          deletes,
          inserts,
          nextOrigIdx,
          original,
          modified,
          originalChildren,
          modifiedChildren,
          origEl,
          diffRoot,
          ref lastRemovedOrReplaced
        );
      }
    }

    return false;
  }

  // ─── LCS edit types and helpers ──────────────────────────────────────────────

  private enum EditOp
  {
    Equal,
    Delete,
    Insert,
  }

  private readonly record struct EditStep(EditOp Op, int IndexA, int IndexB);

  /// <summary>
  /// Computes the LCS-based edit script between two child lists.
  /// Returns a list of Equal/Delete/Insert steps in forward (left-to-right) order.
  /// </summary>
  private static List<EditStep> ComputeDiff(IReadOnlyList<XElement> a, IReadOnlyList<XElement> b)
  {
    int n = a.Count,
      m = b.Count;
    var dp = new int[n + 1, m + 1];
    for (int i = 1; i <= n; i++)
    for (int j = 1; j <= m; j++)
      dp[i, j] = ExactlyMatches(a[i - 1], b[j - 1]) ? dp[i - 1, j - 1] + 1 : Math.Max(dp[i - 1, j], dp[i, j - 1]);

    var result = new List<EditStep>();
    int x = n,
      y = m;
    while (x > 0 || y > 0)
    {
      if (x > 0 && y > 0 && ExactlyMatches(a[x - 1], b[y - 1]) && dp[x, y] == dp[x - 1, y - 1] + 1)
      {
        result.Add(new EditStep(EditOp.Equal, x - 1, y - 1));
        x--;
        y--;
      }
      else if (y > 0 && (x == 0 || dp[x, y - 1] >= dp[x - 1, y]))
      {
        result.Add(new EditStep(EditOp.Insert, -1, y - 1));
        y--;
      }
      else
      {
        result.Add(new EditStep(EditOp.Delete, x - 1, -1));
        x--;
      }
    }
    result.Reverse();
    return result;
  }

  /// <summary>
  /// Processes one edit block (consecutive Delete/Insert steps between two Equal anchors).
  /// Greedily pairs deletes with compatible inserts (same name + ≤1 attr diff),
  /// emitting attribute-change ops, replace ops, remove ops, and add ops.
  /// </summary>
  private void ProcessEditBlock(
    List<int> deletes,
    List<int> inserts,
    int nextOrigIdx,
    XDocument original,
    XDocument modified,
    List<XElement> originalChildren,
    List<XElement> modifiedChildren,
    XElement origEl,
    XElement diffRoot,
    ref int lastRemovedOrReplaced
  )
  {
    var usedInserts = new bool[inserts.Count];
    var paired = new List<(int origIdx, int modIdx)>();
    var unpairedDeletes = new List<int>();

    // Greedily pair each delete with the first compatible insert (same name + ≤1 attr diff)
    foreach (var origIdx in deletes)
    {
      bool found = false;
      for (int ii = 0; ii < inserts.Count; ii++)
      {
        if (usedInserts[ii])
          continue;
        var origElem = originalChildren[origIdx];
        var modElem = modifiedChildren[inserts[ii]];
        if (origElem.Name == modElem.Name && CompareAttributes(origElem, modElem, checkOnly: true).matchedEnough)
        {
          usedInserts[ii] = true;
          paired.Add((origIdx, inserts[ii]));
          found = true;
          break;
        }
      }
      if (!found)
        unpairedDeletes.Add(origIdx);
    }

    var unpairedInserts = new List<int>();
    for (int ii = 0; ii < inserts.Count; ii++)
      if (!usedInserts[ii])
        unpairedInserts.Add(inserts[ii]);

    // Emit paired operations: attribute change (+ child recurse) or full replace
    foreach (var (origIdx, modIdx) in paired)
    {
      var origElem = originalChildren[origIdx];
      var modElem = modifiedChildren[modIdx];
      var (matchedEnough, savedOp) = CompareAttributes(origElem, modElem, checkOnly: false);
      if (matchedEnough)
      {
        if (savedOp != null)
        {
          DiffRootAddOperation(diffRoot, savedOp);
          Logger.Info($"[Operation {savedOp.Name.LocalName}] attribute: {savedOp.Attribute("sel")?.Value}");
          // Attribute was renamed — the element's XPath identity changed, so treat it
          // as "unavailable" for pos="after" anchoring in subsequent add operations.
          lastRemovedOrReplaced = Math.Max(lastRemovedOrReplaced, origIdx);
        }
        CompareElements(original, modified, diffRoot, origElem, modElem, checkOnly: false);
      }
      else
      {
        var xpath = XPathGenerator.GenerateXPath(origElem, _options);
        var replaceOp = new XElement("replace", new XAttribute("sel", xpath));
        replaceOp.Add(new XElement(modElem));
        DiffRootAddOperation(diffRoot, replaceOp);
        Logger.Info($"[Operation replace] {xpath}");
        lastRemovedOrReplaced = origIdx;
      }
    }

    // Emit unpaired removes
    foreach (var origIdx in unpairedDeletes)
    {
      var xpath = XPathGenerator.GenerateXPath(originalChildren[origIdx], _options);
      var removeOp = new XElement("remove", new XAttribute("sel", xpath));
      DiffRootAddOperation(diffRoot, removeOp);
      Logger.Info($"[Operation remove] {xpath}");
      lastRemovedOrReplaced = origIdx;
    }

    // Emit unpaired inserts as a single batched <add>
    if (unpairedInserts.Count > 0)
    {
      int j = unpairedInserts[0];
      int k = unpairedInserts[^1] + 1;

      // Any element touched in this block (removed OR paired/renamed) may have a stale
      // XPath if used as a pos="after" anchor.  Compute the highest touched original
      // index so BuildAddOperation can switch to pos="before" on the first stable Equal
      // element that follows the block.
      int maxBlockOrigIdx = lastRemovedOrReplaced;
      foreach (var (origIdx, _) in paired)
        maxBlockOrigIdx = Math.Max(maxBlockOrigIdx, origIdx);

      var addOp = BuildAddOperation(originalChildren, modifiedChildren, origEl, nextOrigIdx, j, k, maxBlockOrigIdx);
      DiffRootAddOperation(diffRoot, addOp);
      Logger.Info($"[Operation add] {unpairedInserts.Count} element(s)");
    }
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
        if (i < originalChildren.Count)
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
          // No next element exists; prev anchor may be stale (renamed/removed)
          // → fall back to appending to the parent element
          pos = string.Empty;
          sel = XPathGenerator.GenerateXPath(origEl, _options);
        }
      }
      else
      {
        pos = "after";
        sel = prevXPath;
      }
    }

    var addOp = string.IsNullOrEmpty(pos)
      ? new XElement("add", new XAttribute("sel", sel))
      : new XElement("add", new XAttribute("sel", sel), new XAttribute("pos", pos));

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
