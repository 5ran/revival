using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DataModelTreeViewer;

internal sealed class TreeViewerForm : Form
{
    private readonly Panel loadingPanel = new() { Dock = DockStyle.Fill, Visible = false, BackColor = Color.White };
    private readonly Label loadingLabel = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold) };
    private readonly ProgressBar loadingProgress = new() { Dock = DockStyle.Bottom, Height = 22, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 25 };
    private readonly TreeView tree = new() { Dock = DockStyle.Fill, HideSelection = false };
    private readonly ListView details = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
    private readonly ListView offsets = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
    private readonly ListView allNodes = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
    private readonly ListView bookmarks = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
    private readonly TextBox searchBox = new() { Dock = DockStyle.Fill };
    private readonly Label status = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly System.Windows.Forms.Timer liveRefreshTimer = new() { Interval = 1000 };
    private readonly ToolStripStatusLabel toolbarStatus = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private IReadOnlyList<OffsetEntry> parsedOffsets = Array.Empty<OffsetEntry>();
    private LocalOffsetsSource? liveOffsets;
    private DataModelNode? selectedLiveNode;
    private ulong watchedParentPointer;
    private bool isLoading;
    private bool searchBoxMatch;
    private bool searchVisibleText = true;
    private bool searchSubtreeOnly;
    private TabControl? tabs;
    private string lastSearchText = string.Empty;
    private List<TreeNode> rankedSearchHits = new();
    private int rankedSearchIndex = -1;
    private Rectangle boxMatchSelection;
    private bool watchParentEnabled;
    private bool watchParentBusy;
    private readonly List<BookmarkEntry> bookmarkEntries = new();

    public TreeViewerForm()
    {
        Text = "DataModel Tree Viewer";
        Width = 1280;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = BuildToolbar();
        root.Controls.Add(toolbar, 0, 0);

        tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildTreePage());
        tabs.TabPages.Add(BuildAllNodesPage());
        tabs.TabPages.Add(BuildBookmarksPage());
        tabs.TabPages.Add(BuildOffsetsPage());
        tabs.SelectedIndex = 1;
        root.Controls.Add(tabs, 0, 1);
        Controls.Add(root);
        loadingPanel.Controls.Add(loadingLabel);
        loadingPanel.Controls.Add(loadingProgress);
        Controls.Add(loadingPanel);
        liveRefreshTimer.Tick += (_, _) =>
        {
            if (watchParentEnabled)
            {
                _ = ProcessWatchParentBatchAsync();
            }
        };

        Load += (_, _) =>
        {
            LoadOffsets();
            status.Text = "Offsets loaded. Loading live DataModel...";
            BeginInvoke(new Action(RefreshLiveDataModel));
        };
        FormClosed += (_, _) => liveRefreshTimer.Stop();
    }

    private TabPage BuildTreePage()
    {
        var page = new TabPage("DataModel");
        details.Columns.Add("Property", 220);
        details.Columns.Add("Value", 650);
        tree.AfterSelect += (_, e) => ShowNode(e.Node);
        tree.NodeMouseClick += (_, e) =>
        {
            if (tree.SelectedNode == e.Node)
            {
                ClearTreeSelection();
                return;
            }

            tree.SelectedNode = e.Node;
            ShowNode(e.Node);
        };

        var searchPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 34, ColumnCount = 3 };
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        searchPanel.Controls.Add(new Label { Text = "Search", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        searchPanel.Controls.Add(searchBox, 1, 0);
        var find = new Button { Text = "Search", Dock = DockStyle.Fill };
        find.Click += (_, _) => FindNext();
        searchPanel.Controls.Add(find, 2, 0);

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 500 };
        split.Panel1.Controls.Add(tree);
        split.Panel2.Controls.Add(details);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(searchPanel, 0, 1);
        layout.Controls.Add(split, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildOffsetsPage()
    {
        var page = new TabPage("Offsets");
        offsets.Columns.Add("Category", 260);
        offsets.Columns.Add("Name", 240);
        offsets.Columns.Add("Value", 120);
        offsets.Columns.Add("Kind", 90);
        page.Controls.Add(offsets);
        return page;
    }

    private TabPage BuildAllNodesPage()
    {
        var page = new TabPage("All Nodes");
        allNodes.Columns.Add("Name", 220);
        allNodes.Columns.Add("ClassName", 180);
        allNodes.Columns.Add("Address", 150);
        allNodes.Columns.Add("Parent", 220);
        allNodes.Columns.Add("Children", 80);
        allNodes.Columns.Add("Path", 520);
        allNodes.SelectedIndexChanged += (_, _) => ShowAllNodesSelectionSafe();
        allNodes.DoubleClick += (_, _) => SelectAllNodesItemSafe();
        page.Controls.Add(allNodes);
        return page;
    }

    private TabPage BuildBookmarksPage()
    {
        var page = new TabPage("Bookmarks");
        bookmarks.Columns.Add("Name", 220);
        bookmarks.Columns.Add("Path", 520);
        bookmarks.Columns.Add("Address", 150);
        bookmarks.DoubleClick += (_, _) => JumpToBookmark();
        page.Controls.Add(bookmarks);
        return page;
    }

    private static TreeNode BuildTreeNode(DataModelNode node)
    {
        var classPart = string.IsNullOrWhiteSpace(node.ClassName) ? string.Empty : $" ({node.ClassName})";
        var colorPart = GetPropertyText(node, "BackgroundColor3", "TextColor3", "BorderColor3", "ImageColor3", "ImageRectOffset", "Color", "BrickColor", "TeamColor");
        var addressPart = string.IsNullOrWhiteSpace(node.Address) ? string.Empty : $" [{node.Address}]";
        var label = $"{node.Name}{classPart}{colorPart}{addressPart}";
        var treeNode = new TreeNode(label)
        {
            Tag = node,
            ForeColor = GetNodeColor(node),
        };
        foreach (var child in node.Children)
        {
            treeNode.Nodes.Add(BuildTreeNode(child));
        }

        return treeNode;
    }

    private void ShowNode(TreeNode? selected)
    {
        details.Items.Clear();
        if (selected?.Tag is not DataModelNode node)
        {
            selectedLiveNode = null;
            watchedParentPointer = 0;
            NodeHighlightOverlay.Hide();
            return;
        }

        selectedLiveNode = node;
        if (watchParentEnabled)
        {
            watchedParentPointer = node.Pointer;
            ResetWatchParentState(node.Pointer);
        }

        AddDetail("Name", node.Name);
        AddDetail("ClassName", node.ClassName);
        AddDetail("Address", node.Address);
        AddDetail("Path", node.Path);
        AddDetail("Parent", FormatNodeReference(node.Parent));
        AddDetail("IndexInParent", GetSiblingIndexText(node).Trim());
        AddDetail("Children", node.Children.Count.ToString());
        AddDetail("Descendants", CountNodes(node).MinusRoot().ToString("N0"));
        if (node.Parent is not null)
        {
            AddDetail("Siblings", Math.Max(0, node.Parent.Children.Count - 1).ToString("N0"));
        }

        foreach (var property in node.Properties.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            AddDetail(property.Key, property.Value);
        }

        foreach (var child in node.Children.OrderBy(child => child.Name, StringComparer.OrdinalIgnoreCase))
        {
            AddDetail("child", FormatNodeReference(child));
        }

        foreach (var entry in GetApplicableOffsets(node))
        {
            AddDetail($"offset:{entry.Category}.{entry.Name}", $"{entry.Value} ({entry.Kind})");
        }

        NodeHighlightOverlay.Show(node.Bounds, FormatNodeReference(node));
    }

    private void AddDetail(string name, string value)
    {
        details.Items.Add(new ListViewItem(new[] { name, value }));
    }

    private void FindNext()
    {
        if (searchBoxMatch)
        {
            BeginBoxMatchSearch();
            return;
        }

        var text = searchBox.Text.Trim();
        if (text.Length == 0 || tree.Nodes.Count == 0)
        {
            return;
        }

        if (!string.Equals(text, lastSearchText, StringComparison.OrdinalIgnoreCase))
        {
            lastSearchText = text;
            var sourceNodes = searchSubtreeOnly && selectedLiveNode is not null
                ? FlattenNodeTree(FindTreeNodeByPointer(selectedLiveNode.Pointer))
                : Flatten(tree.Nodes);

            rankedSearchHits = sourceNodes
                .Select(node => new { Node = node, Score = ScoreMatch(node, text) })
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Node.Text, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.Node)
                .ToList();
            rankedSearchIndex = -1;
        }

        if (rankedSearchHits.Count == 0)
        {
            toolbarStatus.Text = "No matches found.";
            return;
        }

        rankedSearchIndex = (rankedSearchIndex + 1) % rankedSearchHits.Count;
        var nodeToSelect = rankedSearchHits[rankedSearchIndex];
        tree.SelectedNode = nodeToSelect;
        nodeToSelect.EnsureVisible();
        toolbarStatus.Text = $"Match {rankedSearchIndex + 1} of {rankedSearchHits.Count}.";
    }

    private IEnumerable<TreeNode> FlattenNodeTree(TreeNode? node)
    {
        if (node is null)
        {
            yield break;
        }

        yield return node;
        foreach (TreeNode child in node.Nodes)
        {
            foreach (var descendant in FlattenNodeTree(child))
            {
                yield return descendant;
            }
        }
    }

    private int ScoreMatch(TreeNode node, string text)
    {
        var score = 0;

        if (searchBoxMatch)
        {
            if (string.Equals(node.Text, text, StringComparison.OrdinalIgnoreCase))
            {
                score += 1000;
            }
            else if (node.Text.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                score += 700;
            }
            else if (node.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                score += 400;
            }
        }
        else
        {
            if (string.Equals(node.Text, text, StringComparison.OrdinalIgnoreCase))
            {
                score += 900;
            }
            else if (node.Text.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                score += 650;
            }
            else if (node.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                score += 300;
            }
        }

        if (node.Tag is DataModelNode data)
        {
            if (string.Equals(data.Name, text, StringComparison.OrdinalIgnoreCase))
            {
                score += 500;
            }

            if (data.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                score += 300;
            }

            if (data.Path.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                score += 150;
            }

            if (searchVisibleText)
            {
                if (data.ClassName.Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    score += 120;
                }

                score += data.Properties.Count(p => p.Key.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                                                    p.Value.Contains(text, StringComparison.OrdinalIgnoreCase)) * 40;
            }
        }

        score -= Math.Min(GetTreeDepth(node) * 2, 40);
        return score;
    }

    private void BeginBoxMatchSearch()
    {
        using var picker = new BoxSelectionForm();
        Hide();
        try
        {
            if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedScreenRect.Width <= 0 || picker.SelectedScreenRect.Height <= 0)
            {
                toolbarStatus.Text = "Box match cancelled.";
                return;
            }

            boxMatchSelection = picker.SelectedScreenRect;
            var ranked = Flatten(tree.Nodes)
                .Select(node => new { Node = node, Score = ScoreBoxMatch(node, boxMatchSelection) })
                .Where(item => item.Score < int.MaxValue)
                .OrderBy(item => item.Score)
                .ThenBy(item => item.Node.Text, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.Node)
                .ToList();

            if (ranked.Count == 0)
            {
                toolbarStatus.Text = "No box matches found.";
                return;
            }

            rankedSearchHits = ranked;
            rankedSearchIndex = 0;
            var node = rankedSearchHits[0];
            tree.SelectedNode = node;
            node.EnsureVisible();
            toolbarStatus.Text = $"Box match 1 of {rankedSearchHits.Count}.";
        }
        finally
        {
            Show();
            Activate();
        }
    }

    private int ScoreBoxMatch(TreeNode node, Rectangle box)
    {
        if (node.Tag is not DataModelNode data || data.Bounds is null)
        {
            return int.MaxValue;
        }

        var bounds = ToRectangle(data.Bounds.Value);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return int.MaxValue;
        }

        if (Intersects(bounds, box))
        {
            return 0;
        }

        var centerDx = CenterDistance(bounds, box);
        var edgeDx = RectDistance(bounds, box);
        return (int)(edgeDx * 10 + centerDx);
    }

    private static Rectangle ToRectangle(GuiBounds bounds)
    {
        return new Rectangle(
            (int)Math.Round(bounds.X),
            (int)Math.Round(bounds.Y),
            (int)Math.Round(bounds.Width),
            (int)Math.Round(bounds.Height));
    }

    private static bool Intersects(Rectangle a, Rectangle b)
    {
        return a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;
    }

    private static double CenterDistance(Rectangle a, Rectangle b)
    {
        var ax = a.Left + a.Width / 2.0;
        var ay = a.Top + a.Height / 2.0;
        var bx = b.Left + b.Width / 2.0;
        var by = b.Top + b.Height / 2.0;
        return Math.Sqrt(Math.Pow(ax - bx, 2) + Math.Pow(ay - by, 2));
    }

    private static double RectDistance(Rectangle a, Rectangle b)
    {
        var dx = Math.Max(0, Math.Max(a.Left - b.Right, b.Left - a.Right));
        var dy = Math.Max(0, Math.Max(a.Top - b.Bottom, b.Top - a.Bottom));
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static int GetTreeDepth(TreeNode node)
    {
        var depth = 0;
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            depth++;
        }

        return depth;
    }

    private static IEnumerable<TreeNode> Flatten(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Nodes))
            {
                yield return child;
            }
        }
    }

    private void LoadOffsets()
    {
        offsets.Items.Clear();
        var offsetsPath = Path.Combine(AppContext.BaseDirectory, "offsets.hpp");
        parsedOffsets = OffsetsParser.Parse(offsetsPath);
        liveOffsets = LocalOffsetsSource.Load(offsetsPath);
        foreach (var entry in parsedOffsets)
        {
            offsets.Items.Add(new ListViewItem(new[] { entry.Category, entry.Name, entry.Value, entry.Kind }));
        }
    }

    private async void RefreshLiveDataModel()
    {
        if (isLoading)
        {
            return;
        }

        if (liveOffsets is null || !liveOffsets.IsPopulated)
        {
            status.Text = "Offsets unavailable. offsets.hpp must be next to the executable.";
            return;
        }

        isLoading = true;
        ShowLoading("Loading live DataModel...");
        try
        {
            Cursor = Cursors.WaitCursor;
            status.Text = "Reading live DataModel...";
            var rootNode = await Task.Run(() =>
            {
                using var reader = new DataModelLiveReader(liveOffsets);
                return reader.Read();
            });

            loadingLabel.Text = "Rendering tree...";
            status.Text = "Rendering live DataModel...";

            tree.BeginUpdate();
            tree.Nodes.Clear();
            tree.Nodes.Add(BuildTreeNode(rootNode));
            tree.Nodes[0].Expand();
            tree.EndUpdate();

            PopulateAllNodes(rootNode);
            RefreshBookmarks();
            ReselectLiveNode(rootNode);
            if (tree.SelectedNode is null)
            {
                tree.SelectedNode = tree.Nodes[0];
            }
            ShowNode(tree.SelectedNode);
            RefreshSelectedOverlay();
            status.Text = $"Live DataModel loaded. {CountNodes(rootNode):N0} nodes.";
            toolbarStatus.Text = status.Text;
        }
        catch (Exception ex)
        {
            status.Text = "Live read failed: " + ex.Message;
            toolbarStatus.Text = status.Text;
        }
        finally
        {
            Cursor = Cursors.Default;
            HideLoading();
            isLoading = false;
        }
    }

    private void RefreshSelectedOverlay()
    {
        if (selectedLiveNode?.Bounds is null)
        {
            NodeHighlightOverlay.Hide();
            return;
        }

        NodeHighlightOverlay.Show(selectedLiveNode.Bounds, FormatNodeReference(selectedLiveNode));
    }

    private void ResetWatchParentState(ulong rootPointer)
    {
    }

    private async Task ProcessWatchParentBatchAsync()
    {
        if (!watchParentEnabled || watchedParentPointer == 0 || liveOffsets is null || !liveOffsets.IsPopulated || watchParentBusy)
        {
            return;
        }

        watchParentBusy = true;
        try
        {
            var current = FindTreeNodeByPointer(watchedParentPointer);
            if (current is null || current.Tag is not DataModelNode currentData)
            {
                return;
            }

            var snapshot = await Task.Run(() =>
            {
                using var reader = new DataModelLiveReader(liveOffsets);
                return reader.ReadSubtree(watchedParentPointer);
            });

            if (snapshot is null)
            {
                return;
            }

            MergeSnapshotSubtree(current, currentData, snapshot);
            toolbarStatus.Text = $"Watching {currentData.Name}.";
        }
        finally
        {
            watchParentBusy = false;
        }
    }

    private async Task SeedWatchedParentAsync()
    {
        if (!watchParentEnabled || watchedParentPointer == 0 || liveOffsets is null || !liveOffsets.IsPopulated)
        {
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
            {
                using var reader = new DataModelLiveReader(liveOffsets);
                return reader.ReadSubtree(watchedParentPointer);
            });

            if (snapshot is null)
            {
                return;
            }

            var current = FindTreeNodeByPointer(watchedParentPointer);
            if (current is null || current.Tag is not DataModelNode currentData)
            {
                return;
            }

            MergeSnapshotSubtree(current, currentData, snapshot);
            toolbarStatus.Text = $"Watching {currentData.Name}.";
        }
        catch (Exception ex)
        {
            toolbarStatus.Text = "Watch seed failed: " + ex.Message;
        }
    }

    private void MergeNewDirectChildren(TreeNode currentTreeNode, DataModelNode currentData, IReadOnlyList<DataModelNode> liveChildren)
    {
        foreach (var liveChild in liveChildren)
        {
            var existingTreeChild = currentTreeNode.Nodes.Cast<TreeNode>()
                .FirstOrDefault(node => node.Tag is DataModelNode data && data.Pointer == liveChild.Pointer);

            if (existingTreeChild is null)
            {
                liveChild.Parent = currentData;
                liveChild.Path = currentData.Path + "/" + liveChild.Name;
                currentTreeNode.Nodes.Add(BuildTreeNode(liveChild));
                currentData.Children.Add(liveChild);
                continue;
            }

            if (existingTreeChild.Tag is DataModelNode existingData)
            {
                existingData.Name = liveChild.Name;
                existingData.ClassName = liveChild.ClassName;
                existingData.Address = liveChild.Address;
                existingData.Path = liveChild.Path;
                existingData.Bounds = liveChild.Bounds;
                existingData.Parent = currentData;
                existingData.Properties.Clear();
                foreach (var property in liveChild.Properties)
                {
                    existingData.Properties[property.Key] = property.Value;
                }
            }
        }
    }

    private void MergeSnapshotSubtree(TreeNode currentTreeNode, DataModelNode currentData, DataModelNode liveSnapshot)
    {
        MergeNewDirectChildren(currentTreeNode, currentData, liveSnapshot.Children);

        foreach (var liveChild in liveSnapshot.Children)
        {
            var childNode = currentTreeNode.Nodes.Cast<TreeNode>()
                .FirstOrDefault(node => node.Tag is DataModelNode data && data.Pointer == liveChild.Pointer);
            if (childNode?.Tag is not DataModelNode childData)
            {
                continue;
            }

            MergeSnapshotSubtree(childNode, childData, liveChild);
        }
    }

    private void AddBookmark()
    {
        var node = GetSelectedNode();
        if (node is null)
        {
            toolbarStatus.Text = "Select a node first.";
            return;
        }

        if (bookmarkEntries.Any(entry => entry.Pointer == node.Pointer))
        {
            toolbarStatus.Text = "Bookmark already exists.";
            return;
        }

        bookmarkEntries.Add(new BookmarkEntry(node.Name, node.Pointer, node.Path, node.Address));
        RefreshBookmarks();
        toolbarStatus.Text = $"Bookmarked {node.Name}.";
    }

    private void RefreshBookmarks()
    {
        bookmarks.BeginUpdate();
        bookmarks.Items.Clear();
        foreach (var entry in bookmarkEntries)
        {
            bookmarks.Items.Add(new ListViewItem(new[] { entry.Name, entry.Path, entry.Address }) { Tag = entry });
        }
        bookmarks.EndUpdate();
    }

    private void JumpToBookmark()
    {
        if (bookmarks.SelectedItems.Count == 0 || bookmarks.SelectedItems[0].Tag is not BookmarkEntry entry)
        {
            return;
        }

        var node = FindTreeNodeByPointer(entry.Pointer);
        if (node is null)
        {
            toolbarStatus.Text = "Bookmark not found in tree.";
            return;
        }

        JumpToNode(node);
    }

    private static DataModelNode? FindNodeByPointer(DataModelNode node, ulong pointer)
    {
        if (node.Pointer == pointer)
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindNodeByPointer(child, pointer);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private void JumpToCurrentSearchHit()
    {
        if (rankedSearchHits.Count == 0 || rankedSearchIndex < 0 || rankedSearchIndex >= rankedSearchHits.Count)
        {
            toolbarStatus.Text = "Run a search first.";
            return;
        }

        JumpToNode(rankedSearchHits[rankedSearchIndex]);
    }

    private void JumpToNode(TreeNode node)
    {
        var path = new List<TreeNode>();
        for (var current = node; current is not null; current = current.Parent)
        {
            path.Add(current);
        }

        path.Reverse();
        foreach (var ancestor in path)
        {
            ancestor.Expand();
        }

        tree.SelectedNode = node;
        node.EnsureVisible();
        ShowNode(node);
        tabs?.SelectTab(0);
    }

    private TreeNode? FindTreeNodeByPointer(ulong pointer)
    {
        foreach (var node in Flatten(tree.Nodes))
        {
            if (node.Tag is DataModelNode data && data.Pointer == pointer)
            {
                return node;
            }
        }

        return null;
    }

    private void ReselectLiveNode(DataModelNode rootNode)
    {
        if (selectedLiveNode is null)
        {
            return;
        }

        var liveNode = FindNodeByPointer(rootNode, selectedLiveNode.Pointer);
        if (liveNode is null)
        {
            return;
        }

        selectedLiveNode = liveNode;
    }

    private void ShowLoading(string message)
    {
        loadingLabel.Text = message;
        loadingPanel.Visible = true;
        loadingPanel.BringToFront();
        loadingProgress.MarqueeAnimationSpeed = 25;
    }

    private void HideLoading()
    {
        loadingProgress.MarqueeAnimationSpeed = 0;
        loadingPanel.Visible = false;
    }

    private void PopulateAllNodes(DataModelNode rootNode)
    {
        allNodes.BeginUpdate();
        allNodes.Items.Clear();
        foreach (var node in FlattenData(rootNode))
        {
            var item = new ListViewItem(new[]
            {
                node.Name,
                node.ClassName,
                node.Address,
                FormatNodeReference(node.Parent),
                node.Children.Count.ToString(),
                node.Path,
            })
            {
                Tag = node,
                ForeColor = GetNodeColor(node),
            };
            allNodes.Items.Add(item);
        }
        allNodes.EndUpdate();
    }

    private void SelectAllNodesItemSafe()
    {
        try
        {
            SelectAllNodesItem();
        }
        catch (Exception ex)
        {
            toolbarStatus.Text = "All Nodes jump failed: " + ex.Message;
        }
    }

    private void SelectAllNodesItem()
    {
        if (allNodes.SelectedItems.Count == 0 || allNodes.SelectedItems[0].Tag is not DataModelNode selected)
        {
            return;
        }

        if (tree.SelectedNode?.Tag is DataModelNode selectedTree && selectedTree.Pointer == selected.Pointer)
        {
            ClearTreeSelection();
            allNodes.SelectedItems.Clear();
            NodeHighlightOverlay.Hide();
            return;
        }

        foreach (var node in Flatten(tree.Nodes))
        {
            if (node.Tag is DataModelNode live && live.Pointer == selected.Pointer)
            {
                tree.SelectedNode = node;
                node.EnsureVisible();
                ShowNode(node);
                return;
            }
        }
    }

    private void ClearTreeSelection()
    {
        details.Items.Clear();
        selectedLiveNode = null;
        watchedParentPointer = 0;
        tree.SelectedNode = null;
        tree.HideSelection = false;
        NodeHighlightOverlay.Hide();
        toolbarStatus.Text = "Selection cleared.";
    }

    private void ShowAllNodesSelectionSafe()
    {
        try
        {
            ShowAllNodesSelection();
        }
        catch (Exception ex)
        {
            toolbarStatus.Text = "All Nodes selection failed: " + ex.Message;
            NodeHighlightOverlay.Hide();
        }
    }

    private void ShowAllNodesSelection()
    {
        if (allNodes.SelectedItems.Count == 0 || allNodes.SelectedItems[0].Tag is not DataModelNode selected)
        {
            NodeHighlightOverlay.Hide();
            return;
        }

        if (selected.Bounds is null)
        {
            NodeHighlightOverlay.Hide();
            return;
        }

        NodeHighlightOverlay.Show(selected.Bounds, FormatNodeReference(selected));
    }

    private static IEnumerable<DataModelNode> FlattenData(DataModelNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in FlattenData(child))
            {
                yield return descendant;
            }
        }
    }

    private static string FormatNodeReference(DataModelNode? node)
    {
        if (node is null)
        {
            return string.Empty;
        }

        var classPart = string.IsNullOrWhiteSpace(node.ClassName) ? string.Empty : $" ({node.ClassName})";
        var addressPart = string.IsNullOrWhiteSpace(node.Address) ? string.Empty : $" [{node.Address}]";
        return $"{node.Name}{classPart}{addressPart}";
    }

    private IEnumerable<OffsetEntry> GetApplicableOffsets(DataModelNode node)
    {
        if (parsedOffsets.Count == 0)
        {
            yield break;
        }

        foreach (var category in GetOffsetCategoriesForNode(node))
        {
            foreach (var entry in parsedOffsets.Where(entry => entry.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                                               .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
            {
                yield return entry;
            }
        }
    }

    private static IEnumerable<string> GetOffsetCategoriesForNode(DataModelNode node)
    {
        yield return "Offsets.Instance";

        if (!string.IsNullOrWhiteSpace(node.ClassName))
        {
            yield return "Offsets." + node.ClassName;
        }

        if (node.ClassName.Equals("DataModel", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Equals("DataModel", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Offsets.DataModel";
            yield return "Offsets.FakeDataModel";
        }

        if (node.ClassName.Equals("VisualEngine", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Equals("VisualEngine", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Offsets.VisualEngine";
        }
    }

    private static int CountNodes(DataModelNode node)
    {
        return 1 + node.Children.Sum(CountNodes);
    }

    private static Color GetNodeColor(DataModelNode node)
    {
        if (node.ClassName.Equals("ScreenGui", StringComparison.OrdinalIgnoreCase))
        {
            return Color.MediumPurple;
        }

        if (node.ClassName.Equals("Frame", StringComparison.OrdinalIgnoreCase))
        {
            return Color.RoyalBlue;
        }

        if (node.ClassName.Equals("ImageLabel", StringComparison.OrdinalIgnoreCase))
        {
            return Color.ForestGreen;
        }

        if (node.ClassName.Equals("TextLabel", StringComparison.OrdinalIgnoreCase) ||
            node.ClassName.Equals("TextButton", StringComparison.OrdinalIgnoreCase))
        {
            return Color.DarkOrange;
        }

        if (node.ClassName.Equals("Sound", StringComparison.OrdinalIgnoreCase))
        {
            return Color.SteelBlue;
        }

        if (node.Children.Count > 10)
        {
            return Color.DarkCyan;
        }

        return Color.Black;
    }

    private static string GetPropertyText(DataModelNode node, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (node.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return $" {key}={value}";
            }
        }

        return string.Empty;
    }

    private static string GetSiblingIndexText(DataModelNode node)
    {
        if (node.Parent is null)
        {
            return "root";
        }

        var index = node.Parent.Children.FindIndex(child => child.Pointer == node.Pointer);
        return index >= 0 ? $" #{index + 1}" : string.Empty;
    }

    private ToolStrip BuildToolbar()
    {
        var toolbar = new ToolStrip { Dock = DockStyle.Fill, GripStyle = ToolStripGripStyle.Hidden };

        var refresh = new ToolStripButton("Refresh");
        var bookmark = new ToolStripButton("Bookmark");
        var subtreeSearch = new ToolStripButton("Subtree Search") { CheckOnClick = true };
        var jumpTo = new ToolStripButton("Jump To");
        var watchParent = new ToolStripButton("Watch Parent") { CheckOnClick = true };
        var boxMatch = new ToolStripButton("Box Match") { CheckOnClick = true };
        var showInfo = new ToolStripButton("Show Info");
        var visibleText = new ToolStripButton("Visible Text") { CheckOnClick = true, Checked = true };
        var openLocation = new ToolStripButton("Open Location");
        var copyFullAddress = new ToolStripButton("Copy Full Address");
        var search = new ToolStripButton("Search");

        refresh.Click += (_, _) => RefreshLiveDataModel();
        bookmark.Click += (_, _) => AddBookmark();
        subtreeSearch.CheckedChanged += (_, _) => searchSubtreeOnly = subtreeSearch.Checked;
        jumpTo.Click += (_, _) => JumpToCurrentSearchHit();
        watchParent.CheckedChanged += (_, _) =>
        {
            watchParentEnabled = watchParent.Checked;
            if (watchParentEnabled && selectedLiveNode is not null)
            {
                watchedParentPointer = selectedLiveNode.Pointer;
                watchParent.Text = "Watching...";
                toolbarStatus.Text = "Watching selected parent.";
                ResetWatchParentState(watchedParentPointer);
                liveRefreshTimer.Enabled = true;
                _ = SeedWatchedParentAsync();
            }
            else
            {
                liveRefreshTimer.Enabled = false;
                watchedParentPointer = 0;
                watchParent.Text = "Watch Parent";
                toolbarStatus.Text = "Watch parent disabled.";
            }
        };
        boxMatch.CheckedChanged += (_, _) => searchBoxMatch = boxMatch.Checked;
        showInfo.Click += (_, _) => ShowAllInfo();
        visibleText.CheckedChanged += (_, _) => searchVisibleText = visibleText.Checked;
        openLocation.Click += (_, _) => OpenLocation();
        copyFullAddress.Click += (_, _) => CopyFullAddress();
        search.Click += (_, _) => FindNext();

        toolbar.Items.Add(refresh);
        toolbar.Items.Add(bookmark);
        toolbar.Items.Add(subtreeSearch);
        toolbar.Items.Add(jumpTo);
        toolbar.Items.Add(watchParent);
        toolbar.Items.Add(boxMatch);
        toolbar.Items.Add(showInfo);
        toolbar.Items.Add(visibleText);
        toolbar.Items.Add(openLocation);
        toolbar.Items.Add(copyFullAddress);
        toolbar.Items.Add(new ToolStripControlHost(searchBox) { AutoSize = false, Width = 120 });
        toolbar.Items.Add(search);
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(toolbarStatus);

        return toolbar;
    }

    private void OpenLocation()
    {
        var node = GetSelectedNode();
        if (node is null)
        {
            return;
        }

        var match = FindTreeNodeByReference(node);
        if (match is null)
        {
            toolbarStatus.Text = "Could not locate that node in the tree.";
            return;
        }

        if (tabs is not null)
        {
            tabs.SelectedIndex = 0;
        }
        tree.SelectedNode = match;
        match.EnsureVisible();
        toolbarStatus.Text = $"Opened {node.Path}.";
    }

    private void CopyFullAddress()
    {
        var node = GetSelectedNode();
        if (node is null)
        {
            return;
        }

        try
        {
            Clipboard.SetText(node.Address);
            toolbarStatus.Text = $"Copied {node.Address}.";
        }
        catch (Exception ex)
        {
            toolbarStatus.Text = "Copy failed: " + ex.Message;
        }
    }

    private void ShowAllInfo()
    {
        var node = GetSelectedNode();
        if (node is null)
        {
            toolbarStatus.Text = "Select a node first.";
            return;
        }

        using var form = new Form
        {
            Text = "Show Info",
            Width = 1100,
            Height = 800,
            StartPosition = FormStartPosition.CenterParent,
        };

        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font(FontFamily.GenericMonospace, 9f),
        };
        box.Text = BuildFullInfoDump(node);
        form.Controls.Add(box);
        form.ShowDialog(this);
    }

    private string BuildFullInfoDump(DataModelNode node)
    {
        var lines = new List<string>();
        lines.Add("SELECTED");
        AppendNodeDump(lines, node, 0);
        lines.Add(string.Empty);
        lines.Add("ANCESTORS");

        var ancestors = new List<DataModelNode>();
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            ancestors.Add(current);
        }
        ancestors.Reverse();
        foreach (var ancestor in ancestors)
        {
            AppendNodeDump(lines, ancestor, 0);
        }

        lines.Add(string.Empty);
        lines.Add("DESCENDANTS");
        AppendDescendantDump(lines, node, 0);
        return string.Join(Environment.NewLine, lines);
    }

    private static void AppendDescendantDump(List<string> lines, DataModelNode node, int depth)
    {
        AppendNodeDump(lines, node, depth);
        foreach (var child in node.Children)
        {
            AppendDescendantDump(lines, child, depth + 1);
        }
    }

    private static void AppendNodeDump(List<string> lines, DataModelNode node, int depth)
    {
        var indent = new string(' ', depth * 2);
        lines.Add($"{indent}- {FormatNodeReference(node)}");
        lines.Add($"{indent}  Path: {node.Path}");
        lines.Add($"{indent}  Address: {node.Address}");
        lines.Add($"{indent}  Pointer: 0x{node.Pointer:X}");
        lines.Add($"{indent}  ClassName: {node.ClassName}");
        foreach (var key in new[] { "BackgroundColor3", "TextColor3", "BorderColor3", "ImageColor3", "ImageRectOffset", "Color", "BrickColor", "TeamColor" })
        {
            if (node.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                lines.Add($"{indent}  {key}: {value}");
            }
        }
        lines.Add($"{indent}  Children: {node.Children.Count}");
        lines.Add($"{indent}  Properties:");

        if (node.Properties.Count == 0)
        {
            lines.Add($"{indent}    (none)");
        }
        else
        {
            foreach (var property in node.Properties.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                lines.Add($"{indent}    {property.Key}: {property.Value}");
            }
        }
    }

    private DataModelNode? GetSelectedNode()
    {
        return tree.SelectedNode?.Tag as DataModelNode
            ?? allNodes.SelectedItems.Cast<ListViewItem>().FirstOrDefault()?.Tag as DataModelNode;
    }

    private TreeNode? FindTreeNodeByReference(DataModelNode node)
    {
        foreach (var item in Flatten(tree.Nodes))
        {
            if (ReferenceEquals(item.Tag, node))
            {
                return item;
            }
        }

        return null;
    }
}

internal static class CountExtensions
{
    public static int MinusRoot(this int value)
    {
        return Math.Max(0, value - 1);
    }
}
