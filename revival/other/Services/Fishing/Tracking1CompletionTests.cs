using Client.Services.Fishing;
using Xunit;

namespace Client.Tests.Services.Fishing;

public sealed class Tracking1CompletionTests
{
    [Fact]
    public void UpdateCompletionState_LatchesWhenProgressReachesThreshold()
    {
        var state = Tracking1FishingTracker.UpdateCompletionState(
            completionReached: false,
            maxProgressThisCycle: 42.0,
            progress: 99.5,
            completionThreshold: 99.5);

        Assert.True(state.CompletionReached);
        Assert.Equal(99.5, state.MaxProgressThisCycle);
    }

    [Fact]
    public void UpdateCompletionState_PreservesLatchedCompletionWhenProgressBecomesUnreadable()
    {
        var state = Tracking1FishingTracker.UpdateCompletionState(
            completionReached: true,
            maxProgressThisCycle: 99.5,
            progress: null,
            completionThreshold: 99.5);

        Assert.True(state.CompletionReached);
        Assert.Equal(99.5, state.MaxProgressThisCycle);
    }

    [Fact]
    public void ShouldResetCompletionStateAfterCompletedReelDisappears()
    {
        Assert.True(Tracking1FishingTracker.ShouldResetCompletionStateAfterReelLoss(
            completionReached: true,
            progress: null,
            hasMetrics: false));

        Assert.False(Tracking1FishingTracker.ShouldResetCompletionStateAfterReelLoss(
            completionReached: true,
            progress: 99.5,
            hasMetrics: false));

        Assert.False(Tracking1FishingTracker.ShouldResetCompletionStateAfterReelLoss(
            completionReached: false,
            progress: null,
            hasMetrics: false));

        Assert.False(Tracking1FishingTracker.ShouldResetCompletionStateAfterReelLoss(
            completionReached: true,
            progress: null,
            hasMetrics: true));
    }

    [Fact]
    public void ShouldNotResetCompletionStateWhenMetricsRemainVisible()
    {
        Assert.False(Tracking1FishingTracker.ShouldResetCompletionStateAfterReelLoss(
            completionReached: true,
            progress: 5.0,
            hasMetrics: true));
    }

    [Fact]
    public void ShouldNotResetCompletionStateOnSmallProgressDip()
    {
        Assert.False(Tracking1FishingTracker.ShouldResetCompletionStateAfterReelLoss(
            completionReached: true,
            progress: 90.0,
            hasMetrics: true));
    }
}
