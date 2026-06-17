using System;
using System.Collections.Generic;
using Client.Services.Fishing;

namespace DataModelTreeViewer;

internal sealed class DataModelLiveReader : IDisposable
{
    private readonly RobloxMemory memory;

    public DataModelLiveReader(LocalOffsetsSource offsets)
    {
        memory = new RobloxMemory(offsets);
    }

    public DataModelNode Read()
    {
        memory.EnsureAttached();
        var dataModel = memory.GetDataModel();
        if (!RobloxMemory.IsValidAddress(dataModel))
        {
            throw new InvalidOperationException("DataModel pointer was not valid.");
        }

        var seen = new HashSet<ulong>();
        return ReadNode(dataModel, null, "DataModel", 0, seen);
    }

    public DataModelNode? ReadSubtree(ulong rootPointer)
    {
        memory.EnsureAttached();
        if (!RobloxMemory.IsValidAddress(rootPointer))
        {
            return null;
        }

        var seen = new HashSet<ulong>();
        return ReadNode(rootPointer, null, "Node", 0, seen);
    }

    public IReadOnlyList<DataModelNode> ReadDirectChildren(ulong parentPointer)
    {
        memory.EnsureAttached();
        if (!RobloxMemory.IsValidAddress(parentPointer))
        {
            return Array.Empty<DataModelNode>();
        }

        var parentName = SafeRead(() => memory.ReadName(parentPointer));
        var parentPath = string.IsNullOrWhiteSpace(parentName) ? "Node" : parentName;
        var children = new List<DataModelNode>();
        foreach (var childPointer in memory.ReadChildren(parentPointer))
        {
            if (!RobloxMemory.IsValidAddress(childPointer))
            {
                continue;
            }

            var childName = SafeRead(() => memory.ReadName(childPointer));
            var childClass = SafeRead(() => memory.ReadClass(childPointer));
            if (string.IsNullOrWhiteSpace(childName))
            {
                childName = "Node";
            }

            var child = new DataModelNode
            {
                Name = childName,
                ClassName = childClass,
                Address = FormatAddress(childPointer),
                Path = parentPath + "/" + childName,
                Parent = null,
                Pointer = childPointer,
                Bounds = ReadBounds(childPointer),
            };
            child.Properties["Live"] = "true";
            child.Properties["Pointer"] = FormatAddress(childPointer);
            children.Add(child);
        }

        return children;
    }

    public void Dispose()
    {
        memory.Dispose();
    }

    private DataModelNode ReadNode(
        ulong address,
        DataModelNode? parent,
        string fallbackName,
        int depth,
        HashSet<ulong> seen)
    {
        var name = SafeRead(() => memory.ReadName(address));
        var className = SafeRead(() => memory.ReadClass(address));
        if (string.IsNullOrWhiteSpace(name))
        {
            name = fallbackName;
        }

        var node = new DataModelNode
        {
            Name = name,
            ClassName = className,
            Address = FormatAddress(address),
            Path = parent is null ? name : $"{parent.Path}/{name}",
            Parent = parent,
            Pointer = address,
            Bounds = ReadBounds(address),
        };
        node.Properties["Live"] = "true";
        node.Properties["Depth"] = depth.ToString();
        node.Properties["Pointer"] = FormatAddress(address);

        if (!seen.Add(address))
        {
            node.Properties["Skipped"] = "already visited";
            return node;
        }

        IReadOnlyList<ulong> children = Array.Empty<ulong>();
        try
        {
            children = memory.ReadChildren(address);
        }
        catch (Exception ex)
        {
            node.Properties["ChildrenReadError"] = ex.Message;
        }

        node.Properties["LiveChildren"] = children.Count.ToString();
        foreach (var child in children)
        {
            if (!RobloxMemory.IsValidAddress(child))
            {
                continue;
            }

            node.Children.Add(ReadNode(child, node, FormatAddress(child), depth + 1, seen));
        }

        return node;
    }

    private static string SafeRead(Func<string> read)
    {
        try
        {
            return read();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FormatAddress(ulong address)
    {
        return "0x" + address.ToString("X");
    }

    private GuiBounds? ReadBounds(ulong address)
    {
        try
        {
            var bounds = memory.ReadGuiBounds(address, false);
            return bounds is null ? null : new GuiBounds(bounds.Value.X, bounds.Value.Y, bounds.Value.Width, bounds.Value.Height);
        }
        catch
        {
            return null;
        }
    }
}
