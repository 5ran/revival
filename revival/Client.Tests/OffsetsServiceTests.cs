using System;
using Client.Services;
using Xunit;

namespace Client.Tests;

public sealed class OffsetsServiceTests
{
    [Fact]
    public void Ctor_LoadsLocalOffsetsHeaderSnapshot()
    {
        var svc = new OffsetsService();

        Assert.True(svc.IsPopulated);
        Assert.False(string.IsNullOrWhiteSpace(svc.Version));
        Assert.True(svc.TryGetOffset("TaskScheduler.Pointer", out var pointer));
        Assert.True(pointer > 0);
        Assert.True(svc.TryGetOffset("Name", out var name));
        Assert.True(name > 0);
    }

    [Fact]
    public async System.Threading.Tasks.Task RefreshAsync_IsNoOpForLocalSnapshot()
    {
        var svc = new OffsetsService();
        var beforeVersion = svc.Version;

        await svc.RefreshAsync("ignored");

        Assert.True(svc.IsPopulated);
        Assert.Equal(beforeVersion, svc.Version);
    }

    [Fact]
    public void Clear_DoesNotDropLocalOffsets()
    {
        var svc = new OffsetsService();

        svc.Clear();

        Assert.True(svc.IsPopulated);
        Assert.NotNull(svc.Version);
        Assert.True(svc.TryGetOffset("Instance.ClassName", out var className));
        Assert.True(className > 0);
    }
}
