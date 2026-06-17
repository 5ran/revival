using System.Collections.Generic;

namespace DataModelTreeViewer;

internal sealed class DataModelNode
{
    public string Name { get; set; } = "Node";
    public string ClassName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ulong Pointer { get; set; }
    public GuiBounds? Bounds { get; set; }
    public DataModelNode? Parent { get; set; }
    public Dictionary<string, string> Properties { get; } = new();
    public List<DataModelNode> Children { get; } = new();
}

internal sealed record BookmarkEntry(string Name, ulong Pointer, string Path, string Address);

internal readonly record struct GuiBounds(float X, float Y, float Width, float Height);
